using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Manager.Core;
using _1.Scripts.Quests.Core;
using _1.Scripts.Util;
using UnityEngine;

namespace _1.Scripts.Map.GameEvents
{
    public class SavePoint : MonoBehaviour, IGameEventListener
    {
        [field: Header("Save Point Id")]
        [field: Tooltip("It should be same with corresponding Spawn Trigger Id")]
        [field: SerializeField] public int Id { get; private set; }

        private void OnEnable()
        {
            GameEventSystem.Instance.RegisterListener(this);
            Service.Log($"Registered Save Point: { BaseEventIndex.BaseSavePointIndex + Id }");
        }

        private void Start()
        {
            var save = CoreManager.Instance.gameManager.SaveData;
            if (save == null || !save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info)) return;
            if (info.completionDict.TryGetValue(BaseEventIndex.BaseSavePointIndex + Id, out var value) && value) enabled = false;
        }

        private void OnDisable()
        {
            GameEventSystem.Instance.UnregisterListener(this);
            Service.Log($"Unregistered Save Point: {BaseEventIndex.BaseSavePointIndex + Id}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (!other.TryGetComponent(out Player player)) return;
            
            player.PlayerCondition.UpdateLastSavedTransform();
            
            GameEventSystem.Instance.RaiseEvent(BaseEventIndex.BaseSavePointIndex + Id);
        }

        public void OnEventRaised(int eventID)
        {
            if (eventID != BaseEventIndex.BaseSavePointIndex + Id) return;
            
            var save = CoreManager.Instance.gameManager.SaveData;
            if (save == null ||
                !save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info))
            {
                throw new MissingReferenceException("Save file not found!");
            }

            if (!info.completionDict.TryAdd(BaseEventIndex.BaseSavePointIndex + Id, true))
                info.completionDict[BaseEventIndex.BaseSavePointIndex + Id] = true;
            
            CoreManager.Instance.SaveData_QueuedAsync();
            enabled = false;
        }
    }
}
