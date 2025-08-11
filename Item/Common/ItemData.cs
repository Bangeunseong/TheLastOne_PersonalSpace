using System;
using UnityEngine;

namespace _1.Scripts.Item.Common
{
    [Serializable] public enum ItemType
    {
        Medkit,
        NanoAmple,
        EnergyBar,
        Shield,
    }
    
    [CreateAssetMenu(fileName = "New ItemData", menuName = "ScriptableObjects/Item", order = 0)]
    [Serializable] public class ItemData : ScriptableObject
    {
        [Header("Item Settings")] 
        public string NameKey;
        public string DescriptionKey;
        public ItemType ItemType;
        public bool IsPlayerMovable;
        public int Value;
        public float Delay;
        public Sprite Icon;

        [Header("Item Stack Count")]
        public int MaxStackCount;
    }
}