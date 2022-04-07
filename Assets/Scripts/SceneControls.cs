using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneControls : MonoBehaviour
{

    [SerializeField]
    private bool showScores = false;

    [SerializeField]
    private CameraPositionFinder posFinder = null; // Camera pose finder script. If none is set in editor, script will search for one attached to the same GameObject as this script


    void Start()
    {
        if (this.posFinder is null){
            this.posFinder = this.gameObject.GetComponent<CameraPositionFinder>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)){
            this.posFinder.NextInstruction();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow)){
            this.posFinder.NextPos();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)){
            this.posFinder.PrevPos();
        }

        if (Input.GetKeyDown(KeyCode.F11)){
            this.showScores = !this.showScores;
        }
    }

    void OnGUI(){
        if (this.showScores){
            Debug.LogWarning("Score string not reliable due to being created from non-sorted scores. I.e. the wrong scores are shown for a given position");
            GUI.TextField(new Rect(10, 10, 200, 200), this.posFinder.scoreText); // This is still bugged due to sorting
        }
    }

}
