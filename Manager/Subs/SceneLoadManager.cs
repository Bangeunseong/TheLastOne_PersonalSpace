using System;
using System.Threading.Tasks;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Manager.Core;
using _1.Scripts.UI.Common;
using _1.Scripts.UI.Loading;
using _1.Scripts.UI.Lobby;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace _1.Scripts.Manager.Subs
{
    [Serializable] public enum SceneType
    {
        IntroScene, 
        Loading, 
        Stage1,
        Stage2,
        EndingScene,
    }
    
    [Serializable] public class SceneLoadManager
    {
        [field: Header("Scene Info.")]
        [field: SerializeField, ReadOnly] public SceneType PreviousScene { get; private set; }
        [field: SerializeField, ReadOnly] public SceneType CurrentScene { get; private set; }
        
        // Fields
        private AsyncOperation sceneLoad;
        private bool isInputAllowed;
        private bool isKeyPressed;
        private UIManager uiManager;
        private CoreManager coreManager;
        
        // Properties
        public bool IsLoading { get; private set; }
        public float LoadingProgress { get; set; }
        
        // Methods
        public void Start()
        {
            coreManager = CoreManager.Instance;
            uiManager = CoreManager.Instance.uiManager;
            CurrentScene = SceneType.IntroScene;
        }

        public void Update()
        {
            if (!isInputAllowed) return;
            if (Input.anyKeyDown) isKeyPressed = true;
        }

        public async Task OpenScene(SceneType sceneName)
        {
            IsLoading = true;
            PreviousScene = CurrentScene;
            
            // Reset All Managers
            coreManager.soundManager.StopBGM();
            coreManager.objectPoolManager.ReleaseAll();
            coreManager.spawnManager.ClearAllSpawnedEnemies();
            coreManager.uiManager.HideHUD();
            coreManager.uiManager.ResetUIByGroup(UIType.InGame);
            uiManager.UnregisterDynamicUIByGroup(UIType.InGame);
            
            // Remove all remain resources that belongs to previous scene
            if (PreviousScene != sceneName)
            {
                await coreManager.objectPoolManager.DestroyUnusedStagePools(PreviousScene.ToString());
                await coreManager.resourceManager.UnloadAssetsByLabelAsync(PreviousScene.ToString());
                CurrentScene = sceneName;
                if (CurrentScene == SceneType.IntroScene)
                {
                    await coreManager.objectPoolManager.DestroyUnusedStagePools("Common");
                    await coreManager.resourceManager.UnloadAssetsByLabelAsync("Common");
                }
            }
            
            // Hide Lobby and Show Loading
            uiManager.HideUI<LobbyUI>();
            uiManager.ShowUI<LoadingUI>();
            
            var loadingScene = 
                SceneManager.LoadSceneAsync(coreManager.IsDebug ? 
                    coreManager.DebugPrefix + nameof(SceneType.Loading) : nameof(SceneType.Loading));
            while (!loadingScene!.isDone) { await Task.Yield(); }
            
            // Update Loading Progress
            LoadingProgress = 0f;
            var loadingUI = uiManager.GetUI<LoadingUI>();
            loadingUI.UpdateLoadingProgress(LoadingProgress);
            
            Debug.Log("Resource and Scene Load Started!");
            if (PreviousScene == SceneType.IntroScene)
            {
                // Load Common Resource used in Game Scene
                await coreManager.resourceManager.LoadAssetsByLabelAsync("Common");
            }
            else
            {
                LoadingProgress = 0.4f;
                loadingUI.UpdateLoadingProgress(LoadingProgress);
            }
            
            // Load Resources & Create Pool used in Current Scene
            await coreManager.resourceManager.LoadAssetsByLabelAsync(CurrentScene.ToString());
            coreManager.dialogueManager.CacheDialogueData();
            coreManager.soundManager.CacheSoundGroup();
            await coreManager.soundManager.LoadClips();
            await coreManager.objectPoolManager.CreatePoolsFromResourceBySceneLabelAsync(CurrentScene.ToString());
            
            // Load Scene
            await LoadSceneWithProgress(CurrentScene);
        }
        
        /// <summary>
        /// Current Scene이 로드되는 Task (sceneLoaded event가 실행된다)
        /// </summary>
        /// <param name="sceneName"></param>
        private async Task LoadSceneWithProgress(SceneType sceneName)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            var loadingUI = uiManager.GetUI<LoadingUI>();
            
            sceneLoad = SceneManager.LoadSceneAsync(coreManager.IsDebug ? coreManager.DebugPrefix + sceneName : sceneName.ToString());
            sceneLoad!.allowSceneActivation = false;
            while (sceneLoad.progress < 0.9f)
            {
                loadingUI.UpdateLoadingProgress(LoadingProgress + sceneLoad.progress * 0.2f);
                await Task.Yield();
            }
            LoadingProgress = 1f;
            
            // Wait for user input
            isInputAllowed = true;
            loadingUI.UpdateLoadingProgress(LoadingProgress);
            loadingUI.UpdateProgressText("Press any key to continue...");
            await WaitForUserInput();
            isInputAllowed = false;
            isKeyPressed = false;
            
            sceneLoad!.allowSceneActivation = true;
            
            while (sceneLoad is { isDone: false }) 
            {
                await Task.Yield();
            }
            
            IsLoading = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 씬 이동 시 내부에 있는 더미 건 컴포넌트를 찾아 저장 기능 부여, 나중에 특정 트리거를 찾아 저장하는 기능도 추가 가능
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Step 0. Always dispose all UniTask left in Enemies (It remains in ObjectPool, so remaining tasks need to be disposed)
            // Then, create new CTS(Cancellation Token Source)
            coreManager.spawnManager.DisposeAllUniTasksFromSpawnedEnemies();
            coreManager.CreateNewCTS();
            
            // Check if the player is going to intro or ending scene (loading is obsolete)
            switch (CurrentScene)
            {
                case SceneType.IntroScene: 
                    coreManager.soundManager.PlayBGM(BgmType.Lobby, 0);
                    uiManager.HideUI<LoadingUI>(); uiManager.ShowUI<LobbyUI>();
                    return;
                case SceneType.EndingScene: 
                    coreManager.uiManager.GetUI<EndingCreditUI>().Show();
                    return;
            }
            
            // Notice!! : 이 밑에 넣을 코드들은 본 게임에서 쓰일 것들만 넣기
            // Step 1. Find Player in scene
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null || !playerObj.TryGetComponent(out Player player)) 
                throw new MissingComponentException("Player not found");
            
            // Step 2. Initialize Player, Spawn Mobs, Initialize Quests
            coreManager.gameManager.Initialize_Player(player);
            coreManager.spawnManager.ChangeSpawnDataAndInstantiate(CurrentScene, coreManager.gameManager.SaveData);
            coreManager.questManager.Initialize(coreManager.gameManager.SaveData);
            
            // Step 3. Hide Loading UI & Register InGame UIs
            uiManager.HideUI<LoadingUI>();
            uiManager.RegisterDynamicUIByGroup(UIType.InGame);

            // Step 4. Check if there is a cutscene and intro is played, if not play intro.
            // Then, start the game
            switch (CurrentScene)
            {
                case SceneType.Stage1: 
                    // Play Cutscene If needed
                    PlayCutSceneOrResumeGame(player, true); break;
                case SceneType.Stage2:
                    PlayCutSceneOrResumeGame(player); break;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private async Task WaitForUserInput()
        {
            while (!isKeyPressed)
            {
                await Task.Yield();
            }
        }

        private void PlayCutSceneOrResumeGame(Player player, bool spawn = false, int index = 1)
        {
            var introGo = GameObject.Find("IntroOpening");
            var playable = introGo?.GetComponentInChildren<PlayableDirector>();
            if (!playable || coreManager.gameManager.SaveData != null &&
                coreManager.gameManager.SaveData.stageInfos.TryGetValue(CurrentScene, out var info) &&
                info.isIntroPlayed)
            {
                ResumeGame(player, spawn, index); return;
            }

            PlayCutScene(playable);
        }

        private void PlayCutScene(PlayableDirector director)
        {
            
            if (CurrentScene == SceneType.Stage1)
            {
                director.played += OnCutsceneStarted_Stage1Intro;
                director.stopped += OnCutsceneStopped_Stage1Intro;
            }
            else if (CurrentScene == SceneType.Stage2)
            {
                director.played += OnCutsceneStarted_Stage2Intro;
                director.stopped += OnCutsceneStopped_Stage2Intro;
            }
            director.Play();
        }

        private void ResumeGame(Player player, bool spawn, int index)
        {
            ChangeBGM(0);
            player.PlayerCondition.OnEnablePlayerMovement();
            if (!coreManager.uiManager.ShowHUD()) throw new MissingReferenceException();
            if (spawn) coreManager.spawnManager.SpawnEnemyBySpawnData(index);
        }

        private void ChangeBGM(int index)
        {
            if (Enum.TryParse(CurrentScene.ToString(), out BgmType bgmType)) 
                coreManager.soundManager.PlayBGM(bgmType, index: index);
        }

        private void OnCutsceneStarted_Stage1Intro(PlayableDirector director)
        {
            ChangeBGM(0);
            coreManager.gameManager.Player.InputProvider.enabled = false;
            coreManager.gameManager.PauseGame();
            coreManager.gameManager.Player.PlayerCondition.UpdateLowPassFilterValue(coreManager.gameManager.Player.PlayerCondition.HighestPoint);
            coreManager.uiManager.OnCutsceneStarted(director);
        }

        private void OnCutsceneStarted_Stage2Intro(PlayableDirector director)
        {
            coreManager.gameManager.Player.InputProvider.enabled = false;
            coreManager.gameManager.PauseGame();
            coreManager.gameManager.Player.PlayerCondition.UpdateLowPassFilterValue(coreManager.gameManager.Player.PlayerCondition.HighestPoint);
            coreManager.uiManager.OnCutsceneStarted(director);
        }

        private void OnCutsceneStopped_Stage1Intro(PlayableDirector director)
        {
            var player = coreManager.gameManager.Player;
            player.PlayerCondition.UpdateLowPassFilterValue(player.PlayerCondition.LowestPoint + (player.PlayerCondition.HighestPoint - player.PlayerCondition.LowestPoint) * ((float)player.PlayerCondition.CurrentHealth / player.PlayerCondition.MaxHealth));
            player.InputProvider.enabled = true;
            
            coreManager.spawnManager.SpawnEnemyBySpawnData(1);
            coreManager.gameManager.ResumeGame();
            coreManager.uiManager.OnCutsceneStopped(director);
            
            director.played -= OnCutsceneStarted_Stage1Intro;
            director.stopped -= OnCutsceneStopped_Stage1Intro;
        }

        private void OnCutsceneStopped_Stage2Intro(PlayableDirector director)
        {
            var player = coreManager.gameManager.Player;
            player.PlayerCondition.UpdateLowPassFilterValue(player.PlayerCondition.LowestPoint + (player.PlayerCondition.HighestPoint - player.PlayerCondition.LowestPoint) * ((float)player.PlayerCondition.CurrentHealth / player.PlayerCondition.MaxHealth));
            player.InputProvider.enabled = true;
            
            ChangeBGM(0);
            coreManager.gameManager.ResumeGame();
            coreManager.uiManager.OnCutsceneStopped(director);
            
            director.played -= OnCutsceneStarted_Stage1Intro;
            director.stopped -= OnCutsceneStopped_Stage2Intro;
        }
    }
}
