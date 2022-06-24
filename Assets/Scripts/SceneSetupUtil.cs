using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSetupUtil : MonoBehaviour
{

    [SerializeField]
    private CameraPositionFinder cameraPositionFinder;

    [SerializeField]
    private MovementScript movementScript;

    [SerializeField]
    private SceneControls sceneControls;

    [SerializeField]
    private bool fixShaderErrors = false;

    [SerializeField]
    private bool addScoringTypes = false;

    [SerializeField]
    private bool setupPartsList = false;


    [SerializeField]
    private bool setupAll = false;
    

    // [SerializeField]
    // private Material baseMat = null;
    // private Shader standardShader;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetAllFalse(){
        this.fixShaderErrors = false;
        this.setupPartsList = false;
        this.addScoringTypes = false;
        this.setupAll = false;
    }

    void OnDrawGizmosSelected(){

        if(this.fixShaderErrors){
            this.FixShaders();
            // this.fixShaderErrors = false;
        }
        if(this.setupPartsList){
            this.SetupPartsList();
            // this.addAllToPartsList = false;
        }
        if(this.addScoringTypes){
            this.AddScoringTypes();
            
        }
        if(this.setupAll){
            this.SetupAll();
            // this.setupAll = false;
        }
        this.SetAllFalse();

    }

    private void SetupAll(){
        this.AddScripts();
        this.FixCamera();
        this.FixShaders();
        this.SetupPartsList();
        this.AddScoringTypes();
        this.SetAllFalse();

    }

    private void FixShaders(){
        // Debug.Log("Trying to add materials");
        Shader standardShader = Shader.Find("Standard");

        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        int n = allObjects.Length;
        Debug.Log("Number of elements:" + n.ToString());
        int numIgnored = 0;
        int numEdited = 0;

        foreach(GameObject obj in allObjects){
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null){
                numIgnored++;
                continue;
            }
            if (renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader"){
                renderer.sharedMaterial.shader = standardShader;
                numEdited++;
            }
        }

        Debug.Log("Num Ignored: " + numIgnored.ToString());
        Debug.Log("Num Edited: " + numEdited.ToString());
    }

    private void FixCamera(){

        this.CheckScript();
        Camera mainCam = this.cameraPositionFinder.cam;

        if (mainCam == null){

            Camera[] allCams = UnityEngine.Object.FindObjectsOfType<Camera>();
            if (allCams.Length == 0){
                this.SetAllFalse();
                throw new System.Exception("No main camera could be found. Please set the camera of interest in the 'CameraPostitionFinder' script manually.");
            }
            mainCam = allCams[0];
            if (allCams.Length != 1){
                bool foundCam = false;
                foreach(Camera cam in allCams){
                    if (cam.name == "Main Camera"){
                        mainCam = cam;
                        foundCam = true;
                        break;
                    }
                }
                if (!foundCam){
                    this.SetAllFalse();
                    throw new System.Exception("No main camera could be found. Please set the camera of interest in the 'CameraPostitionFinder' script manually.");
                }
            }
            this.cameraPositionFinder.cam = mainCam;
        }
        mainCam.nearClipPlane = 0.03f;
        this.movementScript.cam = mainCam;
    }

    private void AddScripts(){
        CameraPositionFinder posFinderScript = this.gameObject.GetComponent<CameraPositionFinder>();
        if (posFinderScript == null){
            posFinderScript = this.gameObject.AddComponent<CameraPositionFinder>();
        }
        this.cameraPositionFinder = posFinderScript;
        SceneControls sceneControls = this.gameObject.GetComponent<SceneControls>();
        if (sceneControls == null){
            sceneControls = this.gameObject.AddComponent<SceneControls>();
        }
        this.sceneControls = sceneControls;
        MovementScript moveScript  = this.gameObject.GetComponent<MovementScript>();
        if (moveScript == null){
            moveScript = this.gameObject.AddComponent<MovementScript>();
        }
        this.movementScript = moveScript;

    }

    // TODO: Clean up this method.
    private void SetupPartsList(){
        Debug.Log("Add parts");

        this.CheckScript();

        if(this.cameraPositionFinder.parts != null && this.cameraPositionFinder.parts.Count != 0){
            this.SetAllFalse();
            Debug.LogWarning("CameraPositionsFinder already contains parts in 'parts' variable. Please delete these if you want to automatically add parts.");
        } 
        else {

            this.cameraPositionFinder.parts = new List<GameObject>();

            GameObject listParent = GameObject.Find("PartsList");
            if (listParent == null){
                GameObject[] objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                List<GameObject> potentialPartsLists = new List<GameObject>();
                foreach (GameObject obj in objects){
                    if (obj.transform.parent != null) {
                        continue;
                    }
                    if (obj.name.Contains("parts")){
                        potentialPartsLists.Add(obj);
                    }
                }
                if (potentialPartsLists.Count == 0){
                    this.SetAllFalse();
                    throw new System.Exception("No parts list found. Please add all parts to a top level empty GameObject named 'PartsList'");
                }
                if(potentialPartsLists.Count != 1){
                    this.SetAllFalse();
                    throw new System.Exception("Could not automatically find GameObject containing parts. Please add all parts to a top level empty GameObject named 'PartsList'");
                }
                listParent = potentialPartsLists[0];
            }
            int numParts = listParent.transform.childCount;
            for(int i = 0; i < numParts; i++){
                GameObject obj = listParent.transform.GetChild(i).gameObject;
                // Debug.Log(obj.name);
                this.cameraPositionFinder.parts.Add(obj);
            }

            Debug.Log("Parts added: " + this.cameraPositionFinder.parts.Count.ToString());
        }

        if(this.cameraPositionFinder.initParts != null && this.cameraPositionFinder.initParts.Count != 0){
            this.SetAllFalse();
            Debug.LogWarning("CameraPositionsFinder already contains parts in 'initParts' variable. Please delete these if you want to automatically add parts.");
        }
        else{
            this.cameraPositionFinder.initParts = new List<GameObject>();

            GameObject listParent = GameObject.Find("StartConfiguration");
            if (listParent == null){
                this.SetAllFalse();
                throw new System.Exception("No StartConfiguration found.");
            }
            this.cameraPositionFinder.initParts.Add(listParent);
        }

    }

    private void AddScoringTypes(){
        this.CheckScript();
        CameraPositionFinder camPosScript = this.cameraPositionFinder;

        List<ScoreType> types = camPosScript.scoreTypes;
        List<float> weights = camPosScript.scoreWeights;
        if (types == null){
            types = new List<ScoreType>();
            camPosScript.scoreTypes = types;
        }
        if (weights == null){
            weights = new List<float>();
            camPosScript.scoreWeights = weights;
        }
        if(types.Count != 0 || weights.Count != 0){
            this.SetAllFalse();
            Debug.LogWarning("CameraPositionsFinder already contains scoring information in 'scoreTypes' or ''scoreWeights' variables. Please delete these if you want to automatically add (This exception is thrown to ensure not deleting manual setup)");
            return;
        }
        types.Add(ScoreType.Angle);
        weights.Add(1f);

        types.Add(ScoreType.DistanceToObject);
        weights.Add(1f);

        types.Add(ScoreType.HumanSensibleAngles);
        weights.Add(1f);

        types.Add(ScoreType.LastPositionDistance);
        weights.Add(1f);

        types.Add(ScoreType.Spread);
        weights.Add(1f);

        types.Add(ScoreType.Stability);
        weights.Add(1f);

        types.Add(ScoreType.Visibility);
        weights.Add(1f);


        // for(int i = 0; i < types.Count; i++){
        //     weights.Add(1f);
        // }


    }

    // private void AddColliders(){
    //     this.CheckScript();
    // }

    private void CheckScript(){
        if(this.cameraPositionFinder == null){
            this.SetAllFalse();
            throw new System.Exception("CameraPositionFinder script could not be found. Please add it to scene and assign it to this script");
        }
        if(this.sceneControls == null){
            this.SetAllFalse();
            throw new System.Exception("SceneControls script could not be found. Please add it to scene and assign it to this script");
        }
        if(this.movementScript == null){
            this.SetAllFalse();
            throw new System.Exception("MovementScript script could not be found. Please add it to scene and assign it to this script");
        }
        return;

    }


    private CameraPositionFinder FindCameraPositionFinder(){ // Assumes only one CameraPositionFinder in scene

        GameObject scriptObject = GameObject.Find("ScriptObject");
        CameraPositionFinder cameraPositionFinder = null;
        if (scriptObject != null) {
            cameraPositionFinder = scriptObject.GetComponent<CameraPositionFinder>();
        }
        if (cameraPositionFinder == null){
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach(GameObject obj in allObjects){
                CameraPositionFinder foundCameraPositionFinder = obj.GetComponent<CameraPositionFinder>();
                if (foundCameraPositionFinder != null){
                    cameraPositionFinder = foundCameraPositionFinder;
                    break;
                }
            }
        }
        
        // print(scriptObject);

        return cameraPositionFinder;
    }


}

