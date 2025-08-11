using System.Collections;
using System.Linq;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.NPC;
using _1.Scripts.Interfaces.Weapon;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Manager.Subs;
using _1.Scripts.UI.InGame.HUD;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Hack
{
    public class HackGun : BaseWeapon, IReloadable
    {
        [field: Header("HackGun Data")]
        [field: SerializeField] public HackData HackData { get; private set; }
        
        [field: Header("Current HackGun Settings")]
        [SerializeField] private Transform face;
        [field: SerializeField] public Transform BulletSpawnPoint { get; private set; }
        [field: SerializeField] public int CurrentAmmoCount { get; private set; }
        [field: SerializeField] public int CurrentMaxAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public int CurrentAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public float CurrentAccuracy { get; private set; }
        [field: SerializeField] public float CurrentRecoil { get; private set; }
        [field: SerializeField] public float CurrentMaxWeaponRange { get; private set; }
        
        [field: Header("Current Weapon State")]
        [field: SerializeField] public bool IsRecoiling { get; private set; }
        [field: SerializeField] public bool IsEmpty { get; private set; }
        [field: SerializeField] public bool IsReloading { get; set; }
        
        // Fields
        private float timeSinceLastShotFired;
        private CoreManager coreManager;
        public bool IsAlreadyPlayedEmpty;

        // Properties
        public bool IsReady => !IsEmpty && !IsReloading && !IsRecoiling;
        public bool IsReadyToReload => CurrentMaxAmmoCountInMagazine > CurrentAmmoCountInMagazine && !IsReloading && CurrentAmmoCount > 0;
        
        private void Awake()
        {
            if (!BulletSpawnPoint) BulletSpawnPoint = this.TryGetChildComponent<Transform>("BulletSpawnPoint");
            if (!muzzleFlashParticle) muzzleFlashParticle = this.TryGetChildComponent<ParticleSystem>("MuzzleFlashParticle");
            if (!lightCurves) lightCurves = this.TryGetChildComponent<LightCurves>("LightCurves");
            if (WeaponParts.Count <= 0)
            {
                var weaponPartList = GetComponentsInChildren<WeaponPart>(true);
                foreach(var weaponPart in weaponPartList) WeaponParts.Add(weaponPart.Data.Id, weaponPart);
            }
        }

        private void Reset()
        {
            if (!BulletSpawnPoint) BulletSpawnPoint = this.TryGetChildComponent<Transform>("BulletSpawnPoint");
            if (!muzzleFlashParticle) muzzleFlashParticle = this.TryGetChildComponent<ParticleSystem>("MuzzleFlashParticle");
            if (!lightCurves) lightCurves = this.TryGetChildComponent<LightCurves>("LightCurves");
            if (WeaponParts.Count <= 0)
            {
                var weaponPartList = GetComponentsInChildren<WeaponPart>(true);
                foreach(var weaponPart in weaponPartList) WeaponParts.Add(weaponPart.Data.Id, weaponPart);
            }
        }

        private void Update()
        {
            if (!IsRecoiling || coreManager.gameManager.IsGamePaused) return;
            timeSinceLastShotFired += Time.unscaledDeltaTime;
            
            if (!(timeSinceLastShotFired >= 60f / HackData.HackStat.Rpm)) return;
            timeSinceLastShotFired = 0f;
            IsRecoiling = false;
        }

        public override void Initialize(GameObject ownerObj, DataTransferObject dto = null)
        {
            coreManager = CoreManager.Instance;
            timeSinceLastShotFired = 0f;
            IsRecoiling = false;
            CurrentMaxAmmoCountInMagazine = HackData.HackStat.MaxAmmoCountInMagazine;
            CurrentRecoil = HackData.HackStat.Recoil;
            CurrentMaxWeaponRange = HackData.HackStat.MaxWeaponRange;
            
            owner = ownerObj;
            if (!ownerObj.TryGetComponent(out Player user)) return;
            player = user;
            isOwnedByPlayer = true;
            if (dto != null)
            {
                var weapon = dto.weapons[HackData.HackStat.Type];
                CurrentAmmoCount = weapon.currentAmmoCount;
                CurrentAmmoCountInMagazine = weapon.currentAmmoCountInMagazine;
                if (CurrentAmmoCountInMagazine <= 0) IsEmpty = true;
                
                foreach(var part in weapon.equipableParts) EquipableWeaponParts.Add(part.Key, part.Value);
                foreach (var part in weapon.equippedParts) WeaponParts[part.Value].OnWear();
            }
            else
            {
                CurrentAmmoCount = HackData.HackStat.MaxAmmoCount;
                CurrentAmmoCountInMagazine = HackData.HackStat.MaxAmmoCountInMagazine;
                
                foreach (var part in WeaponParts) EquipableWeaponParts.Add(part.Key, part.Value.Data.IsBasicPart);
                foreach (var part in WeaponParts.Where(val => val.Value.Data.IsBasicPart)) 
                    part.Value.OnWear();
            }
            face = user.CameraPivot;
        }

        public override bool OnShoot()
        {
            if (!IsReady)
            {
                if (!IsEmpty || IsAlreadyPlayedEmpty) return false;
                IsAlreadyPlayedEmpty = true;
                CoreManager.Instance.soundManager.PlaySFX(SfxType.HackGunEmpty, BulletSpawnPoint.position);
                CoreManager.Instance.uiManager.GetUI<WeaponUI>()?.PlayEmptyFlash();
                return false;
            }
            
            if (Physics.Raycast(BulletSpawnPoint.position, GetDirectionOfBullet(), out var hit, CurrentMaxWeaponRange, HittableLayer))
            {
                if (hit.collider.TryGetComponent(out IHackable hackable))
                {
                    var distance = Vector3.Distance(BulletSpawnPoint.position, hit.point);
                    hackable.Hacking(CalculateChance(distance));
                } else if (hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("Wall")))
                {
                    var bulletHole = CoreManager.Instance.objectPoolManager.Get("BulletHole_Wall");
                    bulletHole.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                    bulletHole.transform.SetParent(hit.transform);
                } else if (hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("Ground")))
                {
                    var bulletHole = CoreManager.Instance.objectPoolManager.Get("BulletHole_Ground");
                    bulletHole.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                    bulletHole.transform.SetParent(hit.transform);
                }
            }
            
            IsRecoiling = true;
            if (player) player.PlayerRecoil.ApplyRecoil(-CurrentRecoil * player.PlayerCondition.RecoilMultiplier);
            
            // Play VFX
            if (lightCurves) StartCoroutine(Flicker());
            if (muzzleFlashParticle.isPlaying) muzzleFlashParticle.Stop();
            muzzleFlashParticle.Play();
            
            // Play Randomized Gun Shooting Sound
            CoreManager.Instance.soundManager
                .PlaySFX(SfxType.HackGunShoot, BulletSpawnPoint.position);
            
            CurrentAmmoCountInMagazine--;
            if (CurrentAmmoCountInMagazine <= 0)
            {
                IsEmpty = true;
                if (player)
                    player.PlayerWeapon.WeaponAnimators[player.PlayerCondition.EquippedWeaponIndex]
                        .SetBool(player.AnimationData.EmptyParameterHash, true);
            }
            
            if (IsAlreadyPlayedEmpty) IsAlreadyPlayedEmpty = false;
            if (player) player.PlayerCondition.IsAttacking = false;
            return true;
        }

        public bool OnReload()
        {
            int reloadableAmmoCount;
            if (isOwnedByPlayer)
                reloadableAmmoCount = Mathf.Min(CurrentMaxAmmoCountInMagazine - CurrentAmmoCountInMagazine, CurrentAmmoCount);
            else reloadableAmmoCount = CurrentMaxAmmoCountInMagazine - CurrentAmmoCount;

            if (reloadableAmmoCount <= 0) return false;

            CurrentAmmoCount -= reloadableAmmoCount;
            CurrentAmmoCountInMagazine += reloadableAmmoCount;
            IsEmpty = CurrentAmmoCountInMagazine <= 0;
            coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
            return true;
        }

        public override bool OnRefillAmmo(int ammo)
        {
            if (CurrentAmmoCount >= HackData.HackStat.MaxAmmoCount) return false;
            CurrentAmmoCount = Mathf.Min(CurrentAmmoCount + ammo, HackData.HackStat.MaxAmmoCount);
            return true;
        }
        
        public override bool TryForgeWeapon() { return false; }
        
        public void UnlockAllParts()
        {
            var keys = EquipableWeaponParts.Keys.ToArray();
            foreach (var key in keys) EquipableWeaponParts[key] = true;
        }
        
        public override void UpdateStatValues(WeaponPart data, bool isWorn = true)
        {
            if (isWorn)
            {
                CurrentRecoil -= data.Data.ReduceRecoilRate * HackData.HackStat.Recoil;
                CurrentMaxWeaponRange += data.Data.IncreaseDistanceRate * HackData.HackStat.MaxWeaponRange;
                
                if (data.Data.Type == PartType.ExtendedMag)
                {
                    CurrentMaxAmmoCountInMagazine += data.Data.IncreaseMaxAmmoCountInMagazine;
                    // TODO: Refresh Ammo UI
                }
                
                EquippedWeaponParts.TryAdd(data.Data.Type, data.Data.Id);
            }
            else
            {
                CurrentRecoil += data.Data.ReduceRecoilRate * HackData.HackStat.Recoil;
                CurrentMaxWeaponRange -= data.Data.IncreaseDistanceRate * HackData.HackStat.MaxWeaponRange;
                
                if (data.Data.Type == PartType.ExtendedMag)
                {
                    if (CurrentAmmoCountInMagazine > HackData.HackStat.MaxAmmoCountInMagazine)
                        CurrentAmmoCount += CurrentAmmoCountInMagazine - HackData.HackStat.MaxAmmoCountInMagazine;
                    CurrentMaxAmmoCountInMagazine -= data.Data.IncreaseMaxAmmoCountInMagazine;
                    // TODO: Refresh Ammo UI
                }
                
                EquippedWeaponParts.Remove(data.Data.Type);
            }
        }
        
        private Vector3 GetDirectionOfBullet()
        {
            Vector3 targetPoint;
            
            if (Physics.Raycast(face.position, face.forward, out var hit, CurrentMaxWeaponRange,
                    HittableLayer)) { targetPoint = hit.point; }
            else { targetPoint = face.position + face.forward * CurrentMaxWeaponRange; }

            return (targetPoint - BulletSpawnPoint.position).normalized;
        }

        private float CalculateChance(float distance)
        {
            if (distance >= HackData.HackStat.MaxDistance) return 0f;
            if (distance >= HackData.HackStat.MinDistance)
            {
                var distanceRatio = 1 - (distance - HackData.HackStat.MinDistance) /
                    (HackData.HackStat.MaxDistance - HackData.HackStat.MinDistance);
                return HackData.HackStat.MinChance + 
                       (HackData.HackStat.MaxChance - HackData.HackStat.MinChance) * distanceRatio;
            }
            return HackData.HackStat.MaxChance;
        }
        
        private IEnumerator Flicker()
        {
            lightCurves.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(lightCurves.GraphTimeMultiplier);
            lightCurves.gameObject.SetActive(false);
        }
    }
}