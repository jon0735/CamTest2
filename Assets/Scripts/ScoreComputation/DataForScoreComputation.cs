using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// A class purely to pass needed data on to the scoring strategies in a consistent manner. 
/// If new scoring strategies are implemented, this class might need updating to include needed data in this.
/// </summary>
public class DataForScoreComputation{

    public Vector3 objectPos {get; private set;}

    public Vector3 sceneCentre {get; private set;}

    public Camera cam {get; private set;}
    public Camera dummyCam {get; private set;}

    public float maxDist {get; private set;}

    public int[] collisions {get; private set;}

    public float[] distanceToObjectScore {get; private set;}

    public Vector3[] vertexSamples {get; private set;}

    public List<(Vector3, float, float)> humanUnreasonableAngles {get; private set;}

    public DataForScoreComputation(Vector3 objectPos,
                                   Vector3 sceneCentre, 
                                   Camera cam,
                                   Camera dummyCam,
                                   float maxDist, 
                                   int[] collisions,
                                   float[] distanceToObjectScore,
                                   Vector3[] vertexSamples,
                                   List<(Vector3, float, float)> humanUnreasonableAngles)
    {
        this.objectPos = objectPos;
        this.sceneCentre = sceneCentre;
        this.cam = cam;
        this.dummyCam = dummyCam;
        this.maxDist = maxDist;
        this.collisions = collisions;
        this.distanceToObjectScore = distanceToObjectScore;
        this.vertexSamples = vertexSamples;
        this.humanUnreasonableAngles = humanUnreasonableAngles;

    }



}