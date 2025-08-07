using System;
using System.Collections.Generic;
using _1.Scripts.Manager.Core;
using _1.Scripts.UI;
using _1.Scripts.UI.Common;
using _1.Scripts.UI.InGame.Dialogue;
using _1.Scripts.UI.InGame.HUD;
using _1.Scripts.UI.InGame.Minigame;
using _1.Scripts.UI.InGame.Modification;
using _1.Scripts.UI.InGame.SkillOverlay;
using _1.Scripts.UI.Inventory;
using _1.Scripts.UI.Loading;
using _1.Scripts.UI.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace _1.Scripts.Manager.Subs
{
    public enum UIType
    {
        Persistent,
        InGame,
        InGame_HUD,
    }
    [Serializable] public class UIManager
    {
        [field: Header("UI Mapping")]
        [field: SerializeField] public Canvas RootCanvas { get; private set; }
        [field: SerializeField] public Transform UiRoot { get; private set; }
        
        [field: Header("Handler")]
        [field: SerializeField] public MenuHandler MenuHandler { get; private set; }
        
        private readonly Dictionary<Type, UIBase> uiMap = new();
        private readonly Dictionary<UIType, List<Type>> uiGroupMap = new()
        {
            {
                UIType.Persistent, new List<Type> { typeof(LoadingUI), typeof(FadeUI), typeof(LobbyUI) }
            },
            { 
                UIType.InGame, new List<Type> { typeof(InGameUI), typeof(LowHealthOverLay), typeof(SkillOverlayUI), typeof(DistanceUI), typeof(WeaponUI),
                typeof(QuickSlotUI), typeof(QuestUI), typeof(DialogueUI), typeof(BleedOverlayUI), typeof(MinigameUI), typeof(ModificationUI), typeof(InventoryUI), 
                typeof(PauseMenuUI), typeof(GameOverUI), typeof(EndingCreditUI) } 
            },
            {
                UIType.InGame_HUD, new List<Type>{typeof(InGameUI), typeof(DialogueUI), typeof(BleedOverlayUI), typeof(LowHealthOverLay), typeof(SkillOverlayUI), typeof(DistanceUI), typeof(QuestUI), typeof(WeaponUI)}
            }
        };
        private CoreManager coreManager;
        
        public bool IsCutscene { get; private set; }
        
        public void Start()
        {
            coreManager = CoreManager.Instance;
            var canvas = GameObject.FindGameObjectWithTag("MainCanvas");
            if (canvas)
            {
                RootCanvas = canvas.GetComponent<Canvas>();
                UiRoot = canvas.transform;
            }
            
            RegisterStaticUI<LoadingUI>(); RegisterStaticUI<LobbyUI>(); RegisterStaticUI<FadeUI>();
            ShowUI<LobbyUI>(); GetUI<FadeUI>().FadeIn();
        }

        private bool RegisterStaticUI<T>() where T : UIBase
        {
            var ui = Object.FindObjectOfType<T>(true);
            if (!ui || uiMap.TryGetValue(typeof(T), out var val)) return false;
            
            ui.Initialize(this);
            uiMap.Add(typeof(T), ui);
            return true;
        }
        
        public bool RegisterDynamicUI<T>() where T : UIBase
        {
            var uiResource = coreManager.resourceManager.GetAsset<GameObject>(typeof(T).Name);
            if (!uiResource) return false;
            if (uiMap.TryGetValue(typeof(T), out var ui))
            {
                if (ui is T castedUI) castedUI.Initialize(this);
                return true;
            }
            
            var instance = Object.Instantiate(uiResource, UiRoot);
            if (!instance.TryGetComponent(out T instanceUI)) return false;
            instanceUI.Initialize(this);
            uiMap.Add(typeof(T), instanceUI);
            return true;
        }

        public bool RegisterDynamicUIByGroup(UIType groupType)
        {
            Service.Log("Start RegisterDynamicUIByGroup");
            MenuHandler = Object.FindObjectOfType<MenuHandler>();
            
            if (!uiGroupMap.TryGetValue(groupType, out var value)) return false;
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(RegisterDynamicUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            
            AddHandler(uiMap[typeof(PauseMenuUI)]);
            return true;
        }

        public bool UnregisterDynamicUI<T>() where T : UIBase
        {
            if (!uiMap.TryGetValue(typeof(T), out var dynamicUI)) return false;
            
            uiMap.Remove(typeof(T));
            Object.Destroy(dynamicUI.gameObject);
            return true;
        }

        public bool UnregisterDynamicUIByGroup(UIType groupType)
        {
            if (!uiGroupMap.TryGetValue(groupType, out var value)) return false;
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(UnregisterDynamicUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            return true;
        }
        
        public T GetUI<T>() where T : UIBase
        {
            return uiMap.TryGetValue(typeof(T), out var ui) ? ui as T : null;
        }

        public bool ShowUI<T>() where T : UIBase
        {
            if (!uiMap.TryGetValue(typeof(T), out var ui)) return false;
            ui.Show();
            return true;
        }

        public bool ShowHUD()
        {
            if (!uiGroupMap.TryGetValue(UIType.InGame_HUD, out var value)) return false;
            
            
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(ShowUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            return true;
        }

        public bool ShowPauseMenu()
        {
            if (!HideUIByGroup(UIType.InGame)) return false;
            ShowUI<PauseMenuUI>();
            return true;
        }

        public bool HideUI<T>() where T : UIBase
        {
            if (!uiMap.TryGetValue(typeof(T), out var ui)) return false;
            ui.Hide();
            return true;
        }

        public bool HideHUD()
        {
            if (!uiGroupMap.TryGetValue(UIType.InGame_HUD, out var value)) return false;
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(HideUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            return true;
        }

        public bool HideUIByGroup(UIType groupType)
        {
            if (!uiGroupMap.TryGetValue(groupType, out var value)) return false;
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(HideUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            return true;
        }
        
        public bool HidePauseMenu()
        {
            return HideUIByGroup(UIType.InGame) && ShowHUD();
        }

        public void ResetUI<T>() where T : UIBase
        {
            if (!uiMap.TryGetValue(typeof(T), out var ui)) return;
            ui.ResetUI();
        }
        
        public bool ResetHUD()
        {
            if (!uiGroupMap.TryGetValue(UIType.InGame_HUD, out var value)) return false;
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(ResetUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            return true;
        }

        public bool ResetUIByGroup(UIType groupType)
        {
            if (!uiGroupMap.TryGetValue(groupType, out var value)) return false;
            foreach (var type in value)
            {
                var method = typeof(UIManager).GetMethod(nameof(ResetUI))?.MakeGenericMethod(type);
                method?.Invoke(this, null);
            }
            return true;
        }
        
        public void OnCutsceneStarted(PlayableDirector _)
        {
            IsCutscene = true;
            HideUIByGroup(UIType.InGame);
        }
        
        public void OnCutsceneStopped(PlayableDirector director)
        {
            director.played -= OnCutsceneStarted;
            director.stopped -= OnCutsceneStopped;
            IsCutscene = false;
            if (!ShowHUD()) throw new MissingReferenceException();
        }

        private void AddHandler(UIBase ui)
        {
            if (ui is not PauseMenuUI pauseMenuUI) return;
            
            var pauseHandler = pauseMenuUI.GetComponent<PauseHandler>();
            if (MenuHandler && pauseHandler)
                MenuHandler.SetPauseHandler(pauseHandler);
            pauseHandler.SetPauseMenuUI(pauseMenuUI);
        }
    }
}