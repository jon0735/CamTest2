using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraPositionFinder : MonoBehaviour
{
    
    [SerializeField]
    private List<GameObject> parts;
    [SerializeField]
    private List<GameObject> initParts;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Material basicMat;
    private Vector3 camInitPos;
    private Quaternion camInitRot;
    private List<Vector3> directions;
    private List<Vector3> directionsReduced;
    private List<Vector3[]> vertexSamples;
    private List<Vector3> objectCentres;
    private Vector3 sceneCentre;
    private int currentPart = -1;
    
    private UnityEngine.Color[] colArray;

    [SerializeField]
    private float outOfScopeAngle = 60f;
    [SerializeField]
    private float minDist = 0.2f;
    [SerializeField]
    private float maxDist = 2.5f;
    [SerializeField] 
    private int sphereSampleSize = 200;
    [SerializeField]
    private int vertexSampleSize = 100;
    [SerializeField]
    private float blockedVertexFrac = 0.4f;
    [SerializeField]
    private int blockedVertexIndex = 3; 
    [SerializeField]
    private float distWeight = 1f;
    [SerializeField]
    private float angleWeight = 1f;    
    [SerializeField]
    private float spreadWeight = 1f;
    [SerializeField]
    private float visibilityWeight = 1f;
    [SerializeField]
    private float distToObjWeight = 1f;
    
    [SerializeField]
    private float reasonAngleWeight = 1f;

    [SerializeField]
    private float stabilityWeight = 1f;

    [SerializeField]
    private bool visualDebug = false;

    
    [SerializeField]
    private float fovFactor = .75f;

    private float[] individualMinDists;

    private Camera dummyCam; // Camera to use the camera class methods for computations, without chaning main camera. Might not be the smartest way to do it.


    // Next to fields determine the detail of the grid used to detect if adjusting camera position will cause clipping planes issue
    // If clipDetectionResolutionX = 4 and clipDetectionResolutionY = 3 -> 12 raycasts will be used to determine this.
    [SerializeField]
    int clipDetectionResolutionX = 4;
    [SerializeField]
    int clipDetectionResolutionY = 3;

    float clipPlaneX;
    float clipPlaneY;
    
    private Vector3[] lastCamPositions;

    private int currentPosIndex;

    // [SerializeField] // Apparently doesn't work with list of tuples. Thanks Unity
    private List<(Vector3, float, float)> humanUnreasonableAngles; // Vector3: Impossible/unreasonable direction, float: angle from this direction considered impossible, float: largest angle from direction considered impractical

    private bool showStuffTestingBool = false;

    // private Dictionary<string, float[]> lastScores = null;
    private float[,] lastScores = null;
    public string scoreText {get; private set;} = "";

    private int[] lastCollisions = null; // needed to recompute scores when weights are changed (Not Implemented yet)
    
    private List<ScoreComputer> scoreComputers;

    [SerializeField]
    private List<ScoreType> scoreTypes;
    [SerializeField]
    private List<float> scoreWeights;


    void Start()
    {
        if (this.scoreTypes.Count != this.scoreWeights.Count){
            throw new Exception("The number of score types must be equal to the number of score weights: Types count: " + this.scoreTypes.Count + ", Weights count: " + this.scoreWeights.Count);
        }

        this.scoreComputers = new List<ScoreComputer>(this.scoreTypes.Count);

        for(int i = 0; i < this.scoreTypes.Count; i++){
            scoreComputers.Add(Util.CreateScoreComputerFromScoreType(this.scoreTypes[i], this.scoreWeights[i]));
        }

        this.dummyCam = Instantiate(this.cam, new Vector3(), new Quaternion());

        this.dummyCam.enabled = false;
        this.dummyCam.GetComponent<AudioListener>().enabled = false;
        
        foreach (GameObject part in this.parts){
            part.SetActive(false);
        }

        if (this.humanUnreasonableAngles == null){
            this.humanUnreasonableAngles = new List<(Vector3, float, float)>();
            this.humanUnreasonableAngles.Add((Vector3.up, 30f, 60f));
        }

        this.directions = Util.FibSphereSample(n: this.sphereSampleSize, impossibleAngles: this.humanUnreasonableAngles);
        // Debug.Log(this.directions.Count);
        this.directionsReduced = Util.FibSphereSample(n: 15); // TODO: Fix Hardcoding
        (this.vertexSamples, this.objectCentres, this.sceneCentre) = Util.SampleVerticePoints(this.parts, samples: vertexSampleSize);
        this.camInitPos = cam.transform.position;
        this.camInitRot = cam.transform.rotation;
        this.colArray = new UnityEngine.Color[8] { UnityEngine.Color.black, 
                                                   UnityEngine.Color.blue, 
                                                   UnityEngine.Color.green, 
                                                   UnityEngine.Color.yellow, 
                                                   UnityEngine.Color.magenta, 
                                                   UnityEngine.Color.cyan,
                                                   UnityEngine.Color.grey,
                                                   UnityEngine.Color.black };

        this.individualMinDists = new float[this.parts.Count];
        for (int i = 0; i < this.parts.Count; i++){
            float maxVertexDist = 0f;
            foreach(Vector3 vertex in this.vertexSamples[i]){
                // float thisVertexDist = (this.parts[i].transform.position - vertex).magnitude;
                float thisVertexDist = (this.objectCentres[i] - vertex).magnitude;
                if (thisVertexDist > maxVertexDist){
                    maxVertexDist = thisVertexDist;
                }
            }
            Debug.Log(this.cam.nearClipPlane.ToString() + " " + maxVertexDist.ToString() + this.minDist.ToString() + " " + Mathf.Max((this.cam.nearClipPlane + maxVertexDist) * 1.2f, this.minDist).ToString());
            this.individualMinDists[i] = Mathf.Max((this.cam.nearClipPlane + maxVertexDist) * 1.2f, this.minDist);
            Util.AddCollidersRecursive(this.parts[i]);
        }
        // Debug.Log(arrayToString(this.individualMinDists));
        foreach (GameObject initObj in this.initParts){
            Util.AddCollidersRecursive(initObj);
        }
        (this.clipPlaneX, this.clipPlaneY) = Util.ComputeClipPlaneSizes(this.cam); 

    }

    void Update()
    {

    }

    /// <summary>
    /// Called when next object should be rought into focus. Updated fields, and calls main algorithm to find new camera position
    /// </summary>
    public void NextInstruction(){
        if (currentPart < parts.Count -1){
            currentPart++;
            
            cam.transform.position = this.FindCamPos();

            parts[currentPart].SetActive(true);  
            Util.RecursiveChangeColor(parts[currentPart],  UnityEngine.Color.black);

            cam.transform.LookAt(this.objectCentres[currentPart]);
            if (currentPart > 0){
                Util.RecursiveChangeColor(parts[currentPart - 1],  UnityEngine.Color.grey);
            }

        } else {
            Util.RecursiveChangeColor(parts[currentPart],  UnityEngine.Color.grey);

            this.Reset();
            currentPart = -1;
        }
    }

    /// <summary>
    /// Resets scene view into original setup, with only initial structure activated.
    /// </summary>
    private void Reset(){
        foreach (GameObject part in parts){
            part.SetActive(false);
        }
        cam.transform.rotation = camInitRot;
        cam.transform.position = camInitPos;
    }

    /// <summary>
    /// Goes to the next camera position for the same instruction, i.e. a camera position with slightly lower score.
    /// </summary>
    public void NextPos(){

        if (this.currentPosIndex + 1 < this.lastCamPositions.Length){
            this.currentPosIndex++;
            cam.transform.position = this.lastCamPositions[this.currentPosIndex];
            cam.transform.LookAt(this.objectCentres[currentPart]);
            this.scoreText = this.BuildScoreString(this.lastScores, this.currentPosIndex);
        }
    }

    /// <summary>
    /// Goes to the previous camera position for the same instruction, i.e. a camera position with slightly higher score.
    /// </summary>
    public void PrevPos(){

        if (this.currentPosIndex > 0){
            this.currentPosIndex--;
            cam.transform.position = this.lastCamPositions[this.currentPosIndex];
            cam.transform.LookAt(this.objectCentres[currentPart]);
            this.scoreText = this.BuildScoreString(this.lastScores, this.currentPosIndex);
        }
    }


    /// <summary>
    /// Main algorithm for camera position finding.
    /// 
    /// Uses raycasts from and to the centre of the object in question to find initial camera position proposals. 
    /// Then uses more raycast to approximate how large a portion of the object is visible, and discards the worst fraction of the found proposals.
    /// Then adjusts the distance between camera and object, to fill up most of the camera view, without causing clipping plane isues
    /// Finnaly scores each position based on "ScoreComputer" implementations to find the best position
    /// </summary>
    /// <returns>Best camera position found, or exisitng camera position, if no valid positions can be found</returns>
    private Vector3 FindCamPos(){

        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];

        List<Vector3> positions = this.GetPositionProposals();
        
        // Vector3[] positionsArray;
        // int[] collisions;
        (Vector3[] positionsArray, int[]  collisions) = this.PrunePositionProposals(positions);
        
        float[] fullScores;
        (fullScores, positionsArray) = this.ComputePosScores(positionsArray, collisions);

        Array.Sort(fullScores, positionsArray);

        this.lastCamPositions = positionsArray;
        this.currentPosIndex = 0;

        if (positions.Count > 0 ) {
            return positionsArray[0];
        } else {
            // TODO: Consider doing a more detailed pass to attempt to find useable camera positions
            Debug.Log("No Positions found");
            this.lastCamPositions = new Vector3[]{cam.transform.position};
            return cam.transform.position;
        }
    }

    /// <summary>
    /// Finds initial valid camera position proposals.
    /// </summary>
    /// <returns>List of valid potential camera positions</returns>
    private List<Vector3> GetPositionProposals(){
        
        List<Vector3> positions = new List<Vector3>();
        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];

        foreach(Vector3 direction in this.directions){

            (bool usefulPosition, Vector3 camPosition) = this.FindUsablePosFromDirection(objectPos, direction);
            if (usefulPosition){
                positions.Add(camPosition);
            }

        }

        return positions;
    }

    /// <summary>
    /// Examines if there is a valid camera position, in a given direction form the object in question
    /// </summary>
    /// <param name="objectPos">Position of the object that needs to be viewed</param>
    /// <param name="direction">The direction in which we wish to know if there is a valid camera position</param>
    /// <returns>A tuple containing a boolean value to indicate if there is a valid position, and a Vector3 position containing such a position</returns>
    private (bool, Vector3) FindUsablePosFromDirection(Vector3 objectPos, Vector3 direction){
        
        RaycastHit hit;
        bool wasHit = Physics.Raycast(objectPos, direction, out hit, this.maxDist);

        // If raycast hits nothing -> clear line from centre of object to max-dist camera position.
        if (!wasHit){
            return (true, objectPos + direction * this.maxDist);
        }

        // If the distance before raycast hit is greater than some min dist, there might be a usable camera position before the raycast hit
        if (hit.distance > this.individualMinDists[currentPart]){
            Vector3 camPos = objectPos + direction * (hit.distance - 1.05f * this.minDist); // TODO: Fix hardcoding
            bool usablePos = !Util.IsInSideMesh(camPos) && ValidateVisibility(camPos, objectPos, this.vertexSamples[currentPart]); // TODO: Check and test ValidateVisibility. I am unsure of the impact of the function.
           
            // If there is a reasonable amount of room, and the position is not inside a mesh, we can use the position as a potentially useful camera position
            if (usablePos){
                return (true, camPos);
            } 
            else {
                // TODO: This branch is hit quite often. Check if this is due to poor hardcoding choice of "camPos" above such that there is no room around, or maybe "ValidateVisibility" being outdated?
                // Debug.Log("FindUsablePosFromDirection validation failed. Check it!");
                return (false, new Vector3(0, 0, 0));
            }
        } 
            
        this.parts[this.currentPart].SetActive(true); 
        wasHit = Physics.Raycast(objectPos + direction, -direction, out hit, maxDist);
        this.parts[this.currentPart].SetActive(false);

        if (wasHit){
        
            GameObject hitObj = hit.collider.gameObject;
            bool hitRightObject = Util.IsNestedChild(this.parts[currentPart], hitObj);
            if (hitRightObject){
                return (true, objectPos + direction * this.maxDist);
            }
            else {
                return (false, new Vector3(0, 0, 0));
            }
        } 

        // Should never end up here. If the first raycast hits something, so should the second (and if the first doesn't hit something, we return early)

        return (false, new Vector3(0, 0, 0)); // disallow direction as default 
    }

    /// <summary>
    /// Prunes a list of positions, such that only the positions with the highest fraction of the object being visible are kept. 
    /// Uses raycast colisions to approximate how much of the object is visible. Fewer colisions -> more of the object is visible.
    /// This is to reduce the computational load of further adjusting and scoring of positions.
    /// </summary>
    /// <param name="positions">List of positions to be pruned</param>
    /// <returns>A tuple with the pruned list of positions, and the number of raycast colisions for each of these positions</returns>
    private (Vector3[], int[]) PrunePositionProposals(List<Vector3> positions){
        int[] collisions = new int[positions.Count];
        RaycastHit hit;

        for(int i = 0; i < positions.Count; i++){
            int colls = 0;

            if(this.visualDebug){
                Util.CreateSphere(positions[i], this.colArray[currentPart % this.colArray.Length], scale: 0.05f);
            }

            for(int j = 0; j < this.vertexSamples[currentPart].Length; j++){

                Vector3 camCentreToVertex = this.vertexSamples[currentPart][j] - positions[i];
                float distToVertex = camCentreToVertex.magnitude;
                if (Physics.Raycast(positions[i], camCentreToVertex, out hit, distToVertex)){
                    colls++;
                    if (this.visualDebug){
                        Vector3 hitPoint = positions[i] + (camCentreToVertex * (hit.distance/distToVertex));
                        Util.DrawLine(positions[i], hitPoint, this.colArray[currentPart % this.colArray.Length], this.basicMat);
                        Util.CreateSphere(hitPoint, this.colArray[currentPart % this.colArray.Length]);
                    }
                }
            
            collisions[i] = colls;

            }
        }

        Vector3[] positionsArray = positions.ToArray();  
        Array.Sort(collisions, positionsArray);
        if (this.visualDebug){
            for(int j = 0; j < this.vertexSampleSize; j++){
                Util.CreateSquare(vertexSamples[currentPart][j], this.colArray[currentPart % this.colArray.Length]);     
            }
        }

        int prunedPosCount = positionsArray.Length;

        if (positions.Count > this.blockedVertexIndex){
            int maxAllowedHits = 
                Math.Max((int) ((float) collisions[blockedVertexIndex] * (1f + this.blockedVertexFrac/2)), 
                         (int) ((float) this.vertexSamples[currentPart].Length * this.blockedVertexFrac));  // TODO: Fix and clean this mess
            
            prunedPosCount = collisions.Length;
            for (int i = 0; i < collisions.Length; i++){
                if (collisions[i] > maxAllowedHits){
                    prunedPosCount = i + 1;
                    break;
                }
            }
            Vector3[] newPosArray = new Vector3[prunedPosCount];
            for(int i = 0; i < prunedPosCount; i++){
                newPosArray[i] = positionsArray[i];
            }
            positionsArray = newPosArray;
        }
        return (positionsArray, collisions);
    }

    /// <summary>
    /// Computes scores for each of the found potential camera position. Uses a List of implementations of ScoreComputer for this. Adjusts distance of positions
    /// in this step due to overlap between distance computations and certain score computations.
    /// </summary>
    /// <param name="positionsArray">Array of positions to be assigned a score</param>
    /// <param name="collisions">Number of collisions from earlier pruning step. Needed for VisibilityScore </param>
    /// <returns>Tuple containing List of scores and a list of distace adjusted positions</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private (float[], Vector3[]) ComputePosScores(Vector3[] positionsArray, int[] collisions){

        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];
        int prunedPosCount = positionsArray.Length;

        int scoreCount = this.scoreWeights.Count;
        if(scoreCount <= 0){
            throw new System.NotImplementedException("Missing implementation for handling no scoring system (would that ever be relevant though?)");
        }

        float[,] scores = new float[scoreCount, prunedPosCount];

        (Vector3[] newPositions, float[] distanceToObjectScores) = AdjustDistances(positionsArray, objectPos, vertices, this.individualMinDists[this.currentPart]);

        DataForScoreComputation data = new DataForScoreComputation(objectPos,
                                                                   this.sceneCentre,
                                                                   this.cam,
                                                                   this.dummyCam,
                                                                   this.maxDist,
                                                                   collisions,
                                                                   distanceToObjectScores,
                                                                   this.vertexSamples[this.currentPart],
                                                                   this.humanUnreasonableAngles);

        for(int i = 0; i < scoreCount; i++){
            for (int j = 0; j < prunedPosCount; j++){
                scores[i, j] = this.scoreComputers[i].ComputeScore(objectPos, data, j);
            }
            if (this.scoreComputers[i].needsNormalization){
                Util.NormalizeArrayRow(scores, i);
            }
        }

        float[] fullScores = new float[prunedPosCount];

        for(int i = 0; i < scoreCount; i++){
            for (int j = 0; j < prunedPosCount; j++){
                fullScores[j] += scores[i, j] * this.scoreComputers[i].weight;
            }
        }

        this.lastScores = scores;

        return (fullScores, newPositions);
    }


    // Checks if for any vertex the angle between camPos->vertex and camPos->objectCentre is (approximately) greater than the field of view of camera (I.e. part of object cannot be seen from that camera position.)
    private bool ValidateVisibility(Vector3 camPos, Vector3 objCentre, Vector3[] vertices){
        
        Vector3 camVector = objCentre - camPos;
        Vector3 vertVector;
        foreach (Vector3 vert in vertices) {
            vertVector = vert - camPos;
            float angle = Vector3.Angle(camVector, vertVector);
            if (angle > this.outOfScopeAngle) {
                // Debug.Log("Found large angle " + angle.ToString());
                return false;
            }
        }
        
        return Util.ValidateProximity(camPos, this.directionsReduced, this.minDist);  // TODO: Fix to use individual minDists?
    }

    /// <summary>
    /// Adjusts distances of an array of positions, and computes the distance score for each of these.
    /// </summary>
    /// <param name="positionsArray">Array of positions to be adjusted and scored</param>
    /// <param name="objectPos">Position of the object to be viewed</param>
    /// <param name="vertices">A subset of the vertices of the object mesh</param>
    /// <param name="minDist">Minimal viewing distance</param>
    /// <returns>A tuple containing a list of distance adjusted camera positions, and a list of scores based on these adjsutments</returns>
    private (Vector3[], float[]) AdjustDistances(Vector3[] positionsArray, Vector3 objectPos, Vector3[] vertices, float minDist){

        int n = positionsArray.Length;
        Vector3[] newPositions = new Vector3[n];
        float[] scores = new float[n];

        for (int i = 0; i < n; i++){
            (newPositions[i], scores[i]) = AdjustDistance(positionsArray[i], objectPos, vertices, minDist);
        }

        return (newPositions, scores);
    }

    /// <summary>
    /// Adjusts distances of anpositions, and computes the distance score ascociated with this adjustement.
    /// </summary>
    /// <param name="camPos">Camera position to be adjusted</param>
    /// <param name="objCentre">Centre of object to be viewed</param>
    /// <param name="vertices">Subset of vertices of said object</param>
    /// <param name="objMinDist">Minimal viewing distance of said object</param>
    /// <param name="draw">Debugging param to determine if lines7spehres should be drawn to visualise computations</param>
    /// <returns>A Tuple containing a distance adjusted position, and a score based on this adjsutment</returns>
    private (Vector3, float) AdjustDistance(Vector3 camPos, Vector3 objCentre, Vector3[] vertices, float objMinDist, bool draw=false){
        this.dummyCam.transform.position = camPos;
        this.dummyCam.transform.LookAt(objCentre);

        Matrix4x4 worldToCam = this.dummyCam.transform.worldToLocalMatrix;

        Vector3 pointLocal = worldToCam.MultiplyPoint3x4(vertices[0]);

        float maxX = pointLocal.x;
        float maxY = pointLocal.y;
        float minX = pointLocal.x;
        float minY = pointLocal.y;
        float minZ = pointLocal.z;

        for(int i = 1; i < vertices.Length; i++){
            pointLocal = worldToCam.MultiplyPoint3x4(vertices[i]);
            // Debug.Log("Before: " + vertices[i].ToString() + ", After: " + pointLocal.ToString());
            if (maxX < pointLocal.x) {
                maxX = pointLocal.x;
            }
            else if (maxY < pointLocal.y) {
                maxY = pointLocal.y;
            }
            if (minX > pointLocal.x) {
                minX = pointLocal.x;
            }
            else if (minY > pointLocal.y) {
                minY = pointLocal.y;
            }
            if (minZ > pointLocal.z) {
                minZ = pointLocal.z;
            }
        }

        float optimalDist = 0f;

        float desiredYAngle = this.fovFactor * this.cam.fieldOfView/2 * 3.1415f / 180f;
        float desiredXAngle = this.fovFactor * this.cam.fieldOfView/2 * ((float) Screen.width/(float) Screen.height) * 3.1415f / 180f;

        float[] desiredAngles = new float[]{desiredYAngle, desiredXAngle};
        float[,] cathetusSizes = new float[,]{{maxY, minY},{maxX, minX}};

        for(int i = 0; i < 2; i++){
            float desiredAngle = desiredAngles[i];
            for (int j = 0; j < 2; j++){
                float dist = Mathf.Abs(cathetusSizes[i, j]) * (Mathf.Cos(desiredAngle)/Mathf.Sin(desiredAngle)); // Change to using tangent?
                if (dist > optimalDist){
                    optimalDist = dist;
                }
            }
        }

        if (optimalDist < cam.nearClipPlane){
            optimalDist = cam.nearClipPlane * 1.2f;
        }
        if (optimalDist < objMinDist){
            optimalDist = objMinDist;
        }

        Vector3 camToCentre = objCentre - camPos;
        Vector3 direction = camToCentre/camToCentre.magnitude;

        // Check for collisions between near clipping plane and objects, which would indicate clipping

        float xRes = this.clipDetectionResolutionX + 2f;
        float yRes = this.clipDetectionResolutionY + 2f;

        Vector3 topLeftClip = camPos + 
                              -.5f * dummyCam.transform.right * this.clipPlaneX + 
                               .5f * dummyCam.transform.up * this.clipPlaneY +
                               dummyCam.transform.forward * dummyCam.nearClipPlane;
         
        if(draw){

            Vector3 topRight = topLeftClip + dummyCam.transform.right * this.clipPlaneX;
            Vector3 botLeft = topLeftClip - dummyCam.transform.up * this.clipPlaneY;
            Vector3 botRight = topLeftClip - dummyCam.transform.up * this.clipPlaneY + dummyCam.transform.right * this.clipPlaneX;

            Util.DrawLine(camPos, topLeftClip, UnityEngine.Color.red, this.basicMat);
            Util.DrawLine(camPos, topRight, UnityEngine.Color.green, this.basicMat);
            Util.DrawLine(camPos, botLeft, UnityEngine.Color.blue, this.basicMat);
            Util.DrawLine(camPos, botRight, UnityEngine.Color.yellow, this.basicMat);
        }

        float minDist = (camPos - objCentre).magnitude - optimalDist;
        float maxRayDist = minDist;

        RaycastHit hitInfo;
        bool hit;
        for(float i = 0f; i < xRes; i++){
            for(float j = 0f; j < yRes; j++){
                Vector3 rayStartPos = topLeftClip + 
                                      dummyCam.transform.right * this.clipPlaneX * (i/(xRes-1)) +
                                      -dummyCam.transform.up * this.clipPlaneY * (j/(yRes-1));
                hit = Physics.Raycast(rayStartPos, direction, out hitInfo, maxRayDist);
                if (hit) {
                    float hitDistAdjusted = hitInfo.distance - .5f * dummyCam.nearClipPlane;
                    if (hitDistAdjusted < minDist) {
                        // Debug.Log("minDist updated " + minDist.ToString() + " -> " + (hitDistAdjusted).ToString());
                        minDist = hitDistAdjusted;
                    }
                }
                if (draw){
                    float dist = hit ? hitInfo.distance : maxRayDist;
                    Util.DrawLine(rayStartPos, rayStartPos + direction * dist, hit ? UnityEngine.Color.red : UnityEngine.Color.green, this.basicMat);
                    Util.CreateSphere(rayStartPos, UnityEngine.Color.red, scale: .01f);
                }
            }
        }

        float optimalDistReverse = (camPos - objCentre).magnitude - optimalDist;

        float score = (optimalDistReverse - minDist)/optimalDistReverse;

        return (camPos + minDist * direction, score);
    }


    // TODO: Move somewhere else
    public string BuildScoreString(float[,] scores, int index){

        string text = "";
        int numScores = scores.GetLength(0);

        for(int i = 0; i < numScores; i++){
            text += this.scoreComputers[i].GetScoreName() + ": " + scores[i, index].ToString() + "\n";
        }
        // foreach(var keyValPair in scores){
        //     text += keyValPair.Key + ": " + keyValPair.Value[index].ToString() + "\n";
        // }
        text +=  "Object Pos: " + this.objectCentres[currentPart].ToString() + "\n";
        text +=  "Cam Pos: " + this.lastCamPositions[index].ToString() + "\n";
        text +=  "Cam Vector: " + (this.objectCentres[currentPart] - this.lastCamPositions[index] ).ToString() + "\n";
        text += "FIX: NOT CONSIDERING SORTING DONE AFTERWARDS -> This is all wrong";

        return text;
    }


    // public string GetScoreText(){
    //     return this.scoreText;
    // }

}
