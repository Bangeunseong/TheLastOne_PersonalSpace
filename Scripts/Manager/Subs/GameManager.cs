using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Item.Common;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.Grenade;
using _1.Scripts.Weapon.Scripts.Guns;
using _1.Scripts.Weapon.Scripts.Hack;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Collections;
using CharacterInfo = _1.Scripts.Manager.Data.CharacterInfo;

namespace _1.Scripts.Manager.Subs
{
    [Serializable]
    public class GameManager
    {
        [Header("Save Settings")] [SerializeField, ReadOnly]
        private string SaveDirectoryPath = "Assets/Data/";

        [SerializeField, ReadOnly] private string SaveFileName = "SaveData.json";
        [field: SerializeField] public Player Player { get; private set; }
        [field: SerializeField] public bool IsGamePaused { get; set; }

        private CoreManager coreManager;
        
        public DataTransferObject SaveData { get; private set; }

        public void Start()
        {
            coreManager = CoreManager.Instance;
            SaveData = null;
        }

        // Methods
        public void Initialize_Player(Player player)
        {
            Player = player;
        }

        public async Task TrySaveData()
        {
            if (!Directory.Exists(SaveDirectoryPath)) Directory.CreateDirectory(SaveDirectoryPath);
            
            // Save Current Character Info.
            var save = new DataTransferObject
            {
                characterInfo = new CharacterInfo
                {
                    maxHealth = Player.PlayerCondition.MaxHealth, health = Player.PlayerCondition.CurrentHealth,
                    maxStamina = Player.PlayerCondition.MaxStamina, stamina = Player.PlayerCondition.CurrentStamina,
                    maxShield = Player.PlayerCondition.MaxShield, shield = Player.PlayerCondition.CurrentShield,
                    attackRate = Player.PlayerCondition.AttackRate, damage = Player.PlayerCondition.Damage,
                    focusGauge = Player.PlayerCondition.CurrentFocusGauge, instinctGauge = Player.PlayerCondition.CurrentInstinctGauge,
                },
                currentSceneId = coreManager.sceneLoadManager.CurrentScene,
            };

            if (SaveData == null)
            {
                save.stageInfos = new SerializedDictionary<SceneType, StageInfo>();
                var newStageInfoOfCurrentScene = new StageInfo
                {
                    isIntroPlayed = true,
                    currentCharacterPosition = new SerializableVector3(Player.PlayerCondition.LastSavedPosition),
                    currentCharacterRotation = new SerializableQuaternion(Player.PlayerCondition.LastSavedRotation),
                    completionDict = new SerializedDictionary<int, bool>(),
                };
                save.stageInfos.TryAdd(coreManager.sceneLoadManager.CurrentScene, newStageInfoOfCurrentScene);
            }
            else
            {
                save.stageInfos = new SerializedDictionary<SceneType, StageInfo>(SaveData.stageInfos);
                if (!save.stageInfos.TryGetValue(coreManager.sceneLoadManager.CurrentScene, out var stageInfo))
                {
                    var newStageInfoOfCurrentScene = new StageInfo
                    {
                        isIntroPlayed = true,
                        currentCharacterPosition = new SerializableVector3(Player.PlayerCondition.LastSavedPosition),
                        currentCharacterRotation = new SerializableQuaternion(Player.PlayerCondition.LastSavedRotation),
                        completionDict = new SerializedDictionary<int, bool>(),
                    };
                    save.stageInfos.TryAdd(coreManager.sceneLoadManager.CurrentScene, newStageInfoOfCurrentScene);
                }
                else
                {
                    stageInfo.currentCharacterPosition =
                        new SerializableVector3(Player.PlayerCondition.LastSavedPosition);
                    stageInfo.currentCharacterRotation = 
                        new SerializableQuaternion(Player.PlayerCondition.LastSavedRotation);
                    stageInfo.dynamicSpawnedWeapons =
                        new SerializedDictionary<int, SerializableWeaponProp>(coreManager.spawnManager.DynamicSpawnedWeapons);
                    stageInfo.dynamicSpawnedItems =
                        new SerializedDictionary<int, SerializableItemProp>(coreManager.spawnManager.DynamicSpawnedItems);
                    stageInfo.dynamicSpawnedParts =
                        new SerializedDictionary<int, SerializablePartProp>(coreManager.spawnManager.DynamicSpawnedParts);
                }
            }
            
            // Save Current Weapon Infos
            var newWeaponInfo = new SerializedDictionary<WeaponType, WeaponInfo>();
            var newAvailableWeapons = Player.PlayerWeapon.AvailableWeapons;
            foreach (var weapon in Player.PlayerWeapon.Weapons)
            {
                switch (weapon.Value)
                {
                    case Gun gun:
                        newWeaponInfo.Add(gun.GunData.GunStat.Type, new WeaponInfo
                        {
                            currentAmmoCount = gun.CurrentAmmoCount,
                            currentAmmoCountInMagazine = gun.CurrentAmmoCountInMagazine,
                            equipableParts = new SerializedDictionary<int, bool>(gun.EquipableWeaponParts),
                            equippedParts = new SerializedDictionary<PartType, int>(gun.EquippedWeaponParts),
                        });
                        break;
                    case GrenadeLauncher grenadeLauncher:
                        newWeaponInfo.Add(WeaponType.GrenadeLauncher, new WeaponInfo
                        {
                            currentAmmoCount = grenadeLauncher.CurrentAmmoCount,
                            currentAmmoCountInMagazine = grenadeLauncher.CurrentAmmoCountInMagazine,
                            equipableParts = new SerializedDictionary<int, bool>(grenadeLauncher.EquipableWeaponParts),
                            equippedParts = new SerializedDictionary<PartType, int>(grenadeLauncher.EquippedWeaponParts),
                        });
                        break;
                    case HackGun hackGun:
                        newWeaponInfo.Add(WeaponType.HackGun, new WeaponInfo
                        {
                            currentAmmoCount = hackGun.CurrentAmmoCount,
                            currentAmmoCountInMagazine = hackGun.CurrentAmmoCountInMagazine,
                            equipableParts = new SerializedDictionary<int, bool>(hackGun.EquipableWeaponParts),
                            equippedParts = new SerializedDictionary<PartType, int>(hackGun.EquippedWeaponParts),
                        });
                        break;
                }
            }
            save.weapons = newWeaponInfo;
            save.availableWeapons = new SerializedDictionary<WeaponType, bool>(newAvailableWeapons);

            // Save Current Item Infos
            var newItemCountList = (from ItemType type in Enum.GetValues(typeof(ItemType)) select Player.PlayerInventory.Items[type].CurrentItemCount).ToList();
            save.Items = newItemCountList.ToArray();
            
            // Quest List
            foreach (var quest in coreManager.questManager.activeQuests)
            {
                save.Quests[quest.Key] = new QuestInfo
                {
                    currentObjectiveIndex = quest.Value.currentObjectiveIndex,
                    progresses = new SerializedDictionary<int, int>(quest.Value.Objectives.ToDictionary(val => val.Key, val => val.Value.currentAmount)),
                    completionList = new SerializedDictionary<int, bool>(quest.Value.Objectives.ToDictionary(val => val.Key, val => val.Value.IsCompleted)),
                };
            }
            
            SaveData = save;
            
            var json = JsonConvert.SerializeObject(save, Formatting.Indented);
            await File.WriteAllTextAsync(SaveDirectoryPath + SaveFileName, json);
        }

