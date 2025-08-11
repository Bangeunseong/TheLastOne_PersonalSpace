using System.Collections;
using System.Linq;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Common;
using _1.Scripts.Interfaces.Weapon;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Manager.Subs;
using _1.Scripts.UI.InGame.HUD;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Guns
{
    public class Gun : BaseWeapon, IReloadable
    {
        [field: Header("Gun Data")]
        [field: SerializeField] public GunData GunData { get; protected set; }
        
        [field: Header("Current Weapon Settings")]
        [field: SerializeField] public Transform BulletSpawnPoint { get; private set; }
        [field: SerializeField] public Transform Face { get; private set; }
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
            
            if (!(timeSinceLastShotFired >= 60f / GunData.GunStat.Rpm)) return;
            timeSinceLastShotFired = 0f;
            IsRecoiling = false;
        }

        public override void Initialize(GameObject ownerObj, DataTransferObject dto = null)
        {
            coreManager = CoreManager.Instance;
            timeSinceLastShotFired = 0f;
            IsRecoiling = false;
            CurrentMaxAmmoCountInMagazine = GunData.GunStat.MaxAmmoCountInMagazine;
            CurrentAccuracy = GunData.GunStat.Accuracy;
            CurrentRecoil = GunData.GunStat.Recoil;
            CurrentMaxWeaponRange = GunData.GunStat.MaxWeaponRange;
            
            owner = ownerObj;
            if (ownerObj.TryGetComponent(out Player user))
            {
                player = user;
                isOwnedByPlayer = true;
                if (dto != null)
                {
                    var weapon = dto.weapons[GunData.GunStat.Type];
                    CurrentAmmoCount = weapon.currentAmmoCount;
                    CurrentAmmoCountInMagazine = weapon.currentAmmoCountInMagazine;
                    if (CurrentAmmoCountInMagazine <= 0) IsEmpty = true;
                    
                    foreach(var part in weapon.equipableParts) EquipableWeaponParts.Add(part.Key, part.Value);
                    foreach (var part in weapon.equippedParts) WeaponParts[part.Value].OnWear();
                }
                else
                {
                    CurrentAmmoCount = GunData.GunStat.MaxAmmoCount;
                    CurrentAmmoCountInMagazine = GunData.GunStat.MaxAmmoCountInMagazine;
                    
                    foreach (var part in WeaponParts) EquipableWeaponParts.Add(part.Key, part.Value.Data.IsBasicPart);
                    foreach (var part in WeaponParts.Where(val => val.Value.Data.IsBasicPart))
                    {
                        Service.Log($"Worn Part : {part.Key}");
                        part.Value.OnWear();
                    }
                }
                    
                Face = user.CameraPivot;
            }
            // else if (owner.TryGetComponent(out Enemy enemy)) this.enemy = enemy;
        }

        public override bool OnShoot()
        {
            if (!IsReady)
            {
                if (!IsEmpty || IsAlreadyPlayedEmpty) return false;
                IsAlreadyPlayedEmpty = true;
                CoreManager.Instance.soundManager.PlaySFX(
                    GunData.GunStat.Type switch
                    {
                        WeaponType.Pistol => SfxType.PistolEmpty,
                        WeaponType.Rifle => SfxType.RifleEmpty,
                        _ => SfxType.SniperRifleEmpty
                    }, BulletSpawnPoint.position);
                CoreManager.Instance.uiManager.GetUI<WeaponUI>()?.PlayEmptyFlash();
                return false;
            }
            
            if (Physics.Raycast(BulletSpawnPoint.position, GetDirectionOfBullet(), out var hit, float.MaxValue, HittableLayer))
            {
                if (hit.collider.TryGetComponent(out IDamagable damagable))
                {
                    if (Vector3.Distance(BulletSpawnPoint.position, hit.point) <= CurrentMaxWeaponRange)
                        damagable.OnTakeDamage(GunData.GunStat.Damage);
                    if (isOwnedByPlayer)
                    {
                        bool isHeadShot = hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("Head_E"));
                        coreManager.uiManager.GetUI<InGameUI>().CrosshairController.ShowHitMarker(isHeadShot);
                        if (isHeadShot) coreManager.soundManager.PlaySFX(SfxType.HeadShotSound, transform.position, index:0);
                    }
                } else if(hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("Wall")))
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
                .PlaySFX(GunData.GunStat.Type switch
                    {
                        WeaponType.Pistol => SfxType.PistolShoot,
                        WeaponType.Rifle => SfxType.RifleShoot,
                        _ => SfxType.SniperRifleShoot
                    }, 
                BulletSpawnPoint.position, -1);
            
            CurrentAmmoCountInMagazine--;
            if (CurrentAmmoCountInMagazine <= 0)
            {
                IsEmpty = true;
                if (player)
                    player.PlayerWeapon.WeaponAnimators[player.PlayerCondition.EquippedWeaponIndex]
                        .SetBool(player.AnimationData.EmptyParameterHash, true);
            }
            
            if (IsAlreadyPlayedEmpty) IsAlreadyPlayedEmpty = false;
            if (GunData.GunStat.Type == WeaponType.Rifle) return true;
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
            return true;
        }
        
        public override bool OnRefillAmmo(int ammo)
        {
            if (CurrentAmmoCount >= GunData.GunStat.MaxAmmoCount) return false;
            CurrentAmmoCount = Mathf.Min(CurrentAmmoCount + ammo, GunData.GunStat.MaxAmmoCount);
            coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
            return true;
        }
        
        public override void UpdateStatValues(WeaponPart data, bool isWorn = true)
        {
            if (isWorn)
            {
                CurrentAccuracy -= data.Data.IncreaseAccuracyRate * GunData.GunStat.Accuracy;
                CurrentRecoil -= data.Data.ReduceRecoilRate * GunData.GunStat.Recoil;
                CurrentMaxWeaponRange += data.Data.IncreaseDistanceRate * GunData.GunStat.MaxWeaponRange;

                if (data.Data.Type == PartType.ExtendedMag)
                {
                    CurrentMaxAmmoCountInMagazine += data.Data.IncreaseMaxAmmoCountInMagazine;
                    coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
                }
                
                EquippedWeaponParts[data.Data.Type] = data.Data.Id;
            }
            else
            {
                CurrentAccuracy += data.Data.IncreaseAccuracyRate * GunData.GunStat.Accuracy;
                CurrentRecoil += data.Data.ReduceRecoilRate * GunData.GunStat.Recoil;
                CurrentMaxWeaponRange -= data.Data.IncreaseDistanceRate * GunData.GunStat.MaxWeaponRange;

                if (data.Data.Type == PartType.ExtendedMag)
                {
                    if (CurrentAmmoCountInMagazine > GunData.GunStat.MaxAmmoCountInMagazine)
                        CurrentAmmoCount += CurrentAmmoCountInMagazine - GunData.GunStat.MaxAmmoCountInMagazine;
                    CurrentMaxAmmoCountInMagazine -= data.Data.IncreaseMaxAmmoCountInMagazine;
                    coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);
                }
                
                EquippedWeaponParts.Remove(data.Data.Type);
            }
        }

        public override bool TryForgeWeapon()
        {
            if (!player || GunData.GunStat.Type != WeaponType.Pistol) return false;
            var availableParts = new bool[3];

            foreach (var available in EquipableWeaponParts.Where(val => val.Value))
            {
                if (!WeaponParts.TryGetValue(available.Key, out var part)) continue;
                switch (part.Data.Type)
                {
                    case PartType.Sight: availableParts[0] = true; break;
                    case PartType.ExtendedMag: availableParts[1] = true; break;
                    case PartType.Silencer: availableParts[2] = true; break;
                }
            }

            if (!availableParts.All(val => val)) return false;
            
            player.PlayerWeapon.AvailableWeapons[WeaponType.Pistol] = false;
            player.PlayerWeapon.AvailableWeapons[WeaponType.SniperRifle] = true;
            player.PlayerCondition.OnSwitchWeapon(WeaponType.SniperRifle, 0.5f);
            return true;
        }

        public void UnlockAllParts()
        {
            var keys = EquipableWeaponParts.Keys.ToArray();
            foreach (var key in keys) EquipableWeaponParts[key] = true;
        }

        /* - Utility Method - */
        private void GetOrthonormalBasis(Vector3 forward, out Vector3 right, out Vector3 up)
        {
            right = Vector3.Cross(forward, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(forward, Vector3.right);
            right.Normalize();
    
            up = Vector3.Cross(right, forward).normalized;
        }
        private Vector3 GetDirectionOfBullet()
        {
            Vector3 targetPoint;
            Vector2 randomCirclePoint = Random.insideUnitCircle * CurrentAccuracy;
            
            if (Physics.Raycast(Face.position, Face.forward, out var hit, CurrentMaxWeaponRange,
                    HittableLayer))
            {
                GetOrthonormalBasis(hit.normal, out var right, out var up);
                if (isOwnedByPlayer)
                {
                    if (!player!.PlayerCondition.IsAiming)
                    {
                        var distance = Vector3.Distance(hit.point, Face.position);
                        randomCirclePoint *= distance / CurrentMaxWeaponRange;
                        targetPoint = hit.point + right * randomCirclePoint.x + up * randomCirclePoint.y;
                    } else targetPoint = hit.point;
                }
                else targetPoint = hit.point + right * randomCirclePoint.x + up * randomCirclePoint.y;
            }
            else
            {
                GetOrthonormalBasis(Face.forward, out var right, out var up);
                
                if (isOwnedByPlayer)
                {
                    if (!player!.PlayerCondition.IsAiming)
                    {
                        targetPoint = Face.position + Face.forward * CurrentMaxWeaponRange + right * randomCirclePoint.x + up * randomCirclePoint.y;
                    } else targetPoint = Face.position + Face.forward * CurrentMaxWeaponRange;
                }
                else targetPoint = Face.position + Face.forward * CurrentMaxWeaponRange + right * randomCirclePoint.x + up * randomCirclePoint.y;
            }

            return (targetPoint - BulletSpawnPoint.position).normalized;
        }
        private IEnumerator Flicker()
        {
            lightCurves.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(lightCurves.GraphTimeMultiplier);
            lightCurves.gameObject.SetActive(false);
        }
        /* ------------------- */
    }
}