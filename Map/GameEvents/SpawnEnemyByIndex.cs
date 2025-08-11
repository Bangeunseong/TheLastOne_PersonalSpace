using System;
using System.Collections.Generic;
using _1.Scripts.Entity.Scripts.Npc.StatControllers.Base;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Quests.Core;
using _1.Scripts.Util;
using UnityEngine;
using UnityEngine.Playables;

namespace _1.Scripts.Map.GameEvents
{
    public class SpawnEnemyByIndex : MonoBehaviour, IGameEventListener
    {
        [Header("Spawn Trigger Id")]
        [Tooltip("It should be same with corresponding Save Point Id")]
        [SerializeField] private int spawnIndex;
        [SerializeField] private int killedCount;
        [SerializeField] private int targetCount;
        
        [Header("Invisible Wall")] 
        [SerializeField] private GameObject invisibleWall;

        [Header("Timeline")] 
        [SerializeField] private PlayableDirector timeline;

        [Header("Trigger Settings")] 
        [SerializeField] private bool isRelated;
        [SerializeField] private SpawnEnemyByIndex relatedTrigger;
        
        private CoreManager coreManager;
        private bool isSpawned;
        
        private void Start()
        {
            coreManager = CoreManager.Instance;
            killedCount = targetCount = 0;
            
            if (!coreManager.spawnManager.CurrentSpawnData.EnemySpawnPoints.TryGetValue(spawnIndex,
                    out var spawnPoints))
            {
                Debug.LogError("Couldn't find spawn point, Target Count is currently zero!");
                return;
            }
            foreach (var point in spawnPoints)
                targetCount += point.Key is EnemyType.ShebotRifleDuo or EnemyType.ShebotSwordDogDuo ? point.Value.Count * 2 : point.Value.Count;

            
            DataTransferObject save = coreManager.gameManager.SaveData;
            if (save == null ||
                !save.stageInfos.TryGetValue(coreManager.sceneLoadManager.CurrentScene, out var info) ||
                !info.completionDict.TryGetValue(spawnIndex + BaseEventIndex.BaseSavePointIndex + 1, out var val)) return;

            if (!val) return;
            isSpawned = true;
            enabled = false;
        }

        private void OnDisable()
        {
            GameEventSystem.Instance?.UnregisterListener(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enabled || isSpawned || !other.CompareTag("Player")) return;
            
            Debug.Log("Spawned!");
            
            isSpawned = true;
            if (invisibleWall) invisibleWall.SetActive(true);
            if (isRelated && relatedTrigger) relatedTrigger.enabled = false;
            
            coreManager.spawnManager.SpawnEnemyBySpawnData(spawnIndex);
            GameEventSystem.Instance.RegisterListener(this);
            
            if (timeline) PlayCutScene(timeline);
        }
        
        public void OnEventRaised(int eventID)
        {
            if (eventID != BaseEventIndex.BaseSpawnEnemyIndex + spawnIndex) return;
            
            killedCount++;
            if (killedCount < targetCount) return;
            
            if (timeline) coreManager.soundManager.PlayBGM(BgmType.Stage2, 0);
            if (invisibleWall) invisibleWall.SetActive(false);
            
            enabled = false;
        }
        
        private void PlayCutScene(PlayableDirector director)
        {
            director.played += OnCutsceneStarted;
            director.stopped += OnCutsceneStopped;
            director.Play();
        }
        
        private void OnCutsceneStarted(PlayableDirector director)
        {
            coreManager.gameManager.Player.InputProvider.enabled = false;
            coreManager.gameManager.PauseGame();
            
            coreManager.soundManager.PlayBGM(BgmType.Stage2, 1);
            
            coreManager.gameManager.Player.PlayerCondition.UpdateLowPassFilterValue(coreManager.gameManager.Player.PlayerCondition.HighestPoint);
            coreManager.uiManager.OnCutsceneStarted(director);
        }
        
        private void OnCutsceneStopped(PlayableDirector director)
        {
            var player = coreManager.gameManager.Player;
            player.PlayerCondition.UpdateLowPassFilterValue(player.PlayerCondition.LowestPoint + (player.PlayerCondition.HighestPoint - player.PlayerCondition.LowestPoint) * ((float)player.PlayerCondition.CurrentHealth / player.PlayerCondition.MaxHealth));
            player.InputProvider.enabled = true;
            
            coreManager.gameManager.ResumeGame();
            coreManager.uiManager.OnCutsceneStopped(director);
            
            director.played -= OnCutsceneStarted;
            director.stopped -= OnCutsceneStopped;
        }
    }
}
