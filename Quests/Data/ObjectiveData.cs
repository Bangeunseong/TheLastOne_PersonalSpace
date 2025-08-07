using System;
using UnityEngine;
using UnityEngine.Events;

namespace _1.Scripts.Quests.Data
{
    public enum ObjectiveType
    {
        Console,
        KillDrones,
        CollectItem,
        HackingDrone,
        ClearStage1,
        ClearStage2,
    }
    
    [Serializable] public class ObjectiveData
    {
        public int targetID;
        public string description;
        public ObjectiveType type;
        public int requiredAmount;
        public UnityEvent onCompletedAction;

        public int dialogueKey;
    }
} 