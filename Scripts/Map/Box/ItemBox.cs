using System;
using System.Collections.Generic;
using System.Linq;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Item.Common;
using _1.Scripts.Item.Items;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Quests.Core;
using _1.Scripts.Util;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _1.Scripts.Map.Box
{
    public enum BoxType
    {
        Ammo,
        Item,
        Parts,
    }
    
    public class ItemBox : MonoBehaviour, IInteractable, IGameEventListener
    {
        [field: Header("Components")]
        [field: SerializeField] public Animator Animator { get; private set; }
        [field: SerializeField] public ParticleSystem ParticleSystem { get; private set; }
        [field: SerializeField] public List<Transform> SpawnPoints { get; private set; } = new();
        
        [field: Header("References")]
        [field: Tooltip("Target Id is for the Quest")]
        [field: SerializeField] public int TargetId { get; private set; }
        [field: Tooltip("Instance Id is for the check if is opened last time.")]
        [field: SerializeField] public int InstanceId { get; private set; }
        [field: Tooltip("Changing Value greater than 0 will spawn corresponding prop.")]
        [field: SerializeField] public SerializedDictionary<BoxType, int> GenSettings { get; private set; } = new();
        
        [field: Header("Randomize Options")]
        [field: Tooltip("Randomize Part Spawns")]
        [field: SerializeField] public bool RandomizePart { get; private set; } = true;
        [field: SerializeField] public SerializedDictionary<PartType, WeaponType> Parts { get; private set; } = new();
        
        [Header("Item Box State")]
        [SerializeField] private bool isAlreadyOpened;
        
        private CoreManager coreManager;
        private Player player;
        
        private static readonly int Open = Animator.StringToHash("Open");

        private void Awake()
        {
            if (!Animator) Animator = this.TryGetComponent<Animator>();
            if (!ParticleSystem) ParticleSystem = this.TryGetChildComponent<ParticleSystem>();
            if (SpawnPoints.Count <= 0)
            {
                SpawnPoints.AddRange(this.TryGetChildComponents<Transform>("SpawnPoints"));
                SpawnPoints.RemoveAt(0);
            }
        }

        private void Reset()
        {
            if (!Animator) Animator = this.TryGetComponent<Animator>();
            if (!ParticleSystem) ParticleSystem = this.TryGetChildComponent<ParticleSystem>();
            if (SpawnPoints.Count <= 0)
            {
                SpawnPoints.AddRange(this.TryGetChildComponents<Transform>("SpawnPoints"));
                SpawnPoints.RemoveAt(0);
            }
        }

        private void Start()
        {
            coreManager = CoreManager.Instance;
            player = coreManager.gameManager.Player;
            GameEventSystem.Instance.RegisterListener(this);
            
            var save = CoreManager.Instance.gameManager.SaveData;
            if (save is not { stageInfos: not null } ||
                !save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info)) return;
            if (!info.completionDict.TryGetValue(BaseEventIndex.BaseItemBoxIndex + InstanceId, out var value) || value) return;
            
            GameEventSystem.Instance.UnregisterListener(this);
            isAlreadyOpened = true;
            Animator.SetTrigger(Open);
            gameObject.layer = LayerMask.NameToLayer("Default");
        }

        public void OnInteract(GameObject ownerObj)
        {
            if (!ownerObj.TryGetComponent(out Player p) || isAlreadyOpened) return;
            
            isAlreadyOpened = true;
            
            PlayOpeningAnimation();
            
            GameEventSystem.Instance.RaiseEvent(TargetId);
            GameEventSystem.Instance.RaiseEvent(BaseEventIndex.BaseItemBoxIndex + InstanceId);
        }

        public void OnCancelInteract() { }
        
        public void OnEventRaised(int eventID)
        {
            if (eventID != BaseEventIndex.BaseItemBoxIndex + InstanceId) return;
            
            var save = coreManager.gameManager.SaveData;
            if (save is { stageInfos: not null } && save.stageInfos.TryGetValue(coreManager.sceneLoadManager.CurrentScene, out var info))
                info.completionDict.TryAdd(BaseEventIndex.BaseItemBoxIndex + InstanceId, true);
            
            GameEventSystem.Instance.UnregisterListener(this);
        }
        
        private void PlayOpeningAnimation()
        {
            gameObject.layer = LayerMask.NameToLayer("Default");
            ParticleSystem.Play();

            foreach (var gen in GenSettings.Where(gen => gen.Value > 0))
            {
                switch (gen.Key)
                {
                    case BoxType.Ammo: for(var i = 0; i < gen.Value; i++) GenAmmo(); break;
                    case BoxType.Item: for(var j = 0; j < gen.Value; j++) GenItem(); break;
                    case BoxType.Parts: foreach(var part in Parts) GenPart(RandomizePart, part.Value, part.Key); break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            Animator.SetTrigger(Open);
        }

        private void GenAmmo()
        {
            var randomFloat = Random.Range(0f, 1f);
            var type = player.PlayerWeapon.AvailableWeapons[WeaponType.SniperRifle] switch
            {
                false => randomFloat > 0.5f ? WeaponType.GrenadeLauncher : WeaponType.HackGun,
                true => randomFloat < 0.3f ? WeaponType.GrenadeLauncher : randomFloat < 0.6f ? WeaponType.HackGun : WeaponType.SniperRifle,
            };
                
            var ammo = coreManager.objectPoolManager.Get(type + "_Dummy");
            ammo.transform.SetPositionAndRotation(SpawnPoints.First().position,
                SpawnPoints.First().rotation);
            if (ammo.TryGetComponent(out DummyWeapon ammoComp))
                ammoComp.Initialize(false,
                    coreManager.spawnManager.GetInstanceHashId(ammo, (int)type, ammo.transform));
            coreManager.spawnManager.DynamicSpawnedWeapons.TryAdd(ammoComp.InstanceId,
                new SerializableWeaponProp
                {
                    type = type,
                    transform = new SerializableTransform
                    {
                        position = new SerializableVector3(ammo.transform.position),
                        rotation = new SerializableQuaternion(ammo.transform.rotation)
                    }
                });
        }

        private void GenItem(ItemType itemType = ItemType.Shield)
        {
            var item = coreManager.objectPoolManager.Get(itemType + "_Prefab");
            item.transform.SetPositionAndRotation(SpawnPoints[1].position, SpawnPoints[1].rotation);
            if (item.TryGetComponent(out DummyItem itemComp))
                itemComp.Initialize(false,
                    coreManager.spawnManager.GetInstanceHashId(item, (int)ItemType.Shield,
                        item.transform));
            coreManager.spawnManager.DynamicSpawnedItems.TryAdd(itemComp.InstanceId,
                new SerializableItemProp
                {
                    type = itemType,
                    transform = new SerializableTransform
                    {
                        position = new SerializableVector3(item.transform.position),
                        rotation = new SerializableQuaternion(item.transform.rotation)
                    }
                });
        }

        private void GenPart(bool randomize, WeaponType type = WeaponType.Rifle, PartType partType = PartType.Sight)
        {
            if (randomize) RandomlyAddMissingWeaponPart();
            else SpawnSpecificWeaponPart(type, partType);
        }

        private void SpawnSpecificWeaponPart(WeaponType weaponType, PartType partType)
        {
            var weapon = player.PlayerWeapon.Weapons[weaponType];

            var parts = weapon.WeaponParts.Where(val => val.Value.Data.Type == partType).Select(val => val.Key).ToHashSet();
            var collectableParts = player.PlayerWeapon.Weapons[weaponType].EquipableWeaponParts.Where(val => !val.Value).Select(val => val.Key).ToHashSet();
            collectableParts.IntersectWith(parts);
            foreach(var partId in collectableParts) Service.Log($"{partId}");
            
            if (collectableParts.Count == 0) return;
            
            var part = coreManager.objectPoolManager.Get("WeaponPart_Dummy");
            part.transform.SetPositionAndRotation(SpawnPoints[2].position, SpawnPoints[2].rotation);
            if (part.TryGetComponent(out DummyWeaponPart weaponPart))
                weaponPart.Initialize(weaponType, collectableParts.FirstOrDefault(),
                    coreManager.spawnManager.GetInstanceHashId(part, (int)weaponType,
                        part.transform));
            coreManager.spawnManager.DynamicSpawnedParts.TryAdd(weaponPart.InstanceId,
                new SerializablePartProp
                {
                    type = weaponPart.Type,
                    id = weaponPart.Id,
                    transform = new SerializableTransform
                    {
                        position = new SerializableVector3(part.transform.position),
                        rotation = new SerializableQuaternion(part.transform.rotation)
                    }
                });
        }
        
        private void RandomlyAddMissingWeaponPart()
        {
            var missingPartIds = new List<(WeaponType, int)>();
            foreach(var weapon in player.PlayerWeapon.Weapons)
                missingPartIds.AddRange(weapon.Value.EquipableWeaponParts.Where(val => !val.Value).Select(val => (weapon.Key, val.Key)));

            var part = missingPartIds[Random.Range(0, missingPartIds.Count)];
            var weaponPart = coreManager.objectPoolManager.Get("WeaponPart_Dummy");
            weaponPart.transform.SetPositionAndRotation(SpawnPoints[2].position, SpawnPoints[2].rotation);
            if (weaponPart.TryGetComponent(out DummyWeaponPart partComp))
                partComp.Initialize(part.Item1, part.Item2, coreManager.spawnManager.GetInstanceHashId(weaponPart, (int)part.Item1, partComp.transform));
            coreManager.spawnManager.DynamicSpawnedParts.TryAdd(partComp.InstanceId,
                new SerializablePartProp
                {
                    type = partComp.Type,
                    id = partComp.Id,
                    transform = new SerializableTransform
                    {
                        position = new SerializableVector3(partComp.transform.position),
                        rotation = new SerializableQuaternion(partComp.transform.rotation)
                    }
                });
        }
    }
}
