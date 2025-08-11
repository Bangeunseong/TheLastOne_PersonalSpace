using System;
using System.Collections;
using System.Collections.Generic;
using _1.Scripts.Manager.Core;
using _1.Scripts.Quests.Core;
using UnityEngine;

namespace _1.Scripts.Map.ClearZone
{
    public class ClearStage : MonoBehaviour
    {
        [SerializeField] private int targetID = 100;
        private int completedQuests = 0;
        private bool alreadyRaised = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (alreadyRaised) return;
            
            if (other.CompareTag("Player"))
            {
                Quest mainQuest = CoreManager.Instance.questManager.activeQuests[0];
                foreach (ObjectiveProgress obj in mainQuest.Objectives.Values)
                {
                    if (obj.IsCompleted) completedQuests++;
                }

                if (completedQuests == targetID)
                {
                    Service.Log("퀘스트 다끝남");
                    GameEventSystem.Instance.RaiseEvent(targetID);
                    alreadyRaised = true;
                }
                else
                {
                    completedQuests = 0;
                }
            }
        }
    }
}
