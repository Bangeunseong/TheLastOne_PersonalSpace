using System;
using _1.Scripts.Item.Common;
using _1.Scripts.Item.Items;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.UI.InGame;
using _1.Scripts.UI.InGame.HUD;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        [field: Header("Items")]
        [field: SerializeField] public SerializedDictionary<ItemType, BaseItem> Items { get; private set; }
        [field: SerializeField] public ItemType CurrentItem { get; private set; }
        
        [field: Header("QuickSlot Settings")]
        [field: SerializeField] public float HoldDurationToOpen { get; private set; }
        [field: SerializeField] public bool IsOpenUIAction { get; private set; }
        
        private CoreManager coreManager;
        private Player player;
        private bool isPressed;
        private float timeSinceLastPressed;

        private void Update()
        {
            if (!isPressed || IsOpenUIAction) return;
            IsOpenUIAction = Time.unscaledTime - timeSinceLastPressed >= HoldDurationToOpen;

            if (!IsOpenUIAction) return;
            
            coreManager.uiManager.ShowUI<QuickSlotUI>();
            player.Pov.m_HorizontalAxis.Reset();
            player.Pov.m_VerticalAxis.Reset();
            player.InputProvider.enabled = false;
        }

        public void Initialize(DataTransferObject dto = null)
        {
            coreManager = CoreManager.Instance;
            player = coreManager.gameManager.Player;
            
            Items = new SerializedDictionary<ItemType, BaseItem>();
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                BaseItem item = type switch
                {
                    ItemType.Medkit => new Medkit(),
                    ItemType.EnergyBar => new EnergyBar(),
                    ItemType.NanoAmple => new NanoAmple(),
                    ItemType.Shield => new Shield(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                item.Initialize(coreManager, dto);
                if (Items.TryAdd(type, item)) Service.Log($"Successfully added {type}.");
            }
        }

        public void OnItemActionStarted()
        {
            IsOpenUIAction = false;
            isPressed = true;
            timeSinceLastPressed = Time.unscaledTime;
        }
        
        public void OnItemActionCanceled()
        {
            isPressed = false;
            switch (IsOpenUIAction)
            {
                case true:
                    coreManager.uiManager.HideUI<QuickSlotUI>();
                    player.InputProvider.enabled = true; break;
                case false: OnUseItem(); break;
            }
            IsOpenUIAction = false;
        }

        public void OnSelectItem(ItemType itemType)
        {
            Service.Log($"Attempting to select {itemType}");
            if (!Items.ContainsKey(itemType) || Items[itemType].CurrentItemCount <= 0)
            {
                CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast("FailSelect_Key");
                return;
            }
            CurrentItem = itemType;
            CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast(Items[itemType].ItemData);
        }

        public bool OnRefillItem(ItemType itemType)
        {
            Service.Log($"Attempting to refill {itemType}");
            return Items[itemType].OnRefill();
        }
        
        private bool OnUseItem()
        {
            Service.Log($"Attempting to use {CurrentItem}.");
            if (!Items.ContainsKey(CurrentItem) || Items[CurrentItem].CurrentItemCount <= 0)
            {
                CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast("FailUse_Key");
                return false;
            }
            switch (Items[CurrentItem])
            {
                case Medkit medkit: return medkit.OnUse(gameObject);
                case NanoAmple nanoAmple: return nanoAmple.OnUse(gameObject);
                case EnergyBar energyBar: return energyBar.OnUse(gameObject);
                case Shield shield: return shield.OnUse(gameObject);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}