# 🎇 게임 이벤트

<br/>

# 📁 폴더 구조

```
Map/                              # 맵 환경요소 모음
├── GameEvents/                   # 이벤트 발생 요소 모음
├──── SavePoint.cs                # 세이브 이벤트 발생 
├──── SpawnEnemyByIndex.cs        # 적 스폰 이벤트 발생
├── Box/                             
├──── ItemBox.cs                  # 박스 오픈 이벤트 발생 (박스 오픈 여부 저장)

Quests/                           # 퀘스트 관련 요소
├── Core/
├──── GameEventSystem.cs          # 게임 내의 이벤트 전역 관리
├──── IGameEventListener.cs       # 이벤트 발생 요소가 상속받는 인터페이스
├──── ObjectiveProgress.cs        # 퀘스트 내의 개별 임무
├──── Quest.cs                    # Objective를 모은 퀘스트
```

<br/>

---

<br>

# 💻 코드 샘플 및 주석

### 1. 게임 이벤트 통합 관리 시스템 - GameEventSystem 
- [이벤트 관련 아키텍처 설명](https://github.com/Neronem/TheLastOne_Public/blob/main/Game%20Client%20Architecture/Client%20Architecture%206_GameEventSystem.md)

```csharp
public class GameEventSystem : MonoBehaviour
{
    private static GameEventSystem instance;
    public static GameEventSystem Instance => instance ??= FindObjectOfType<GameEventSystem>();

    private readonly LinkedList<IGameEventListener> listeners = new();

    private void OnDestroy() // 파괴 시 예외처리
    {
        listeners.Clear();
        instance = null;
    }

    public void RegisterListener(IGameEventListener listener)
    {
        if (!listeners.Contains(listener))
            listeners.AddLast(listener);
    }

    public void UnregisterListener(IGameEventListener listener)
    {
        if (listeners.Contains(listener))
            listeners.Remove(listener);
    }

    public void RaiseEvent(int eventID) 
    {
        var node = listeners.First;
        while (node != null)
        {
            var current = node;
            node = node.Next;
            current.Value.OnEventRaised(eventID);
        }
    }
}
```
- 게임 내의 모든 IGameEventListener들을 통합해 관리
- 어떠한 이벤트 발생 시 모든 이벤트들을 ID 전송하며 호출
- 등록/해제 빈도가 높고, 순회 중 삭제 안정성을 고려해 LinkedList 채택
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Quests/Core/GameEventSystem.cs)


<br>

---

<br>

### 2. 이벤트 등록자 - SavePoint
```csharp
public class SavePoint : MonoBehaviour, IGameEventListener
{
    [field: Header("Save Point Id")]
    [field: Tooltip("It should be same with corresponding Spawn Trigger Id")]
    [field: SerializeField] public int Id { get; private set; }

    private void OnEnable()
    {
        GameEventSystem.Instance.RegisterListener(this);
        Service.Log($"Registered Save Point: { BaseEventIndex.BaseSavePointIndex + Id }");
    }

    private void Start()
    {
        var save = CoreManager.Instance.gameManager.SaveData;
        if (save == null || !save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info)) return;
        if (info.completionDict.TryGetValue(BaseEventIndex.BaseSavePointIndex + Id, out var value) && value) enabled = false;
    }

    private void OnDisable()
    {
        GameEventSystem.Instance.UnregisterListener(this);
        Service.Log($"Unregistered Save Point: {BaseEventIndex.BaseSavePointIndex + Id}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || !other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out Player player)) return;
        
        player.PlayerCondition.UpdateLastSavedTransform();
        
        GameEventSystem.Instance.RaiseEvent(BaseEventIndex.BaseSavePointIndex + Id);
    }

    public void OnEventRaised(int eventID)
    {
        if (eventID != BaseEventIndex.BaseSavePointIndex + Id) return;
        
        var save = CoreManager.Instance.gameManager.SaveData;
        if (save == null ||
            !save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info))
        {
            throw new MissingReferenceException("Save file not found!");
        }

        if (!info.completionDict.TryAdd(BaseEventIndex.BaseSavePointIndex + Id, true))
            info.completionDict[BaseEventIndex.BaseSavePointIndex + Id] = true;
        
        CoreManager.Instance.SaveData_QueuedAsync();
        enabled = false;
    }
}
```

- 객체 활성화 시 본인을 GameEventSystem에 등록
- 플레이어가 트리거에 접촉 시 이벤트 호출
- 이벤트 호출 시 본인의 인덱스를 DTO에 저장
- 만약 본인의 ID가 아니라면 return
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Map/GameEvents/SavePoint.cs)


<br>

---

<br>

### 3. 이벤트 등록자 - SpawnEnemyByIndex
```csharp
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
}
```
- 플레이어가 트리거에 접촉 시 이벤트 등록
- 만약 적이 사망하거나, 해킹당할 시 이벤트 호출하여 killedCount 증가
- targetCount를 넘을 시 투명벽을 해제 후 본인 비활성화
- 만약 본인의 ID가 아니라면 return
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Map/GameEvents/SpawnEnemyByIndex.cs)


<br>

---

<br>

