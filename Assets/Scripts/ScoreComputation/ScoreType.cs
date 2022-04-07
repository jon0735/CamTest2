
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


/// <summary>
/// Enums corresponding to types of scores. Useful for allowing score types to be defined in unity editor.
/// </summary>

[Serializable]
public enum ScoreType{
    LastPositionDistance,
    Angle,
    Spread,
    Visibility,
    DistanceToObject,
    HumanSensibleAngles,
    Stability

}

