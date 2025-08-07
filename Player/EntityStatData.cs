using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Common
{
    [CreateAssetMenu(fileName = "Stats", menuName = "ScriptableObjects/Entity")]
    public class EntityStatData : ScriptableObject
    {
        [Header("Entity type")] 
        public string entityName;
        public bool isPlayer;
        public bool isAlly;

        [Header("Stats")] 
        public int maxHealth;
        public int baseDamage;
        public float baseAttackRate;
        public float armor;
        public float maxArmor;
        
        [Header("Movement")] 
        public float moveSpeed;
        public float runMultiplier;
        public float walkMultiplier;
        public float airMultiplier;

        [Header("Audio")] 
        public AudioClip[] footStepSounds;
        public AudioClip[] hitSounds;
        public AudioClip[] deathSounds;
        
        [Header("SpawnIndex")]
        public int spawnIndex;
    }
}