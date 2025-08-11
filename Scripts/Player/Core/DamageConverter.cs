using _1.Scripts.Interfaces.Common;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class DamageConverter : MonoBehaviour, IDamagable
    {
        [field: Header("Damage Multiplier")] 
        [field: SerializeField] public float DamageMultiplier { get; private set; }

        private PlayerCondition condition;
        
        public void Initialize(PlayerCondition entity)
        {
            condition = entity;
        }
        
        public void OnTakeDamage(int damage)
        {
            Service.Log($"플레이어 데미지 입음 {damage}");
            condition.OnTakeDamage(Mathf.CeilToInt(damage *  DamageMultiplier));
        }
    }
}