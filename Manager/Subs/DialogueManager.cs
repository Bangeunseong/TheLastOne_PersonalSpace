using System;
using System.Collections.Generic;
using _1.Scripts.Dialogue;
using _1.Scripts.Manager.Core;
using _1.Scripts.UI.InGame.Dialogue;
using _1.Scripts.Util;
using UnityEngine;

namespace _1.Scripts.Manager.Subs
{
    [Serializable] public class DialogueManager
    {
        [SerializeField] private List<DialogueDataSO> dialogueDataList = new();
        private Dictionary<int, DialogueDataSO> dialogueDataDict = new();
        private CoreManager coreManager;

        public void Start()
        {
            coreManager = CoreManager.Instance;

        }
        public bool HasPlayed(int dialogueKey)
        {
            int saveKey = BaseEventIndex.BaseDialogueIndex + dialogueKey;
            var sceneType = coreManager.sceneLoadManager.CurrentScene;
            return CoreManager.Instance.gameManager.SaveData.stageInfos[sceneType].completionDict.TryGetValue(saveKey, out var done) && done;
        }
        public void MarkAsPlayed(int dialogueKey)
        {
            int saveKey = BaseEventIndex.BaseDialogueIndex + dialogueKey;
            var sceneType = coreManager.sceneLoadManager.CurrentScene;
            CoreManager.Instance.gameManager.SaveData.stageInfos[sceneType].completionDict[saveKey] = true;
        }

        public void CacheDialogueData()
        {
            dialogueDataDict = new Dictionary<int, DialogueDataSO>();
            dialogueDataList.Clear();
            
            var loadedSO = coreManager.resourceManager.GetAllAssetsOfType<DialogueDataSO>();
            if (loadedSO == null || loadedSO.Count == 0) return;
            
            foreach (var so in loadedSO)
            {
                if (so && !dialogueDataDict.ContainsKey(so.dialogueKey))
                {
                    dialogueDataList.Add(so);
                    dialogueDataDict.Add(so.dialogueKey, so);
                }
            }
        }

        public void TriggerDialogue(int key)
        {
            if (!dialogueDataDict.TryGetValue(key, out var data)) 
            {
                Debug.LogWarning($"Dialogue key not found: {key}");
                return;
            }

            var dialogueUI = coreManager.uiManager.GetUI<DialogueUI>();
            if (dialogueUI)
                dialogueUI.ShowSequence(data.sequence);
            else
                Debug.LogWarning("DialogueUI is not available.");
        }
    }
}