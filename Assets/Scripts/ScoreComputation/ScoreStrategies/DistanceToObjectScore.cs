using UnityEngine;

/// <summary>
/// Scoring strategy that scores based the adjsuted distance to the object. Better score means that the camera position is closer to what the 
/// CameraPositionFinder.AdjustDistance() method considers optimal distance. 
/// For efficiency reasons, the score is computed in CameraPositionFinder.AdjustDistance() and simply stored in the data variable for this strategy,
/// </summary>
public class DistanceToObjectScore : ScoreComputer{

    public DistanceToObjectScore(float weight) : base(weight) {}

    public override float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex)
    {
        return data.distanceToObjectScore[directionIndex];
    }
}