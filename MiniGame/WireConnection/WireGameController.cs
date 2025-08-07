using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.UI.InGame;
using _1.Scripts.UI.InGame.Minigame;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Console = _1.Scripts.Map.Console.Console;
using Random = UnityEngine.Random;

namespace _1.Scripts.MiniGame.WireConnection
{
    public class WireGameController : BaseMiniGame
    {
        [field: Header("Components")]
        [SerializeField] private GameObject wirePrefab;
        [SerializeField] private GameObject socketPrefab;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [field: SerializeField] public RectTransform WireContainer { get; private set; }

        [field: Header("Game Settings")]
        [field: SerializeField] public string Description { get; private set; } = "CONNECT WIRES";
        [field: Range(2, 5)][field: SerializeField] public int SocketCount { get; private set; } = 3;
        [field: SerializeField] public float Duration { get; private set; } = 10f;
        [field: SerializeField] public float Delay { get; private set; } = 3f;
        
        private readonly List<(Socket, Socket, GameObject)> connections = new();
        private readonly List<GameObject> sockets = new();
        private WireConnectionUI wireConnectionUI;
        private CancellationTokenSource countdownCTS;
        private CancellationTokenSource endgameCTS;
        
        private void Initialize(Canvas can, WireConnectionUI ui)
        {
            canvas = can;
            top = ui.Top;
            bottom = ui.Bottom;
            WireContainer = ui.WireContainer;
        }
        
        public override void StartMiniGame(Console con, Player ply)
        {
            base.StartMiniGame(con, ply);
            
            uiManager.GetUI<MinigameUI>().SetMiniGame(Description);
            wireConnectionUI = uiManager.GetUI<MinigameUI>().GetWireConnectionUI(); 
            Initialize(uiManager.RootCanvas, wireConnectionUI); 
            wireConnectionUI.Show();
            enabled = true;
        }

        protected override void OnEnable()
        {
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); 
            endgameCTS?.Cancel(); endgameCTS?.Dispose();
            countdownCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.UiCTS.Token);
            endgameCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.UiCTS.Token);
            base.OnEnable();
        }

        protected override void Update()
        {
            if (coreManager.gameManager.IsGamePaused || isFinished) return;

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
            uiManager.GetUI<MinigameUI>().UpdateTimeSlider(remaining);
            
            if (!(Time.unscaledTime - startTime >= Duration)) return;
            FinishGame(false, IsCleared, 1.5f);
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); countdownCTS = null;
            endgameCTS?.Cancel(); endgameCTS?.Dispose(); endgameCTS = null;
            ResetAllConnections();
            ResetAllSockets();
        }

        public override void CancelMiniGame()
        {
            if (!isActiveAndEnabled || isFinished) return;
            
            // Clear all remaining sockets and line renderers
            countdownCTS?.Cancel(); countdownCTS?.Dispose(); countdownCTS = null;
            ResetAllConnections();
            ResetAllSockets();
            
            FinishGame(true);
        }
        
        private void CreateSockets()
        {
            var colorList = GetRandomColors();
            foreach (var color in colorList)
            {
                var topSocket = Instantiate(socketPrefab, top);
                
                if (topSocket.TryGetComponent(out Socket topSo)) topSo.Initialize(color, SocketType.Start); 
                if (topSocket.TryGetComponent(out WireDragger topDrag)) topDrag.Initialize(canvas, this);
                sockets.Add(topSocket);
            }
            
            Shuffle(colorList);
            foreach (var color in colorList)
            {
                var bottomSocket = Instantiate(socketPrefab, bottom);
                if (bottomSocket.TryGetComponent(out Socket bottomSo)) bottomSo.Initialize(color, SocketType.End); 
                if (bottomSocket.TryGetComponent(out WireDragger bottomDrag)) bottomDrag.Initialize(canvas, this);
                sockets.Add(bottomSocket);                                                  
            }
        }

        public GameObject CreateLine(Transform wireContainer)
        {
            GameObject go = Instantiate(wirePrefab, wireContainer);
            return go;
        }

        public void RegisterConnection(Socket start, Socket end, GameObject line)
        {
            connections.Add((start, end, line));
            if (connections.Count < SocketCount) return;
            FinishGame(false, IsCleared = true, 1.5f);
        }

        private void ResetAllConnections()
        {
            foreach (var (start, end, line) in connections)
            {
                start.IsConnected = end.IsConnected = false;
                Destroy(line);
            }
            connections.Clear();
        }

        private void ResetAllSockets()
        {
            foreach (var socket in sockets) Destroy(socket);
            sockets.Clear();
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
            
            uiManager.GetUI<MinigameUI>().ShowCountdownText(false);
            uiManager.GetUI<MinigameUI>().StartTimerUI(Duration);
            
            CreateSockets();
            IsCounting = false;
            startTime = Time.unscaledTime;
            
            countdownCTS.Dispose(); countdownCTS = null;
        }
        
        protected override async UniTask EndGame_Async(bool cancel, bool success, float duration)
        {
            Service.Log(success ? "Cleared MiniGame!" : "Better Luck NextTime");
            uiManager.GetUI<MinigameUI>().ShowEndResult(success);
            wireConnectionUI.Hide();
            await UniTask.WaitForSeconds(duration, true, cancellationToken: endgameCTS.Token, cancelImmediately: true);
            uiManager.GetUI<MinigameUI>().Hide();
            
            if (!cancel) console.OnCleared(success);
            
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
            endgameCTS.Dispose(); endgameCTS = null;
            enabled = false;
        }

        private static void Shuffle<T>(List<T> list)
        {
            var rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
        
        private List<SocketColor> GetRandomColors()
        {
            Array colors = Enum.GetValues(typeof(SocketColor));
            var colorList = colors.Cast<SocketColor>().ToList();
            for (var i = 0; i < colors.Length - SocketCount; i++)
                colorList.RemoveAt(Random.Range(0, colorList.Count));
            return colorList;
        }
    }
}