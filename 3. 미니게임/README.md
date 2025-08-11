# 🧐 MiniGame 구현

<br/>

# 📁 폴더 구조

```
MiniGame/                           # MiniGame 관련 코드 모음
├── AlphbetMatch/                   # 알파벳 매칭 미니게임 관련 코드 모음
├── ChargeBars/                     # 바 채우기 미니게임 관련 코드 모음
└── WireConnection/                 # 알맞은 선 잇기 게임 관련 코드 모음

```

<br/>

# 💻 코드 샘플 및 주석
### 1. MiniGame 구조
- 각 미니게임들은 [BaseMiniGame.cs](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/tree/master/Scripts/MiniGame)내 `BaseMiniGame` 클래스를 상속받아 만들어 졌다.
- `BaseMiniGame` 클래스는 추상 클래스로 각 미니게임 클래스들은 오버라이딩 방식으로 함수를 구현하여 기능을 추가해야 한다.
- Awake, Start, Update, Reset과 같은 Monobehavior 함수는 각자 알아서 필요하면 구현하도록 virtual 함수로 만들었으면 모든 미니게임에 공통적으로 필요한 코드인 Player Movement 제한 코드 및 Cursor Lock 및 Visibility 제어는 추가해놨다.
- `StartMiniGame` 함수를 호출하여 게임을 시작하며 `CancelMiniGame`을 통해 미니게임을 도중에 중단할 수 있다.
- 모든 비동기적인 활동은 UniTask를 통해 이루어지며 CancellationToken을 통해 중단이 된다.
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/MiniGame/BaseMiniGame.cs)

```csharp
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

        protected virtual async UniTask StartCountdown_Async() { await UniTask.Yield(); }
        protected virtual async UniTask EndGame_Async(bool cancel, bool success, float duration) { await UniTask.Yield(); }
    }
}
```

### 2. MiniGame 작동 방식
- 각 미니게임은 자신만의 Controller가 존재하여 Update 구문을 통해 입력을 읽고 타임을 계산하여 제한시간 안에 성공을 했는지 여부를 판단한다.
- 만약 제한시간 안에 미니게임을 성공하면 `EndGame_Async` 메소드를 실행하여 비동기적으로 게임 완료 메시지를 출력하고 게임을 종료한다.(게임 종료 시 이벤트가 연결되어 있다면 이벤트 실행)
- 제한시간 안에 성공을 못할 시 실패 메시지 출력 후 자동 종료된다. (이벤트 실행 X)
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/MiniGame/AlphabetMatch/AlphabetGameController.cs)

```csharp
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
```