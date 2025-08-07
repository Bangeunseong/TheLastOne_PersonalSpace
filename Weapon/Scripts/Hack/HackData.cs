using System;
using _1.Scripts.Weapon.Scripts.Common;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Hack
{
    [Serializable] public class HackStat : WeaponStat
    {
        [field: Header("Hack Gun Settings")]
        [field: SerializeField] public WeaponType Type { get; private set; }
        [field: SerializeField] public int MaxAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public float ReloadTime { get; private set; }
        [field: SerializeField] public float Rpm { get; private set; }
        [field: SerializeField] public float MaxChance { get; private set; } = 0.75f;
        [field: SerializeField] public float MinChance { get; private set; } = 0.25f;
        [field: SerializeField] public float MaxDistance { get; private set; } = 25;
        [field: SerializeField] public float MinDistance { get; private set; } = 5;
    }
    
    [CreateAssetMenu(fileName = "New HackData", menuName = "ScriptableObjects/Weapon/Create New HackData", order = 0)]
    public class HackData : ScriptableObject
    {
        [field: SerializeField] public HackStat HackStat { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
    }
}