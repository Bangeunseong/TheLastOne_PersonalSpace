using System;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Console = _1.Scripts.Map.Console.Console;

namespace _1.Scripts.MiniGame
{
    public abstract class BaseMiniGame : MonoBehaviour
    {
        [field: Header("Game State")]
        [field: SerializeField] public bool IsPlaying { get; protected set; }
        [field: SerializeField] public bool IsCounting { get; protected set; }
        [field: SerializeField] public bool IsCleared { get; protected set; }
        
        protected Console console;
        protected CoreManager coreManager;
        protected UIManager uiManager;
        protected Player player;
        protected float startTime;
        protected bool isFinished;

        protected virtual void Awake()
        {
            coreManager = CoreManager.Instance;
            uiManager = coreManager.uiManager;
        }
        protected virtual void Reset() { }
        protected virtual void Start() { }
        protected virtual void Update() { }
        
        protected virtual void OnEnable()
        {
            isFinished = IsPlaying = IsCounting = false;
            player.PlayerCondition.IsInMiniGame = true;
            player.PlayerCondition.OnDisablePlayerMovement();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        protected virtual void OnDisable()
        {
            player.PlayerCondition.IsInMiniGame = false;
            player.PlayerCondition.OnEnablePlayerMovement();
        }
        
        public virtual void StartMiniGame(Console con, Player ply)
        {
            console = con;
            player = ply;
        }

        public virtual void CancelMiniGame() { }
        
        protected void FinishGame(bool isCanceled, bool isSuccess = false, float duration = 0f)
        {
            // Service.Log("Finished Game");
            isFinished = true;
            _ = EndGame_Async(isCanceled, isSuccess, duration);
        }
        
        protected virtual async UniTask StartCountdown_Async() { }
        protected virtual async UniTask EndGame_Async(bool cancel, bool success, float duration) { }
    }
}