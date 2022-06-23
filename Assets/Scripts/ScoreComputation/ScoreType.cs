
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


/// <summary>
/// Enums corresponding to types of scores. Needed for allowing score types to be defined in unity editor. (TODO: Check if there is a better way of doing this)
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

