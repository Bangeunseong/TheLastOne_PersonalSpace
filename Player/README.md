# 🧐 Player 구현방식

<br/>

# 📁 폴더 구조

```
Player/                     # Player 관련 기능 모음 폴더
├── Core/                   # Player의 주요 기능 모음 폴더 (ex: Player.cs, PlayerCondition.cs)
├── Data/                   # Player 캐릭터의 체력, 스태미나 등 정보 모음 폴더
└── StateMachineScripts/    # Player 캐릭터의 FSM
  └── States/               # Player의 상태 모음 폴더
    ├── Air/                # Player가 공중에 있을 때 상태 모음 폴더
    └── Ground/             # Player가 지상에 있을 때 상태 모음 폴더
```

<br/>

# 💻 코드 샘플 및 주석

### 1. 플레이어의 현재 상태 관리
- SaveFile이 먼저 존재하는지 확인한 후 존재할 시 SaveFile의 정보를 토대로 Initialize 진행
- 없을 시 기존의 PlayerStatData를 베이스로 Initialize 진행
- 데미지 처리는 PlayerDamageReceiver를 통해 선언된 부위별 DamageConverter들이 계산된 Damage를 PlayerCondition의 OnTakeDamage를 호출하여 전달하여 PlayerCondition에서 갱신
- 플레이어의 조준 여부, 공격 여부, 움직일 수 있는 상태 여부 등 여러가지 상태들을 저장하여 전체적인 사격, 조준, 스킬 사용, 무기 교체 등 여러가지 기능의 수행조건을 부여.
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/Core/PlayerCondition.cs)
``` 
namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerCondition : MonoBehaviour, IBleedable
    {
        너무 많아서 생략...

        /// <summary>
        /// Initialize Player Stat., using Saved data if exists.
        /// </summary>
        /// <param name="data">DataTransferObject of Saved Data</param>
        public void Initialize(DataTransferObject data)
        {
            coreManager = CoreManager.Instance;
            StatData = coreManager.resourceManager.GetAsset<PlayerStatData>("Player");
            
            if (data == null)
            {
                Service.Log("DataTransferObject is null");
                MaxHealth = StatData.maxHealth;
                CurrentHealth = 10;
                MaxStamina = CurrentStamina = StatData.maxStamina;
                MaxShield = (int)StatData.maxArmor; CurrentShield = 0;
                Damage = StatData.baseDamage;
                AttackRate = StatData.baseAttackRate;
                CurrentFocusGauge = CurrentInstinctGauge = 0f;
                UpdateLastSavedTransform();
            }
            else
            {
                Service.Log("DataTransferObject is not null");
                MaxHealth = data.characterInfo.maxHealth; CurrentHealth = data.characterInfo.health;
                MaxStamina = data.characterInfo.maxStamina; CurrentStamina = data.characterInfo.stamina;
                MaxShield = data.characterInfo.maxShield; CurrentShield = data.characterInfo.shield;
                AttackRate = data.characterInfo.attackRate; Damage = data.characterInfo.damage;
                CurrentFocusGauge = data.characterInfo.focusGauge;
                CurrentInstinctGauge = data.characterInfo.instinctGauge;

                if (data.stageInfos.TryGetValue(coreManager.sceneLoadManager.CurrentScene, out var info))
                {
                    LastSavedPosition = info.currentCharacterPosition.ToVector3(); 
                    LastSavedRotation = info.currentCharacterRotation.ToQuaternion(); 
                    transform.SetPositionAndRotation(LastSavedPosition, LastSavedRotation); 
                    Service.Log(LastSavedPosition + "," +  LastSavedRotation);
                }
                else
                {
                    UpdateLastSavedTransform();
                }
                
                UpdateLowPassFilterValue(LowestPoint + (HighestPoint - LowestPoint) * ((float)CurrentHealth / MaxHealth));
            }

            Speed = StatData.moveSpeed;
            JumpForce = StatData.jumpHeight;
            CrouchSpeedModifier = StatData.crouchMultiplier;
            WalkSpeedModifier = StatData.walkMultiplier;
            RunSpeedModifier = StatData.runMultiplier;
            AirSpeedModifier = StatData.airMultiplier;
            
            UpdateMouseSensitivity(PlayerPrefs.GetFloat("LookSensitivity_H", 0.1f), PlayerPrefs.GetFloat("LookSensitivity_V", 0.06f),
                PlayerPrefs.GetFloat("AimSensitivity_H", 0.1f), PlayerPrefs.GetFloat("AimSensitivity_V", 0.06f));
            ChangeMouseSensitivity(false);

            OnInstinctRecover_Idle();
            player.Controller.enabled = true;
        }

        너무 많아서 생략...

        /// <summary>
        /// Reduce Health Point, Can customize event when player got damage using 'OnDamage' event
        /// </summary>
        /// <param name="damage">Value of damage</param>
        public void OnTakeDamage(int damage)
        {
            if (IsDead) return;
            if (CurrentShield <= 0)
            {
                CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
                
                switch (CurrentHealth)
                {
                    case <= 75 when !health75:
                        health75 = true; 
                        coreManager.soundManager.PlayUISFX(SfxType.PlayerHit);
                        break;
                    case <= 50 when !health50:
                        health50 = true; health75 = true; 
                        coreManager.soundManager.PlayUISFX(SfxType.PlayerHit);
                        break;
                    case <= 25 when !health25:
                        health25 = true; health50 = true; health75 = true; 
                        coreManager.soundManager.PlayUISFX(SfxType.PlayerHit);
                        break;
                }
                
                if (itemCTS != null) CancelItemUsage();
                OnRecoverInstinctGauge(InstinctGainType.Hit);
                player.PlayerInteraction.OnCancelInteract();
            }
            else
            {
                if (CurrentShield < damage)
                {
                    CurrentHealth = Mathf.Max(CurrentHealth + CurrentShield - damage, 0);
                    coreManager.soundManager.PlayUISFX(SfxType.PlayerShieldHit);
                    coreManager.soundManager.PlayUISFX(SfxType.PlayerHit);
                    if (itemCTS != null) CancelItemUsage();
                    OnRecoverInstinctGauge(InstinctGainType.Hit);
                    player.PlayerInteraction.OnCancelInteract();
                }
                CurrentShield = Mathf.Max(CurrentShield - damage, 0);
                
                coreManager.uiManager.GetUI<InGameUI>()?.UpdateArmorSlider(CurrentShield, MaxShield);
            }
            
            UpdateLowPassFilterValue(LowestPoint + (HighestPoint - LowestPoint) * ((float)CurrentHealth / MaxHealth));
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateHealthSlider(CurrentHealth, MaxHealth);
            OnDamage?.Invoke();
            
            if (CurrentHealth <= 0) { OnDead(); }
        }
        
        너무 많아서 생략...

    }
}
```

