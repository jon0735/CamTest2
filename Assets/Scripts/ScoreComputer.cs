

public interface ScoreComputer{

    public float ComputeScore(CameraPositionFinder posFinder, DataForScoreContainer data);

    public string GetScoreName();

}