### 4. 이벤트 등록자 - ItemBox
```csharp
public class ItemBox : MonoBehaviour, IInteractable, IGameEventListener
{
    [field: Header("References")]
    [field: Tooltip("Target Id is for the Quest")]
    [field: SerializeField] public int TargetId { get; private set; }
    [field: Tooltip("Instance Id is for the check if is opened last time.")]
    [field: SerializeField] public int InstanceId { get; private set; }
    [field: Tooltip("Changing Value greater than 0 will spawn corresponding prop.")]
    [field: SerializeField] public SerializedDictionary<BoxType, int> GenSettings { get; private set; } = new();
    
    [Header("Item Box State")]
    [SerializeField] private bool isAlreadyOpened;
    
    private CoreManager coreManager;
    private Player player;
    
    private void Awake()
    {
        if (!Animator) Animator = this.TryGetComponent<Animator>();
        if (!ParticleSystem) ParticleSystem = this.TryGetChildComponent<ParticleSystem>();
        if (SpawnPoints.Count <= 0)
        {
            SpawnPoints.AddRange(this.TryGetChildComponents<Transform>("SpawnPoints"));
            SpawnPoints.RemoveAt(0);
        }
    }

    private void Reset()
    {
        if (!Animator) Animator = this.TryGetComponent<Animator>();
        if (!ParticleSystem) ParticleSystem = this.TryGetChildComponent<ParticleSystem>();
        if (SpawnPoints.Count <= 0)
        {
            SpawnPoints.AddRange(this.TryGetChildComponents<Transform>("SpawnPoints"));
            SpawnPoints.RemoveAt(0);
        }
    }

    private void Start()
    {
        coreManager = CoreManager.Instance;
        player = coreManager.gameManager.Player;
        GameEventSystem.Instance.RegisterListener(this);
        
        var save = CoreManager.Instance.gameManager.SaveData;
        if (save is not { stageInfos: not null } ||
            !save.stageInfos.TryGetValue(CoreManager.Instance.sceneLoadManager.CurrentScene, out var info)) return;
        if (!info.completionDict.TryGetValue(BaseEventIndex.BaseItemBoxIndex + InstanceId, out var value) || value) return;
        
        GameEventSystem.Instance.UnregisterListener(this);
        isAlreadyOpened = true;
        Animator.SetTrigger(Open);
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public void OnInteract(GameObject ownerObj)
    {
        if (!ownerObj.TryGetComponent(out Player p) || isAlreadyOpened) return;
        
        isAlreadyOpened = true;
        
        PlayOpeningAnimation();
        
        GameEventSystem.Instance.RaiseEvent(TargetId);
        GameEventSystem.Instance.RaiseEvent(BaseEventIndex.BaseItemBoxIndex + InstanceId);
    }
    
    public void OnEventRaised(int eventID)
    {
        if (eventID != BaseEventIndex.BaseItemBoxIndex + InstanceId) return;
        
        var save = coreManager.gameManager.SaveData;
        if (save is { stageInfos: not null } && save.stageInfos.TryGetValue(coreManager.sceneLoadManager.CurrentScene, out var info))
            info.completionDict.TryAdd(BaseEventIndex.BaseItemBoxIndex + InstanceId, true);
        
        GameEventSystem.Instance.UnregisterListener(this);
    }
```
- 생성 시 이벤트에 본인 등록하나, 만약 DTO에서 본인이 "이미 열려있다"라는 정보 얻을 시 곧바로 해제
- 플레이어가 상호작용 시 이벤트 호출 (퀘스트에 반영하기 위한 ID와 세이브데이터에 반영하기위한 본인의 ID 동시호출)
- 이벤트 호출 시 본인이 열렸다라는 정보를 DTO에 저장 후 이벤트 해제
- 본인의 ID가 아닐시 return
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Map/Box/ItemBox.cs)

<br>

---

<br>

### 5. 이벤트 등록자 - ObjectiveProgress
```csharp
[Serializable] public class ObjectiveProgress : IGameEventListener
{
    public int questId;
    public ObjectiveData data;
    public int currentAmount;
    public bool IsActivated;
    public bool IsCompleted => currentAmount >= data.requiredAmount;
    
    public void Activate()
    {
        IsActivated = true;
        GameEventSystem.Instance.RegisterListener(this);
    }

    public void Deactivate()
    {
        IsActivated = false;
        GameEventSystem.Instance.UnregisterListener(this);
    }

    public void RemoveIncludedEvents()
    {
        data.onCompletedAction.RemoveAllListeners();
    }

    /// <summary>
    /// GameEventSystem에 등록되었을 때 이것이 불리게 되어 갱신하고 진행도 저장하는 함수
    /// </summary>
    /// <param name="eventID"></param>
    public void OnEventRaised(int eventID)
    {
        if (IsCompleted) return;

        if (data.targetID == eventID)
        {
            currentAmount++;
            CoreManager.Instance.gameManager.Player.PlayerCondition.UpdateLastSavedTransform();
            Service.Log($"[Objective] {data.description} 진행도: {currentAmount}/{data.requiredAmount}");
            CoreManager.Instance.questManager.UpdateProgress(questId, eventID);
        }
    }
}
```
- ObjectiveProgress는 Quest에서 통합 관리, Quest는 QuestManager에서 통합 관리
- 게임 시작 시 저장데이터 여부에따라 QuestManager가 Activate() 실행하여 알맞는 이벤트 등록
- QuestManager의 UpdateProgress()를 실행했을 때, IsCompleted가 true라면 이벤트 해제
- 이벤트 호출 시 currentAmount 증가 후 UpdateProgress() 호출
- 본인의 ID가 아니라면 return
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Scripts/Quests/Core/ObjectiveProgress.cs)

<br>

---

