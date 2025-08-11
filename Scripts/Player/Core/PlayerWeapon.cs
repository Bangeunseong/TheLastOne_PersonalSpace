using System;
using System.Collections.Generic;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.Grenade;
using _1.Scripts.Weapon.Scripts.Guns;
using _1.Scripts.Weapon.Scripts.Hack;
using _1.Scripts.Weapon.Scripts.Melee;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using AYellowpaper.SerializedCollections;
using UnityEngine;

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