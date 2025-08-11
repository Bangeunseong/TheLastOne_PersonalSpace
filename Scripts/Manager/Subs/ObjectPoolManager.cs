using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _1.Scripts.Manager.Core;
using _1.Scripts.Static;
using _1.Scripts.UI.Loading;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace _1.Scripts.Manager.Subs
{
    /// <summary>
    /// 풀매니저 (풀 생성 및 관리 담당)
    /// </summary>
    [Serializable] 
    public class ObjectPoolManager
    {
        private Dictionary<string, ObjectPool<GameObject>> pools = new(); // 풀들 모음
        private Dictionary<string, HashSet<GameObject>> activeObjects = new(); // Get()으로 빠져나간 Clone을 추적하기위해 만듬, HashSet으로 한 이유 1. 성능 2. 중복방지
        private Dictionary<string, HashSet<string>> scenePrefabMap = new() // 풀로 만들 프리팹들의 정보 모아놓은 딕셔너리
        {
            { "Stage1", PoolableGameObjects_Stage1.prefabs },
            { "Stage2", PoolableGameObjects_Stage2.prefabs },
            { "Common", PoolableGameObjects_Common.prefabs },
        };
            
        private Transform poolRoot; // 풀들 부모
        private CoreManager coreManager;
        
        private int defaultCapacity = 10; // 용량설정
        private int maxCapacity = 500;
        
        /// <summary>
        /// 풀매니저 생성자, poolRoot 생성
        /// </summary>
        public void Start()
        {
            coreManager = CoreManager.Instance;
            poolRoot = new GameObject("ObjectPoolManager").transform;
            poolRoot.SetParent(CoreManager.Instance.transform);
        }
        
        /// <summary>
        /// 오브젝트풀 생성
        /// 최소/최대 용량지정은 해도되고 안해도됨
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="capacity"></param>
        /// <param name="maxSize"></param>
        public void CreatePool(GameObject prefab, int capacity = -1, int maxSize = -1)
        {
            if (pools.ContainsKey(prefab.name))
            {
                return;
            }
    
            int poolCapacity = capacity > 0 ? capacity : defaultCapacity;
            int poolMaxSize = maxSize > 0 ? maxSize : maxCapacity;

            Transform parent = new GameObject($"{prefab.name}_Parent").transform;
            parent.SetParent(poolRoot);
            
            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject obj = UnityEngine.Object.Instantiate(prefab, parent);

                    if (obj.TryGetComponent(out NavMeshAgent agent))
                    {
                        agent.enabled = false;
                    }

                    return obj;
                },
                actionOnGet: item => item.gameObject.SetActive(true),
                actionOnRelease: item =>
                {
                    item.gameObject.SetActive(false);
                    item.transform.SetParent(parent);
                },
                actionOnDestroy: item => UnityEngine.Object.Destroy(item.gameObject),
                collectionCheck: false,
                defaultCapacity: poolCapacity,
                maxSize: poolMaxSize);

            List<GameObject> preloadedObjects = new List<GameObject>(poolCapacity);
            for (int i = 0; i < poolCapacity; ++i)
            {
                GameObject obj = pool.Get();
                preloadedObjects.Add(obj);
            }

            foreach (var obj in preloadedObjects)
            {
                pool.Release(obj);
            }

            pools[prefab.name] = pool;
            activeObjects[prefab.name] = new HashSet<GameObject>();
        }

        /// <summary>
        /// 풀에서 꺼내오기, 만약 풀에 없을시엔 Instantiate
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public GameObject Get(string prefabName)
        {
            // 1. 풀 있으면 풀에서 꺼냄
            if (pools.TryGetValue(prefabName, out ObjectPool<GameObject> pool))
            {
                GameObject obj = pool.Get();
                activeObjects[prefabName].Add(obj);
                return obj;
            }

            // 2. 풀 없으면 Instantiate로 새로 생성만 해서 리턴
            var prefab = coreManager.resourceManager.GetAsset<GameObject>(prefabName);
            if (prefab) return UnityEngine.Object.Instantiate(prefab);
            Debug.LogWarning($"리소스에서 '{prefabName}' 프리팹을 찾을 수 없음.");
            return null;
        }
        
        /// <summary>
        /// 풀에 반환하기, 풀 없을시엔 그냥 파괴
        /// </summary>
        /// <param name="obj"></param>
        public void Release(GameObject obj)
        {
            string originalName = obj.name;
            string postfix = "(Clone)";
            string cleanedName = "";
            
            if (obj.name.EndsWith(postfix)) { cleanedName = originalName.Substring(0, originalName.Length - postfix.Length); }
            
            if (pools.TryGetValue(cleanedName, out ObjectPool<GameObject> pool))
            {
                // Service.Log("Found Pool");
                if (activeObjects.TryGetValue(cleanedName, out HashSet<GameObject> set))
                {
                    set.Remove(obj);
                }
                pool.Release(obj);
            }
            else
            {
                // 풀 없으면 그냥 파괴
                UnityEngine.Object.Destroy(obj);
            }
        }
        
        /// <summary>
        /// 씬 전환 전 풀에서 꺼내간 것 전부 반환
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var kvp in pools)
            {
                string prefabName = kvp.Key;
                ObjectPool<GameObject> pool = kvp.Value;

                if (activeObjects.TryGetValue(prefabName, out HashSet<GameObject> set))
                {
                    foreach (GameObject obj in set)
                    {
                        pool.Release(obj);
                    }
                    set.Clear();
                }
            }
        }
        
        /// <summary>
        /// 현재 씬에서 사용하지 않는 풀은 삭제 (메모리 최적화)
        /// </summary>
        public async Task DestroyUnusedStagePools(string currentScene) // 이 currentScene은 다음씬으로 넘어가기 전의 현재 씬
        {
            ReleaseAll();
            
            if (scenePrefabMap.TryGetValue(currentScene, out HashSet<string> prefabsToDestroy)) // 현재 씬 기준으로 필요한 프리팹들
            {
                foreach (string prefabName in prefabsToDestroy)
                {
                    pools.Remove(prefabName); // 풀에서 삭제
                    activeObjects.Remove(prefabName); // 추적 해시에서 삭제
                    
                    // 부모오브젝트 찾아서 삭제
                    Transform parent = coreManager.GetComponentInChildrenOfTarget<Transform>(
                        poolRoot.gameObject, $"{prefabName}_Parent", true);
                    if (parent) { UnityEngine.Object.Destroy(parent.gameObject); }
                    // Service.Log($"{parent.name}");
                    
                    await Task.Yield(); // 한프레임 양보 (파괴작업이니까)
                }
            }
        }
        
        /// <summary>
        /// 현재 씬 이름에 따라 알맞은 풀 생성
        /// </summary>
        /// <param name="sceneName"></param>
        public async Task CreatePoolsFromResourceBySceneLabelAsync(string sceneName)
        {
            if (Enum.TryParse(sceneName, out SceneType sceneType))
            {
                if (sceneType != SceneType.IntroScene)
                {
                    var commonSet = new HashSet<string>(PoolableGameObjects_Common.prefabs);
                    if (!commonSet.IsSubsetOf(pools.Keys)) // 교집합 계산 (비용 적음)
                    {
                        await CreatePoolsFromListAsync(PoolableGameObjects_Common.prefabs); // 없다면 생성
                    }
                }
            }
            coreManager.sceneLoadManager.LoadingProgress += 0.2f;
            
            if (!scenePrefabMap.TryGetValue(sceneName, out HashSet<string> prefabsToLoad))
            {
                // 풀 만들기가 필요없는 씬
                return;
            }
            await CreatePoolsFromListAsync(prefabsToLoad);
            coreManager.sceneLoadManager.LoadingProgress += 0.2f;
        }
        
        /// <summary>
        /// 풀 생성 메소드
        /// </summary>
        /// <param name="prefabNames"></param>
        private async Task CreatePoolsFromListAsync(HashSet<string> prefabNames)
        {
            prefabNames.ExceptWith(pools.Keys); // 이미 있는 풀 제거, ContainsKey 대신 비용절감을 위해 사용
            
            int total = prefabNames.Count;
            int current = 0;
            
            foreach (string prefabName in prefabNames)
            {
                GameObject prefab = CoreManager.Instance.resourceManager.GetAsset<GameObject>(prefabName);
                if (!prefab)
                {
                    // Debug.LogWarning($"리소스에서 '{prefabName}' 프리팹을 찾을 수 없음.");
                    continue;
                }

                CreatePool(prefab);
                current++;
                
                float progress = (float)current / total;
                coreManager.uiManager.GetUI<LoadingUI>()?.UpdateLoadingProgress(coreManager.sceneLoadManager.LoadingProgress + progress * 0.2f);
                await Task.Yield(); 
            }
        }
    }
}