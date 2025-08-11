using System;
using System.Collections.Generic;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Item.Common;
using _1.Scripts.Manager.Core;
using _1.Scripts.Quests.Core;
using _1.Scripts.UI.InGame;
using _1.Scripts.UI.InGame.HUD;
using UnityEngine;

namespace _1.Scripts.Item.Items
{
    public class DummyItem : MonoBehaviour, IInteractable
    {
        [field: Header("Dummy Item Settings")]
        [field: SerializeField] public bool IsStatic { get; private set; }
        [field: SerializeField] public int TargetId { get; private set; }
        [field: SerializeField] public int InstanceId { get; private set; }
        [field: SerializeField] public ItemType ItemType { get; private set; }
        [field: SerializeField] public List<Transform> Renderers { get; private set; }
        
        public event Action OnPicked;

        private void Awake()
        {
            if (Renderers.Count <= 0)
            {
                Renderers.AddRange(this.TryGetChildComponents<Transform>("Body"));
                Renderers.RemoveAt(0);
            }
        }

        private void Reset()
        {
            if (Renderers.Count <= 0)
            {
                Renderers.AddRange(this.TryGetChildComponents<Transform>("Body"));
                Renderers.RemoveAt(0);
            }
        }

        private void OnEnable()
        {
            ChangeLayerOfBody(CoreManager.Instance.spawnManager.IsVisible);
            OnPicked += RemoveSelfFromSpawnedList;
        }
        
        private void OnDisable()
        {
            ChangeLayerOfBody(false);
            OnPicked -= RemoveSelfFromSpawnedList;
        }
        
        public void ChangeLayerOfBody(bool isTransparent)
        {
            foreach(var render in Renderers)
                render.gameObject.layer = isTransparent ? LayerMask.NameToLayer("Stencil_Key") : LayerMask.NameToLayer("Default");
        }

        public void RemoveSelfFromSpawnedList()
        {
            if (IsStatic) CoreManager.Instance.spawnManager.RemovePropFromSpawnedList(gameObject);
        }
        
        public void Initialize(bool isStatic, int instanceId)
        {
            IsStatic = isStatic;
            InstanceId = instanceId;
        } 

        public void OnInteract(GameObject ownerObj)
        {
            if (!ownerObj.TryGetComponent(out Player player)) return;
            if (player.PlayerInventory.OnRefillItem(ItemType))
            {
                if (IsStatic)
                {
                    var save = CoreManager.Instance.gameManager.SaveData;
                    if (save is { stageInfos: not null } &&
                        save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info))
                        if(!info.completionDict.TryAdd(InstanceId, true)) info.completionDict[InstanceId] = true;
                }
                else
                {
                    CoreManager.Instance.spawnManager.DynamicSpawnedItems.Remove(InstanceId);
                }
                
                OnPicked?.Invoke();
                CoreManager.Instance.objectPoolManager.Release(gameObject);
            }
            else
            {
                // Service.Log($"Failed to refill {ItemType}");
                CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast("Failed to refill {ItemType}");
            }
            GameEventSystem.Instance.RaiseEvent(TargetId);
        }

        public void OnCancelInteract() { }
    }
}
