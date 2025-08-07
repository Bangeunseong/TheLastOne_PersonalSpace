using System;
using System.Collections.Generic;
using _1.Scripts.Item.Common;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace _1.Scripts.Manager.Data
{
    [Serializable] public class SerializableVector3
    {
        public float x, y, z;
        
        public SerializableVector3(){}
        public SerializableVector3(Vector3 v){ x = v.x; y = v.y; z = v.z; }

        public Vector3 ToVector3() { return new Vector3(x, y, z); }
    }

    [Serializable] public class SerializableQuaternion
    {
        public float x, y, z, w;
        
        public SerializableQuaternion(){}
        public SerializableQuaternion(Quaternion q){ x = q.x; y = q.y; z = q.z; w = q.w; }
        
        public Quaternion ToQuaternion() { return new Quaternion(x, y, z, w); }
    }

    [Serializable] public class SerializableTransform
    {
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        
        public SerializableTransform(){}
        public SerializableTransform(SerializableTransform transform)
        {
            position = transform.position; rotation = transform.rotation;
        }
        
        public Vector3 GetPosition() { return position.ToVector3(); }
        public Quaternion GetRotation() { return rotation.ToQuaternion(); }
    }

    [Serializable] public class SerializableWeaponProp
    {
        public WeaponType type;
        public SerializableTransform transform;

        public SerializableWeaponProp(){}
        public SerializableWeaponProp(SerializableWeaponProp data)
        {
            type = data.type;
            transform = new SerializableTransform(data.transform);
        }
        
        public WeaponType GetWeaponType() { return type; }
    }

    [Serializable] public class SerializableItemProp
    {
        public ItemType type;
        public SerializableTransform transform;

        public SerializableItemProp(){}
        public SerializableItemProp(SerializableItemProp data)
        {
            type = data.type;
            transform = new SerializableTransform(data.transform);
        }
        
        public ItemType GetItemType() { return type; }
    }

    [Serializable] public class SerializablePartProp
    {
        public WeaponType type;
        public int id;
        public SerializableTransform transform;
        
        public SerializablePartProp(){}

        public SerializablePartProp(SerializablePartProp data)
        {
            type = data.type;
            id = data.id;
            transform = new SerializableTransform(data.transform);
        }
        
        public int GetPartId() { return id; }
    }

    [Serializable] public class CharacterInfo
    {
        public int maxHealth;
        public int health;
        public float maxStamina;
        public float stamina;
        public int maxShield;
        public int shield;
        public float damage;
        public float attackRate;
        public float focusGauge;
        public float instinctGauge;
    }

    [Serializable] public class WeaponInfo
    {
        public int currentAmmoCount;
        public int currentAmmoCountInMagazine;
        public SerializedDictionary<PartType, int> equippedParts;
        public SerializedDictionary<int, bool> equipableParts;
    }

    [Serializable] public class StageInfo
    {
        [Header("Intro Play Verification")]
        public bool isIntroPlayed;
        
        [Header("Last Saved Position & Rotation")]
        public SerializableVector3 currentCharacterPosition;
        public SerializableQuaternion currentCharacterRotation;

        [Header("Completed Scene Event Ids")] 
        public SerializedDictionary<int, bool> completionDict;
        public SerializedDictionary<int, SerializableWeaponProp> dynamicSpawnedWeapons;
        public SerializedDictionary<int, SerializableItemProp> dynamicSpawnedItems;
        public SerializedDictionary<int, SerializablePartProp> dynamicSpawnedParts;
    }

    [Serializable] public class QuestInfo
    {
        public int currentObjectiveIndex;
        public SerializedDictionary<int, int> progresses;
        public SerializedDictionary<int, bool> completionList;
    }
    
    [Serializable] public class DataTransferObject
    {
        [Header("Character Stat.")]
        [SerializeField] public CharacterInfo characterInfo;

        [Header("Character Weapons")] 
        [SerializeField] public SerializedDictionary<WeaponType, WeaponInfo> weapons;
        [SerializeField] public SerializedDictionary<WeaponType, bool> availableWeapons;

        [field: Header("Character Items")]
        [field: SerializeField] public int[] Items { get; set; }
        
        [field: Header("Quests")]
        [field: SerializeField] public SerializedDictionary<int, QuestInfo> Quests { get; private set; } = new();
        
        [Header("Stage Info.")] 
        [SerializeField] public SceneType currentSceneId;
        [SerializeField] public SerializedDictionary<SceneType, StageInfo> stageInfos;
        
        public override string ToString()
        {
            return
                $"Character Stat.\n{characterInfo.maxHealth}, " +
                $"{characterInfo.health}\n{characterInfo.damage}, " +
                $"{characterInfo.attackRate}\n" +
                "Weapon Info.\n" +
                $"{weapons}\n" +
                $"{availableWeapons}\n" +
                $"Quest Info.\n{Quests.Values}";
        }
    }
}