        public async Task TryLoadData()
        {
            if (File.Exists(SaveDirectoryPath + SaveFileName))
            {
                var str = await File.ReadAllTextAsync(SaveDirectoryPath + SaveFileName);
                SaveData = JsonConvert.DeserializeObject<DataTransferObject>(str);
            }
            else SaveData = null;
        }

        public void TryRemoveSavedData()
        {
            if (!Directory.Exists(SaveDirectoryPath)) return;
            File.Delete(SaveDirectoryPath + SaveFileName);
            SaveData = null;
        }
        
        public void PauseGame()
        {
            if (!Player) return;
            
            Service.Log("Game Paused!");
            IsGamePaused = true;
               
            coreManager.timeScaleManager.ChangeTimeScale(0);
            Player.PlayerCondition.OnDisablePlayerMovement();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            if (!Player) return;

            Service.Log("Game Resumed!");
            IsGamePaused = false;
            
            if (Player.PlayerCondition.IsUsingFocus) coreManager.timeScaleManager.ChangeTimeScale(0.5f);
            else coreManager.timeScaleManager.ChangeTimeScale(1);
            
            if (Player.PlayerCondition.IsInMiniGame) return;
            Player.PlayerCondition.OnEnablePlayerMovement();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ExitGame()
        {
            IsGamePaused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}