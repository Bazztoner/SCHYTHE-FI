using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BOSS_LoweringWall : BOSS_DynamicRoomObject
{
    public string activateParam = "On";
    Animator _an;

    void Start()
    {
        _an = GetComponent<Animator>();
    }

    public override void ChangeActivationState(bool activation)
    {
        _an.SetTrigger(activateParam);
    }
}
