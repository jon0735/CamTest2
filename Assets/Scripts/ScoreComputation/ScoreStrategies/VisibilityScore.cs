using UnityEngine;

/// <summary>
/// Scoring Strategy that prefers positions from which a larger part of the object is visible.
/// For efficiency reasons these computations/raycasts are done in a CameraPositionFinder.PrunePositions() 
/// and the found values are simply passed to this class via the data parameter.
/// </summary>

public class VisibilityScore: ScoreComputer {

    public VisibilityScore(float weight) : base(weight){}

    public override float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex) {
        return data.collisions[directionIndex];
    }

    public override bool NeedsNormalization(){
        return true;
    }
}