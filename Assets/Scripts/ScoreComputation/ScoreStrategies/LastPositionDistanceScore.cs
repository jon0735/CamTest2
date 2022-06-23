using UnityEngine;

/// <summary>
/// Scoring strategy that prefers camera positions closer to the current camera position.
/// </summary>
public class LastPositionDistanceScore: ScoreComputer {

    public LastPositionDistanceScore(float weight) : base(weight) {}

    override public float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex){
        return Vector3.Distance(position, data.cam.transform.position) / (2f * data.maxDist);
    }

    public override bool NeedsNormalization(){
        return true;
    }

}