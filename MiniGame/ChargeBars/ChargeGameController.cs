using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using _1.Scripts.UI.InGame;
using _1.Scripts.UI.InGame.Minigame;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Console = _1.Scripts.Map.Console.Console;

namespace _1.Scripts.MiniGame.ChargeBars
{
    public class ChargeGameController : BaseMiniGame
    {
        [field: Header("Components")]
        [field: SerializeField] public GameObject BarPrefab { get; private set; }
        [field: SerializeField] public RectTransform BarLayout { get; private set; }
        [field: SerializeField] public RectTransform ControlLayout { get; private set; }
        [field: SerializeField] public RectTransform TargetObj { get; private set; }
        [field: SerializeField] public RectTransform ControlObj { get; private set; }
        
        [field: Header("Game Settings")]
        [field: SerializeField] public string Description { get; private set; } = "CHARGE BARS";
        [field: Range(2, 5)][field: SerializeField] public int BarCount { get; private set; } = 3;
        [field: SerializeField] public float Duration { get; private set; } = 15f;
        [field: SerializeField] public float Delay { get; private set; } = 3f;
        [field: SerializeField] public float ChargeRate { get; private set; } = 0.25f;
        [field: SerializeField] public float LossRate { get; private set; } = 0.1f;
        [field: SerializeField] public float PenaltyRate { get; private set; } = 0.125f;
        [field: SerializeField] public float Speed { get; private set; } = 5f;
        [field: SerializeField] public int CurrentBarIndex { get; private set; }
        
        private List<Bar> bars = new();
        private Vector2 direction = Vector2.right;
        private ChargeBarUI chargeBarUI;
        private CancellationTokenSource countdownCTS;
        private CancellationTokenSource endgameCTS;
        
        private void Initialize(ChargeBarUI ui)
        {
            BarLayout = ui.BarLayout;
            ControlLayout = ui.ControlLayout;
            TargetObj = ui.TargetObj;
            ControlObj = ui.ControlObj;
        }

