using System.Linq;
using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Weapon;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Manager.Subs;
using _1.Scripts.UI.InGame.HUD;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Grenade
{
    public class GrenadeLauncher : BaseWeapon, IReloadable
    {
        [field: Header("Gun Data")]
        [field: SerializeField] public GrenadeData GrenadeData { get; protected set; }
        
        [field: Header("Current Weapon Settings")]
        [field: SerializeField] public Transform ThrowPoint { get; private set; }
        [field: SerializeField] public int CurrentAmmoCount { get; private set; }
        [field: SerializeField] public int MaxAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public int CurrentAmmoCountInMagazine { get; private set; }
        
        [SerializeField] private Transform face;
        
        [field: Header("Current Weapon State")]
        [SerializeField] private bool isEmpty;
        [SerializeField] private bool isRecoiling;
        [field: SerializeField] public bool IsReloading { get; set; }
        
        private float timeSinceLastShotFired;
        private CoreManager coreManager;
        public bool IsAlreadyPlayedEmpty;
        
        public bool IsReady => !isEmpty && !IsReloading && !isRecoiling;
        public bool IsReadyToReload => MaxAmmoCountInMagazine > CurrentAmmoCountInMagazine && !IsReloading && CurrentAmmoCount > 0;

        private void Awake()
        {
            if (!ThrowPoint) ThrowPoint = this.TryGetChildComponent<Transform>("ThrowPoint");
            if (!muzzleFlashParticle)
                muzzleFlashParticle = this.TryGetChildComponent<ParticleSystem>("MuzzleFlashParticle");
            if (WeaponParts.Count <= 0)
            {
                var weaponPartList = GetComponentsInChildren<WeaponPart>(true);
                foreach(var weaponPart in weaponPartList) WeaponParts.Add(weaponPart.Data.Id, weaponPart);
            }
        }

        private void Reset()
        {
            if (!ThrowPoint) ThrowPoint = this.TryGetChildComponent<Transform>("ThrowPoint");
            if (!muzzleFlashParticle)
                muzzleFlashParticle = this.TryGetChildComponent<ParticleSystem>("MuzzleFlashParticle");
            if (WeaponParts.Count <= 0)
            {
                var weaponPartList = GetComponentsInChildren<WeaponPart>(true);
                foreach(var weaponPart in weaponPartList) WeaponParts.Add(weaponPart.Data.Id, weaponPart);
            }
        }

        private void Update()
        {
            if (!isRecoiling || coreManager.gameManager.IsGamePaused) return;
            timeSinceLastShotFired += Time.deltaTime;
            
            if (!(timeSinceLastShotFired >= 60f / GrenadeData.GrenadeStat.Rpm)) return;
            timeSinceLastShotFired = 0f;
            isRecoiling = false;
        }

        public override void Initialize(GameObject ownerObj, DataTransferObject dto = null)
        {
            coreManager = CoreManager.Instance;
            timeSinceLastShotFired = 0f;
            isRecoiling = false;
            MaxAmmoCountInMagazine = GrenadeData.GrenadeStat.MaxAmmoCountInMagazine;
            
            owner = ownerObj;
            if (!ownerObj.TryGetComponent(out Player user)) return;
            
            player = user;
            isOwnedByPlayer = true;
            if (dto != null)
            {
                var weapon = dto.weapons[GrenadeData.GrenadeStat.Type];
                CurrentAmmoCount = weapon.currentAmmoCount;
                CurrentAmmoCountInMagazine = weapon.currentAmmoCountInMagazine;
                if (CurrentAmmoCountInMagazine <= 0) isEmpty = true;
                
                foreach(var part in weapon.equipableParts) EquipableWeaponParts.Add(part.Key, part.Value);
                foreach (var part in weapon.equippedParts) WeaponParts[part.Value].OnWear();
            }
            else
            {
                CurrentAmmoCount = GrenadeData.GrenadeStat.MaxAmmoCount;
                CurrentAmmoCountInMagazine = GrenadeData.GrenadeStat.MaxAmmoCountInMagazine;
                
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
                if (isEmpty && !IsAlreadyPlayedEmpty)
                {
                    IsAlreadyPlayedEmpty = true;
                    CoreManager.Instance.soundManager.PlaySFX(SfxType.GrenadeLauncherEmpty, ThrowPoint.position);
                }
                return false;
            }
            
            var obj = CoreManager.Instance.objectPoolManager.Get(GrenadeData.GrenadeStat.GrenadePrefabId); 
            if (!obj.TryGetComponent(out Grenade grenade)) return false;
            grenade.Initialize(ThrowPoint.position, GetDirectionOfBullet(), HittableLayer,
                GrenadeData.GrenadeStat.Damage, GrenadeData.GrenadeStat.ThrowForce, GrenadeData.GrenadeStat.Force, 
                GrenadeData.GrenadeStat.Radius, GrenadeData.GrenadeStat.Delay, GrenadeData.GrenadeStat.StunDuration);
            
            isRecoiling = true;
            if (player) player.PlayerRecoil.ApplyRecoil(-GrenadeData.GrenadeStat.Recoil * player.PlayerCondition.RecoilMultiplier);
            
            // Play VFX
            if (muzzleFlashParticle.isPlaying) muzzleFlashParticle.Stop();
            muzzleFlashParticle.Play();
            
            // Play Randomized Gun Shooting Sound
            CoreManager.Instance.soundManager.PlaySFX(SfxType.GrenadeLauncherShoot, ThrowPoint.position);
            
            CurrentAmmoCountInMagazine--;
            if (CurrentAmmoCountInMagazine <= 0)
            {
                isEmpty = true;
                if (player)
                    player.PlayerWeapon.WeaponAnimators[player.PlayerCondition.EquippedWeaponIndex]
                        .SetBool(player.AnimationData.EmptyParameterHash, true);
            }

            if (IsAlreadyPlayedEmpty) IsAlreadyPlayedEmpty = false;
            if (player) player.PlayerCondition.IsAttacking = false;
            return true;
        }

        public override bool OnRefillAmmo(int ammo)
        {
            if (CurrentAmmoCount >= GrenadeData.GrenadeStat.MaxAmmoCount) return false;
            CurrentAmmoCount = Mathf.Min(CurrentAmmoCount + ammo, GrenadeData.GrenadeStat.MaxAmmoCount);
            coreManager.uiManager.GetUI<WeaponUI>()?.Refresh(false);

            return true;
        }

        public bool OnReload()
        {
            int reloadableAmmoCount;
            if (isOwnedByPlayer) 
                reloadableAmmoCount = Mathf.Min(MaxAmmoCountInMagazine - CurrentAmmoCountInMagazine, CurrentAmmoCount);
            else reloadableAmmoCount = MaxAmmoCountInMagazine - CurrentAmmoCount;
            
            if (reloadableAmmoCount <= 0) return false;
            
            if (isOwnedByPlayer) CurrentAmmoCount -= 1;
            CurrentAmmoCountInMagazine += 1;
            isEmpty = CurrentAmmoCountInMagazine <= 0;
            return true;
        }
        
        public override void UpdateStatValues(WeaponPart data, bool isWorn = true)
        {
            if (isWorn)
            {
                EquippedWeaponParts.TryAdd(data.Data.Type, data.Data.Id);
            }
            else
            {
                EquippedWeaponParts.Remove(data.Data.Type);
            }
        }

        public override bool TryForgeWeapon() { return false; }
        
        public void UnlockAllParts()
        {
            var keys = EquipableWeaponParts.Keys.ToArray();
            foreach (var key in keys) EquipableWeaponParts[key] = true;
        }

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
            Vector2 randomCirclePoint = Random.insideUnitCircle * GrenadeData.GrenadeStat.Accuracy;
            
            if (Physics.Raycast(face.position, face.forward, out var hit, GrenadeData.GrenadeStat.MaxWeaponRange,
                    HittableLayer))
            {
                GetOrthonormalBasis(hit.normal, out var right, out var up);
                if (isOwnedByPlayer)
                {
                    if (!player!.PlayerCondition.IsAiming)
                    {
                        targetPoint = hit.point + right * randomCirclePoint.x + up * randomCirclePoint.y;
                    } else targetPoint = hit.point;
                }
                else targetPoint = hit.point + right * randomCirclePoint.x + up * randomCirclePoint.y;
            }
            else
            {
                GetOrthonormalBasis(face.forward, out var right, out var up);
                
                if (isOwnedByPlayer)
                {
                    if (!player!.PlayerCondition.IsAiming)
                    {
                        targetPoint = face.position + face.forward * GrenadeData.GrenadeStat.MaxWeaponRange + right * randomCirclePoint.x + up * randomCirclePoint.y;
                    } else targetPoint = face.position + face.forward * GrenadeData.GrenadeStat.MaxWeaponRange;
                }
                else targetPoint = face.position + face.forward * GrenadeData.GrenadeStat.MaxWeaponRange + right * randomCirclePoint.x + up * randomCirclePoint.y;
            }

            return (targetPoint - ThrowPoint.position).normalized;
        }
    }
}