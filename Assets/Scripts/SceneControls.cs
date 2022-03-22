using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneControls : MonoBehaviour
{

    [SerializeField]
    private bool showScores = false;

    [SerializeField]
    private CameraPositionFinder posFinder = null;


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
            GUI.TextField(new Rect(10, 10, 200, 200), this.posFinder.GetScoreText());
        }
    }

}
