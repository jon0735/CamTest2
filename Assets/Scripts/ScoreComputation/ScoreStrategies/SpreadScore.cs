using UnityEngine;

/// <summary>
/// Scoring strategy that prefers positions in which the vertices of the desired object are spread out, 
/// hopefully meaning we are seeing the object from the "broadest" direction
/// </summary>
public class SpreadScore : ScoreComputer {

    public SpreadScore(float weight) : base(weight) {}

    //TODO: fix all this hard-coding
    public override float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex)
    {
        float avgSpred = 0f;
        Vector3 camToObj = data.objectPos - position;
        // Debug.Log(position.ToString() + ", " + data.objectPos.ToString());
        float dist = camToObj.magnitude;
        // Debug.Log(dist);
        Vector3 scaledCamPos = position + (camToObj * (1 - 1/dist)); // Distance to object will interfere with avg angles. Use this scaled camPos to avoid that.
        for(int j = 0; j < data.vertexSamples.Length; j++){ 
            Vector3 test = position;

            avgSpred += Vector3.Angle(data.objectPos - scaledCamPos, data.vertexSamples[j] - scaledCamPos);
        }
        avgSpred = avgSpred / data.vertexSamples.Length;

        // Debug.Log(data.vertexSamples.Length);

        return (1f - avgSpred / 60f); 
    }

    public override bool NeedsNormalization(){
        return true;
    }

}