        protected override void OnEnable()
        {
            countdownCTS?.Cancel(); countdownCTS?.Dispose();
            endgameCTS?.Cancel(); endgameCTS?.Dispose();
            countdownCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.MapCTS.Token);
            endgameCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.MapCTS.Token);
            base.OnEnable();
        }

        public override void StartMiniGame(Console con, Player ply)
        {
            base.StartMiniGame(con, ply);
            
            // Initialize MiniGame
            uiManager.GetUI<MinigameUI>().SetMiniGame(Description);
            chargeBarUI = uiManager.GetUI<MinigameUI>().GetChargeBarUI();
            Initialize(chargeBarUI);
            enabled = true;
        }
        
        protected override void Update()
        {
            if (coreManager.gameManager.IsGamePaused || isFinished) return;

            if (!IsPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _ = StartCountdown_Async(); 
                    IsCounting = IsPlaying = true; 
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Z)) FinishGame(true);
                return;
            }
            
            if (IsCounting) return;
            
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (IsOverlapping(TargetObj, ControlObj))
                {
                    bars[CurrentBarIndex].IncreaseValue(ChargeRate);
                    RepositionTargetObj();
                } else bars[CurrentBarIndex].DecreaseValue(PenaltyRate);
            }
            if (CurrentBarIndex < BarCount)
                bars[CurrentBarIndex].DecreaseValue(LossRate * Time.unscaledDeltaTime);
            MoveControlObj();
            
            float elapsed = Time.unscaledTime - startTime;
            float remaining = Mathf.Max(0, Duration - elapsed);
            uiManager.GetUI<MinigameUI>().UpdateTimeSlider(remaining);
            
            if (!(Time.unscaledTime - startTime >= Duration)) return;
            FinishGame(false, IsCleared, 1.5f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); countdownCTS = null;
            endgameCTS?.Cancel(); endgameCTS?.Dispose(); endgameCTS = null;
            ResetAllBars();
        }

        public override void CancelMiniGame()
        {
            if (!isActiveAndEnabled || isFinished) return;
            
            // Clear all remaining bars
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); countdownCTS = null;
            ResetAllBars();

            FinishGame(true);
        }

        public void OnBarFilled()
        {
            CurrentBarIndex++;
            if (CurrentBarIndex < bars.Count) return;
            FinishGame(false, IsCleared = true, 1.5f);
        }

        private void CreateBars()
        {
            bars = new List<Bar>{Capacity = BarCount};
            
            for (var i = 0; i < BarCount; i++)
            {
                var go = Instantiate(BarPrefab, BarLayout);
                if (!go.TryGetComponent(out Bar bar)) continue;
                bar.Initialize(this);
                bars.Add(bar);
            }
        }

        private void ResetAllBars()
        {
            foreach (var bar in bars) Destroy(bar.gameObject);
            bars.Clear();
            CurrentBarIndex = 0;
        }

        private void MoveControlObj()
        {
            var offset = ControlObj.rect.width * 0.5f;
            var farLeft = ControlLayout.rect.xMin + offset;
            var farRight = ControlLayout.rect.xMax - offset;

            var delta = direction * (Speed * Time.unscaledDeltaTime);
            
            if ((ControlObj.anchoredPosition + delta).x <= farLeft) {ControlObj.anchoredPosition = new Vector2(farLeft, ControlObj.anchoredPosition.y); direction = Vector2.right;}
            else if ((ControlObj.anchoredPosition + delta).x >= farRight) {ControlObj.anchoredPosition = new Vector2(farRight, ControlObj.anchoredPosition.y); direction = Vector2.left;}
            else ControlObj.anchoredPosition += delta;
        }

        private void RepositionTargetObj()
        {
            var offset = TargetObj.rect.width * 0.5f;
            var xPos = UnityEngine.Random.Range(ControlLayout.rect.xMin + offset,  ControlLayout.rect.xMax - offset);
            var yPos = ControlLayout.rect.center.y;
            TargetObj.anchoredPosition = new Vector2(xPos, yPos);
        }
        
        private bool IsOverlapping(RectTransform rect1, RectTransform rect2)
        {
            // 바운드를 월드 공간으로 변환
            var worldCorners1 = GetWorldRect(rect1);
            var worldCorners2 = GetWorldRect(rect2);

            return worldCorners1.Overlaps(worldCorners2);
        }
        
        private static Rect GetWorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners); // 순서: 좌하, 좌상, 우상, 우하

            float x = corners[0].x;
            float y = corners[0].y;
            float width = corners[2].x - corners[0].x;
            float height = corners[2].y - corners[0].y;

            return new Rect(x, y, width, height);
        }
        
        protected override async UniTask StartCountdown_Async()
        {
            uiManager.GetUI<MinigameUI>().StartCountdownUI(Delay);
            
            var t = 0f;
            while (t < Delay)
            {
                if (!coreManager.gameManager.IsGamePaused) 
                    t += Time.unscaledDeltaTime;
                uiManager.GetUI<MinigameUI>().SetCountdownText(Delay - t);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: countdownCTS.Token, cancelImmediately: true);
            }
            chargeBarUI.Show();
            uiManager.GetUI<MinigameUI>().ShowCountdownText(false);
            uiManager.GetUI<MinigameUI>().StartTimerUI(Duration);
            
            CreateBars();
            RepositionTargetObj();
            IsCounting = false; 
            startTime = Time.unscaledTime;
            
            countdownCTS.Dispose(); countdownCTS = null;
        }

        protected override async UniTask EndGame_Async(bool cancel, bool success, float duration)
        {
            Service.Log(success ? "Cleared MiniGame!" : "Better Luck NextTime");
            uiManager.GetUI<MinigameUI>().ShowEndResult(success);
            chargeBarUI.Hide();
            await UniTask.WaitForSeconds(duration, true, cancellationToken: endgameCTS.Token, cancelImmediately: true);
            uiManager.GetUI<MinigameUI>().Hide();
            
            if (!cancel) console.OnCleared(success);
            
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
            endgameCTS.Dispose(); endgameCTS = null;
            enabled = false;
        }
    }
}