using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _1.Scripts.Manager.Core;
using _1.Scripts.UI.Loading;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace _1.Scripts.Manager.Subs
{
    [Serializable] 
    public class ResourceManager
    {
        [Header("Resources")]
        [SerializeField] private SerializedDictionary<string, Object> resources = new();
        private Dictionary<string, List<AsyncOperationHandle>> handlesByLabel = new();

        private CoreManager coreManager;
        
        public void Start()
        {
            coreManager = CoreManager.Instance;
        }

        public void OnDestroy()
        {
            foreach (var handles in handlesByLabel.Values)
            { 
                foreach (var handle in handles) Addressables.Release(handle);
            }
        }
        
        /// <summary>
        /// 씬 라벨을 기준으로 필요한 리소스들 전부 불러옴
        /// </summary>
        /// <param name="label"></param>
        /// <typeparam name="T"></typeparam>
        public async Task LoadAssetsByLabelAsync(string label)
        {
            var handle = Addressables.LoadAssetsAsync<Object>(label, null);

            while (!handle.IsDone)
            {
                float progress = handle.PercentComplete;
                coreManager.uiManager.GetUI<LoadingUI>()?.UpdateLoadingProgress(coreManager.sceneLoadManager.LoadingProgress + progress * 0.2f);
                await Task.Yield();
            }
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (!handlesByLabel.ContainsKey(label))
                    handlesByLabel[label] = new List<AsyncOperationHandle>();
                
                handlesByLabel[label].Add(handle);
                
                foreach (var asset in handle.Result)
                {
                    if (!resources.ContainsKey(asset.name))
                        resources.Add(asset.name, asset);
                }
                coreManager.sceneLoadManager.LoadingProgress += 0.2f;
            }
        }

        /// <summary>
        /// 에셋 이름 기입하고, 자료형 입력 시 반환
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAsset<T>(string name) where T : Object
        {
            if (resources.TryGetValue(name, out Object obj))
            {
                return obj as T;
            }
            return null;
        }
        
        public List<T> GetAllAssetsOfType<T>() where T : Object
        {
            var assets = new List<T>();
            foreach (var resource in resources.Values)
            {
                if (resource is T asset)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }

        /// <summary>
        /// 씬 라벨 기준으로 로드했던 자료들 언로드
        /// </summary>
        /// <param name="label"></param>
        public async Task UnloadAssetsByLabelAsync(string label)
        {
            if (!handlesByLabel.TryGetValue(label, out var handles))
            {
                // Debug.LogWarning($"can't find AsyncOperationHandle {label}");
                return;
            }

            foreach (var handle in handles)
            {
                switch (handle.Result)
                {
                    case IList<Object> list:
                        foreach (var obj in list)
                        {
                            if (obj)
                            {
                                resources.Remove(obj.name);
                            }
                        }
                        break;

                    case Object singleObj:
                        if (singleObj) resources.Remove(singleObj.name);
                        break;
                }
                
                Addressables.Release(handle);
                await Task.Yield();
            }
            
            handlesByLabel.Remove(label);
        }
    }
}
