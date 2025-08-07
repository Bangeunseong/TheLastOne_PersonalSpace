using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Weapon;
using _1.Scripts.Manager.Data;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using AYellowpaper.SerializedCollections;
using JetBrains.Annotations;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Common
{
    public enum WeaponType
    {
        Punch,
        Rifle,
        Pistol,
        SniperRifle,
        GrenadeLauncher,
        HackGun,
    }
    
    public abstract class BaseWeapon : MonoBehaviour, IShootable
    {
        [Header("Owner")] 
        [SerializeField] protected GameObject owner;
        
        [Header("Components")] 
        [SerializeField] protected ParticleSystem muzzleFlashParticle;
        [SerializeField] protected LightCurves lightCurves;
        
        [field: Header("Hittable Layer")]
        [field: SerializeField] public LayerMask HittableLayer { get; protected set; }
        
        [field: Header("Weapon Parts")]
        [field: SerializeField] public SerializedDictionary<int, WeaponPart> WeaponParts { get; private set; }
        [field: SerializeField] public SerializedDictionary<PartType, int> EquippedWeaponParts { get; private set; } = new();
        [field: SerializeField] public SerializedDictionary<int, bool> EquipableWeaponParts { get; private set; } = new();
        
        // Fields
        [CanBeNull] protected Player player;
        // private Enemy enemy;
        protected bool isOwnedByPlayer;
        
        public bool TryCollectWeaponPart(int id)
        {
            if (!EquipableWeaponParts.TryGetValue(id, out bool value)) return false;
            if (value) return false;
            EquipableWeaponParts[id] = true;
            return true;
        }

        public bool TryEquipWeaponPart(PartType type, int id)
        {
            if (!WeaponParts.TryGetValue(id, out WeaponPart part)) return false;
            if (!EquipableWeaponParts.TryGetValue(id, out bool isEquipable)) return false;
            
            if (EquippedWeaponParts.TryGetValue(type, out int oldId))
            {
                if (WeaponParts.TryGetValue(oldId, out WeaponPart oldPart)) oldPart.OnUnWear();
            }
            
            part.OnWear();
            EquippedWeaponParts[type] = id;
            return true;
        }

        public bool TryUnequipWeaponPart(PartType type, int id)
        {
            if (!WeaponParts.TryGetValue(id, out WeaponPart part)) return false;
            if (!EquippedWeaponParts.TryGetValue(type, out int currentPartId)) return false;
            if (!currentPartId.Equals(id)) return false;
            
            part.OnUnWear();
            EquippedWeaponParts.Remove(type);
            return true;
        }

        public abstract bool TryForgeWeapon();
        public abstract void Initialize(GameObject ownerObj, DataTransferObject dto = null);
        public abstract bool OnShoot();
        public abstract bool OnRefillAmmo(int ammo);
        public abstract void UpdateStatValues(WeaponPart data, bool isWorn = true);
    }
}