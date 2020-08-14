using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BOSS_ScriptedEvents : MonoBehaviour
{
    public BOSS_RoboPhase1 bossPhase1Prefab;
    public BOSS_RoboPhase2 bossPhase2Prefab;

    BOSS_RoboPhase1 _bossPhase1;
    BOSS_RoboPhase2 _bossPhase2;

    public BOSS_DynamicRoomObject[] firstSetOfDoors;
    public BOSS_DynamicRoomObject[] secondSetOfDoors;
    public BOSS_DynamicRoomObject finalDoor;
    public BOSS_DynamicRoomObject[] thirdSetOfDoors;

    public Collider[] eventTriggers;
    byte _triggerIndex = 0;
    public Transform bossPhase1Spawner;
    public Transform bossPhase2Spawner;

    public void WinConditionTriggerBehaviour()
    {
        FindObjectOfType<GameFlowManager>().EndGame(true);
    }

    IEnumerator Phase1BossHandler(byte triggerIndex)
    {
        eventTriggers[triggerIndex].gameObject.SetActive(false);

        FindObjectOfType<GameFlowManager>().PlayLevelMusic(true);

        //do fucking something
        _bossPhase1 = GameObject.Instantiate(bossPhase1Prefab, bossPhase1Spawner.position, Quaternion.identity);
        _bossPhase1.transform.forward = bossPhase1Spawner.forward;

        var controllerComponent = _bossPhase1.GetComponent<EnemyController>();
        var bossPath = FindObjectOfType<BOSS_PatrolPath>();

        controllerComponent.patrolPath = bossPath;
        bossPath.AddEnemy(controllerComponent);

        yield return new WaitForSeconds(.2f);

        foreach (var item in firstSetOfDoors)
        {
            item.ChangeActivationState(true);
        }

        yield return new WaitForSeconds(.5f);

        while (true)
        {
            if (_bossPhase1.isDead)
            {
                //do shit
                eventTriggers[triggerIndex + 1].gameObject.SetActive(true);

                foreach (var item in secondSetOfDoors)
                {
                    item.ChangeActivationState(true);
                }

                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator Phase2BossHandler(byte triggerIndex)
    {
        eventTriggers[triggerIndex].gameObject.SetActive(false);
        if (_bossPhase1 != null) Destroy(_bossPhase1.gameObject);

        finalDoor.GetComponentInChildren<Light>().color = new Color(0.8f, 0, 0);
        finalDoor.ChangeActivationState(false);

        //do fucking something
        _bossPhase2 = GameObject.Instantiate(bossPhase2Prefab, bossPhase2Spawner.position, Quaternion.identity);
        _bossPhase2.transform.forward = bossPhase2Spawner.forward;

        yield return new WaitForEndOfFrame();

        while (true)
        {
            if (_bossPhase2.isDead)
            {
                //do shit
                eventTriggers[triggerIndex + 1].gameObject.SetActive(true);
                foreach (var item in thirdSetOfDoors)
                {
                    finalDoor.GetComponentInChildren<Light>().color = new Color(0.6494749f, 0.990566f, 0.6534527f);
                    item.ChangeActivationState(true);
                }
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        var coll = other.GetComponent<PlayerCharacterController>();

        if (coll != null)
        {
            switch (_triggerIndex)
            {
                case 0:
                    StartCoroutine(Phase1BossHandler(_triggerIndex));
                    break;
                case 1:
                    StartCoroutine(Phase2BossHandler(_triggerIndex));
                    break;
                case 2:
                    WinConditionTriggerBehaviour();
                    break;
                default:
                    print("Sampaoli la re concha de tu madre");
                    break;
            }
            _triggerIndex++;
        }
    }
}
