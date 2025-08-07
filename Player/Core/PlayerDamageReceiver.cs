using System.Collections.Generic;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerDamageReceiver : MonoBehaviour
    {
        [Header("Components")] 
        [SerializeField] private PlayerCondition playerCondition;
        
        [field: Header("Damage Converters")]
        [field: SerializeField] public List<DamageConverter> DamageConverters { get; private set; } = new();

        private void Awake()
        {
            if (!playerCondition) playerCondition = this.TryGetComponent<PlayerCondition>();
            if (DamageConverters.Count <= 0) 
                DamageConverters.AddRange(GetComponentsInChildren<DamageConverter>(true));
        }

        private void Reset()
        {
            if (!playerCondition) playerCondition = this.TryGetComponent<PlayerCondition>();
            if (DamageConverters.Count <= 0) 
                DamageConverters.AddRange(GetComponentsInChildren<DamageConverter>(true));
        }

        public void Initialize()
        {
            // Initialize Damage Converters
            foreach (var converter in DamageConverters) converter.Initialize(playerCondition);
        }
    }
}