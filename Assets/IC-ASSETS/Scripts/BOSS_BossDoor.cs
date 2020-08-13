using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BOSS_BossDoor : BOSS_DynamicRoomObject
{
    const string _openAnParam = "open", _closeAnParam = "close";
    public string doorName = "ActualDoor";
    Animator _an;

    void Start()
    {
        _an = GetComponent<Animator>();
    }

    public override void ChangeActivationState(bool activation)
    {
        var param = activation ? _openAnParam : _closeAnParam;
        _an.SetTrigger(param);
    }
}
