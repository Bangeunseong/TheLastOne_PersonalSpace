using System;
using System.Collections.Generic;
using _1.Scripts.Entity.Scripts.Npc.StatControllers.Base;
using _1.Scripts.VisualEffects;
using _1.Scripts.Item.Items;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Static;
using _1.Scripts.Util;
using _1.Scripts.Weapon.Scripts.Common;
using AYellowpaper.SerializedCollections;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace _1.Scripts.Manager.Subs
{
    [Serializable] public class SpawnManager
    {
        [field: Header("Spawn Point Data")]
        [field: SerializeField] public SpawnData CurrentSpawnData { get; private set; }
        [field: SerializeField] public SerializedDictionary<int, SerializableWeaponProp> DynamicSpawnedWeapons { get; private set; } = new();
        [field: SerializeField] public SerializedDictionary<int, SerializableItemProp> DynamicSpawnedItems { get; private set; } = new();
        [field: SerializeField] public SerializedDictionary<int, SerializablePartProp> DynamicSpawnedParts { get; private set; } = new(); 
        
        [field: Header("Visibility")]
        [field: SerializeField] public bool IsVisible { get; private set; }
        [field: SerializeField] public bool IsFocused { get; private set; }
        
        private HashSet<GameObject> spawnedEnemies = new();
        private HashSet<GameObject> spawnedProps = new();
        private CoreManager coreManager;
        private int dynamicIndex;
        
        public void Start()
        {
            coreManager = CoreManager.Instance;
        }
        
        public void ChangeSpawnDataAndInstantiate(SceneType sceneType, DataTransferObject dto)
        {
            CurrentSpawnData = sceneType switch
            {
                SceneType.Stage1 => coreManager.resourceManager.GetAsset<SpawnData>("SpawnPoints_Stage1"),
                SceneType.Stage2 => coreManager.resourceManager.GetAsset<SpawnData>("SpawnPoints_Stage2"),
                _ => null
            };
            SpawnPropsBySpawnData(sceneType, dto);
        }

        public void SpawnEnemyBySpawnData(int index)
        {
            if (CurrentSpawnData == null) return;
            if (!CurrentSpawnData.EnemySpawnPoints.TryGetValue(index, out var spawnPoints)) return;
            
            foreach (var pair in spawnPoints)
            {
                foreach (var val in pair.Value)
                {
                    GameObject enemy = coreManager.objectPoolManager.Get(pair.Key.ToString());
                    
                    var behaviorTrees = enemy.GetComponentsInChildren<BehaviorTree>();
                    var statControllers = enemy.GetComponentsInChildren<BaseNpcStatController>();
                    var agents = enemy.GetComponentsInChildren<NavMeshAgent>();
                    
                    foreach (var behaviorTree in behaviorTrees) { behaviorTree.SetVariableValue(BehaviorNames.CanRun, false); }
                    foreach (var statController in statControllers) { statController.RuntimeStatData.SpawnIndex = index + BaseEventIndex.BaseSpawnEnemyIndex; }
                    
                    enemy.transform.position = val.position;
                    enemy.transform.rotation = val.rotation;
                    
                    foreach (var agent in agents) { agent.enabled = true; } // 적 객체마다 OnEnable에서 키면 위에서 Get()할때 켜져서 디폴트 위치로 가는 버그 재발함. 위치 지정 후 켜야함
                    foreach (var behaviorTree in behaviorTrees) { behaviorTree.SetVariableValue(BehaviorNames.CanRun, true); }
                    
                    spawnedEnemies.Add(enemy);
                }
            }
        }

        private void SpawnPropsBySpawnData(SceneType sceneType, DataTransferObject dto = null)
        {
            if (CurrentSpawnData == null) return;
            int weaponIndex = 0, itemIndex = 0;
            foreach (var pair in CurrentSpawnData.WeaponSpawnPoints)
            {
                foreach (var val in pair.Value)
                {
                    if (dto == null || !dto.stageInfos.TryGetValue(sceneType, out var info) || 
                        !info.completionDict.TryGetValue(BaseEventIndex.BaseWeaponIndex + weaponIndex, out var value) ||
                        !value)
                    {
                        var obj = coreManager.objectPoolManager.Get(pair.Key + "_Dummy");
                        obj.transform.SetPositionAndRotation(val.position, val.rotation);
                        if (obj.TryGetComponent(out DummyWeapon weapon)) weapon.Initialize(true, BaseEventIndex.BaseWeaponIndex + weaponIndex);
                        spawnedProps.Add(obj);
                    }
                    weaponIndex++;
                }
            }
            foreach (var pair in CurrentSpawnData.ItemSpawnPoints)
            {
                foreach (var val in pair.Value)
                {
                    if (dto == null || !dto.stageInfos.TryGetValue(sceneType, out var info) || 
                        !info.completionDict.TryGetValue(BaseEventIndex.BaseItemIndex + itemIndex, out var value) ||
                        !value)
                    {
                        var obj = coreManager.objectPoolManager.Get(pair.Key + "_Prefab");
                        obj.transform.SetPositionAndRotation(val.position, val.rotation);
                        if (obj.TryGetComponent(out DummyItem item)) item.Initialize(true, BaseEventIndex.BaseItemIndex + itemIndex);
                        spawnedProps.Add(obj);
                    }
                    itemIndex++;
                }
            }

            if (dto == null) return;
            if (!dto.stageInfos.TryGetValue(coreManager.sceneLoadManager.CurrentScene, out var dynamicInfo)) return;

            if (dynamicInfo.dynamicSpawnedWeapons is { Count: > 0 })
            {
                foreach (var pair in dynamicInfo.dynamicSpawnedWeapons)
                {
                    var obj = coreManager.objectPoolManager.Get(pair.Value.type + "_Dummy");
                    obj.transform.SetPositionAndRotation(pair.Value.transform.position.ToVector3(),
                        pair.Value.transform.rotation.ToQuaternion());
                    if (obj.TryGetComponent(out DummyWeapon weapon)) weapon.Initialize(false, pair.Key);
                    DynamicSpawnedWeapons.Add(pair.Key, new SerializableWeaponProp(pair.Value));
                    spawnedProps.Add(obj);
                }
            }
            else dynamicInfo.dynamicSpawnedWeapons = new SerializedDictionary<int, SerializableWeaponProp>();

            if (dynamicInfo.dynamicSpawnedItems is { Count: > 0 })
            {
                foreach (var pair in dynamicInfo.dynamicSpawnedItems)
                {
                    var obj = coreManager.objectPoolManager.Get(pair.Value.type + "_Prefab");
                    obj.transform.SetPositionAndRotation(pair.Value.transform.position.ToVector3(), pair.Value.transform.rotation.ToQuaternion());
                    if (obj.TryGetComponent(out DummyItem item)) item.Initialize(false, pair.Key);
                    DynamicSpawnedItems.Add(pair.Key, new SerializableItemProp(pair.Value));
                    spawnedProps.Add(obj);
                }
            }
            else dynamicInfo.dynamicSpawnedItems = new SerializedDictionary<int, SerializableItemProp>();

            if (dynamicInfo.dynamicSpawnedParts is { Count: > 0 })
            {
                foreach (var pair in dynamicInfo.dynamicSpawnedParts)
                {
                    var obj = coreManager.objectPoolManager.Get("WeaponPart_Dummy");
                    obj.transform.SetPositionAndRotation(pair.Value.transform.position.ToVector3(), pair.Value.transform.rotation.ToQuaternion());
                    if(obj.TryGetComponent(out DummyWeaponPart part)) part.Initialize(pair.Value.type, pair.Value.id, pair.Key);
                    DynamicSpawnedParts.Add(pair.Key, new SerializablePartProp(pair.Value));
                    spawnedProps.Add(obj);
                }
            } else dynamicInfo.dynamicSpawnedParts = new SerializedDictionary<int, SerializablePartProp>();
        }

        public int GetInstanceHashId(GameObject obj, int type, Transform transform)
        {
            int count = 0;
            var id = HashCode.Combine(obj.GetInstanceID(), type, transform.position, transform.rotation);
            while (id is >= 1000 and < 2000)
            {
                if (count > 5) { id += 1000 + UnityEngine.Random.Range(0,1000); return id; }
                id = HashCode.Combine(obj.GetInstanceID(), type, transform.position, transform.rotation);
                count++;
            }
            return id;
        }

        public void RemovePropFromSpawnedList(GameObject obj)
        {
            spawnedProps.Remove(obj);
        }
        
        public void ChangeStencilLayer(bool isOn)
        {
            IsVisible = isOn;
            ChangeStencilLayerAllNpc(isOn);
            ChangeLayerOfProps(isOn);
        }

        /// <summary>
        /// true일 시 스텐실레이어 활성화, false일 시 해제
        /// </summary>
        /// <param name="isOn"></param>
        private void ChangeStencilLayerAllNpc(bool isOn)
        {
            foreach (GameObject obj in spawnedEnemies)
            {
                if (obj.TryGetComponent<StencilAbleForNpc>(out var stencilAbleForNpc))
                {
                    stencilAbleForNpc.StencilLayerOnOrNot(isOn);
                }
            }
        }

        public void FocusModeOnOrNot(bool isOn)
        {
            IsFocused = isOn;
            
            foreach (GameObject obj in spawnedEnemies)
            {
                if (obj.TryGetComponent<FocusAbleForNpc>(out var focusAbleForNpc))
                {
                    focusAbleForNpc.FocusOnOrNot(isOn);
                }
            }
        }

        private void ChangeLayerOfProps(bool isTransparent)
        {
            foreach (var obj in spawnedProps)
            {
                if (obj.TryGetComponent(out DummyWeapon weapon))
                     weapon.ChangeLayerOfBody(isTransparent);
                else if(obj.TryGetComponent(out DummyItem item))
                    item.ChangeLayerOfBody(isTransparent);
                else if(obj.TryGetComponent(out DummyWeaponPart part))
                    part.ChangeLayerOfBody(isTransparent);
            }
        }

        public void DisposeAllUniTasksFromSpawnedEnemies()
        {
            coreManager.NpcCTS?.Cancel();
            foreach (var obj in spawnedEnemies)
            {
                if (!obj.TryGetComponent(out BaseNpcStatController statController)) continue;
                statController.DisposeAllUniTasks();
            }
        }
        
        public void RemoveMeFromSpawnedEnemies(GameObject enemy)
        {
            spawnedEnemies.Remove(enemy);
        }

        public void ClearAllSpawnedEnemies()
        {
            spawnedEnemies.Clear(); 
        }

        public void ClearAllProps()
        {
            spawnedProps.Clear();
            DynamicSpawnedWeapons.Clear();
            DynamicSpawnedItems.Clear();
            DynamicSpawnedParts.Clear();
        }

        public void Reset()
        {
            ChangeStencilLayer(false);
            ClearAllSpawnedEnemies();
            ClearAllProps();
            CurrentSpawnData = null;
        }
    }
}
