using System;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.WeaponDetails
{
    [Serializable] public enum PartType
    {
        Sight,
        FlameArrester,
        Suppressor,
        Silencer,
        ExtendedMag,
    }
    
    [CreateAssetMenu(fileName = "New WeaponPart Data", menuName = "ScriptableObjects/Weapon/Create New WeaponPart Data", order = 0)]
    public class WeaponPartData : ScriptableObject
    {
        [field: Header("Part Settings")]
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public PartType Type { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public float IncreaseDistanceRate { get; private set; }
        [field: SerializeField] public float IncreaseAccuracyRate { get; private set; }
        [field: SerializeField] public int IncreaseMaxAmmoCountInMagazine { get; private set; }
        [field: SerializeField] public float ReduceRecoilRate { get; private set; }
        [field: SerializeField] public bool IsBasicPart { get; private set; }
        
        [field: SerializeField] public string NameKey { get; private set; }
        [field: SerializeField] public string DescKey { get; private set; }
    }
}