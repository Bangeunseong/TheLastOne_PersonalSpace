using System;
using System.Text;
using System.Threading;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using _1.Scripts.UI.InGame;
using _1.Scripts.UI.InGame.Minigame;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Console = _1.Scripts.Map.Console.Console;
using Random = UnityEngine.Random;

namespace _1.Scripts.MiniGame.AlphabetMatch
{
    public class AlphabetGameController : BaseMiniGame
    {
        [field: Header("Game Settings")]
        [field: SerializeField] public int AlphabetLength { get; private set; } = 3;
        [field: SerializeField] public float Duration { get; private set; } = 5f;
        [field: SerializeField] public float Delay { get; private set; } = 3f;
        [field: SerializeField] public bool IsLoop { get; private set; }
        [field: SerializeField] public int LoopCount { get; private set; } = 2;
        
        [field: Header("Current Game State")]
        [field: SerializeField] public string Description { get; private set; } = "PASSWORD";
        [field: SerializeField] public string CurrentAlphabets { get; private set; }
        [field: SerializeField] public int CurrentIndex { get; private set; }
        [field: SerializeField] public int CurrentLoopCount { get; private set; }

        private AlphabetMatchingUI alphabetUI;
        private CancellationTokenSource countdownCTS;
        private CancellationTokenSource endgameCTS;
        
        protected override void OnEnable()
        {
            countdownCTS?.Cancel(); countdownCTS?.Dispose();
            endgameCTS?.Cancel(); endgameCTS?.Dispose();
            countdownCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.MapCTS.Token);
            endgameCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.MapCTS.Token);
            
            CurrentAlphabets = GetAlphabets();
            CurrentLoopCount = 0;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); countdownCTS = null;
            endgameCTS?.Cancel(); endgameCTS?.Dispose(); endgameCTS = null;
        }

        protected override void Update()
        {
            if (coreManager.gameManager.IsGamePaused || isFinished) return;
            
            // Minigame 초입
            if (!IsPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    _ = StartCountdown_Async();
                    IsCounting = IsPlaying = true;
                    return;
                }
                
                if (Input.GetKeyDown(KeyCode.Z)) FinishGame(true);
                return;
            }

            if (IsCounting) return;
            
            float elapsed = Time.unscaledTime - startTime;
            float remaining = Mathf.Max(0, Duration - elapsed);
            coreManager.uiManager.GetUI<MinigameUI>().UpdateTimeSlider(remaining);
            
            // Minigame 달성 여부 확인
            if (CurrentIndex >= AlphabetLength)
            {
                CurrentLoopCount++;
                if (!IsLoop || CurrentLoopCount >= LoopCount)
                {
                    FinishGame(false, IsCleared = true, 1.5f); return;
                }
                ResetGame();
                return;
            }
            
            // Minigame 메인 로직
            if (Time.unscaledTime - startTime >= Duration)
            {
                FinishGame(false, IsCleared, 1.5f); return;
            }
            if (!Input.anyKeyDown) return;
            if (Input.inputString == null) return;
            if (string.Compare(Input.inputString, CurrentAlphabets[CurrentIndex].ToString(),
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                alphabetUI.AlphabetAnim(CurrentIndex, true);
                CurrentIndex++;
            } else alphabetUI.AlphabetAnim(CurrentIndex, false);
        }

        public override void StartMiniGame(Console con, Player ply)
        {
            base.StartMiniGame(con, ply);
            
            uiManager.GetUI<MinigameUI>().SetMiniGame();
            uiManager.GetUI<MinigameUI>().SetDescriptionText(Description);
            alphabetUI = uiManager.GetUI<MinigameUI>().GetAlphabetMatchingUI();
            alphabetUI.ResetUI();
            enabled = true;
        }

        public override void CancelMiniGame()
        {
            if (!isActiveAndEnabled || isFinished) return;
            
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); countdownCTS = null;
            FinishGame(true);
        }
        
        private void ResetGame()
        {
            startTime = Time.unscaledTime;
            CurrentAlphabets = GetAlphabets();
            uiManager.GetUI<MinigameUI>().ShowPanel();
            uiManager.GetUI<MinigameUI>().ShowLoopText(IsLoop);
            if (IsLoop && LoopCount > 0) 
                uiManager.GetUI<MinigameUI>().UpdateLoopCount(CurrentLoopCount + 1, LoopCount);
            alphabetUI.CreateAlphabet(CurrentAlphabets);
            alphabetUI.ShowAlphabet(true);
            CurrentIndex = 0;
        }

        private string GetAlphabets()
        {
            StringBuilder builder = new();
            for (var i = 0; i < AlphabetLength; i++) builder.Append($"{(char)Random.Range('A', 'Z' + 1)}");
            return builder.ToString();
        }

        protected override async UniTask StartCountdown_Async()
        {
            uiManager.GetUI<MinigameUI>().StartCountdownUI(Delay);
            uiManager.GetUI<MinigameUI>().ShowAlphabetMatching(true);
            
            var t = 0f;
            while (t < Delay)
            {
                if (!coreManager.gameManager.IsGamePaused) t += Time.unscaledDeltaTime;
                uiManager.GetUI<MinigameUI>().SetCountdownText(Delay - t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: countdownCTS.Token, cancelImmediately: true);
            }
            
            uiManager.GetUI<MinigameUI>().ShowCountdownText(false);
            
            alphabetUI.CreateAlphabet(CurrentAlphabets);
            alphabetUI.ShowAlphabet(true);
            uiManager.GetUI<MinigameUI>().StartTimerUI(Duration);
            if (IsLoop && LoopCount > 0)
                uiManager.GetUI<MinigameUI>().UpdateLoopCount(CurrentLoopCount + 1, LoopCount);
            
            CurrentIndex = 0;
            IsCounting = false; 
            startTime = Time.unscaledTime;
            
            countdownCTS.Dispose(); countdownCTS = null;
        }

        protected override async UniTask EndGame_Async(bool cancel, bool success, float duration)
        {
            uiManager.GetUI<MinigameUI>().ShowEndResult(success);
            alphabetUI.ShowAlphabet(false);
            await UniTask.WaitForSeconds(duration, true, cancellationToken: endgameCTS.Token, cancelImmediately: true);
            uiManager.HideUI<MinigameUI>();
            alphabetUI = null;
            
            if (!cancel) console.OnCleared(success);
            
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
            endgameCTS.Dispose(); endgameCTS = null;
            enabled = false;
        }
    }
}