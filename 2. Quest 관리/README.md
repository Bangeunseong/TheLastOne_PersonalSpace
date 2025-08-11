# 🧐 Quest 구현 및 관리 방식

<br/>

# 📁 폴더 구조

```
Quests/                     # Quest 관련 코드 모음
├── Core/                   # Quest 및 Quest를 진행하는데 필요한 목표, 목표를 완료했을 때 이벤트를 처리할 Event System 코드 모음
└── Data/                   # 목표 및 퀘스트 S.O.
```

<br/>

# 💻 코드 샘플 및 주석

### 1. Quest 구현 및 로직
- Quest는 QuestData란 S.O. 데이터를 통해 Initialize 되며 현재 진행해야 하는 목표를 Index 값으로 저장하는 Parameter가 존재한다.
- Quest의 목표는 Dictionary 형태로 저장이 되며 위의 Index 값을 통해 찾을 수 있다.
- isCompleted 란 Boolean 값을 통해 퀘스트의 완료 여부를 확인할 수 있다.
- `StartQuest` 메소드 호출 시 퀘스트를 처음부터 시작, `ResumeQuest` 메소드 호출 시 `QuestInfo`를 기준으로 퀘스트를 시작. (`QuestInfo`는 [SaveFile](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Manager/Data/DataTransferObject.cs)을 통해 가져오는 정보)
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Quests/Core/Quest.cs)

```csharp
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
```

### 2. Objective Progress 선언 및 로직
- 목표 진행도는 `ObjectiveData`란 S.O.를 통해 수행해야 하는 목표 ID 값을 이용하여 `GameEventSystem`에 등록을 하여 `OnEventRaised` 메소드를 활용하여 목표 ID와 같은 값이 들어오면 진행도를 올린다.
- 선언될 때에는 `GameEventSystem` 에 Register를 하고 완료 시에는 Unregister를 하는 방식으로 목표를 관리한다.
- 완료된 목표는 SaveFile에 저장되어 게임 로딩 시 이미 완료된 목표는 등록이 안된다.
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Quests/Core/ObjectiveProgress.cs)
```csharp
namespace _1.Scripts.Quests.Core
{
    [Serializable] public class ObjectiveProgress : IGameEventListener
    {
        public int questId;
        public ObjectiveData data;
        public int currentAmount;
        public bool IsActivated;
        public bool IsCompleted => currentAmount >= data.requiredAmount;
        
        public void Activate()
        {
            IsActivated = true;
            GameEventSystem.Instance.RegisterListener(this);
        }

        public void Deactivate()
        {
            IsActivated = false;
            GameEventSystem.Instance.UnregisterListener(this);
        }

        public void RemoveIncludedEvents()
        {
            data.onCompletedAction.RemoveAllListeners();
        }

        /// <summary>
        /// GameEventSystem에 등록되었을 때 이것이 불리게 되어 갱신하고 진행도 저장하는 함수
        /// </summary>
        /// <param name="eventID"></param>
        public void OnEventRaised(int eventID)
        {
            if (IsCompleted) return;

            if (data.targetID == eventID)
            {
                currentAmount++;
                CoreManager.Instance.gameManager.Player.PlayerCondition.UpdateLastSavedTransform();
                Service.Log($"[Objective] {data.description} 진행도: {currentAmount}/{data.requiredAmount}");
                CoreManager.Instance.questManager.UpdateProgress(questId, eventID);
            }
        }
    }
}
```