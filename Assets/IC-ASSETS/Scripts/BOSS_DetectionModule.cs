using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BOSS_DetectionModule : DetectionModule
{
    PlayerCharacterController player; 

    void Awake()
    {
        player = GameObject.FindObjectOfType<PlayerCharacterController>();
        knownDetectedTarget = player.gameObject;
    }
    void Update()
    {
        knownDetectedTarget = player.gameObject;
    }
    void LateUpdate()
    {
        knownDetectedTarget = player.gameObject;
    }
}
