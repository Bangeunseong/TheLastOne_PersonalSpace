using System;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.Guns;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Grenade
{
    [Serializable] public class GrenadeStat : WeaponStat
    {
        [field: Header("Grenade Settings")]
        [field: SerializeField] public float Force { get; private set; } // 밀쳐지는 힘
        [field: SerializeField] public float Radius { get; private set; } // 반경
        [field: SerializeField] public float Delay { get; private set; } // 수류탄이 터지기 전 딜레이
        [field: SerializeField] public float StunDuration { get; private set; }
        [field: SerializeField] public string GrenadePrefabId { get; private set; }
        
        [field: Header("Grenade Launcher Settings")]
        [field: SerializeField] public WeaponType Type { get; private set; }
        [field: SerializeField] public int MaxAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public float Accuracy { get; private set; }
        [field: SerializeField] public float ReloadTime { get; private set; }
        [field: SerializeField] public float ThrowForce { get; private set; } // 던지는 힘
        [field: SerializeField] public float Rpm { get; private set; } // 다음 수류탄을 던지는데 걸리는 딜레이
    }
    
    [CreateAssetMenu(fileName = "New GrenadeData", menuName = "ScriptableObjects/Weapon/Create New GrenadeData", order = 0)]
    public class GrenadeData : ScriptableObject
    {
        [field: SerializeField] public GrenadeStat GrenadeStat { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }

    }
}