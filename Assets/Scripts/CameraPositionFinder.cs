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

    // [SerializeField]
    private Camera dummyCam;

    [SerializeField]
    int clipDetectionResolutionX = 4;
    [SerializeField]
    int clipDetectionResolutionY = 3;

    float clipPlaneX;
    float clipPlaneY;
    // For Debugging
    private Vector3[] lastCamPositions;

    private int currentPosIndex;

    [SerializeField] // Apparently doesn't work with list of tuples
    private List<(Vector3, float, float)> humanUnreasonableAngles; // Vector3: Impossible/unreasonable direction, float: angle from this direction considered impossible, float: largest angle from direction considered impractical
    
    // [SerializeField]
    // private List<GameObject> dummyObjects;

    private bool showStuffTestingBool = false;

    private Dictionary<string, float[]> lastScores = null;
    private string scoreText = "";

    private int[] lastCollisions = null; // needed to recompute scores when weights are changed


    // Start is called before the first frame update
    void Start()
    {
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

    public void NextInstruction(){
        if (currentPart < parts.Count -1){
            currentPart++;
            
            
            cam.transform.position = FindCamPos();

            parts[currentPart].SetActive(true);  
            Util.RecursiveChangeColor(parts[currentPart],  UnityEngine.Color.black);
            // parts[currentPart].transform.GetChild(0).GetComponent<MeshRenderer>().material.color = UnityEngine.Color.white;

            cam.transform.LookAt(this.objectCentres[currentPart]);
            if (currentPart > 0){
                Util.RecursiveChangeColor(parts[currentPart - 1],  UnityEngine.Color.grey);
                // parts[currentPart - 1].transform.GetChild(0).GetComponent<MeshRenderer>().material.color = UnityEngine.Color.grey;
            }

        } else {
            Util.RecursiveChangeColor(parts[currentPart],  UnityEngine.Color.grey);

            Reset();
            currentPart = -1;
        }
    }

    private void Reset(){
        foreach (GameObject part in parts){
            part.SetActive(false);
        }
        cam.transform.rotation = camInitRot;
        cam.transform.position = camInitPos;
    }

    public void NextPos(){

        if (this.currentPosIndex + 1 < this.lastCamPositions.Length){
            this.currentPosIndex++;
            cam.transform.position = this.lastCamPositions[this.currentPosIndex];
            cam.transform.LookAt(this.objectCentres[currentPart]);
            // Debug.Log(this.objectCentres[currentPart]);
            this.scoreText = this.BuildScoreString(this.lastScores, this.currentPosIndex);
        }
    }

    public void PrevPos(){

        if (this.currentPosIndex > 0){
            this.currentPosIndex--;
            cam.transform.position = this.lastCamPositions[this.currentPosIndex];
            cam.transform.LookAt(this.objectCentres[currentPart]);
            // cam.transform.LookAt(parts[currentPart].transform);
            this.scoreText = this.BuildScoreString(this.lastScores, this.currentPosIndex);
        }
    }


    // Main algorithm
    private Vector3 FindCamPos(){

        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];

        List<Vector3> positions = this.GetPositionProposals();
        
        Vector3[] positionsArray;
        int[] collisions;
        (positionsArray, collisions) = this.PrunePositionProposals(positions);
        
        float[] fullScores;
        (fullScores, positionsArray) = this.ComputePosScores(positionsArray, collisions);

        Array.Sort(fullScores, positionsArray);

        this.lastCamPositions = positionsArray;
        this.currentPosIndex = 0;

        if (positions.Count > 0 ) {
            return positionsArray[0];
        } else {
            Debug.Log("No Positions found");
            return cam.transform.position;
        }
    }

    private List<Vector3> GetPositionProposals(){
        
        List<Vector3> positions = new List<Vector3>();
        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];

        RaycastHit hit;
        foreach(Vector3 direction in this.directions){

            bool wasHit = Physics.Raycast(objectPos, direction, out hit, maxDist);
            bool usablePosBeforeHit = false; 
            bool inverseHitRightObject = false;
            Vector3 camPos = objectPos + direction * maxDist;

            if (wasHit){
                if (hit.distance > this.individualMinDists[currentPart]){
                    camPos = objectPos + direction * (hit.distance - 1.05f * this.minDist); // TODO: Fix hardcoding
                    usablePosBeforeHit = !Util.IsInSideMesh(camPos) && ValidateVisibility(camPos, objectPos, vertices);
                } else {
                    this.parts[this.currentPart].SetActive(true);
                    if (Physics.Raycast(objectPos + direction, -direction, out hit, maxDist)){
                    
                        GameObject obj = hit.collider.gameObject;
                        inverseHitRightObject = Util.IsNestedChild(this.parts[currentPart], obj);
                        // Debug.Log("Inverse correct hit: " + inverseHitRightObject.ToString());
                        // if (currentPart == 3){
                        //     drawLine(objectPos, objectPos + direction, UnityEngine.Color.blue);
                        // }
                    this.parts[this.currentPart].SetActive(false);
                }
                } 
            } 

            if (!wasHit || usablePosBeforeHit || inverseHitRightObject){
                positions.Add(camPos);
            }
        }
        return positions;
    }

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

    private (float[], Vector3[]) ComputePosScores(Vector3[] positionsArray, int[] collisions){

        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];
        int prunedPosCount = positionsArray.Length;

        float[] distScores = new float[prunedPosCount];
        float[] angleScores = new float[prunedPosCount];
        float[] spreadScores = new float[prunedPosCount];
        float[] visibilityScores = new float[prunedPosCount];
        float[] distToObjScores = new float[prunedPosCount];
        float[] reasonableAngleScores = new float[prunedPosCount];
        float[] stabilityScores = new float[prunedPosCount];
        // TODO?: Add something to represent there being space around the found area

        Vector3[] newPositionsArray = new Vector3[prunedPosCount];
        Vector3 centre = this.sceneCentre; 
        float[] fullScores = new float[prunedPosCount];

        // TODO: Fix all hardcoding

        for(int i = 0; i < prunedPosCount; i++){

            angleScores[i] = Vector3.Angle(objectPos - positionsArray[i], centre - positionsArray[i]) / 180f;
            spreadScores[i] = ComputeSpreadScore(positionsArray[i], objectPos);
            visibilityScores[i] = (float) collisions[i];
            (newPositionsArray[i] , distToObjScores[i]) = AdjustDistance(positionsArray[i], objectPos, vertices, this.individualMinDists[currentPart]);
            distScores[i] = Vector3.Distance(positionsArray[i], this.cam.transform.position) / (2f * this.maxDist);
            reasonableAngleScores[i] = ComputeHumanAngleScore(positionsArray[i], objectPos);
            stabilityScores[i] = ComputeStabilityScore(newPositionsArray[i], objectPos);
            // Debug.Log("StabilityScore: " + stabilityScores[i].ToString());
        }

        Util.NormalizeArray(distScores);
        Util.NormalizeArray(angleScores);
        Util.NormalizeArray(spreadScores);
        Util.NormalizeArray(visibilityScores);
        Util.NormalizeArray(distToObjScores);
        // this.normalizeArray(reasonableAngleScores); // Already normalised
        // this.normalizeArray(stabilityScores); // Already normalised

        for(int i = 0; i < spreadScores.Length; i++){
            fullScores[i] = distScores[i] * this.distWeight + 
                            angleScores[i] * this.angleWeight + 
                            spreadScores[i] * this.spreadWeight +
                            visibilityScores[i] * this.visibilityWeight +
                            distToObjScores[i] * this.distToObjWeight +
                            reasonableAngleScores[i] * this.reasonAngleWeight + 
                            stabilityScores[i] * this.stabilityWeight;
        }

        Dictionary<string, float[]> scoreDict = new Dictionary<string, float[]>();
        scoreDict.Add("distScores", distScores);
        scoreDict.Add("angleScores", angleScores);
        scoreDict.Add("spreadScores", spreadScores);
        scoreDict.Add("visibilityScores", visibilityScores);
        scoreDict.Add("distToObjScores", distToObjScores);
        scoreDict.Add("reasonableAngleScores", reasonableAngleScores);
        scoreDict.Add("stabilityScores", stabilityScores);
        scoreDict.Add("fullScores", fullScores);
        this.lastScores = scoreDict;

        return (fullScores, newPositionsArray);
    }

    private float ComputeStabilityScore(Vector3 position, Vector3 objectPos){
        Transform t = this.dummyCam.transform;
        t.position = position;
        t.LookAt(objectPos);

        float dist = (position - objectPos).magnitude;
        float desiredAngle = 8f * 3.1415f / 180f; // TODO: Fix hardcoding?
        float distToStabilityPoints = Mathf.Sin(desiredAngle) * dist / Mathf.Sin(3.1415f/2 - desiredAngle); 

        Vector3 up = t.up * distToStabilityPoints;
        Vector3 right = t.right * distToStabilityPoints;

        // if (this.visualDebug || true){
        //     Debug.Log("up: " + up.ToString());
        //     Debug.Log("right: " + right.ToString());
        //     Debug.Log("distToStabilityPoints: " + distToStabilityPoints.ToString());
        //     Debug.Log("DesiredAngle: " + desiredAngle.ToString());
        // }

        Vector3 newPos;
        float hits = 0f;
        int points = 5;
        dist = dist * .90f; // To prevent hit with self
        for(int i = 0; i < points; i++){
            for(int j = 0; j < points; j++){
                if (i == (points-1)/2 && j == (points-1)/2){
                    continue;
                }
                newPos = position + (i-(points-1)/2) * up + (j-(points-1)/2) * right;
                RaycastHit hit;
                if(Physics.Raycast(newPos, objectPos - newPos, out hit, dist)){
                    hits++;
                    // createSphere(newPos, UnityEngine.Color.green);
                    // Vector3 endpoint = newPos + (objectPos - newPos).normalized * hit.distance;
                    // drawLine(newPos, endpoint, UnityEngine.Color.green);
                }

                if(this.showStuffTestingBool){
                    Util.CreateSphere(newPos, UnityEngine.Color.cyan, scale: 0.01f);
                }
            }
        }
        this.showStuffTestingBool = false;


        // TODO
        return hits/(points * points - 1);
    }


    private float ComputeSpreadScore(Vector3 position, Vector3 objectPos){
        
        float avgSpred = 0f;
        Vector3 camToObj = objectPos - position;
        float dist = camToObj.magnitude;
        Vector3 scaledCamPos = position + (camToObj * (1 - 1/dist)); // Distance to object will interfere with avg angles. Use this scaled camPos to avoid that.
        for(int j = 0; j < this.vertexSamples[this.currentPart].Length; j++){ 
            Vector3 test = position;

            avgSpred += Vector3.Angle(objectPos - scaledCamPos, this.vertexSamples[this.currentPart][j] - scaledCamPos);
        }
        avgSpred = avgSpred / this.vertexSamples[this.currentPart].Length;
        return (1f - avgSpred / 60f);
    }

    private float ComputeHumanAngleScore(Vector3 camPosition, Vector3 objPosition){
        float reasonableAngleScore = 0f;
        Vector3 camVector = objPosition - camPosition;
        for(int j = 0; j < this.humanUnreasonableAngles.Count; j++){
            float angle = Vector3.Angle(camVector, this.humanUnreasonableAngles[j].Item1);
            if (angle < this.humanUnreasonableAngles[j].Item3){
                float score = (angle - this.humanUnreasonableAngles[j].Item2) / this.humanUnreasonableAngles[j].Item3;
                // Debug.Log("CamPos: " + camPosition.ToString() + 
                //           "\nobjPos: " + objPosition.ToString() +
                //           "\ncamVector: " + camVector.ToString() + 
                //           "\nBad Direction:  " + this.humanUnreasonableAngles[j].Item1.ToString() +
                //           "\nAngle: " + angle.ToString() + 
                //           "\nscore: " + score.ToString());
                // Debug.Log("score: " + score.ToString());
                reasonableAngleScore = Mathf.Max(reasonableAngleScore, score);
            }
        }
        return reasonableAngleScore;
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
    public string BuildScoreString(Dictionary<string, float[]> scores, int index){

        string text = "";
        foreach(var keyValPair in scores){
            text += keyValPair.Key + ": " + keyValPair.Value[index].ToString() + "\n";
        }
        text +=  "Object Pos: " + this.objectCentres[currentPart].ToString() + "\n";
        text +=  "Cam Pos: " + this.lastCamPositions[index].ToString() + "\n";
        text +=  "Cam Vector: " + (this.objectCentres[currentPart] - this.lastCamPositions[index] ).ToString() + "\n";
        text += "FIX: NOT CONSIDERING SORTING DONE AFTERWARDS -> This is all wrong";

        return text;
    }

    public string GetScoreText(){
        return this.scoreText;
    }

}