### 2. 플레이어의 State 관리
- [StateMachine.cs](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/StateMachineScripts/StateMachine.cs)를 상속받아 꼭 필요한 프로퍼티 및 States를 관리하는 공간
- [StateMachine.cs](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/StateMachineScripts/StateMachine.cs)에 정의된 `ChangeState` 메소드를 통해 이곳에서 선언된 State로 전환이 가능
- 각 State에서는 PlayerStateMachine을 부를 수 있어 다른 State로 전환 시 선언된 PlayerStateMachine Parameter를 통해 `ChangeState` 메소드로 접근하여 전환.
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/StateMachineScripts/PlayerStateMachine.cs)
```
namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts
{
    [Serializable] public class PlayerStateMachine : StateMachine
    {
        // Sub States
        public IdleState IdleState { get; private set; }
        public WalkState WalkState { get; private set; }
        public RunState RunState { get; private set; }
        public CrouchState CrouchState { get; private set; }
        public CrouchWalkState CrouchWalkState { get; private set; }
        public FallState FallState { get; private set; }
        public JumpState JumpState { get; private set; }
        
        // Properties
        public Player Player { get; }
        public Vector2 MovementDirection { get; set; }
        public float MovementSpeed { get; private set; }
        public float RotationDamping { get; private set; }
        public float MovementSpeedModifier { get; set; }
        public float JumpHeight { get; set; }
        public Transform MainCameraTransform { get; set; }

        public PlayerStateMachine(Player player)
        {
            Player = player;
            JumpHeight = player.PlayerCondition.JumpForce;
            MainCameraTransform = player.MainCameraTransform;
            MovementSpeed = player.PlayerCondition.Speed;
            RotationDamping = player.PlayerCondition.RotationDamping;

            IdleState = new IdleState(this);
            WalkState = new WalkState(this);
            RunState = new RunState(this);
            CrouchState = new CrouchState(this);
            CrouchWalkState = new CrouchWalkState(this);

            FallState = new FallState(this);
            JumpState = new JumpState(this);
        }
    }
}
```

