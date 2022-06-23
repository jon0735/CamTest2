using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Scoring strategy that scores based on the angle between the cameras forward vector and the vector between the camera and the centre of the scene
/// </summary>
public class AngleScore : ScoreComputer {

    public AngleScore(float weight): base(weight) {}


    // public float ComputeScore(CameraPositionFinder posFinder, DataForScoreComputation data){
    override public float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex){
        
        // float score = Vector3.Angle(data.objectPos - position, data.sceneCentre - position) / 180f; // TODO: Fix hardcoding
        float angle = Vector3.Angle(data.objectPos - position, data.sceneCentre - position);
        float score = angle > 20f ? (angle - 20) / 180f : 0f;
        // float anglePis = angle / 180f;
        // Debug.Log(score.ToString() + " " +  angle + " ");

        return score;

    }

    public override bool NeedsNormalization(){
        return true;
    }



}