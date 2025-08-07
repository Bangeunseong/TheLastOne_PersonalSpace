using System.Collections.Generic;
using UnityEngine;

namespace _1.Scripts.Quests.Data
{
    [CreateAssetMenu(fileName = "New QuestData", menuName = "ScriptableObjects/Quest/Create New QuestData")]
    public class QuestData : ScriptableObject
    {
        public int questID;
        public string title;
        public string description;
        public List<ObjectiveData> objectives;
        
    }
}