### 3. 플레이어 현재 보유 무기 관리
- 플레이어가 보유하고 있는 무기를 먼저 SaveFile이 있는지 확인 후 있으면 그 파일을 기준으로 Initialize
- 없을 시 기존 프리팹으로 선언된 설정을 기준으로 보유 무기 및 파츠를 Initialize
- 외부 라이브러리인 `SerializedDictionary`를 활용하여 Dictionary 형태로 무기의 보유 상태와 프리팹을 통해 선언된 무기들이 제대로 등록되는지 Inspector 창을 통해 확인(디버그에 용이)
- 또한 나중에 DTO를 활용하여 Json으로 Serialize하여 저장하기도 용이
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/Core/PlayerWeapon.cs)
```
namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerWeapon : MonoBehaviour
    {
        [field: Header("Weapons")]
        [field: SerializeField] public GameObject ArmPivot { get; private set; }
        [field: SerializeField] public SerializedDictionary<WeaponType, Animator> WeaponAnimators { get; private set; } = new();
        [field: SerializeField] public SerializedDictionary<WeaponType, BaseWeapon> Weapons { get; private set; } = new();
        [field: SerializeField] public SerializedDictionary<WeaponType, bool> AvailableWeapons { get; private set; } = new();

        private CoreManager coreManager;
        
        private void Awake()
        {
            if (!ArmPivot) ArmPivot = this.TryFindFirstChild("ArmPivot");
        }

        private void Reset()
        {
            if (!ArmPivot) ArmPivot = this.TryFindFirstChild("ArmPivot");
        }

        public void Initialize(DataTransferObject data)
        {
            coreManager = CoreManager.Instance;
            
            // Initialize Weapons
            var listOfGuns = GetComponentsInChildren<BaseWeapon>(true);
            foreach (var weapon in listOfGuns)
            {
                weapon.Initialize(gameObject, data);
                var type = weapon switch
                {
                    Gun gun => gun.GunData.GunStat.Type, 
                    GrenadeLauncher => WeaponType.GrenadeLauncher,
                    HackGun => WeaponType.HackGun,
                    Punch => WeaponType.Punch,
                    _ => throw new ArgumentOutOfRangeException()
                };
                Weapons.TryAdd(type, weapon);
                WeaponAnimators.TryAdd(type, weapon.GetComponent<Animator>());
                AvailableWeapons.TryAdd(type, false);
            }
            if (AvailableWeapons.Count > 0) AvailableWeapons[WeaponType.Punch] = true;

            if (data == null) return;
            foreach (var weapon in data.availableWeapons)
                AvailableWeapons[weapon.Key] = weapon.Value;
        }

        public bool EquipPart(WeaponType type, PartType partType, int id)
        {
            if (!AvailableWeapons.TryGetValue(type, out var isAvailable)) return false;
            if (!isAvailable) return false;

            if (!Weapons.TryGetValue(type, out var weapon)) return false;

            return weapon.TryEquipWeaponPart(partType, id);
        }

        public bool UnequipPart(WeaponType type, PartType partType, int id)
        {
            if (!AvailableWeapons.TryGetValue(type, out var isAvailable)) return false;
            if (!isAvailable) return false;

            if (!Weapons.TryGetValue(type, out var weapon)) return false;

            return weapon.TryUnequipWeaponPart(partType, id);
        }

        public bool ForgeWeapon()
        {
            if (!AvailableWeapons.TryGetValue(WeaponType.Pistol, out var isAvailable)) return false;
            if (!isAvailable) return false;

            if (!Weapons.TryGetValue(WeaponType.Pistol, out var weapon)) return false;
            
            return ((Gun)weapon).TryForgeWeapon();
        }
        
        public void GetAllAvailableParts()
        {
            foreach (var weapon in Weapons)
            {
                switch (weapon.Value)
                {
                    case Gun gun: gun.UnlockAllParts(); break;
                    case GrenadeLauncher gL: gL.UnlockAllParts(); break;
                    case HackGun hackGun: hackGun.UnlockAllParts(); break;
                }
            }
        }
    }
}
```

