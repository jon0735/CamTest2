using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MainScript : MonoBehaviour
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
    private int sphereSampleSize = 100;
    [SerializeField]
    private int vertexSampleSize = 25;
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
    private bool visualDebug = false;
    [SerializeField]
    private bool computeCamPos = true; 
    
    [SerializeField]
    private float fovFactor = .75f;

    private float[] individualMinDists;

    [SerializeField]
    private Camera dummyCam;

    [SerializeField]
    int clipDetectionResolutionX = 0;
    [SerializeField]
    int clipDetectionResolutionY = 0;

    float clipPlaneX;
    float clipPlaneY;
    // For Debugging
    private Vector3[] lastCamPositions;

    private int currentPosIndex;

    [SerializeField] // Apparently doesn't work with list of tuples
    private List<(Vector3, float, float)> humanUnreasonableAngles; // Vector3: Impossible/unreasonable direction, float: angle from this direction considered impossible, float: largest angle from direction considered impractical
    
    [SerializeField]
    private List<GameObject> dummyObjects;



    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject part in this.parts){
            part.SetActive(false);
        }

        if (this.humanUnreasonableAngles == null){
            this.humanUnreasonableAngles = new List<(Vector3, float, float)>();
            this.humanUnreasonableAngles.Add((Vector3.down, 30f, 60f));
        }

        this.directions = fibSphereSample(n: sphereSampleSize, impossibleAngles: this.humanUnreasonableAngles);
        Debug.Log(this.directions.Count);
        this.directionsReduced = fibSphereSample(n: 15); // TODO: Fix Hardcoding
        (this.vertexSamples, this.objectCentres, this.sceneCentre) = sampleVerticePoints(samples: vertexSampleSize);
        this.camInitPos = cam.transform.position;
        this.camInitRot = cam.transform.rotation;
        this.colArray = new UnityEngine.Color[8] {UnityEngine.Color.black, 
                                                  UnityEngine.Color.blue, 
                                                  UnityEngine.Color.green, 
                                                  UnityEngine.Color.yellow, 
                                                  UnityEngine.Color.magenta, 
                                                  UnityEngine.Color.cyan,
                                                  UnityEngine.Color.grey,
                                                  UnityEngine.Color.black};

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
            addCollidersRecursive(this.parts[i]);
        }
        // Debug.Log(arrayToString(this.individualMinDists));
        foreach (GameObject initObj in this.initParts){
            addCollidersRecursive(initObj);
        }
        (this.clipPlaneX, this.clipPlaneY) = computeClipPlaneSizes(this.cam); 

        
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)){
            nextInstruction();
        }


        if (Input.GetKeyDown(KeyCode.RightArrow)){
            if (this.currentPosIndex + 1 < this.lastCamPositions.Length){
                this.currentPosIndex++;
                cam.transform.position = this.lastCamPositions[this.currentPosIndex];
                cam.transform.LookAt(this.objectCentres[currentPart]);
                // Debug.Log(this.objectCentres[currentPart]);
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)){
            if (this.currentPosIndex > 0){
                this.currentPosIndex--;
                cam.transform.position = this.lastCamPositions[this.currentPosIndex];
                cam.transform.LookAt(this.objectCentres[currentPart]);
                // cam.transform.LookAt(parts[currentPart].transform);
            }
        }

        //Debugging stuff
        if (Input.GetKeyDown(KeyCode.A)){
            this.directions = fibSphereSample(n:sphereSampleSize);
            foreach(Vector3 dir in this.directions){
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.transform.position = dir;
                g.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
        }

        if (Input.GetKeyDown(KeyCode.O)){
            setUpTestAdjustDistance();            
        }
        if (Input.GetKeyDown(KeyCode.P)){
            testAdjustDistance();            
        }
    }

    private void nextInstruction(){
        if (currentPart < parts.Count -1){
            currentPart++;
            
            if (this.computeCamPos) {
                cam.transform.position = findCamPos();
            }

            parts[currentPart].SetActive(true);  
            recursiveChangeColor(parts[currentPart],  UnityEngine.Color.black);
            // parts[currentPart].transform.GetChild(0).GetComponent<MeshRenderer>().material.color = UnityEngine.Color.white;

            cam.transform.LookAt(this.objectCentres[currentPart]);
            if (currentPart > 0){
                recursiveChangeColor(parts[currentPart - 1],  UnityEngine.Color.grey);
                // parts[currentPart - 1].transform.GetChild(0).GetComponent<MeshRenderer>().material.color = UnityEngine.Color.grey;
            }

        } else {
            recursiveChangeColor(parts[currentPart],  UnityEngine.Color.grey);

            reset();
            currentPart = -1;
        }
    }

    private void reset(){
        foreach (GameObject part in parts){
            part.SetActive(false);
        }
        cam.transform.rotation = camInitRot;
        cam.transform.position = camInitPos;
    }


    // Main algorithm
    private Vector3 findCamPos(){

        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];

        List<Vector3> positions = this.getPositionProposals();
        
        Vector3[] positionsArray;
        int[] collisions;
        (positionsArray, collisions) = this.prunePositionProposals(positions);
        
        float[] fullScores;
        (fullScores, positionsArray) = computePosScores(positionsArray, collisions);

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

    private List<Vector3> getPositionProposals(){
        
        List<Vector3> positions = new List<Vector3>();
        Vector3 objectPos = this.objectCentres[currentPart];
        Vector3[] vertices = this.vertexSamples[currentPart];

        RaycastHit hit;
        foreach(Vector3 direction in this.directions){

            bool wasHit = Physics.Raycast(objectPos, direction, out hit, maxDist);
            bool usablePosBeforeHit = false; 
            Vector3 camPos = objectPos + direction * maxDist;

            if (wasHit){
                if (hit.distance > this.individualMinDists[currentPart]){
                    camPos = objectPos + direction * (hit.distance - 1.05f * this.minDist); // TODO: Fix hardcoding
                    usablePosBeforeHit = !isInSideMesh(camPos) && validateVisibility(camPos, objectPos, vertices);
                }
            } 

            if (!wasHit || usablePosBeforeHit){
                positions.Add(camPos);
            }
        }
        return positions;
    }

    private (Vector3[], int[]) prunePositionProposals(List<Vector3> positions){
        int[] collisions = new int[positions.Count];
        RaycastHit hit;

        for(int i = 0; i < positions.Count; i++){
            int colls = 0;

            if(this.visualDebug){
                createSphere(positions[i], this.colArray[currentPart % this.colArray.Length], scale: 0.05f);
            }

            for(int j = 0; j < this.vertexSamples[currentPart].Length; j++){

                Vector3 camCentreToVertex = this.vertexSamples[currentPart][j] - positions[i];
                float distToVertex = camCentreToVertex.magnitude;
                if (Physics.Raycast(positions[i], camCentreToVertex, out hit, distToVertex)){
                    colls++;
                    if (this.visualDebug){
                        Vector3 hitPoint = positions[i] + (camCentreToVertex * (hit.distance/distToVertex));
                        drawLine(positions[i], hitPoint, this.colArray[currentPart % this.colArray.Length]);
                        createSphere(hitPoint, this.colArray[currentPart % this.colArray.Length]);
                    }
                }
            
            collisions[i] = colls;

            }
        }

        Vector3[] positionsArray = positions.ToArray();  
        Array.Sort(collisions, positionsArray);
        if (this.visualDebug){
            for(int j = 0; j < this.vertexSampleSize; j++){
                createSquare(vertexSamples[currentPart][j], this.colArray[currentPart % this.colArray.Length]);     
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

    private (float[], Vector3[]) computePosScores(Vector3[] positionsArray, int[] collisions){

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
            spreadScores[i] = computeSpreadScore(positionsArray[i], objectPos);
            visibilityScores[i] = (float) collisions[i];
            (newPositionsArray[i] , distToObjScores[i]) = adjustDistance(positionsArray[i], objectPos, vertices, this.individualMinDists[currentPart]);
            distScores[i] = Vector3.Distance(positionsArray[i], this.cam.transform.position) / (2f * this.maxDist);
            reasonableAngleScores[i] = computeHumanAngleScore(positionsArray[i]);
            stabilityScores[i] = computeStabilityScore(positionsArray[i], objectPos);

        }

        this.normalizeArray(distScores);
        this.normalizeArray(angleScores);
        this.normalizeArray(spreadScores);
        this.normalizeArray(visibilityScores);
        this.normalizeArray(distToObjScores);
        // this.normalizeArray(reasonableAngleScores); // Already normalised

        for(int i = 0; i < spreadScores.Length; i++){
            fullScores[i] = distScores[i] * this.distWeight + 
                            angleScores[i] * this.angleWeight + 
                            spreadScores[i] * this.spreadWeight +
                            visibilityScores[i] * this.visibilityWeight +
                            distToObjScores[i] * this.distToObjWeight +
                            reasonableAngleScores[i] * this.reasonAngleWeight;
        }

        return (fullScores, newPositionsArray);
    }

    private float computeStabilityScore(Vector3 position, Vector3 objectPos){


        // TODO
        return 0f;
    }


    private float computeSpreadScore(Vector3 position, Vector3 objectPos){
        
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

    private float computeHumanAngleScore(Vector3 position){
        float reasonableAngleScore = 0f;
        for(int j = 0; j < this.humanUnreasonableAngles.Count; j++){
            float angle = Vector3.Angle(position, this.humanUnreasonableAngles[j].Item1);
            if (angle < this.humanUnreasonableAngles[j].Item3){
                float score = (angle - this.humanUnreasonableAngles[j].Item2) / this.humanUnreasonableAngles[j].Item3;
                
                reasonableAngleScore = Mathf.Max(reasonableAngleScore, score);
            }
        }
        return reasonableAngleScore;
    }

    // Checks if for any vertex the angle between camPos->vertex and camPos->objectCentre is (approximately) greater than the field of view of camera (I.e. part of object cannot be seen from that camera position.)
    private bool validateVisibility(Vector3 camPos, Vector3 objCentre, Vector3[] vertices){
        
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
        
        return validateProximity(camPos);
    }

    private bool isInSideMesh(Vector3 pos, float maxDist=2f){
        RaycastHit[] hitsIn;
        RaycastHit[] hitsOut;

        hitsIn = Physics.RaycastAll(pos, Vector3.up, maxDist); // TODO: May be too fickle. Maybe just straight up from cam and to cam?
        hitsOut = Physics.RaycastAll(pos - maxDist * Vector3.up, -Vector3.up, maxDist);

        return hitsIn.Length == hitsOut.Length;
    }

    private bool validateProximity(Vector3 camPos){

        foreach( Vector3 direction in this.directionsReduced){
            if (Physics.Raycast(camPos, direction, this.minDist)){
                return false;
            }
        }

        return true;
    }

    private (Vector3, float) adjustDistance(Vector3 camPos, Vector3 objCentre, Vector3[] vertices, float objMinDist, bool draw=false){
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

            drawLine(camPos, topLeftClip, UnityEngine.Color.red);
            drawLine(camPos, topRight, UnityEngine.Color.green);
            drawLine(camPos, botLeft, UnityEngine.Color.blue);
            drawLine(camPos, botRight, UnityEngine.Color.yellow);
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
                        Debug.Log("minDist updated " + minDist.ToString() + " -> " + (hitDistAdjusted).ToString());
                        minDist = hitDistAdjusted;
                    }
                }
                if (draw){
                    float dist = hit ? hitInfo.distance : maxRayDist;
                    drawLine(rayStartPos, rayStartPos + direction * dist, hit ? UnityEngine.Color.red : UnityEngine.Color.green);
                    createSphere(rayStartPos, UnityEngine.Color.red, scale: .01f);
                }
            }
        }

        float optimalDistReverse = (camPos - objCentre).magnitude - optimalDist;

        float score = (optimalDistReverse - minDist)/optimalDistReverse;

        return (camPos + minDist * direction, score);
    }

    private (float, float) computeClipPlaneSizes(Camera cam){
        Debug.Log(Screen.width.ToString() + " " + Screen.height.ToString());
        float yAngle = cam.fieldOfView/2 * 3.1415f / 180f;
        float xAngle = cam.fieldOfView/2 * ((float) Screen.width/(float) Screen.height)* 3.1415f / 180f;
        Debug.Log(yAngle.ToString() + " " + xAngle.ToString() + " " + Screen.width.ToString() + " " + Screen.height.ToString());

        float halfXBox = cam.nearClipPlane * Mathf.Tan(xAngle);
        float halfYBox = cam.nearClipPlane * Mathf.Tan(yAngle);
        Debug.Log((2f * halfXBox).ToString() + " " + (2f * halfYBox).ToString());
        return (2f * halfXBox, 2f * halfYBox);
    }

    private List<Vector3> fibSphereSample(int n=25, List<(Vector3, float, float)> impossibleAngles=null){

        List<Vector3> points = new List<Vector3>();
        
        int numPruneAngles = impossibleAngles != null ? impossibleAngles.Count : 0;

        for(int i = 0; i < n; i++){
            float j = i + 0.5f;
            float phi = Mathf.Acos((1 - 2 * j / n));
            float theta = 10.1664f * j;  // precompute to (extremely slightly) lower computational load (3.1415f * (1f + Mathf.Sqrt(5f)) * j)
            Vector3 direction = new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), 
                                   Mathf.Sin(theta) * Mathf.Sin(phi),
                                   Mathf.Cos(phi));
            
            bool keepDirection = true;
                
            for( int k = 0; k < numPruneAngles; k++){
                float angle = Vector3.Angle(direction, impossibleAngles[k].Item1);
                if (angle <= impossibleAngles[k].Item2){
                    keepDirection = false;
                    break;
                }
            }
                
            if(keepDirection){
                points.Add(new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), 
                           Mathf.Sin(theta) * Mathf.Sin(phi),
                           Mathf.Cos(phi)));
            }
        }

        return points;
    } 

    // Selection sampling vertices
    private (List<Vector3[]>, List<Vector3>, Vector3) sampleVerticePoints(int samples=10){

        Vector3[] vertices;
        System.Random rand = new System.Random();
        List<Vector3[]> vertexSamples = new List<Vector3[]>();
        List<Vector3> meshCentres = new List<Vector3>();
        Vector3 sceneCentre = new Vector3(0f, 0f, 0f);
        int totalVerticesCount = 0;
        for(int i = 0; i < parts.Count; i++){

            vertices = recursiveGetVertices(parts[i].transform);
            int n = vertices.Length;
            int sampleSize = n <= samples ? n : samples;
            Vector3[] sample = new Vector3[samples];
            
            Vector3 center = new Vector3(0, 0, 0);
            int picked = 0;

            for (int j = 0; j < n; j++){
                double r = rand.NextDouble(); 

                if(r < ((double)(sampleSize - picked)/(double)(n - j))){
                    sample[picked] = vertices[j];
                    picked++;
                }
                center += vertices[j]; // TODO: If sample is big enough, could just use average of samples? (for slight performance increase)
            }
            vertexSamples.Add(sample);
            meshCentres.Add(center/vertices.Length);
            sceneCentre += center;
            totalVerticesCount += vertices.Length;

            // Debug.Log(i.ToString() + " " + vertices.Length.ToString() + " " + samples.ToString());
        }

        return (vertexSamples, meshCentres, sceneCentre/totalVerticesCount);
    }

    private Vector3[] recursiveGetVertices(Transform trans){
        Vector3[][] vertices = new Vector3[trans.childCount + 1][];
        Component mainMesh = trans.GetComponent("MeshFilter");
        Matrix4x4 localToWorld = trans.localToWorldMatrix;


        if (mainMesh != null){
            vertices[0] = ((MeshFilter) mainMesh).mesh.vertices;
            for(int i = 0; i < vertices[0].Length; i++){
                vertices[0][i] = localToWorld.MultiplyPoint3x4(vertices[0][i]); 
            }
        } else {
            vertices[0] = new Vector3[0];
        }

        for(int i = 0; i < trans.childCount; i++){
            vertices[i+1] = recursiveGetVertices(trans.GetChild(i));
        }

        return mergeArrays(vertices);
    }

    private T[] mergeArrays<T>(T[][] arrays){
        int combinedLength = 0;
        foreach(T[] array in arrays){
            combinedLength += array.Length;
        }
        T[] combinedArray = new T[combinedLength];
        int index = 0;
        foreach(T[] array in arrays){
            foreach (T element in array){
                combinedArray[index] = element;
                index++;
            }
        }

        return combinedArray;
    }

    private void recursiveChangeColor(GameObject obj, UnityEngine.Color col){
        Renderer r = obj.GetComponent<Renderer>();
        if( r != null){
            r.material.color = col;
        }
        foreach(Transform childT in obj.transform){
            recursiveChangeColor(childT.gameObject, col);
        }
    }

    private void addCollidersRecursive(GameObject obj){
        MeshRenderer ren = obj.GetComponent<MeshRenderer>();
        if (ren != null){
            MeshCollider collider = obj.GetComponent<MeshCollider>();
            if (collider == null){
                obj.AddComponent<MeshCollider>();
            }
        }
        foreach (Transform childT in obj.transform){
            addCollidersRecursive(childT.gameObject);
        }
    }

    public void normalizeArray(float[] arr){

        float minVal = float.MaxValue;
        float maxVal = float.MinValue;

        foreach (float val in arr){
            if (val < minVal){
                minVal = val;
            }
            if (val > maxVal){
                maxVal = val;
            }
        } 
        if (maxVal == minVal){
            maxVal += 1; // hack s.t. single value arrays return 0 for all values instead of NaN
        }

        for(int i = 0; i < arr.Length; i++){
            arr[i] = (arr[i] - minVal) / (maxVal - minVal);
        }
    }

    // Following functions are only for testing and not important for main functionality.

    public string arrayToString<T>(T[] arr){
		string res = string.Join(" , ", arr);
		return "[" + res + "]";
	}

    private void testAdjustDistance(){
        Vector3 camPos = this.cam.transform.position;
        Vector3 objCentre = new Vector3(0,0,0);
        Vector3[] vertices = new Vector3[this.dummyObjects.Count];
        
        for(int i = 0; i < this.dummyObjects.Count; i++){
            objCentre = objCentre + this.dummyObjects[i].transform.position;
            vertices[i] = this.dummyObjects[i].transform.position;
        }

        objCentre = objCentre / this.dummyObjects.Count;
        createSquare(objCentre, UnityEngine.Color.black, scale: 0.1f);
        float score;

        (this.cam.transform.position, score) = adjustDistance(camPos, objCentre, vertices, 0f);
        this.cam.transform.LookAt(objCentre);

    }

    private void setUpTestAdjustDistance(){
        // Flytte camera til test-position
        this.cam.transform.position = this.dummyCam.transform.position;
        this.cam.transform.rotation = this.dummyCam.transform.rotation;
    }

    // Debugging functions to visualise raycast and vertices
    private void createSphere(Vector3 pos, UnityEngine.Color col, float scale=0.02f){
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.transform.position = pos;
        g.transform.localScale = new Vector3(scale, scale, scale);
        g.GetComponent<Renderer>().material.color = col;
        g.GetComponent<Collider>().enabled = false;
    }

    private void createSquare(Vector3 pos, UnityEngine.Color col, float scale=0.02f){
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); 
        g.transform.position = pos;
        g.transform.localScale = new Vector3(scale, scale, scale);
        g.GetComponent<Renderer>().material.color = col;
        g.GetComponent<Collider>().enabled = false;
    }

    private void drawLine(Vector3 start, Vector3 end, Color color, float width=0.002f){
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        Material m = new Material(basicMat);
        m.color = color;
        lr.material = m;
        // lr.SetColors(color, color);
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        // lr.SetWidth(width, width);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        // GameObject.Destroy(myLine, duration);
    } 
}
