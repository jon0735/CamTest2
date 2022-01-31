using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
    private bool visualDebug = false;
    [SerializeField]
    private bool computeCamPos = true; 
    
    [SerializeField]
    private float fovFactor = .9f;

    private float[] individualMinDists;


    // For Debugging
    private Vector3[] lastCamPositions;

    private int currentPosIndex;

    [SerializeField]
    private List<GameObject> dummyObjects;

    [SerializeField]
    private Camera dummyCam;


    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject part in this.parts){
            part.SetActive(false);
        }
        this.directions = fibSphereSample(n: sphereSampleSize);
        this.directionsReduced = fibSphereSample(n: 15); // TODO: Fix Hardcoding
        (this.vertexSamples, this.objectCentres) = sampleVerticePoints(samples: vertexSampleSize);
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
                float thisVertexDist = (this.parts[i].transform.position - vertex).magnitude;
                if (thisVertexDist > maxVertexDist){
                    maxVertexDist = thisVertexDist;
                }
            }
            this.individualMinDists[i] = Mathf.Max(this.cam.nearClipPlane + maxVertexDist, this.minDist);
            addCollidersRecursive(this.parts[i]);
        }
        // Debug.Log(arrayToString(this.individualMinDists));
        foreach (GameObject initObj in this.initParts){
            addCollidersRecursive(initObj);
        }
        
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
                Debug.Log(this.objectCentres[currentPart]);
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

            // parts[currentPart].transform.GetChild(0).GetComponent<MeshRenderer>().material.color = UnityEngine.Color.grey;
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

    // Main algorithmz
    private Vector3 findCamPos(){
        // Debug.Log("WTF-1");

        List<Vector3> positions = new List<Vector3>();

        // Vector3 objectPos = parts[currentPart].transform.position;
        Vector3 objectPos = this.objectCentres[currentPart];

        // int hits = 0;
        // int notHits = 0;

        RaycastHit hit;
        RaycastHit[] hitsIn;
        RaycastHit[] hitsOut;
        foreach(Vector3 direction in directions){

            if (Physics.Raycast(objectPos, direction, out hit, maxDist)){
                // Debug.Log("Hit: " + hit.distance.ToString());
                if (hit.distance > individualMinDists[currentPart]){
                    Vector3 camPos = objectPos + direction * (hit.distance - 1.05f * this.minDist);
                    if (this.validateVisibility(camPos, objectPos, this.vertexSamples[currentPart])) {
                        // Vector3 camPos = objectPos + direction * (hit.distance - 1.05f * this.minDist);
                        float dist = hit.distance - 0.1f;
                        hitsIn = Physics.RaycastAll(camPos, -direction, dist); // TODO: May be to fickle. Maybe just straight up from cam and to cam?
                        hitsOut = Physics.RaycastAll(objectPos, direction, dist);
                        Debug.Log("Hits In: " + hitsIn.Length.ToString() + ",  Hits Out: " + hitsOut.Length.ToString());
                        if (hitsIn.Length == hitsOut.Length){
                            positions.Add(camPos); // TODO: Fix hardcoding
                        } else {
                            Debug.Log("Found Inside Mesh");
                        }

                    }
                }
                // hits++;
                // if (this.visualDebug){
                //     createSquare(objectPos + direction * hit.distance, this.colArray[currentPart % this.colArray.Length]);
                // }

            } else {
                // Debug.Log("No hit");
                if (this.validateVisibility(objectPos + direction * maxDist, objectPos, this.vertexSamples[currentPart])) {
                    positions.Add(objectPos + direction * maxDist);
                }
                // notHits++;
            }
        }


        int[] collisions = new int[positions.Count];
        // Debug.Log("WTF0");

        for(int i = 0; i < positions.Count; i++){
            int colls = 0;
            // Vector3 camToObj = objectPos - positions[i];
            // float camDist = camToObj.magnitude;
            // Debug.Log("WTF1");
            // float dist = camToObj.magnitude;
            if(this.visualDebug){
                createSphere(positions[i], this.colArray[currentPart % this.colArray.Length], scale: 0.05f);
            }
            for(int j = 0; j < this.vertexSamples[currentPart].Length; j++){
                // Debug.Log("WTF2");
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
        Debug.Log(arrayToString(collisions));
        if (this.visualDebug){
            for(int j = 0; j < this.vertexSampleSize; j++){
                // createSphere(vertexSamples[currentPart][j], this.colArray[currentPart % this.colArray.Length]);     
                createSquare(vertexSamples[currentPart][j], this.colArray[currentPart % this.colArray.Length]);     
            }
        }
        Debug.Log("Collisions:" + arrayToString(collisions));
        // Debug.Log("WTF4");


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
            // positions = positions.GetRange(0, prunedPosCount);
        }


        // Debug.Log("1");

        float[] distScores = new float[prunedPosCount];
        float[] angleScores = new float[prunedPosCount];
        float[] spreadScores = new float[prunedPosCount];
        float[] visibilityScores = new float[prunedPosCount];
        // TODO: Add something to represent there being space around the found area

        float[] fullScores = new float[prunedPosCount];

        Vector3 centre = new Vector3(0f, 0f, 0f); // TODO: Assumes item is centred in unity scene. Consider other ways. 

        // TODO: Fix all hardcoding

        for(int i = 0; i < prunedPosCount; i++){

            distScores[i] = Vector3.Distance(positionsArray[i], cam.transform.position) / (2f * this.maxDist);
            angleScores[i] = Vector3.Angle(objectPos - positionsArray[i], centre - positionsArray[i]) / 180f;
            
            float avgSpred = 0f;
            Vector3 camToObj = objectPos - positionsArray[i];
            float dist = camToObj.magnitude;
            Vector3 scaledCamPos = positionsArray[i] + (camToObj * (1 - 1/dist));
            for(int j = 0; j < this.vertexSamples[this.currentPart].Length; j++){ // TODO: Handle distance to object
                Vector3 test = positionsArray[i];

                avgSpred += Vector3.Angle(objectPos - scaledCamPos, this.vertexSamples[this.currentPart][j] - scaledCamPos);
            }

            // Debug.Log("6 " + i.ToString());

            avgSpred = avgSpred / this.vertexSamples[this.currentPart].Length;
            spreadScores[i] = (1f - avgSpred / 60f);
            
            // Debug.Log("7 " + i.ToString());

            visibilityScores[i] = ((float) collisions[i] / (float) this.vertexSamples[this.currentPart].Length); 
            Debug.Log("Colls: " + ((float) collisions[i]).ToString());
            Debug.Log("sampleSize: " + (((float) this.vertexSamples[this.currentPart].Length)).ToString());

            // fullScores[i] = distScores[i] + angleScores[i] + spreadScores[i] + visibilityScores[i];
            // Debug.Log("8 " + i.ToString());
        }

        this.normalizeArray(distScores);
        this.normalizeArray(angleScores);
        this.normalizeArray(spreadScores);
        this.normalizeArray(visibilityScores);


        for(int i = 0; i < spreadScores.Length; i++){
            fullScores[i] = distScores[i] * this.distWeight + 
                            angleScores[i] * this.angleWeight + 
                            spreadScores[i] * this.spreadWeight +
                            visibilityScores[i] * this.visibilityWeight;
        }


        // Debug.Log("WTF7");


        // Debug.Log("Before:" + arrayToString(positionsArray));
        
        // positionsArray = positions.ToArray();  
        Array.Sort(fullScores, positionsArray);

        // Debug.Log("After:" + arrayToString(positionsArray));


        Debug.Log("distScores:" + arrayToString(distScores));
        Debug.Log("angleScores:" + arrayToString(angleScores));
        Debug.Log("spreadScores:" + arrayToString(spreadScores));
        Debug.Log("visibilityScores:" + arrayToString(visibilityScores));
        Debug.Log("Full:" + arrayToString(fullScores));

        

        // Debug.Log("WTF1");
        this.lastCamPositions = positionsArray;
        this.currentPosIndex = 0;
        if (positions.Count > 0 ) {
            for (int i = 0; i < positionsArray.Length; i++){
                positionsArray[i] = 
                    adjustDistance(positionsArray[i], 
                                   this.objectCentres[this.currentPart], 
                                   this.vertexSamples[this.currentPart]);
            }
            return positionsArray[0];
        } else {
            
            // Debug.Log("WTF2");
            return cam.transform.position;
        }
    }

    private List<Vector3> fibSphereSample(int n=25){

        List<Vector3> points = new List<Vector3>();
        
        for(int i = 0; i < n; i++){
            float j = i + 0.5f;
            float phi = Mathf.Acos((1 - 2 * j / n));
            // float theta = 3.1415f * (1f + Mathf.Sqrt(5f)) * j;
            float theta = 10.1664f * j;  // precompute to (extremely slightly) lower computational load
            points.Add(new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), 
                                   Mathf.Sin(theta) * Mathf.Sin(phi),
                                   Mathf.Cos(phi)));
        }

        return points;
    } 

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

    private bool validateProximity(Vector3 camPos){

        foreach( Vector3 direction in this.directionsReduced){
            if (Physics.Raycast(camPos, direction, this.minDist)){
                return false;
            }
        }

        return true;
    }

    private Vector3 adjustDistance(Vector3 camPos, Vector3 objCentre, Vector3[] vertices){
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
            Debug.Log("Before: " + vertices[i].ToString() + ", After: " + pointLocal.ToString());
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

        float newDist = 0f;

        float desiredYAngle = this.fovFactor * this.cam.fieldOfView/2 * 3.1415f / 180f;
        float desiredXAngle = this.fovFactor * this.cam.fieldOfView/2 * (Screen.width/Screen.height) * 3.1415f / 180f;

        float[] desiredAngles = new float[]{desiredYAngle, desiredXAngle};
        float[,] cathetusSizes = new float[,]{{maxY, minY},{maxX, minX}};

        for(int i = 0; i < 2; i++){
            float desiredAngle = desiredAngles[i];
            for (int j = 0; j < 2; j++){
                float dist = Mathf.Abs(cathetusSizes[i, j]) * (Mathf.Cos(desiredAngle)/Mathf.Sin(desiredAngle));
                if (dist > newDist){
                    newDist = dist;
                }
            }
        }

        if (newDist < cam.nearClipPlane){
            newDist = cam.nearClipPlane * 1.1f;
        }

        Vector3 camToCentre = objCentre - camPos;
        Vector3 direction = camToCentre/camToCentre.magnitude;

        // float distToView = (camPos - objCentre).magnitude;


        return camPos + (minZ - newDist) * direction;

        // Vector3 screenPos = this.dummyCam.WorldToScreenPoint(vertices[0]);

        // float maxX = screenPos.x;
        // float maxY = screenPos.y;
        // float minX = screenPos.x;
        // float minY = screenPos.y;

        // Vector3 maxXPoint = screenPos;
        // Vector3 maxYPoint = screenPos;
        // Vector3 minXPoint = screenPos;
        // Vector3 minYPoint = screenPos;

        // for(int i = 1; i < vertices.Length; i++){
        //     screenPos = this.dummyCam.WorldToScreenPoint(vertices[i]);
        //     Debug.Log("Before: " + vertices[i].ToString() + ", After: " + screenPos.ToString());
        //     if (maxX < screenPos.x) {
        //         maxX = screenPos.x;
        //         maxXPoint = vertices[i];
        //     }
        //     if (maxY < screenPos.y) {
        //         maxY = screenPos.y;
        //         maxYPoint = vertices[i];
        //     }
        //     if (minX > screenPos.x) {
        //         minX = screenPos.x;
        //         minXPoint = vertices[i];
        //     }
        //     if (minY > screenPos.y) {
        //         minY = screenPos.y;
        //         minYPoint = vertices[i];
        //     }

        // }

        // Debug.Log("MaxX: " + maxXPoint.ToString());
        // Debug.Log("MaxY: " + maxYPoint.ToString());
        // Debug.Log("MinX: " + minXPoint.ToString());
        // Debug.Log("MinY: " + minYPoint.ToString());

        // if (this.visualDebug){
        //     this.createSquare(maxXPoint, UnityEngine.Color.blue, scale: 0.1f);
        //     this.createSquare(maxYPoint, UnityEngine.Color.red, scale: 0.1f);
        //     this.createSquare(minXPoint, UnityEngine.Color.yellow, scale: 0.1f);
        //     this.createSquare(minYPoint, UnityEngine.Color.green, scale: 0.1f);
        //     Debug.Log("Created");
        }

    // private Vector3 adjustDistance(Vector3 camPos, Vector3 objCentre, Vector3[] vertices){
    //     this.dummyCam.transform.position = camPos;
    //     this.dummyCam.transform.LookAt(objCentre);

    //     Vector3 screenPos = this.dummyCam.WorldToScreenPoint(vertices[0]);

    //     float maxX = screenPos.x;
    //     float maxY = screenPos.y;
    //     float minX = screenPos.x;
    //     float minY = screenPos.y;

    //     Vector3 maxXPoint = screenPos;
    //     Vector3 maxYPoint = screenPos;
    //     Vector3 minXPoint = screenPos;
    //     Vector3 minYPoint = screenPos;

    //     for(int i = 1; i < vertices.Length; i++){
    //         screenPos = this.dummyCam.WorldToScreenPoint(vertices[i]);
    //         Debug.Log("Before: " + vertices[i].ToString() + ", After: " + screenPos.ToString());
    //         if (maxX < screenPos.x) {
    //             maxX = screenPos.x;
    //             maxXPoint = vertices[i];
    //         }
    //         if (maxY < screenPos.y) {
    //             maxY = screenPos.y;
    //             maxYPoint = vertices[i];
    //         }
    //         if (minX > screenPos.x) {
    //             minX = screenPos.x;
    //             minXPoint = vertices[i];
    //         }
    //         if (minY > screenPos.y) {
    //             minY = screenPos.y;
    //             minYPoint = vertices[i];
    //         }

    //     }

    //     Debug.Log("MaxX: " + maxXPoint.ToString());
    //     Debug.Log("MaxY: " + maxYPoint.ToString());
    //     Debug.Log("MinX: " + minXPoint.ToString());
    //     Debug.Log("MinY: " + minYPoint.ToString());

    //     if (this.visualDebug){
    //         this.createSquare(maxXPoint, UnityEngine.Color.blue, scale: 0.1f);
    //         this.createSquare(maxYPoint, UnityEngine.Color.red, scale: 0.1f);
    //         this.createSquare(minXPoint, UnityEngine.Color.yellow, scale: 0.1f);
    //         this.createSquare(minYPoint, UnityEngine.Color.green, scale: 0.1f);
    //         Debug.Log("Created");
    //     }


        // Project all points onto screen plane
        // Find min and max for x and y (4 values total)
        // (Maybe TODO: Consider if finetuning rotation (lookAt) based on this makes sense)
        // For each axis find the point to base zoom on (Which point is furthest from centre)
        // Zoom cam pos based on the above found points s.t. the points are relatively close to screen edge

    //     return camPos;
    // }

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

        this.cam.transform.position = adjustDistance(camPos, objCentre, vertices);
        this.cam.transform.LookAt(objCentre);

        // zoome ind på test-objecter
    }

    private void setUpTestAdjustDistance(){
        // Flytte camera til test-position
        this.cam.transform.position = this.dummyCam.transform.position;
        this.cam.transform.rotation = this.dummyCam.transform.rotation;
    }

    private (List<Vector3[]>, List<Vector3>) sampleVerticePoints(int samples=10){

        // Mesh m;
        Vector3[] vertices;
        // List<Matrix4x4> localToWorldTransforms;
        System.Random rand = new System.Random();
        List<Vector3[]> vertexSamples = new List<Vector3[]>();
        List<Vector3> meshCentres = new List<Vector3>();
        for(int i = 0; i < parts.Count; i++){
            Debug.Log("Starting " + i.ToString());
            // m = ((MeshFilter) parts[i].GetComponent("MeshFilter")).mesh;
            // m = ((MeshFilter) parts[i].transform.Find("default").gameObject.GetComponent("MeshFilter")).mesh;
            // vertices = m.vertices;
            // (vertices, localToWorldTransforms) = recursiveGetVertices2(parts[i].transform, new List<Vector3[]>(), new List<Matrix4x4>());
            vertices = recursiveGetVertices(parts[i].transform);
            // int vertexCount = 0;
            // foreach(Vector3[] v in vertices){
            //     vertexCount += v.Length;
            // }
            int vertexCount = vertices.Length;
            Debug.Log("Vertice length: " + vertexCount.ToString());
            // Matrix4x4 localToWorld = parts[i].transform.localToWorldMatrix;
            
            int sampleSize = vertexCount <= samples ? vertexCount : samples;
            Vector3[] sample = new Vector3[samples];
            int picked = 0;
            int n = vertexCount; // TODO: Fix having two variables for one value
            // int[] indecies = new int[sampleSize];

            // int j = 0;
            // // int alreadyChecked = 0;
            // // Debug.Log("i: " + i.ToString() + ", sampleSize: " +  sampleSize.ToString() + ", n: " + n.ToString());
            // for(int k = 0; k < vertices.Count; k++){
            //     Matrix4x4 localToWorld = localToWorldTransforms[k];
            //     Vector3[] thisTransVertices = vertices[k];
                
            //     for(int t = 0; t < thisTransVertices.Length; t++){
            //         double r = rand.NextDouble(); 
            //         if(r < ((double)(sampleSize - picked)/(double)(n - j))){
            //             // Debug.Log("i: " + i.ToString() + 
            //             //           ", ss: " +  sampleSize.ToString() + 
            //             //           ", n: " + n.ToString() + 
            //             //           ", j: " + j.ToString() +  
            //             //           ", k: " + k.ToString() + 
            //             //           ", t: " + t.ToString() + 
            //             //         //   ", alreadyChecked: " + alreadyChecked.ToString() + 
            //             //           ", picked: " + picked.ToString() + 
            //             //         //   ", index: " + (j - alreadyChecked).ToString() +
            //             //           ", arrLength: " + thisTransVertices.Length.ToString());
            //             sample[picked] = localToWorld.MultiplyPoint3x4(thisTransVertices[t]);
            //             // indecies[picked] = j;
            //             // Debug.Log(picked.ToString() + " " + j.ToString());
            //             picked++;
            //             if (picked == samples){
            //                 break;
            //             }
            //         }
            //         j++;
            //     }
            //     // alreadyChecked += thisTransVertices.Length;
                
            // }
            // vertexSamples.Add(sample);
            Vector3 center = new Vector3(0, 0, 0);

            for (int j = 0; j < n; j++){
                double r = rand.NextDouble(); 
                // Debug.Log(r.ToString() + " " + ((double)(sampleSize - picked)/(double)(n - j)).ToString());
                if(r < ((double)(sampleSize - picked)/(double)(n - j))){
                    
                    // sample[picked] = localToWorld.MultiplyPoint3x4(vertices[j]);
                    sample[picked] = vertices[j];
                    // indecies[picked] = j;
                    // Debug.Log(picked.ToString() + " " + j.ToString());
                    picked++;
                    // if (picked == samples){
                    //     break;
                    // }
                }
                center += vertices[j]; // TODO: If big enough sample could just use average of samples?
            }
            vertexSamples.Add(sample);
            meshCentres.Add(center/vertices.Length);

            Debug.Log(i.ToString() + " " + vertices.Length.ToString() + " " + samples.ToString());
            // Debug.Log(arrayToString(indecies));
        }

        return (vertexSamples, meshCentres);
    }

    private (List<Vector3[]>, List<Matrix4x4>) recursiveGetVertices2(Transform trans, List<Vector3[]> vertices, List<Matrix4x4> localToWorldTransforms){
        // Vector3[][] vertices = new Vector3[trans.childCount + 1][];
        Component mainMesh = trans.GetComponent("MeshFilter");

        if (mainMesh != null){
            vertices.Add(((MeshFilter) mainMesh).mesh.vertices);
            localToWorldTransforms.Add(trans.localToWorldMatrix);
        } 
        // else {
        //     vertices[0] = new Vector3[0];
        //     localToWorldTransforms.Add(Matrix4x4.identity);
        // }

        for(int i = 0; i < trans.childCount; i++){
            (vertices, localToWorldTransforms) = recursiveGetVertices2(trans.GetChild(i), vertices, localToWorldTransforms);
        }

        return (vertices, localToWorldTransforms);
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
            maxVal += 1; // hack to single value arrays return 0 for all values instead of NaN
        }

        for(int i = 0; i < arr.Length; i++){
            arr[i] = (arr[i] - minVal) / (maxVal - minVal);
        }
    }

    public string arrayToString<T>(T[] arr){
		string res = string.Join(" , ", arr);
		return "[" + res + "]";
	}

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
