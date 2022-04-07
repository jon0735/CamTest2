

    // private (float[], Vector3[]) ComputePosScoresOLD(Vector3[] positionsArray, int[] collisions){

    //     Vector3 objectPos = this.objectCentres[currentPart];
    //     Vector3[] vertices = this.vertexSamples[currentPart];
    //     int prunedPosCount = positionsArray.Length;

    //     float[] distScores = new float[prunedPosCount];
    //     float[] angleScores = new float[prunedPosCount];
    //     float[] spreadScores = new float[prunedPosCount];
    //     float[] visibilityScores = new float[prunedPosCount];
    //     float[] distToObjScores = new float[prunedPosCount];
    //     float[] reasonableAngleScores = new float[prunedPosCount];
    //     float[] stabilityScores = new float[prunedPosCount];
    //     // TODO?: Add something to represent there being space around the found area

    //     Vector3[] newPositionsArray = new Vector3[prunedPosCount];
    //     Vector3 centre = this.sceneCentre; 
    //     float[] fullScores = new float[prunedPosCount];

    //     // TODO: Fix all hardcoding

    //     for(int i = 0; i < prunedPosCount; i++){

    //         angleScores[i] = Vector3.Angle(objectPos - positionsArray[i], centre - positionsArray[i]) / 180f;
    //         spreadScores[i] = ComputeSpreadScore(positionsArray[i], objectPos);
    //         visibilityScores[i] = (float) collisions[i];
    //         (newPositionsArray[i] , distToObjScores[i]) = AdjustDistance(positionsArray[i], objectPos, vertices, this.individualMinDists[currentPart]);
    //         distScores[i] = Vector3.Distance(positionsArray[i], this.cam.transform.position) / (2f * this.maxDist);
    //         reasonableAngleScores[i] = ComputeHumanAngleScore(positionsArray[i], objectPos);
    //         stabilityScores[i] = ComputeStabilityScore(newPositionsArray[i], objectPos);
    //         // Debug.Log("StabilityScore: " + stabilityScores[i].ToString());
    //     }

    //     Util.NormalizeArray(distScores);
    //     Util.NormalizeArray(angleScores);
    //     Util.NormalizeArray(spreadScores);
    //     Util.NormalizeArray(visibilityScores);
    //     Util.NormalizeArray(distToObjScores);
    //     // this.normalizeArray(reasonableAngleScores); // Already normalised
    //     // this.normalizeArray(stabilityScores); // Already normalised

    //     for(int i = 0; i < spreadScores.Length; i++){
    //         fullScores[i] = distScores[i] * this.distWeight + 
    //                         angleScores[i] * this.angleWeight + 
    //                         spreadScores[i] * this.spreadWeight +
    //                         visibilityScores[i] * this.visibilityWeight +
    //                         distToObjScores[i] * this.distToObjWeight +
    //                         reasonableAngleScores[i] * this.reasonAngleWeight + 
    //                         stabilityScores[i] * this.stabilityWeight;
    //     }

    //     Dictionary<string, float[]> scoreDict = new Dictionary<string, float[]>();
    //     scoreDict.Add("distScores", distScores);
    //     scoreDict.Add("angleScores", angleScores);
    //     scoreDict.Add("spreadScores", spreadScores);
    //     scoreDict.Add("visibilityScores", visibilityScores);
    //     scoreDict.Add("distToObjScores", distToObjScores);
    //     scoreDict.Add("reasonableAngleScores", reasonableAngleScores);
    //     scoreDict.Add("stabilityScores", stabilityScores);
    //     scoreDict.Add("fullScores", fullScores);
    //     // this.lastScores = scoreDict;

    //     return (fullScores, newPositionsArray);
    // }

    // private float ComputeStabilityScore(Vector3 position, Vector3 objectPos){
    //     Transform t = this.dummyCam.transform;
    //     t.position = position;
    //     t.LookAt(objectPos);

    //     float dist = (position - objectPos).magnitude;
    //     float desiredAngle = 8f * 3.1415f / 180f; // TODO: Fix hardcoding?
    //     float distToStabilityPoints = Mathf.Sin(desiredAngle) * dist / Mathf.Sin(3.1415f/2 - desiredAngle); 

    //     Vector3 up = t.up * distToStabilityPoints;
    //     Vector3 right = t.right * distToStabilityPoints;

    //     // if (this.visualDebug || true){
    //     //     Debug.Log("up: " + up.ToString());
    //     //     Debug.Log("right: " + right.ToString());
    //     //     Debug.Log("distToStabilityPoints: " + distToStabilityPoints.ToString());
    //     //     Debug.Log("DesiredAngle: " + desiredAngle.ToString());
    //     // }

    //     Vector3 newPos;
    //     float hits = 0f;
    //     int points = 5;
    //     dist = dist * .90f; // To prevent hit with self (Am I sure this will work more generally? (probably not))
    //     for(int i = 0; i < points; i++){
    //         for(int j = 0; j < points; j++){
    //             if (i == (points-1)/2 && j == (points-1)/2){
    //                 continue;
    //             }
    //             newPos = position + (i-(points-1)/2) * up + (j-(points-1)/2) * right;
    //             RaycastHit hit;
    //             if(Physics.Raycast(newPos, objectPos - newPos, out hit, dist)){
    //                 hits++;
    //                 // createSphere(newPos, UnityEngine.Color.green);
    //                 // Vector3 endpoint = newPos + (objectPos - newPos).normalized * hit.distance;
    //                 // drawLine(newPos, endpoint, UnityEngine.Color.green);
    //             }

    //             // if(this.showStuffTestingBool){
    //             //     Util.CreateSphere(newPos, UnityEngine.Color.cyan, scale: 0.01f);
    //             // }
    //         }
    //     }
    //     // this.showStuffTestingBool = false;

    //     return hits/(points * points - 1);
    // }


    // private float ComputeSpreadScore(Vector3 position, Vector3 objectPos){
        
    //     float avgSpred = 0f;
    //     Vector3 camToObj = objectPos - position;
    //     float dist = camToObj.magnitude;
    //     Vector3 scaledCamPos = position + (camToObj * (1 - 1/dist)); // Distance to object will interfere with avg angles. Use this scaled camPos to avoid that.
    //     for(int j = 0; j < this.vertexSamples[this.currentPart].Length; j++){ 
    //         Vector3 test = position;

    //         avgSpred += Vector3.Angle(objectPos - scaledCamPos, this.vertexSamples[this.currentPart][j] - scaledCamPos);
    //     }
    //     avgSpred = avgSpred / this.vertexSamples[this.currentPart].Length;
    //     return (1f - avgSpred / 60f);
    // }

    // private float ComputeHumanAngleScore(Vector3 camPosition, Vector3 objPosition){
    //     float reasonableAngleScore = 0f;
    //     Vector3 camVector = objPosition - camPosition;
    //     for(int j = 0; j < this.humanUnreasonableAngles.Count; j++){
    //         float angle = Vector3.Angle(camVector, this.humanUnreasonableAngles[j].Item1);
    //         if (angle < this.humanUnreasonableAngles[j].Item3){
    //             float score = (angle - this.humanUnreasonableAngles[j].Item2) / this.humanUnreasonableAngles[j].Item3;
    //             // Debug.Log("CamPos: " + camPosition.ToString() + 
    //             //           "\nobjPos: " + objPosition.ToString() +
    //             //           "\ncamVector: " + camVector.ToString() + 
    //             //           "\nBad Direction:  " + this.humanUnreasonableAngles[j].Item1.ToString() +
    //             //           "\nAngle: " + angle.ToString() + 
    //             //           "\nscore: " + score.ToString());
    //             // Debug.Log("score: " + score.ToString());
    //             reasonableAngleScore = Mathf.Max(reasonableAngleScore, score);
    //         }
    //     }
    //     return reasonableAngleScore;
    // }

    // public string BuildScoreStringOLD(Dictionary<string, float[]> scores, int index){

    //     string text = "";
    //     foreach(var keyValPair in scores){
    //         text += keyValPair.Key + ": " + keyValPair.Value[index].ToString() + "\n";
    //     }
    //     text +=  "Object Pos: " + this.objectCentres[currentPart].ToString() + "\n";
    //     text +=  "Cam Pos: " + this.lastCamPositions[index].ToString() + "\n";
    //     text +=  "Cam Vector: " + (this.objectCentres[currentPart] - this.lastCamPositions[index] ).ToString() + "\n";
    //     text += "FIX: NOT CONSIDERING SORTING DONE AFTERWARDS -> This is all wrong";

    //     return text;
    // }