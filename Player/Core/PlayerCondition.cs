using System;
using System.Threading;
using _1.Scripts.Entity.Scripts.Player.Data;
using _1.Scripts.Interfaces.Common;
using _1.Scripts.Item.Common;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Sound;
using _1.Scripts.UI.InGame.HUD;
using _1.Scripts.UI.InGame.SkillOverlay;
using _1.Scripts.VisualEffects;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.Grenade;
using _1.Scripts.Weapon.Scripts.Guns;
using _1.Scripts.Weapon.Scripts.Hack;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerCondition : MonoBehaviour, IBleedable
    {
        [Header("Components")] 
        [SerializeField] private Player player;
        [SerializeField] private PlayerWeapon playerWeapon;
        
        [field: Header("Base Condition Data")]
        [field: SerializeField] public PlayerStatData StatData { get; private set; }
        
        [field: Header("Current Condition Data")]
        [field: SerializeField] public float CurrentSpeed { get; set; }
        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public int CurrentHealth { get; private set; }
        [field: SerializeField] public float MaxStamina { get; private set; }
        [field: SerializeField] public float CurrentStamina { get; private set; }
        [field: SerializeField] public int MaxShield { get; private set; }
        [field: SerializeField] public int CurrentShield { get; private set; }
        [field: SerializeField] public float CurrentFocusGauge { get; private set; }
        [field: SerializeField] public float CurrentInstinctGauge { get; private set; }
        [field: SerializeField] public float SkillSpeedMultiplier { get; private set; } = 1f;
        [field: SerializeField] public float ItemSpeedMultiplier { get; private set; } = 1f;
        [field: SerializeField] public float WeightSpeedMultiplier { get; private set; } = 1f;
        [field: SerializeField] public float Damage { get; private set; }
        [field: SerializeField] public float AttackRate { get; private set; }
        [field: SerializeField] public bool IsCrouching { get; set; }
        [field: SerializeField] public bool IsUsingFocus { get; private set; }
        [field: SerializeField] public bool IsUsingInstinct { get; private set; }
        [field: SerializeField] public bool IsInMiniGame { get; set; }
        [field: SerializeField] public bool IsPlayerHasControl { get; set; } = true;
        [field: SerializeField] public bool IsDead { get; private set; }
        
        [field: Header("Current Physics Data")] 
        [field: SerializeField] public float Speed { get; private set; }
        [field: SerializeField] public float JumpForce { get; private set; }
        [field: SerializeField] public float RotationDamping { get; private set; } = 10f;  // Rotation Speed

        [field: Header("Speed Modifiers")]
        [field: SerializeField] public float CrouchSpeedModifier { get; private set; }
        [field: SerializeField] public float WalkSpeedModifier { get; private set; }
        [field: SerializeField] public float RunSpeedModifier { get; private set; }
        [field: SerializeField] public float AirSpeedModifier { get; private set; }
        
        [field: Header("Weapon States")]
        [field: SerializeField] public WeaponType EquippedWeaponIndex { get; private set; }
        [field: SerializeField] public float RecoilMultiplier { get; private set; } = 1f;
        [field: SerializeField] public bool IsAttacking { get; set; }
        [field: SerializeField] public bool IsSwitching { get; private set; }
        [field: SerializeField] public bool IsAiming { get; private set; }
        [field: SerializeField] public bool IsReloading { get; private set; }
        
        [field: Header("Low Pass Filter Settings")]
        [field: SerializeField] public AudioLowPassFilter LowPassFilter { get; private set; }
        [field: SerializeField] public float LowestPoint { get; private set; }
        [field: SerializeField] public float HighestPoint { get; private set; }
        
        [field: Header("Saved Position & Rotation")]
        [field: SerializeField] public Vector3 LastSavedPosition { get; set; }
        [field: SerializeField] public Quaternion LastSavedRotation { get; set; }
        
        [field: Header("Focus Mode")]
        [field: SerializeField] public PostProcessEditForFocus PostProcessEditForFocus { get; private set; }
        
        // Fields
        private CoreManager coreManager;
        private SoundPlayer reloadPlayer;

        private CancellationTokenSource playerCTS;
        private CancellationTokenSource aimCTS;
        private CancellationTokenSource switchCTS;
        private CancellationTokenSource reloadCTS;
        private CancellationTokenSource itemCTS;
        private CancellationTokenSource focusCTS;
        private CancellationTokenSource instinctCTS;
        private CancellationTokenSource instinctRecoveryCTS;
        private CancellationTokenSource staminaCTS;
        private CancellationTokenSource crouchCTS;
        private CancellationTokenSource bleedCTS;
        
        // Action events
        [CanBeNull] public event Action OnDamage, OnDeath;

        private void Awake()
        {
            if (!player) player = this.TryGetComponent<Player>();
            if (!playerWeapon) playerWeapon = this.TryGetComponent<PlayerWeapon>();
            if (!LowPassFilter) LowPassFilter = FindFirstObjectByType<AudioLowPassFilter>();
            if (!PostProcessEditForFocus) PostProcessEditForFocus = FindFirstObjectByType<PostProcessEditForFocus>();
        }

        private void Reset()
        {
            if (!player) player = this.TryGetComponent<Player>();
            if (!playerWeapon) playerWeapon = this.TryGetComponent<PlayerWeapon>();
            if (!LowPassFilter) LowPassFilter = FindFirstObjectByType<AudioLowPassFilter>();
            if (!PostProcessEditForFocus) PostProcessEditForFocus = FindFirstObjectByType<PostProcessEditForFocus>();
        }

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

            OnInstinctRecover_Idle();
            player.Controller.enabled = true;
        }

        public void UpdateLowPassFilterValue(float value)
        {
            LowPassFilter.cutoffFrequency = value;
        }

        public void UpdateLastSavedTransform()
        {
            LastSavedPosition = player.transform.position;
            LastSavedRotation = player.transform.rotation;
        }
        
        private void OnDestroy()
        {
            StopAllUniTasks();
        }

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
                if (itemCTS != null) CancelItemUsage();
                OnRecoverInstinctGauge(InstinctGainType.Hit);
                player.PlayerInteraction.OnCancelInteract();
            }
            else
            {
                if (CurrentShield < damage)
                {
                    CurrentHealth = Mathf.Max(CurrentHealth + CurrentShield - damage, 0);
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

        public void OnBleed(int totalTick, float tickInterval, int damagePerTick)
        {
            if (bleedCTS != null) { bleedCTS.Cancel(); bleedCTS.Dispose(); }
            bleedCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            
            _= BleedAsync(totalTick, tickInterval, damagePerTick, bleedCTS.Token);
        }

        private async UniTaskVoid BleedAsync(int totalTick, float tickInterval, int damagePerTick, CancellationToken token)
        {
            for (int i = 0; i < totalTick; i++)
            {
                coreManager.uiManager.GetUI<BleedOverlayUI>().Flash();
                OnTakeDamage(damagePerTick);
                await UniTask.WaitForSeconds(tickInterval, cancellationToken:token);
            }
        }

        /// <summary>
        /// Recover Health Point
        /// </summary>
        /// <param name="value">Value of hp to recover</param>
        private void OnRecoverHealth(int value)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(CurrentHealth + value, MaxHealth);
            UpdateLowPassFilterValue(LowestPoint + (HighestPoint - LowestPoint) * ((float)CurrentHealth / MaxHealth));
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateHealthSlider(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Recover Shield Point
        /// </summary>
        /// <param name="value">Value of shield to recover</param>
        private void OnRecoverShield(int value)
        {
            if (IsDead) return;
            CurrentShield = Mathf.Min(CurrentShield + value, MaxShield);
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateArmorSlider(CurrentShield, MaxShield);
        }

        /// <summary>
        /// Consume Stamina Point
        /// </summary>
        /// <param name="stamina">Value to consume from player stamina point</param>
        private void OnConsumeStamina(float stamina) 
        { 
            if (IsDead) return; 
            CurrentStamina = Mathf.Max(CurrentStamina - stamina, 0);
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateStaminaSlider(CurrentStamina, MaxStamina);
        }

        /// <summary>
        /// Recover Stamina Point
        /// </summary>
        /// <param name="stamina">Value of stamina to recover</param>
        private void OnRecoverStamina(float stamina)
        {
            if (IsDead) return;
            CurrentStamina = Mathf.Min(CurrentStamina + stamina, MaxStamina);
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateStaminaSlider(CurrentStamina, MaxStamina);
        }
        
        /// <summary>
        /// Consume Focus Gauge
        /// </summary>
        /// <param name="value">Value to consume focus</param>
        /// <returns>Returns true, if there are enough points to consume. If not, return false.</returns>
        public bool OnConsumeFocusGauge(float value = 1f)
        {
            if (IsDead || CurrentFocusGauge < value || IsUsingFocus || IsUsingInstinct) return false;
            CurrentFocusGauge = Mathf.Max(CurrentFocusGauge - value, 0f);
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateFocus(CurrentFocusGauge);
            OnFocusEngaged();
            return true;
        }

        /// <summary>
        /// Recover Focus Point
        /// </summary>
        /// <param name="value">Value to recover focus</param>
        public void OnRecoverFocusGauge(FocusGainType value)
        {
            if (IsDead || IsUsingFocus || IsUsingInstinct) return;
            
            CurrentFocusGauge = value switch
            {
                FocusGainType.Kill => Mathf.Min(CurrentFocusGauge + StatData.focusGaugeRefillRate_OnKill, 1f),
                FocusGainType.HeadShot => Mathf.Min(CurrentFocusGauge + StatData.focusGaugeRefillRate_OnHeadShot, 1f),
                FocusGainType.Hack => Mathf.Min(CurrentFocusGauge + StatData.focusGaugeRefillRate_OnHacked, 1f),
                FocusGainType.Debug => Mathf.Min(CurrentFocusGauge + 1f, 1f),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateFocus(CurrentFocusGauge);
        }

        /// <summary>
        /// Consume Instinct Gauge
        /// </summary>
        /// <param name="value">Value to consume instinct</param>
        /// <returns>Returns true, if there are enough points to consume. If not, return false.</returns>
        public bool OnConsumeInstinctGauge(float value = 1f)
        {
            if (IsDead || CurrentInstinctGauge < value || IsUsingInstinct || IsUsingFocus || CurrentHealth >= MaxHealth * 0.5f) return false;
            CurrentInstinctGauge = Mathf.Max(CurrentInstinctGauge - value, 0f);
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateInstinct(CurrentInstinctGauge);
            OnInstinctEngaged();
            return true;
        }
        
        /// <summary>
        /// Recover Instinct Point
        /// </summary>
        /// <param name="value">Value to recover instinct</param>
        public void OnRecoverInstinctGauge(InstinctGainType value)
        {
            if (IsDead || IsUsingInstinct || IsUsingFocus) return;

            CurrentInstinctGauge = value switch
            {
                InstinctGainType.Idle => Mathf.Min(CurrentInstinctGauge + StatData.instinctGaugeRefillRate_OnIdle, 1f),
                InstinctGainType.Hit => Mathf.Min(CurrentInstinctGauge + StatData.instinctGaugeRefillRate_OnHit, 1f),
                InstinctGainType.Debug => Mathf.Min(CurrentInstinctGauge + 1f, 1f),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            coreManager.uiManager.GetUI<InGameUI>()?.UpdateInstinct(CurrentInstinctGauge);
        }

        private void OnDead()
        {
            IsDead = true;
            IsPlayerHasControl = false;
            playerWeapon.ArmPivot.SetActive(false);
            player.Pov.m_HorizontalAxis.Reset();
            player.Pov.m_VerticalAxis.Reset();
            player.InputProvider.enabled = false;
            player.PlayerInput.enabled = false;
            
            FallCamera();
            OnDeath?.Invoke();
        }

        private void FallCamera()
        {
            Sequence seq = DOTween.Sequence();
            Transform cam = player.FirstPersonCamera.transform;
            
            var hardLock = player.FirstPersonCamera.GetCinemachineComponent<CinemachineHardLockToTarget>();
            if (hardLock != null) Destroy(hardLock);
            
            cam.rotation = player.transform.rotation;
            
            seq.Append(
                cam.DOLocalRotate(new Vector3(0f, 0f, 90f), 0.5f)
                    .SetEase(Ease.InCubic)
            );
            seq.Join(
                cam.DOLocalMove(cam.localPosition + new Vector3(0f, 0f, 2f), 0.5f)
                    .SetEase(Ease.OutSine)
            );

            seq.Append(cam.DOShakeRotation(0.25f, 10f, 20, 90f, true));
        }

        public void OnAttack()
        {
            if (!IsAttacking) return;
            switch (playerWeapon.Weapons[EquippedWeaponIndex])
            {
                case Gun gun:
                    if (gun.OnShoot())
                    {
                        playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetTrigger(player.AnimationData.ShootParameterHash);
                        coreManager.uiManager.GetUI<WeaponUI>().Refresh(false);
                    }
                    break;
                case GrenadeLauncher grenadeThrower:
                    if (grenadeThrower.OnShoot())
                    {
                        playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetTrigger(player.AnimationData.ShootParameterHash);
                        coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
                    }
                    break;
                case HackGun hackingGun:
                    if (hackingGun.OnShoot())
                    {
                        playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetTrigger(player.AnimationData.ShootParameterHash);
                        coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
                    }
                    break;
            }
        }

        public void OnEnablePlayerMovement()
        {
            IsPlayerHasControl = true;
            player.InputProvider.XYAxis.action.Enable();
        }

        public void OnDisablePlayerMovement()
        {
            IsPlayerHasControl = false;
            
            player.InputProvider.XYAxis.action.Disable();
            player.Pov.m_HorizontalAxis.Reset(); 
            player.Pov.m_VerticalAxis.Reset();
        }

        private void StopAllUniTasks()
        {
            coreManager.PlayerCTS?.Cancel();
            
            crouchCTS?.Dispose(); crouchCTS = null;
            staminaCTS?.Dispose(); staminaCTS = null;
            reloadCTS?.Dispose(); reloadCTS = null;
            itemCTS?.Dispose(); itemCTS = null;
            aimCTS?.Dispose(); aimCTS = null; 
            switchCTS?.Dispose(); switchCTS = null;
            focusCTS?.Dispose(); focusCTS = null; 
            instinctCTS?.Dispose(); instinctCTS = null;
            instinctRecoveryCTS?.Dispose(); instinctRecoveryCTS = null;
            bleedCTS?.Dispose(); bleedCTS = null;
        }
        
        /* - Crouch 관련 메소드 - */
        public void OnCrouch(bool isCrouch, float duration)
        {
            IsCrouching = isCrouch;
            if (crouchCTS != null) { crouchCTS.Cancel(); crouchCTS.Dispose(); }
            crouchCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = Crouch_Async(IsCrouching, duration, crouchCTS.Token); 
        }
        private async UniTaskVoid Crouch_Async(bool isCrouch, float duration, CancellationToken token)
        {
            var currentPosition = player.CameraPivot.localPosition;
            var currentOffset = player.Controller.center;
            var currentHeight = player.Controller.height;
            
            var targetPosition = !isCrouch
                ? player.IdlePivot.localPosition
                : player.CrouchPivot.localPosition;
            var targetOffset = !isCrouch 
                ? player.OriginalOffset 
                : player.OriginalOffset - (player.IdlePivot.localPosition - 
                                           player.CrouchPivot.localPosition) / 2;
            var targetHeight = !isCrouch
                ? player.OriginalHeight
                : player.OriginalHeight - (player.IdlePivot.localPosition -
                                           player.CrouchPivot.localPosition).y;
            
            float t = 0f;
            while (t < duration)
            {
                if (!coreManager.gameManager.IsGamePaused) t += Time.unscaledDeltaTime;
                float elapsed = t / duration;
                player.CameraPivot.localPosition = Vector3.Lerp(currentPosition, targetPosition, elapsed);
                player.Controller.center = Vector3.Lerp(currentOffset, targetOffset, elapsed);
                player.Controller.height = Mathf.Lerp(currentHeight, targetHeight, elapsed);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
            }
            
            player.CameraPivot.localPosition = targetPosition;
            crouchCTS.Dispose(); crouchCTS = null;
        }
        /* -------------------- */
        
        /* - Stamina 관련 메소드 - */
        public void OnConsumeStamina(float consumeRate, float interval)
        {
            if (staminaCTS != null) { staminaCTS?.Cancel(); staminaCTS?.Dispose(); }
            staminaCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = ConsumeStamina_Async(consumeRate, interval, staminaCTS.Token);
        }
        public void OnRecoverStamina(float recoverRate, float interval)
        {
            if (staminaCTS != null) { staminaCTS?.Cancel(); staminaCTS?.Dispose(); }
            staminaCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = RecoverStamina_Async(recoverRate, interval, staminaCTS.Token);
        }
        public void CancelStaminaTask()
        {
            staminaCTS?.Cancel();
            staminaCTS?.Dispose();
            staminaCTS = null;
        }
        private async UniTaskVoid ConsumeStamina_Async(float consumeRate, float interval, CancellationToken token)
        {
            while (CurrentStamina > 0)
            {
                if (!coreManager.gameManager.IsGamePaused)
                    OnConsumeStamina(consumeRate);
                await UniTask.WaitForSeconds(interval, true, cancellationToken: token, cancelImmediately: true);
            }
        }
        private async UniTaskVoid RecoverStamina_Async(float recoverRate, float interval, CancellationToken token)
        {
            while (CurrentStamina < MaxStamina)
            {
                if (!coreManager.gameManager.IsGamePaused)
                    OnRecoverStamina(recoverRate);
                await UniTask.WaitForSeconds(interval, true, cancellationToken: token, cancelImmediately: true);
            }
        }
        /* --------------------- */
        
        /* - Aim 관련 메소드 - */
        public void OnAim(bool isAim, float targetFoV, float transitionTime)
        {
            aimCTS?.Cancel(); aimCTS?.Dispose();
            aimCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = AimAsync(isAim, targetFoV, transitionTime, aimCTS.Token);
        }
        private async UniTaskVoid AimAsync(bool isAim, float targetFoV, float transitionTime, CancellationToken token)
        {
            float currentFoV = player.FirstPersonCamera.m_Lens.FieldOfView;
            playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.AimParameterHash, isAim);
            
            var time = 0f;
            while (time < transitionTime)
            {
                time += Time.unscaledDeltaTime;
                float t = time / transitionTime;
                var value = Mathf.Lerp(currentFoV, targetFoV, t);
                player.FirstPersonCamera.m_Lens.FieldOfView = value;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
            }
            
            IsAiming = isAim;
            player.FirstPersonCamera.m_Lens.FieldOfView = targetFoV;
            aimCTS.Dispose(); aimCTS = null;
        }
        /* ----------------- */
        
        /* - Reload 관련 메소드 - */
        public bool TryStartReload()
        {
            if (IsDead || IsSwitching || EquippedWeaponIndex == WeaponType.Punch) return false;
            
            switch (playerWeapon.Weapons[EquippedWeaponIndex])
            {
                case Gun { IsReadyToReload: false }:
                    return false;
                case Gun gun:
                {
                    if (reloadCTS != null)
                    {
                        reloadCTS?.Cancel(); reloadCTS?.Dispose();
                        gun.IsReloading = false;
                        IsReloading = false;
                        playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.ReloadParameterHash, false);
                    }
                    
                    // Play Reload AudioClip
                    float reloadTime = gun.GunData.GunStat.ReloadTime;
                    reloadPlayer = coreManager.soundManager.PlayUISFX(
                        gun.GunData.GunStat.Type switch
                        {
                            WeaponType.Pistol => SfxType.PistolReload,
                            WeaponType.Rifle => SfxType.RifleReload,
                            _ => SfxType.SniperRifleReload
                        }, reloadTime);
                    
                    // Start Reload Coroutine
                    reloadCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
                    _ = ReloadAsync(reloadTime, reloadCTS.Token);
                    break;
                }
                case GrenadeLauncher { IsReadyToReload: false }:
                    return false;
                case GrenadeLauncher grenadeLauncher:
                {
                    if (reloadCTS != null)
                    {
                        reloadCTS?.Cancel(); reloadCTS?.Dispose();
                        grenadeLauncher.IsReloading = false;
                        IsReloading = false;
                        playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.ReloadParameterHash, false);
                    }

                    // Player Reload AudioClip
                    float reloadTime = grenadeLauncher.GrenadeData.GrenadeStat.ReloadTime;
                    reloadPlayer = coreManager.soundManager.PlayUISFX(SfxType.GrenadeLauncherReload, reloadTime);
                    
                    // Start Reload Coroutine
                    reloadCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
                    _ = ReloadAsync(reloadTime,  reloadCTS.Token);
                    break;
                }
                case HackGun {IsReadyToReload: false}:
                    return false;
                case HackGun hackGun:
                {
                    if (reloadCTS != null)
                    {
                        reloadCTS?.Cancel(); reloadCTS?.Dispose();
                        hackGun.IsReloading = false;
                        IsReloading = false;
                        playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.ReloadParameterHash, false);
                    }
                    
                    // Player Reload AudioClip
                    float reloadTime = hackGun.HackData.HackStat.ReloadTime;
                    reloadPlayer = coreManager.soundManager.PlayUISFX(SfxType.HackGunReload, reloadTime);
                    
                    // Start Reload Coroutine
                    reloadCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
                    _ = ReloadAsync(reloadTime,  reloadCTS.Token);
                    break;
                }
            }

            return true;
        }
        public bool TryCancelReload()
        {
            if (IsDead || EquippedWeaponIndex == WeaponType.Punch) return false;
            
            reloadCTS?.Cancel(); reloadCTS?.Dispose(); reloadCTS = null;
            switch (playerWeapon.Weapons[EquippedWeaponIndex])
            {
                case Gun gun:
                    gun.IsReloading = false;
                    playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.ReloadParameterHash, false);
                    break;
                case GrenadeLauncher grenadeLauncher:
                    grenadeLauncher.IsReloading = false;
                    playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.ReloadParameterHash, false);
                    break;
                case HackGun crossbow:
                    crossbow.IsReloading = false;
                    playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetBool(player.AnimationData.ReloadParameterHash, false);
                    break;
            }
            playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetFloat(player.AnimationData.AniSpeedMultiplierHash, 1f);
            
            if (reloadPlayer) reloadPlayer.Stop();
            reloadPlayer = null;
            IsReloading = false;
            return true;
        }
        private async UniTaskVoid ReloadAsync(float interval, CancellationToken token)
        {
            var currentAnimator = playerWeapon.WeaponAnimators[EquippedWeaponIndex];
            
            if (playerWeapon.Weapons[EquippedWeaponIndex] is Gun gun)
            {
                if (gun.CurrentAmmoCount <= 0 || gun.CurrentAmmoCountInMagazine == gun.CurrentMaxAmmoCountInMagazine) return;

                // Animation Control (Reload Start)
                currentAnimator.SetBool(player.AnimationData.ReloadParameterHash, true);
                var animationSpeed = gun.GunData.GunStat.Type switch
                {
                    WeaponType.Pistol => player.AnimationData.PistolReloadClipTime / gun.GunData.GunStat.ReloadTime,
                    WeaponType.Rifle => player.AnimationData.RifleReloadClipTime / gun.GunData.GunStat.ReloadTime,
                    _ => player.AnimationData.SniperRifleReloadClipTime / gun.GunData.GunStat.ReloadTime
                };
                currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, animationSpeed);
                
                gun.IsReloading = true;
                IsReloading = true;

                var t = 0f;
                while (t < interval)
                {
                    if (coreManager.gameManager.IsGamePaused)
                    {
                        if(currentAnimator.GetFloat(player.AnimationData.AniSpeedMultiplierHash) != 0f)
                            currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, 0f);
                    }
                    else
                    {
                        if (!Mathf.Approximately(currentAnimator.GetFloat(player.AnimationData.AniSpeedMultiplierHash), animationSpeed))
                            currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, animationSpeed);
                        t += Time.unscaledDeltaTime;
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
                }
                
                // Service.Log("Gun reloaded");
                gun.OnReload();
                IsReloading = false;
                gun.IsReloading = false;
                
                // Animation Control (Reload End)
                currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, 1f);
                currentAnimator.SetBool(player.AnimationData.ReloadParameterHash, false);
                currentAnimator.SetBool(player.AnimationData.EmptyParameterHash, false);
            } else if (playerWeapon.Weapons[EquippedWeaponIndex] is GrenadeLauncher grenadeLauncher)
            {
                if (grenadeLauncher.CurrentAmmoCount <= 0 || grenadeLauncher.CurrentAmmoCountInMagazine ==
                    grenadeLauncher.MaxAmmoCountInMagazine)
                    return;

                // Animation Control (Reload Start)
                currentAnimator.SetBool(player.AnimationData.ReloadParameterHash, true);
                var animationSpeed = player.AnimationData.GrenadeLauncherReloadClipTime /
                                     grenadeLauncher.GrenadeData.GrenadeStat.ReloadTime;
                currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, animationSpeed);
                
                grenadeLauncher.IsReloading = true;
                IsReloading = true;
                
                while (true)
                {
                    if (coreManager.gameManager.IsGamePaused)
                    {
                        if(currentAnimator.GetFloat(player.AnimationData.AniSpeedMultiplierHash) != 0f)
                            currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, 0f);
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
                    }
                    else
                    {
                        if (!Mathf.Approximately(currentAnimator.GetFloat(player.AnimationData.AniSpeedMultiplierHash), animationSpeed))
                            currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, animationSpeed);
                        await UniTask.WaitForSeconds(interval / grenadeLauncher.MaxAmmoCountInMagazine, 
                                                true, cancellationToken: token, cancelImmediately: true);
                        if (grenadeLauncher.OnReload()) continue;
                        reloadPlayer.Stop(); break;
                    }
                }
                
                // Service.Log("GL reloaded");
                IsReloading = false;
                grenadeLauncher.IsReloading = false;
                
                // Animation Control (Reload End)
                currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, 1f);
                currentAnimator.SetBool(player.AnimationData.ReloadParameterHash, false);
                currentAnimator.SetBool(player.AnimationData.EmptyParameterHash, false);
            } else if (playerWeapon.Weapons[EquippedWeaponIndex] is HackGun crossbow)
            {
                if (crossbow.CurrentAmmoCount <= 0 ||
                    crossbow.CurrentAmmoCountInMagazine == crossbow.CurrentMaxAmmoCountInMagazine)
                    return;
                
                // Animation Control (Reload Start)
                currentAnimator.SetBool(player.AnimationData.ReloadParameterHash, true);
                var animationSpeed = player.AnimationData.HackGunReloadClipTime /
                                     crossbow.HackData.HackStat.ReloadTime;
                currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, animationSpeed);
                
                crossbow.IsReloading = true;
                IsReloading = true;
                var t = 0f;
                while (t < interval)
                {
                    if (coreManager.gameManager.IsGamePaused)
                    {
                        if(currentAnimator.GetFloat(player.AnimationData.AniSpeedMultiplierHash) != 0f)
                            currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, 0f);
                    }
                    else
                    {
                        if (!Mathf.Approximately(currentAnimator.GetFloat(player.AnimationData.AniSpeedMultiplierHash), animationSpeed))
                            currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, animationSpeed);
                        t += Time.unscaledDeltaTime;
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
                }
                
                // Service.Log("HackGun reloaded");
                crossbow.OnReload();
                IsReloading = false;
                crossbow.IsReloading = false;
                
                // Animation Control (Reload End)
                currentAnimator.SetFloat(player.AnimationData.AniSpeedMultiplierHash, 1f);
                currentAnimator.SetBool(player.AnimationData.ReloadParameterHash, false);
                currentAnimator.SetBool(player.AnimationData.EmptyParameterHash, false);
            }
            reloadPlayer = null;
            reloadCTS.Dispose(); reloadCTS = null;
            coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
        }
        /* --------------------- */
        
        /* - Weapon Switch 메소드 - */
        public void OnSwitchWeapon(WeaponType currentWeaponIndex, float duration)
        {
            IsAttacking = false;
            if (IsReloading) TryCancelReload();
            WeaponType previousWeaponIndex = EquippedWeaponIndex;
            EquippedWeaponIndex = currentWeaponIndex;
            
            if (switchCTS != null)
            {
                switchCTS?.Cancel(); switchCTS?.Dispose();
                IsSwitching = false;
            }

            switchCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = SwitchAsync(previousWeaponIndex, currentWeaponIndex, duration, switchCTS.Token);
        }
        private async UniTaskVoid SwitchAsync(WeaponType previousWeaponIndex, WeaponType currentWeaponIndex, float duration, CancellationToken token)
        {
            if (previousWeaponIndex == currentWeaponIndex) { switchCTS = null; return; }
            IsSwitching = true;
            
            // Service.Log($"Switching from {previousWeaponIndex} to {currentWeaponIndex}");
            if (IsAiming) OnAim(false, 67.5f, 0.2f);
            
            switch (previousWeaponIndex)
            {
                case WeaponType.Punch: playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(
                    player.AnimationData.AniSpeedMultiplierHash, 
                    player.AnimationData.HandToOtherWeaponClipTime / duration); break; 
                case WeaponType.Pistol: playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(
                    player.AnimationData.AniSpeedMultiplierHash, 
                    player.AnimationData.PistolToOtherWeaponClipTime / duration); break; 
                case WeaponType.Rifle: playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(
                    player.AnimationData.AniSpeedMultiplierHash, 
                    player.AnimationData.RifleToOtherWeaponClipTime / duration); break;
                case WeaponType.GrenadeLauncher: playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(
                    player.AnimationData.AniSpeedMultiplierHash, 
                    player.AnimationData.GrenadeLauncherToOtherWeaponClipTime / duration); break;
                case WeaponType.HackGun: playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(
                    player.AnimationData.AniSpeedMultiplierHash,
                    player.AnimationData.HackGunToOtherWeaponClipTime / duration); break;
                case WeaponType.SniperRifle: playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(
                    player.AnimationData.AniSpeedMultiplierHash,
                    player.AnimationData.SniperRifleToOtherWeaponClipTime / duration); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(previousWeaponIndex), previousWeaponIndex, null);
            }
            
            // Service.Log("Switch Weapon");
            WeightSpeedMultiplier = 1f;
            playerWeapon.WeaponAnimators[previousWeaponIndex].SetTrigger(player.AnimationData.HideParameterHash);
            await UniTask.WaitForSeconds(duration, true, cancellationToken: token, cancelImmediately: true);
            playerWeapon.WeaponAnimators[previousWeaponIndex].SetFloat(player.AnimationData.AniSpeedMultiplierHash, 1f);
            playerWeapon.Weapons[previousWeaponIndex].gameObject.SetActive(false);
            
            coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(true);
            
            // Service.Log("Wield Weapon");
            playerWeapon.Weapons[EquippedWeaponIndex].gameObject.SetActive(true);
            await UniTask.DelayFrame(1, cancellationToken: token, cancelImmediately: true);
            playerWeapon.WeaponAnimators[EquippedWeaponIndex].SetFloat(player.AnimationData.AniSpeedMultiplierHash, 1f);
            await UniTask.WaitForSeconds(duration, true, cancellationToken: token, cancelImmediately: true);
            WeightSpeedMultiplier = playerWeapon.Weapons[EquippedWeaponIndex] switch
            {
                Gun gun => 1f - gun.GunData.GunStat.WeightPenalty,
                GrenadeLauncher grenadeLauncher => 1f - grenadeLauncher.GrenadeData.GrenadeStat.WeightPenalty,
                HackGun crossbow => 1f - crossbow.HackData.HackStat.WeightPenalty,
                _ => 1f
            };
            IsSwitching = false;
            switchCTS.Dispose(); switchCTS = null;
        }
        /* --------------------- */
        
        /* - Skill 관련 메소드 - */
        private void OnFocusEngaged()
        {
            focusCTS?.Cancel(); focusCTS?.Dispose();
            focusCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            coreManager.uiManager.GetUI<SkillOverlayUI>()?.ShowFocusOverlay();
            coreManager.spawnManager.FocusModeOnOrNot(true);
            PostProcessEditForFocus?.FocusModeOnOrNot(true);
            _ = FocusAsync(StatData.focusSkillTime, focusCTS.Token);
        }
        private async UniTaskVoid FocusAsync(float duration, CancellationToken token)
        {
            IsUsingFocus = true;
            coreManager.timeScaleManager.ChangeTimeScale(0.5f);
            RecoilMultiplier = 0.5f;
            var t = 0f;
            while (t < duration)
            {
                if (!coreManager.gameManager.IsGamePaused) t += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
            }
            RecoilMultiplier = 1f;
            coreManager.timeScaleManager.ChangeTimeScale(1f);
            IsUsingFocus = false;
            focusCTS.Dispose(); focusCTS = null;
            
            coreManager.uiManager.GetUI<SkillOverlayUI>()?.HideFocusOverlay();
            coreManager.spawnManager.FocusModeOnOrNot(false);
            PostProcessEditForFocus?.FocusModeOnOrNot(false);
        }
        private void OnInstinctEngaged()
        {
            instinctCTS?.Cancel(); instinctCTS?.Dispose();
            instinctCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            coreManager.uiManager.GetUI<SkillOverlayUI>()?.ShowInstinctOverlay();
            _ = InstinctAsync(StatData.instinctSkillTime, instinctCTS.Token);
        }
        private async UniTaskVoid InstinctAsync(float duration, CancellationToken token)
        {
            IsUsingInstinct = true;
            coreManager.spawnManager.ChangeStencilLayer(true);
            
            SkillSpeedMultiplier = StatData.instinctSkillMultiplier;
            var t = 0f;
            while (t < duration)
            {
                if (token.IsCancellationRequested)
                {
                    coreManager.spawnManager.ChangeStencilLayer(false);
                    coreManager.uiManager.GetUI<SkillOverlayUI>()?.HideInstinctOverlay();
                    return;
                }
                if (!coreManager.gameManager.IsGamePaused) t += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
            }
            SkillSpeedMultiplier = 1f;
            
            coreManager.spawnManager.ChangeStencilLayer(false);
            IsUsingInstinct = false;
            instinctCTS.Dispose(); instinctCTS = null;
            coreManager.uiManager.GetUI<SkillOverlayUI>()?.HideInstinctOverlay();
        }
        private void OnInstinctRecover_Idle()
        {
            instinctRecoveryCTS?.Cancel(); instinctRecoveryCTS?.Dispose();
            instinctRecoveryCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = InstinctRecover_Async(1, instinctRecoveryCTS.Token);
        }
        private async UniTaskVoid InstinctRecover_Async(float delay, CancellationToken token)
        {
            while (!IsDead)
            {
                if (coreManager.gameManager.IsGamePaused) { await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, true); }
                else
                {
                    await UniTask.WaitForSeconds(delay, true, cancellationToken: token, cancelImmediately: true);
                    OnRecoverInstinctGauge(InstinctGainType.Idle);
                }
            }
        }
        /* -------------------- */
        
        /* - Item 관련 메소드 - */
        public void OnItemUsed(BaseItem usedItem)
        {
            if (itemCTS != null) { CancelItemUsage(); return; }
            itemCTS = CancellationTokenSource.CreateLinkedTokenSource(coreManager.PlayerCTS.Token);
            _ = Item_Async(usedItem, itemCTS.Token);
        }
        private void CancelItemUsage()
        {
            var inGameUI = coreManager.uiManager.GetUI<InGameUI>();
            inGameUI.HideItemProgress(); ItemSpeedMultiplier = 1f;
            itemCTS?.Cancel(); itemCTS?.Dispose(); itemCTS = null;
        }
        private async UniTaskVoid Item_Async(BaseItem item, CancellationToken token)
        {
            if (!item.ItemData.IsPlayerMovable) ItemSpeedMultiplier = 0f;
            var t = 0f;
            var inGameUI = coreManager.uiManager.GetUI<InGameUI>();
            inGameUI.ShowItemProgress();
            inGameUI.UpdateItemProgress(0f);
            while (t < item.ItemData.Delay)
            {
                if (token.IsCancellationRequested)
                {
                    inGameUI.HideItemProgress(); ItemSpeedMultiplier = 1f;
                    itemCTS.Dispose(); itemCTS = null; return;
                }
                if (!coreManager.gameManager.IsGamePaused)
                {
                    t += Time.unscaledDeltaTime;
                    inGameUI.UpdateItemProgress(t / item.ItemData.Delay);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token, cancelImmediately: true);
            }
            
            inGameUI.UpdateItemProgress(1f);
            inGameUI.HideItemProgress();
            
            switch (item.ItemData.ItemType)
            {
                case ItemType.Medkit: 
                case ItemType.NanoAmple: OnRecoverHealth(item.ItemData.Value); break;
                case ItemType.EnergyBar: OnRecoverStamina(item.ItemData.Value); break;
                case ItemType.Shield: OnRecoverShield(item.ItemData.Value); break;
                default: throw new ArgumentOutOfRangeException();
            }
            item.OnConsume();
            ItemSpeedMultiplier = 1f;
            itemCTS.Dispose(); itemCTS = null;
        }
        /* ------------------- */
    }
}
