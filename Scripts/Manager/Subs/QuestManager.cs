using System;
using System.Linq;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Quests.Core;
using _1.Scripts.Quests.Data;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Object = UnityEngine.Object;
using Console = _1.Scripts.Map.Console.Console;

namespace _1.Scripts.Manager.Subs
{
    [Serializable] public class QuestManager
    {
        public SerializedDictionary<int, Quest> activeQuests = new();
        
        private CoreManager coreManager;
        
        public void Start()
        {
            coreManager = CoreManager.Instance;
        }
        
        public void Initialize(DataTransferObject dto)
        {
            var consoles = Object
                .FindObjectsByType(typeof(Console), FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(val => val as Console).ToArray();
            
            if (dto == null)
            {
                var mainQuest = new Quest
                {
                    data = coreManager.resourceManager.GetAsset<QuestData>("MainQuest")
                };
                mainQuest.Initialize();
                mainQuest.StartQuest();
                activeQuests.TryAdd(mainQuest.data.questID, mainQuest);
            }
            else
            {
                var quests = dto.Quests;
                foreach (var quest in quests)
                {
                    var val = new Quest { data = coreManager.resourceManager.GetAsset<QuestData>(quest.Key == 0 ? "MainQuest" : $"SubQuest_{quest.Key}") };
                    val.Initialize();
                    val.ResumeQuest(dto.Quests[quest.Key], consoles);
                    activeQuests.TryAdd(val.data.questID, val);
                }
            }
            Debug.Log($"퀘스트 초기화 완료: {activeQuests.Count}개 등록됨");
            foreach(var q in activeQuests)
                Debug.Log($"퀘스트ID: {q.Key} / {q.Value.data.questID}");
        }

        public void Reset()
        {
            foreach (var objective in activeQuests.SelectMany(quest => quest.Value.Objectives))
                objective.Value.RemoveIncludedEvents();
            activeQuests.Clear();
        }

        public void UpdateProgress(int questId, int objectiveId)
        {
            if (!activeQuests.TryGetValue(questId, out var quest)) return;
            if (quest.isCompleted) return;
            
            quest.UpdateObjectiveProgress(objectiveId);
        }
    }
}