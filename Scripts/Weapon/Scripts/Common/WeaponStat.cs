using System;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Common
{
    [Serializable] public class WeaponStat
    {
        [field: Header("Basic Weapon Settings")]
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public int MaxAmmoCount { get; private set; }
        [field: SerializeField] public float MaxWeaponRange { get; private set; }
        [field: SerializeField] public float Recoil { get; private set; }
        [field: SerializeField] public int Damage { get; private set; }
        [field: SerializeField] public float WeightPenalty { get; private set; }
    }
}