using System;
using System.Collections.Generic;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Map.Doors;
using _1.Scripts.MiniGame;
using _1.Scripts.Quests.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace _1.Scripts.Map.Console
{
    public class Console : MonoBehaviour, IInteractable
    {
        [field: Header("Console Settings")]
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public bool IsCleared { get; private set; }
        [field: SerializeField] public float Cooldown { get; private set; }
        [field: SerializeField] public bool IsOnCooldown { get; private set; }
        [field: SerializeField] public GameObject AlertBody { get; private set; }
        [field: SerializeField] public List<ConsoleDoor> Doors { get; private set; }
        
        
        [field: Header("Minigames")]
        [field: SerializeField] public List<BaseMiniGame> MiniGames { get; private set; }
        [field: SerializeField] public int CurrentMiniGame { get; private set; }
        
        [field: Header("CutScene")]
        [field: SerializeField] public PlayableDirector CutScene { get; private set; }
        
        [Header("Include BGM Change")]
        [SerializeField] private bool shouldChangeBGM = false;
        [SerializeField] private int indexOfBGM;
        
        private CoreManager coreManager;
        private float timeSinceLastFailed;
        private bool isInteracted;
        
        private void Awake()
        {
            if (MiniGames.Count <= 0) MiniGames = new List<BaseMiniGame>(GetComponentsInChildren<BaseMiniGame>());
            if (Doors.Count <= 0) Doors = new List<ConsoleDoor>(GetComponentsInChildren<ConsoleDoor>());
        }

        private void Reset()
        {
            if (MiniGames.Count <= 0) MiniGames = new List<BaseMiniGame>(GetComponentsInChildren<BaseMiniGame>());
            if (Doors.Count <= 0) Doors = new List<ConsoleDoor>(GetComponentsInChildren<ConsoleDoor>());
        }

        private void Start()
        {
            coreManager = CoreManager.Instance;
            foreach(var door in Doors) door.Initialize(IsCleared);
        }

        private void Update()
        {
            if (!IsOnCooldown) return;
            if (timeSinceLastFailed >= Cooldown){
                IsOnCooldown = false; timeSinceLastFailed = 0f; AlertBody?.SetActive(false);
                return;
            }
            timeSinceLastFailed += Time.unscaledDeltaTime;
        }

        public void OnInteract(GameObject ownerObj)
        {
            if (!ownerObj.TryGetComponent(out Player player)) return;
            Service.Log("Interacted!");
            
            if (IsCleared || IsOnCooldown || isInteracted) return;
            isInteracted = true;
            CurrentMiniGame = UnityEngine.Random.Range(0, MiniGames.Count);
            MiniGames[CurrentMiniGame].StartMiniGame(this, player);
        }

        public void OnCancelInteract()
        {
            isInteracted = false;
            MiniGames[CurrentMiniGame].CancelMiniGame();
        }

        public void OnCleared(bool success)
        {
            if (success) OnClear();
            else
            {
                isInteracted = false;
                IsOnCooldown = true;
                AlertBody?.SetActive(true);
            }
        }

        public void OpenDoors()
        {
            IsCleared = true;
            foreach(var door in Doors) door.Initialize(true);
        }
        
        private void OnClear()
        {
            IsCleared = true; isInteracted = false;
            if (!CutScene)
            {
                coreManager.gameManager.Player.PlayerCondition.OnEnablePlayerMovement();
                foreach (var door in Doors) door.OpenDoor();
            }
            else
            {
                CutScene.played += coreManager.uiManager.OnCutsceneStarted;
                CutScene.stopped += coreManager.uiManager.OnCutsceneStopped;
                CutScene.Play();
            }
            GameEventSystem.Instance.RaiseEvent(Id);

            if (shouldChangeBGM)
            {
                if (Enum.TryParse(coreManager.sceneLoadManager.CurrentScene.ToString(), out BgmType bgmType))
                {
                    coreManager.soundManager.PlayBGM(bgmType, index:indexOfBGM);
                }
            }
        }
    }
}