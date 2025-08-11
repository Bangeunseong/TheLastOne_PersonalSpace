using System;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Quests.Core;
using _1.Scripts.UI.Inventory;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Common
{
    public class DummyWeapon : MonoBehaviour, IInteractable
    {
        [field: Header("DummyGun Settings")]
        [field: SerializeField] public bool IsStatic { get; private set; }
        [field: SerializeField] public int TargetId { get; private set; }
        [field: SerializeField] public int InstanceId { get; private set; } = -1;
        [field: SerializeField] public WeaponType Type { get; private set; }
        [field: SerializeField] public Transform[] Renderers { get; private set; }
        
        public event Action OnPicked;

        private void Awake()
        {
            if (Renderers.Length > 0) return;
            Renderers = this.TryGetChildComponents<Transform>("Gun");
        }

        private void Reset()
        {
            Renderers = this.TryGetChildComponents<Transform>("Gun");
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
            foreach (var render in Renderers)
            {
                render.gameObject.layer = isTransparent ? LayerMask.NameToLayer("Stencil_Key") : LayerMask.NameToLayer("Default");
            }
        }

        private void RemoveSelfFromSpawnedList()
        {
            CoreManager.Instance.spawnManager.RemovePropFromSpawnedList(gameObject);
        }

        public void Initialize(bool isStatic, int instanceId)
        {
            IsStatic = isStatic;
            InstanceId = instanceId;
        } 

        public void OnInteract(GameObject ownerObj)
        {
            if (!ownerObj.TryGetComponent(out Player player)) return;
            if (!player.PlayerWeapon.AvailableWeapons.TryGetValue(Type, out var value)) return;
            
            if (!value)
            {
                if (Type != WeaponType.SniperRifle)
                {
                    if (player.PlayerWeapon.AvailableWeapons[WeaponType.SniperRifle] && Type == WeaponType.Pistol) return;
                    player.PlayerWeapon.AvailableWeapons[Type] = true;
                    player.PlayerCondition.OnSwitchWeapon(Type, 0.5f);
                }
                else
                {
                    player.PlayerWeapon.Weapons[Type].OnRefillAmmo(10);
                }
            }
            else
            {
                var result = player.PlayerWeapon.Weapons[Type].OnRefillAmmo(
                    Type switch
                    {
                        WeaponType.HackGun => 5,
                        WeaponType.GrenadeLauncher => 6,
                        WeaponType.Pistol => 30,
                        WeaponType.Rifle => 60,
                        WeaponType.SniperRifle => 10,
                        WeaponType.Punch => 0,
                        _ => throw new ArgumentOutOfRangeException()
                    });
                if (!result) return;
            }

            if (IsStatic)
            {
                var save = CoreManager.Instance.gameManager.SaveData;
                if (save is { stageInfos: not null } && save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info))
                    if(!info.completionDict.TryAdd(InstanceId, true)) info.completionDict[InstanceId] = true;
            }
            else
            {
                CoreManager.Instance.spawnManager.DynamicSpawnedWeapons.Remove(InstanceId);
            }
            
            OnPicked?.Invoke();
            GameEventSystem.Instance.RaiseEvent(TargetId);
            CoreManager.Instance.uiManager.GetUI<InventoryUI>()?.RefreshInventoryUI();
            CoreManager.Instance.objectPoolManager.Release(gameObject);
        }
        public void OnCancelInteract() { }
    }
}