### 4. 플레이어 현재 보유 아이템 관리
- 플레이어가 보유하고 있는 아이템을 먼저 SaveFile이 있는지 확인 후 있으면 그 파일을 기준으로 Initialize
- 없을 시 아이템 보유 수는 `0`으로 통일
- 외부 라이브러리인 `SerializedDictionary`를 활용하여 Dictionary 형태로 아이템의 보유 상태를 Inspector 창을 통해 확인(디버그에 용이)
- 또한 나중에 DTO를 활용하여 Json으로 Serialize하여 저장하기도 용이
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/Core/PlayerInventory.cs)
```
namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        [field: Header("Items")]
        [field: SerializeField] public SerializedDictionary<ItemType, BaseItem> Items { get; private set; }
        [field: SerializeField] public ItemType CurrentItem { get; private set; }
        
        [field: Header("QuickSlot Settings")]
        [field: SerializeField] public float HoldDurationToOpen { get; private set; }
        [field: SerializeField] public bool IsOpenUIAction { get; private set; }
        
        private CoreManager coreManager;
        private Player player;
        private bool isPressed;
        private float timeSinceLastPressed;

        private void Update()
        {
            if (!isPressed || IsOpenUIAction) return;
            IsOpenUIAction = Time.unscaledTime - timeSinceLastPressed >= HoldDurationToOpen;

            if (!IsOpenUIAction) return;
            
            coreManager.uiManager.ShowUI<QuickSlotUI>();
            player.Pov.m_HorizontalAxis.Reset();
            player.Pov.m_VerticalAxis.Reset();
            player.InputProvider.enabled = false;
        }

        public void Initialize(DataTransferObject dto = null)
        {
            coreManager = CoreManager.Instance;
            player = coreManager.gameManager.Player;
            
            Items = new SerializedDictionary<ItemType, BaseItem>();
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                BaseItem item = type switch
                {
                    ItemType.Medkit => new Medkit(),
                    ItemType.EnergyBar => new EnergyBar(),
                    ItemType.NanoAmple => new NanoAmple(),
                    ItemType.Shield => new Shield(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                item.Initialize(coreManager, dto);
                if (Items.TryAdd(type, item)) Service.Log($"Successfully added {type}.");
            }
        }

        public void OnItemActionStarted()
        {
            IsOpenUIAction = false;
            isPressed = true;
            timeSinceLastPressed = Time.unscaledTime;
        }
        
        public void OnItemActionCanceled()
        {
            isPressed = false;
            switch (IsOpenUIAction)
            {
                case true:
                    coreManager.uiManager.HideUI<QuickSlotUI>();
                    player.InputProvider.enabled = true; break;
                case false: OnUseItem(); break;
            }
            IsOpenUIAction = false;
        }

        public void OnSelectItem(ItemType itemType)
        {
            Service.Log($"Attempting to select {itemType}");
            if (!Items.ContainsKey(itemType) || Items[itemType].CurrentItemCount <= 0)
            {
                CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast("FailSelect_Key");
                return;
            }
            CurrentItem = itemType;
            CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast(Items[itemType].ItemData);
        }

        public bool OnRefillItem(ItemType itemType)
        {
            Service.Log($"Attempting to refill {itemType}");
            return Items[itemType].OnRefill();
        }
        
        private bool OnUseItem()
        {
            Service.Log($"Attempting to use {CurrentItem}.");
            if (!Items.ContainsKey(CurrentItem) || Items[CurrentItem].CurrentItemCount <= 0)
            {
                CoreManager.Instance.uiManager.GetUI<InGameUI>()?.ShowToast("FailUse_Key");
                return false;
            }
            switch (Items[CurrentItem])
            {
                case Medkit medkit: return medkit.OnUse(gameObject);
                case NanoAmple nanoAmple: return nanoAmple.OnUse(gameObject);
                case EnergyBar energyBar: return energyBar.OnUse(gameObject);
                case Shield shield: return shield.OnUse(gameObject);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
```