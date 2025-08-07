using System;
using _1.Scripts.Interfaces.Item;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using UnityEngine;

namespace _1.Scripts.Item.Common
{
    [Serializable] public class BaseItem : IItem
    {
        [field: Header("Item Data")]
        [field: SerializeField] public ItemData ItemData { get; protected set; }
        
        [field: Header("Current Item Stat.")]
        [field: SerializeField] public int CurrentItemCount { get; protected set; }

        public virtual void Initialize(CoreManager coreManager, DataTransferObject dto = null) { }

        public virtual bool OnUse(GameObject interactor) { return false; }
        
        public void OnConsume() => CurrentItemCount--;
        
        public virtual bool OnRefill(int value = 1)
        {
            if (CurrentItemCount >= ItemData.MaxStackCount) return false;
            CurrentItemCount = Mathf.Min(CurrentItemCount + value, ItemData.MaxStackCount);
            return true;
        }
    }
}