using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generel Util class for general utility functions. TODO: Consider splitting class up
/// </summary>
public class Util 
{
    /// <summary>
    /// Checks (using raycasts) if a position is inside some active mesh (which has a physics collider ascociated with it)
    /// </summary>
    /// <param name="pos">Position that might be inside mesh</param>
    /// <param name="maxDist">Maximum distance for raycast</param>
    /// <returns>True if position is inside some mesh, false otherwise.</returns>
    public static bool IsInSideMesh(Vector3 pos, float maxDist=2f){
        RaycastHit[] hitsIn;
        RaycastHit[] hitsOut;

        hitsIn = Physics.RaycastAll(pos, Vector3.up, maxDist); 
        hitsOut = Physics.RaycastAll(pos - maxDist * Vector3.up, -Vector3.up, maxDist);

        return hitsIn.Length == hitsOut.Length;
    }

    // This is inefficient. TODO: Fix
    /// <summary>
    /// Checks if there is some minimum distance of space around a given position. Uses raycasts.
    /// </summary>
    /// <param name="camPos">Position to be validated</param>
    /// <param name="directionsReduced">A small list of directions to be checked for the minimal distance</param>
    /// <param name="minDist">Minumum distance need for position to be valid.</param>
    /// <returns>True if there is pace around positions, false otherwise</returns>
    public static bool ValidateProximity(Vector3 camPos, List<Vector3> directionsReduced, float minDist){

        foreach( Vector3 direction in directionsReduced){
            if (Physics.Raycast(camPos, direction, minDist)){
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Computes the size of the near clip plane of a camera
    /// </summary>
    /// <param name="cam">Cmare whose near clip plane size is desired.</param>
    /// <returns>(Near clip plane size x, near clip plane size y)</returns>
    public static (float, float) ComputeClipPlaneSizes(Camera cam){
        // Debug.Log(Screen.width.ToString() + " " + Screen.height.ToString());
        float yAngle = cam.fieldOfView/2 * 3.1415f / 180f;
        float xAngle = cam.fieldOfView/2 * ((float) Screen.width/(float) Screen.height)* 3.1415f / 180f;
        // Debug.Log(yAngle.ToString() + " " + xAngle.ToString() + " " + Screen.width.ToString() + " " + Screen.height.ToString());

        float halfXBox = cam.nearClipPlane * Mathf.Tan(xAngle);
        float halfYBox = cam.nearClipPlane * Mathf.Tan(yAngle);
        // Debug.Log((2f * halfXBox).ToString() + " " + (2f * halfYBox).ToString());
        return (2f * halfXBox, 2f * halfYBox);
    }

    /// <summary>
    /// Selects a set of points on a sphere, s.t. they are reasonably equidistant from each other. 
    /// </summary>
    /// <param name="n">Number of samples</param>
    /// <param name="impossibleAngles">A list of directions containing a dierection and 2 float angle values. 
    /// Makes the sampling exlude points that are within the first angle from this direction. 
    /// If not null, the number of samples returned will be smaller than n</param>
    /// <returns>A list of reasonably uniformy distrubuted directions (or points on a sphere with radius 1).</returns>
    public static List<Vector3> FibSphereSample(int n=25, List<(Vector3, float, float)> impossibleAngles=null){

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
                float angle = Vector3.Angle(-direction, impossibleAngles[k].Item1);
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

    /// <summary>
    /// Recursively checks if a given gameobject is the child of another given gameobject.
    /// </summary>
    /// <param name="parent">Potential child</param>
    /// <param name="child">Potential parent</param>
    /// <returns>True if "child" gameobject is a nested child of "parent" gameobject, otherwise false</returns>
    public static bool IsNestedChild(GameObject parent, GameObject child){
        if (parent == child) {
            return true;
        }
        Transform trans = child.transform;
        while(!(trans.parent is null)){
            // Debug.Log(trans.parent.gameObject.name + " : " + parent.name);
            if (GameObject.ReferenceEquals(trans.parent.gameObject, parent)){
                return true;
            }
            trans = trans.parent;
        }
        // Debug.Log(trans.parent + " " + parent.name);
        return false;

    }

    /// <summary>
    /// Randomly samples a subset of vertice points from a List of given gameobjects (I.e. a subset of points from the meshes of these gameobjects) 
    /// </summary>
    /// <param name="parts">List of gameobjects for which a sample is desired</param>
    /// <param name="samples">Number of point in each sample. If mesh sieze is smaller than this value, the set of mesh vertice points are returned.</param>
    /// <returns>A tuple containing the samples vertice points, the centres of each of the meshes, and the centre of all the meshes.</returns>
    public static (List<Vector3[]>, List<Vector3>, Vector3) SampleVerticePoints(List<GameObject> parts, int samples=10){

        Vector3[] vertices;
        System.Random rand = new System.Random();
        List<Vector3[]> vertexSamples = new List<Vector3[]>();
        List<Vector3> meshCentres = new List<Vector3>();
        Vector3 sceneCentre = new Vector3(0f, 0f, 0f);
        int totalVerticesCount = 0;
        for(int i = 0; i < parts.Count; i++){

            vertices = Util.RecursiveGetVertices(parts[i].transform);
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
                center += vertices[j]; // TODO: If sample is big enough, could just use average of samples? (for slight performance increase?)
            }
            vertexSamples.Add(sample);
            meshCentres.Add(center/vertices.Length);
            sceneCentre += center;
            totalVerticesCount += vertices.Length;

            // Debug.Log(i.ToString() + " " + vertices.Length.ToString() + " " + samples.ToString());
        }

        return (vertexSamples, meshCentres, sceneCentre/totalVerticesCount);
    }

    /// <summary>
    /// Recusiveley finds all vertice points for all meshes ascociated with a given transform.
    /// </summary>
    /// <param name="trans">Transform of the parent for all the desired mesh vertice points.</param>
    /// <returns>An array of all mesh vertice point of transform and all its children.</returns>
    public static Vector3[] RecursiveGetVertices(Transform trans){
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
            vertices[i+1] = RecursiveGetVertices(trans.GetChild(i));
        }

        return Util.MergeArrays(vertices);
    }

    /// <summary>
    /// Merges an array of arrays to a single array.
    /// </summary>
    /// <typeparam name="T">Type of the variables contained in the array of arrays</typeparam>
    /// <param name="arrays">The array of arrays to be merged.</param>
    /// <returns>The merged array</returns>
    public static T[] MergeArrays<T>(T[][] arrays){
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

    /// <summary>
    /// Recursively changes the color of a gameobjects material as well as the materials of all its children.
    /// </summary>
    /// <param name="obj">Gameobjects whose color need changing</param>
    /// <param name="col">The color to change to.</param>
    public static void RecursiveChangeColor(GameObject obj, UnityEngine.Color col){
        Renderer r = obj.GetComponent<Renderer>();
        if( r != null){
            r.material.color = col;
        }
        foreach(Transform childT in obj.transform){
            RecursiveChangeColor(childT.gameObject, col);
        }
    }

    /// <summary>
    /// Recursively adds mesh colliders to a gameobjet and all its children.
    /// </summary>
    /// <param name="obj">The gameobject that needs to have mesh coliders added.</param>
    public static void AddCollidersRecursive(GameObject obj){
        MeshRenderer ren = obj.GetComponent<MeshRenderer>();
        if (ren != null){
            MeshCollider collider = obj.GetComponent<MeshCollider>();
            if (collider == null){
                obj.AddComponent<MeshCollider>();
            }
        }
        foreach (Transform childT in obj.transform){
            AddCollidersRecursive(childT.gameObject);
        }
    }

    /// <summary>
    /// Normalises the values of an array to be in the range [0f, 1f]. Mutates original array.
    /// </summary>
    /// <param name="arr">The array to be normalised.</param>
    public static void NormalizeArray(float[] arr){

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

    /// <summary>
    /// Normalises a row of a 2D array. Mutates array.
    /// </summary>
    /// <param name="arr">Array with row to be normalised.</param>
    /// <param name="rowIndex">Row index to be normalised.</param>
    public static void NormalizeArrayRow(float[,] arr, int rowIndex){

        float minVal = float.MaxValue;
        float maxVal = float.MinValue;

        int cols = arr.GetLength(1);

        // foreach (float val in arr[rowIndex]){
        for (int i = 0; i < cols; i++){
            float val = arr[rowIndex, i];
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

        for(int i = 0; i < cols; i++){
            arr[rowIndex, i] = (arr[rowIndex, i] - minVal) / (maxVal - minVal);
        }
    }

    /// <summary>
    /// Creates a ScoreComputer object based on a ScoreType.
    /// </summary>
    /// <param name="scoreType">Type of ScoreComputer to be created.</param>
    /// <param name="weight">Weight of the ScoreComputer to be created.</param>
    /// <returns>A ScoreComputer of the desied type and with the desired weight.</returns>
    /// <exception cref="System.ArgumentException">If new Score types are added without this method being updated it will cause an expetion.</exception>
    public static ScoreComputer CreateScoreComputerFromScoreType(ScoreType scoreType, float weight){
        
        ScoreComputer scoreComputer = null;
        switch(scoreType) 
        {
        case ScoreType.LastPositionDistance:
            scoreComputer = new LastPositionDistanceScore(weight);
            break;
        case ScoreType.Angle:
            scoreComputer = new AngleScore(weight);
            break;
        case ScoreType.DistanceToObject:
            scoreComputer = new DistanceToObjectScore(weight);
            break;
        case ScoreType.HumanSensibleAngles:
            scoreComputer = new HumanSensibleAnglesScore(weight);
            break;
        case ScoreType.Spread:
            scoreComputer = new SpreadScore(weight);
            break;
        case ScoreType.Stability:
            scoreComputer = new StabilityScore(weight);
            break;
        case ScoreType.Visibility:
            scoreComputer = new VisibilityScore(weight);
            break;
        default:
            // code block
            throw new System.ArgumentException("ScoreType " + scoreType.ToString() + " not recognised. If newly created score, add case to switch statement");
        }

        return scoreComputer;
    }


    public static string ArrayToString<T>(T[] arr){
		string res = string.Join(" , ", arr);
		return "[" + res + "]";
	}

    // Debugging fucntions.

    public static void CreateSphere(Vector3 pos, UnityEngine.Color col, float scale=0.02f){
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.transform.position = pos;
        g.transform.localScale = new Vector3(scale, scale, scale);
        g.GetComponent<Renderer>().material.color = col;
        g.GetComponent<Collider>().enabled = false;
    }

    public static void CreateSquare(Vector3 pos, UnityEngine.Color col, float scale=0.02f){
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); 
        g.transform.position = pos;
        g.transform.localScale = new Vector3(scale, scale, scale);
        g.GetComponent<Renderer>().material.color = col;
        g.GetComponent<Collider>().enabled = false;
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, Material basicMat, float width=0.002f){
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
