using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public abstract class BOSS_DynamicRoomObject : MonoBehaviour
{
    /// <summary>
    /// True means open, if it's a door
    /// </summary>
    /// <param name="activation"></param>
    public abstract void ChangeActivationState(bool activation);
}
