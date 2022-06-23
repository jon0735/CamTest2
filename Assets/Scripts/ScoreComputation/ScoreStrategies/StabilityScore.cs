using UnityEngine;

/// <summary>
/// Scoring strategy that examines if the object is still visible if position is slightly moved. 
/// I.e. it prefers positions that can be slightly changes without destroyng the quality of the position.
/// Uses raycasts for this.
/// </summary>
public class StabilityScore: ScoreComputer {

    public StabilityScore(float weight) : base(weight){}

    public override float ComputeScore(Vector3 position, DataForScoreComputation data, int directionIndex) {
        Transform t = data.dummyCam.transform;
        t.position = position;
        t.LookAt(data.objectPos);

        float dist = (position - data.objectPos).magnitude;
        float desiredAngle = 8f * 3.1415f / 180f; // TODO: Fix hardcoding?
        float distToStabilityPoints = Mathf.Sin(desiredAngle) * dist / Mathf.Sin(3.1415f/2 - desiredAngle); 

        Vector3 up = t.up * distToStabilityPoints;
        Vector3 right = t.right * distToStabilityPoints;

        Vector3 newPos;
        float hits = 0f;
        int points = 5;
        dist = dist * .90f; // To prevent hit with self (Am I sure this will work more generally? (probably not)) TODO: Check and fix this. 
        for(int i = 0; i < points; i++){
            for(int j = 0; j < points; j++){
                if (i == (points-1)/2 && j == (points-1)/2){ // middel point, i.e. the one that has to hit for a point to be considered valid
                    continue;
                }
                newPos = position + (i-(points-1)/2) * up + (j-(points-1)/2) * right;
                RaycastHit hit;
                if(Physics.Raycast(newPos, data.objectPos - newPos, out hit, dist)){
                    hits++;
                }

            }
        }

        return hits/(points * points - 1);
    }

    public override bool NeedsNormalization(){
        return false;
    }
}

// TODO: Consider changing stability s.t. if a point closer to the centre is blocked, it is considered worse than if a point at the edge is blocked.