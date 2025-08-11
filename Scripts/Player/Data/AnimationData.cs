using System;
using Unity.Collections;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Data
{
    [Serializable] public class AnimationData
    {
        [Header("Animation Parameters on ground")]
        [SerializeField, ReadOnly] private string groundParameterName = "@Ground";
        [SerializeField, ReadOnly] private string speedParameterName = "Speed";
        [SerializeField, ReadOnly] private string idleParameterName = "Idle";
        [SerializeField, ReadOnly] private string walkParameterName = "Walk";
        [SerializeField, ReadOnly] private string runParameterName = "Run";
        [SerializeField, ReadOnly] private string crouchParameterName = "Crouch";

        [Header("Animation Parameters on attack")] 
        [SerializeField, ReadOnly] private string aniSpeedMultiplierName = "AniSpeedMultiplier";
        [SerializeField, ReadOnly] private string normalAttackParameterName = "Attack";
        [SerializeField, ReadOnly] private string aimParameterName = "Aim";
        [SerializeField, ReadOnly] private string shootParameterName = "Shoot";
        [SerializeField, ReadOnly] private string emptyParameterName = "Empty";
        [SerializeField, ReadOnly] private string reloadParameterName = "Reload";
        [SerializeField, ReadOnly] private string inspectParameterName = "Inspect";
        [SerializeField, ReadOnly] private string hideParameterName = "Hide";
        
        [Header("Animation Parameters on air")]
        [SerializeField, ReadOnly] private string airParameterName = "@Air";
        [SerializeField, ReadOnly] private string jumpParameterName = "Jump";
        [SerializeField, ReadOnly] private string fallParameterName = "Fall";
        
        [Header("Animation Parameter on death")]
        [SerializeField, ReadOnly] private string deathParameterName = "Dead";
        [SerializeField, ReadOnly] private string hitParameterName = "Hit";

        [Header("Animation Clip Time")]
        [SerializeField] private float pistolReloadClipTime = 1.333f;
        [SerializeField] private float rifleReloadClipTime = 2.667f;
        [SerializeField] private float grenadeLauncherReloadClipTime = 10.333f;
        [SerializeField] private float hackGunReloadClipTime = 4.167f;
        [SerializeField] private float sniperRifleReloadClipTime = 4.633f;
        
        [SerializeField] private float handToOtherWeaponClipTime = 1f;
        [SerializeField] private float pistolToOtherWeaponClipTime = 0.667f;
        [SerializeField] private float rifleToOtherWeaponClipTime = 1f;
        [SerializeField] private float grenadeLauncherToOtherWeaponClipTime = 0.867f;
        [SerializeField] private float hackGunToOtherWeaponClipTime = 1f;
        [SerializeField] private float sniperRifleToOtherWeaponClipTime = 0.733f;
        
        [SerializeField] private float weaponWieldClipTime = 1f;
        
        // Properties of parameter hash
        public int GroundParameterHash { get; private set; }
        public int IdleParameterHash { get; private set; }
        public int WalkParameterHash { get; private set; }
        public int RunParameterHash { get; private set; }
        public int CrouchParameterHash { get; private set; }
        public int SpeedParameterHash { get; private set; }
        public int AniSpeedMultiplierHash { get; private set; }
        public int NormalAttackParameterHash { get; private set; }
        public int AimParameterHash { get; private set; }
        public int ShootParameterHash { get; private set; }
        public int EmptyParameterHash { get; private set; }
        public int ReloadParameterHash { get; private set; }
        public int InspectParameterHash { get; private set; }
        public int HideParameterHash { get; private set; }
        public int AirParameterHash { get; private set; }
        public int JumpParameterHash { get; private set; }
        public int FallParameterHash { get; private set; }
        public int DeathParameterHash { get; private set; }
        public int HitParameterHash { get; private set; }
        public float PistolReloadClipTime { get; private set; }
        public float RifleReloadClipTime { get; private set; }
        public float GrenadeLauncherReloadClipTime { get; private set; }
        public float HackGunReloadClipTime { get; private set; }
        public float SniperRifleReloadClipTime { get; private set; }
        public float HandToOtherWeaponClipTime { get; private set; }
        public float PistolToOtherWeaponClipTime { get; private set; }
        public float RifleToOtherWeaponClipTime { get; private set; }
        public float GrenadeLauncherToOtherWeaponClipTime { get; private set; }
        public float HackGunToOtherWeaponClipTime { get; private set; }
        public float SniperRifleToOtherWeaponClipTime { get; private set; }
        public float WeaponWieldClipTime { get; private set; }
        
        public void Initialize()
        {
            GroundParameterHash = Animator.StringToHash(groundParameterName);
            SpeedParameterHash = Animator.StringToHash(speedParameterName);
            IdleParameterHash = Animator.StringToHash(idleParameterName);
            WalkParameterHash = Animator.StringToHash(walkParameterName);
            RunParameterHash = Animator.StringToHash(runParameterName);
            CrouchParameterHash = Animator.StringToHash(crouchParameterName);
            AniSpeedMultiplierHash = Animator.StringToHash(aniSpeedMultiplierName);
            
            AirParameterHash = Animator.StringToHash(airParameterName);
            JumpParameterHash = Animator.StringToHash(jumpParameterName);
            FallParameterHash = Animator.StringToHash(fallParameterName);
            
            DeathParameterHash = Animator.StringToHash(deathParameterName);
            HitParameterHash = Animator.StringToHash(deathParameterName);
            
            NormalAttackParameterHash = Animator.StringToHash(normalAttackParameterName);
            AimParameterHash = Animator.StringToHash(aimParameterName);
            ShootParameterHash = Animator.StringToHash(shootParameterName);
            EmptyParameterHash = Animator.StringToHash(emptyParameterName);
            ReloadParameterHash = Animator.StringToHash(reloadParameterName);
            InspectParameterHash = Animator.StringToHash(inspectParameterName);
            HideParameterHash = Animator.StringToHash(hideParameterName);

            PistolReloadClipTime = pistolReloadClipTime;
            RifleReloadClipTime = rifleReloadClipTime;
            GrenadeLauncherReloadClipTime = grenadeLauncherReloadClipTime;
            HackGunReloadClipTime = hackGunReloadClipTime;
            SniperRifleReloadClipTime = sniperRifleReloadClipTime;
            
            HandToOtherWeaponClipTime = handToOtherWeaponClipTime;
            PistolToOtherWeaponClipTime = pistolToOtherWeaponClipTime;
            RifleToOtherWeaponClipTime = rifleToOtherWeaponClipTime;
            GrenadeLauncherToOtherWeaponClipTime = grenadeLauncherToOtherWeaponClipTime;
            HackGunToOtherWeaponClipTime = hackGunToOtherWeaponClipTime;
            SniperRifleToOtherWeaponClipTime = sniperRifleToOtherWeaponClipTime;
            
            WeaponWieldClipTime = weaponWieldClipTime;
        }
    }
}