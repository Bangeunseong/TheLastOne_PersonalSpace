using System;
using System.Collections.Generic;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Manager.Core;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Common
{
    public class DummyWeaponPart : MonoBehaviour, IInteractable
    {
        [field: Header("Components")]
        [field: SerializeField] public List<MeshRenderer> Renderers { get; private set; } = new();
        
        [field: Header("Weapon Part Settings")]
        [field: SerializeField] public WeaponType Type { get; private set; }
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public int InstanceId { get; private set; }

        public event Action OnPicked;
        
        public void Initialize(WeaponType type, int id, int instanceId)
        {
            Type = type;
            Id = id;
            InstanceId = instanceId;
        }

        private void Awake()
        {
            if (Renderers.Count <= 0) Renderers.AddRange(GetComponentsInChildren<MeshRenderer>());
        }

        private void Reset()
        {
            if (Renderers.Count <= 0) Renderers.AddRange(GetComponentsInChildren<MeshRenderer>());
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
        
        private void RemoveSelfFromSpawnedList()
        {
            CoreManager.Instance.spawnManager.RemovePropFromSpawnedList(gameObject);
        }

        public void ChangeLayerOfBody(bool isTransparent)
        {
            foreach (var render in Renderers)
            {
                render.gameObject.layer = isTransparent ? LayerMask.NameToLayer("Stencil_Key") : LayerMask.NameToLayer("Default");
            }
        }
        
        public void OnInteract(GameObject ownerObj)
        {
            if (!ownerObj.TryGetComponent(out Player player)) return;
            if (!player.PlayerWeapon.Weapons.TryGetValue(Type, out var value)) return;
            value.TryCollectWeaponPart(Id);
            
            OnPicked?.Invoke();
            CoreManager.Instance.spawnManager.DynamicSpawnedParts.Remove(InstanceId);
            CoreManager.Instance.objectPoolManager.Release(gameObject);
        }

        public void OnCancelInteract() { }
    }
}
