using System;
using _1.Scripts.Manager.Core;
using _1.Scripts.Quests.Data;

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