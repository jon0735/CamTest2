using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util 
{
    public static bool isInSideMesh(Vector3 pos, float maxDist=2f){
        RaycastHit[] hitsIn;
        RaycastHit[] hitsOut;

        hitsIn = Physics.RaycastAll(pos, Vector3.up, maxDist); 
        hitsOut = Physics.RaycastAll(pos - maxDist * Vector3.up, -Vector3.up, maxDist);

        return hitsIn.Length == hitsOut.Length;
    }

    // This is inefficient. TODO: Fix
    public static bool validateProximity(Vector3 camPos, List<Vector3> directionsReduced, float minDist){

        foreach( Vector3 direction in directionsReduced){
            if (Physics.Raycast(camPos, direction, minDist)){
                return false;
            }
        }

        return true;
    }

    public static (float, float) computeClipPlaneSizes(Camera cam){
        Debug.Log(Screen.width.ToString() + " " + Screen.height.ToString());
        float yAngle = cam.fieldOfView/2 * 3.1415f / 180f;
        float xAngle = cam.fieldOfView/2 * ((float) Screen.width/(float) Screen.height)* 3.1415f / 180f;
        Debug.Log(yAngle.ToString() + " " + xAngle.ToString() + " " + Screen.width.ToString() + " " + Screen.height.ToString());

        float halfXBox = cam.nearClipPlane * Mathf.Tan(xAngle);
        float halfYBox = cam.nearClipPlane * Mathf.Tan(yAngle);
        Debug.Log((2f * halfXBox).ToString() + " " + (2f * halfYBox).ToString());
        return (2f * halfXBox, 2f * halfYBox);
    }

    public static List<Vector3> fibSphereSample(int n=25, List<(Vector3, float, float)> impossibleAngles=null){

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

    public static bool isNestedChild(GameObject parent, GameObject child){
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

    public static (List<Vector3[]>, List<Vector3>, Vector3) sampleVerticePoints(List<GameObject> parts, int samples=10){

        Vector3[] vertices;
        System.Random rand = new System.Random();
        List<Vector3[]> vertexSamples = new List<Vector3[]>();
        List<Vector3> meshCentres = new List<Vector3>();
        Vector3 sceneCentre = new Vector3(0f, 0f, 0f);
        int totalVerticesCount = 0;
        for(int i = 0; i < parts.Count; i++){

            vertices = Util.recursiveGetVertices(parts[i].transform);
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

    public static Vector3[] recursiveGetVertices(Transform trans){
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

        return Util.mergeArrays(vertices);
    }

    public static T[] mergeArrays<T>(T[][] arrays){
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

    public static void recursiveChangeColor(GameObject obj, UnityEngine.Color col){
        Renderer r = obj.GetComponent<Renderer>();
        if( r != null){
            r.material.color = col;
        }
        foreach(Transform childT in obj.transform){
            recursiveChangeColor(childT.gameObject, col);
        }
    }

    public static void addCollidersRecursive(GameObject obj){
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

    public static void normalizeArray(float[] arr){

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


    public static string arrayToString<T>(T[] arr){
		string res = string.Join(" , ", arr);
		return "[" + res + "]";
	}

    public static void createSphere(Vector3 pos, UnityEngine.Color col, float scale=0.02f){
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.transform.position = pos;
        g.transform.localScale = new Vector3(scale, scale, scale);
        g.GetComponent<Renderer>().material.color = col;
        g.GetComponent<Collider>().enabled = false;
    }

    public static void createSquare(Vector3 pos, UnityEngine.Color col, float scale=0.02f){
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); 
        g.transform.position = pos;
        g.transform.localScale = new Vector3(scale, scale, scale);
        g.GetComponent<Renderer>().material.color = col;
        g.GetComponent<Collider>().enabled = false;
    }

    public static void drawLine(Vector3 start, Vector3 end, Color color, Material basicMat, float width=0.002f){
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
