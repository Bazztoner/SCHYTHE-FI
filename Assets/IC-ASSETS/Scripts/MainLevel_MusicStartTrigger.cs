using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MainLevel_MusicStartTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerCharacterController>() != null)
        {
            FindObjectOfType<GameFlowManager>().PlayLevelMusic(true);
            this.gameObject.SetActive(false);
        }
    }
}
