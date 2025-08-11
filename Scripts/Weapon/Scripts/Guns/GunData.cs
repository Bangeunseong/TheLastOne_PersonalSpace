using System;
using _1.Scripts.Weapon.Scripts.Common;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Guns
{
    [Serializable] public class GunStat : WeaponStat
    {
        [field: Header("Gun Settings")]
        [field: SerializeField] public WeaponType Type { get; private set; }
        [field: SerializeField] public int MaxAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public float Accuracy { get; private set; }
        [field: SerializeField] public float ReloadTime { get; private set; }
        [field: SerializeField] public float Rpm { get; private set; }
    }
    
    [CreateAssetMenu(fileName = "New GunData", menuName = "ScriptableObjects/Weapon/Create New GunData", order = 0)]
    public class GunData : ScriptableObject
    {
        [field: SerializeField] public GunStat GunStat { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
    }
}