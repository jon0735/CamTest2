using UnityEngine;

/// <summary>
/// Scoring strategy that scores based on user defined undesireable angles.
/// </summary>
public class HumanSensibleAnglesScore : ScoreComputer {

    public HumanSensibleAnglesScore(float weight) : base(weight){}

    public override float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex)
    {
        float reasonableAngleScore = 0f;
        Vector3 camVector = data.objectPos - position;
        for(int j = 0; j < data.humanUnreasonableAngles.Count; j++){
            float angle = Vector3.Angle(camVector, data.humanUnreasonableAngles[j].Item1);
            if (angle < data.humanUnreasonableAngles[j].Item3){
                float score = (angle - data.humanUnreasonableAngles[j].Item2) / data.humanUnreasonableAngles[j].Item3;

                reasonableAngleScore = Mathf.Max(reasonableAngleScore, score);
            }
        }
        return reasonableAngleScore;
    }

    public override bool NeedsNormalization(){
        return false;
    }

}