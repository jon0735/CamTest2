using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Abstract class to based score computation strategies on.
/// For a few implementations of this, the score computations might me more efficiently done 
/// in an earlier part of the algorithm. In this case the pre-computed scores are passed to the 
/// strategies in the data parameter of the ComputeScore method.
/// </summary>
public abstract class ScoreComputer{

    public float weight {get; private set;}
    public bool needsNormalization {get; private set;}

    /// <summary>
    /// Standard Constructor. 
    /// </summary>
    /// <param name="weight">Weight of the score</param>
    /// <param name="needsNormalization">boolean indicating if scores are natually normalised, or if they need to be normalised manually.</param>
    public ScoreComputer(float weight, bool needsNormalization=false){
        this.weight = weight;
        this.needsNormalization = needsNormalization;
    }

    /// <summary>
    /// Method for computation of a camera position score.
    /// </summary>
    /// <param name="position">Camera position to be scores</param>
    /// <param name="data">Class data container for all necessary date for all score computations. Might need updating if further score types are added.</param>
    /// <param name="directionIndex">The index of the camera position currently having its score computed. 
    /// Inelegant way of giving access to score values that are computed in steps previous to score computation.</param>
    /// <returns></returns>
    public abstract float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex);

    public string GetScoreName(){
        return this.GetType().Name;
    }

}