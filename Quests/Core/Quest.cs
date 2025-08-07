using System;
using System.Collections.Generic;
using System.Linq;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Quests.Data;
using _1.Scripts.UI.InGame.HUD;
using _1.Scripts.UI.InGame.Mission;
using UnityEngine.SceneManagement;
using Console = _1.Scripts.Map.Console.Console;

namespace _1.Scripts.Quests.Core
{
    [Serializable] public class Quest
    {
        public QuestData data;
        public int currentObjectiveIndex;
        public Dictionary<int, ObjectiveProgress> Objectives;
        public bool isCompleted;

        public void Initialize()
        {
            Objectives = new Dictionary<int, ObjectiveProgress>();
            foreach (var objective in data.objectives)
            {
                Objectives.Add(objective.targetID, new ObjectiveProgress{questId = data.questID, data = objective});

                switch (objective.type)
                {
                    case ObjectiveType.ClearStage1:
                        objective.onCompletedAction.AddListener(() => CoreManager.Instance.MoveToNextScene(SceneType.Stage2));
                        break;
                    case ObjectiveType.ClearStage2:
                        objective.onCompletedAction.AddListener(() => CoreManager.Instance.MoveToNextScene(SceneType.EndingScene));
                        break;
                }
            }
        }
        
        public void StartQuest()
        {
            currentObjectiveIndex = 0;
            foreach(var objective in Objectives) objective.Value.Activate();

            var currentObjective = Objectives[currentObjectiveIndex];
            QuestTargetBinder.Instance.SetCurrentTarget(currentObjective.data.targetID);
        }

        public void ResumeQuest(QuestInfo info, Console[] consoles)
        {
            currentObjectiveIndex = info.currentObjectiveIndex;
            foreach (var objective in info.progresses)
            {
                Objectives[objective.Key].currentAmount = objective.Value;
                if (Objectives[objective.Key].IsCompleted)
                {
                    foreach (var console in consoles)
                    {
                        Service.Log($"{console.Id}, {Objectives[objective.Key].data.targetID}");
                        if (console.Id == Objectives[objective.Key].data.targetID)
                        {
                            console.OpenDoors();
                        }
                    }
                    Objectives[objective.Key].Deactivate();
                    Objectives[objective.Key].RemoveIncludedEvents();
                }
                else Objectives[objective.Key].Activate();
            }
            
            if (info.completionList.All(val => val.Value))
            { 
                isCompleted = true;
                return;
            }
            
            var currentObjective = Objectives[currentObjectiveIndex];
            QuestTargetBinder.Instance.SetCurrentTarget(currentObjective.data.targetID);
        }

        public void UpdateObjectiveProgress(int objectiveId)
        {
            var objective = Objectives[objectiveId];
            CoreManager.Instance.uiManager.GetUI<QuestUI>()?.Refresh();
            
            if (!objective.IsCompleted) return;
            objective.Deactivate();
            
            if (objective.data.dialogueKey > 0)
                CoreManager.Instance.dialogueManager.TriggerDialogue(objective.data.dialogueKey);

            if (Objectives.Any(val => val.Value.IsActivated))
                currentObjectiveIndex = Objectives.FirstOrDefault(val => val.Value.IsActivated).Key;
            else currentObjectiveIndex = -1;
            
            
            CoreManager.Instance.gameManager.Player.PlayerCondition.UpdateLastSavedTransform();
            CoreManager.Instance.SaveData_QueuedAsync();
            
            if (currentObjectiveIndex != -1)
            {
                var currentObjective = Objectives[currentObjectiveIndex];
                QuestTargetBinder.Instance.SetCurrentTarget(currentObjective.data.targetID);
                CoreManager.Instance.uiManager.GetUI<QuestUI>()?.Refresh();
            } else 
            {
                Service.Log("Quest Completed!");
                isCompleted = true;
                CoreManager.Instance.uiManager.HideUI<DistanceUI>();
                CoreManager.Instance.uiManager.HideUI<QuestUI>();
                CoreManager.Instance.gameManager.Player.PlayerCondition.UpdateLastSavedTransform();
                CoreManager.Instance.SaveData_QueuedAsync();
            }
            
            objective.data.onCompletedAction?.Invoke(); // 오브젝트 끝났을 시 이벤트 실행
            objective.RemoveIncludedEvents();
        }
    }
}