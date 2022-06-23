using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    public Camera cam;

    [SerializeField]
    private float speed = 1f;

    private Vector3 startLocation;
    private Vector3 endLocation;
    private Vector3 startLookAt;
    private Vector3 endLookAt;
    private float t = 0.0f;
    public bool isRunning {get; private set;} = false;

    private float tFactor = 1f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (this.isRunning && this.t < 1f){
            bool finished = this.Step();
            if (finished){
                this.isRunning = false;
            }
        }
    }

    public void StartAnimation(Vector3 startLocation, Vector3 endLocation, Vector3 startLookAt, Vector3 endLookAt){
        this.startLocation = new Vector3(startLocation.x, startLocation.y, startLocation.z) ;
        this.endLocation = new Vector3(endLocation.x, endLocation.y, endLocation.z);
        this.startLookAt = new Vector3(startLookAt.x, startLookAt.y, startLookAt.z);
        this.endLookAt = new Vector3(endLookAt.x, endLookAt.y, endLookAt.z);
        this.t = 0f;
        this.isRunning = true;

        float camDistance = (startLocation - endLocation).magnitude;
        float lookAtDistance = (startLookAt - endLookAt).magnitude;

        float distance = Mathf.Max(camDistance, lookAtDistance);

        distance = Mathf.Clamp(distance, 0.2f, 1.8f);

        this.tFactor = this.speed / distance;
    }

    private bool Step(){
        this.t += Time.deltaTime *tFactor;
        this.t = Mathf.Clamp(this.t, 0f, 1f);

        Vector3 pos = (1 - t)*this.startLocation + t*this.endLocation;
        Vector3 lookPos = (1 - t)*this.startLookAt + t*this.endLookAt;

        this.cam.transform.position = pos;
        this.cam.transform.LookAt(lookPos);


        return t >= 1f;
    }

}
