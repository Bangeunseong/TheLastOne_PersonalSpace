using System;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Manager.Core;
using _1.Scripts.UI.InGame.Modification;
using UnityEngine;

namespace _1.Scripts.Map.Table
{
    public class ForgeTable : MonoBehaviour, IInteractable
    {
        private CoreManager coreManager;

        private void Start()
        {
            coreManager = CoreManager.Instance;
        }

        public void OnInteract(GameObject ownerObj)
        {
            if (!ownerObj.TryGetComponent(out Player player)) return;
            
            if (!coreManager.uiManager.ShowUI<ModificationUI>())
            {
                player.PlayerCondition.OnEnablePlayerMovement();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void OnCancelInteract() { }
    }
}