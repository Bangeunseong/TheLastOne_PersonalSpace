using System;
using _1.Scripts.Interfaces.Common;
using _1.Scripts.Interfaces.NPC;
using _1.Scripts.Interfaces.Weapon;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.Guns;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace _1.Scripts.Weapon.Scripts.Grenade
{
    public class Grenade : MonoBehaviour
    {
        [field: Header("Components")] 
        [field: SerializeField] public Rigidbody RigidBody { get; private set; }
        
        [field: Header("Grenade Settings")]
        [field: SerializeField] public float Radius { get; private set; } = 10f;
        [field: SerializeField] public float Force { get; private set; } = 50f;
        [field: SerializeField] public float Damage { get; private set; } = 10f;
        [field: SerializeField] public float StunDuration { get; private set; } = 3f;
        [field: SerializeField] public LayerMask HittableLayer { get; private set; }
        
        private CoreManager coreManager;
        private bool isExploded;

        private void Awake()
        {
            if (!RigidBody) RigidBody = this.TryGetComponent<Rigidbody>();
        }

        private void Reset()
        {
            if (!RigidBody) RigidBody = this.TryGetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            isExploded = false;
        }

        private void Start()
        {
            coreManager = CoreManager.Instance;
        }

        public void Initialize(Vector3 position, Vector3 direction, LayerMask hittableLayer, float damage,
            float throwForce, float force, float radius, float delay, float stunDuration)
        {
            coreManager = CoreManager.Instance;
            HittableLayer = hittableLayer;
            Damage = damage;
            Force = force;
            Radius = radius;
            StunDuration = stunDuration;
            
            transform.SetPositionAndRotation(position, Quaternion.identity);
            RigidBody.AddForce(direction * throwForce, ForceMode.Impulse);
            Invoke(nameof(Explode), delay);
        }

        private void Explode()
        {
            if (isExploded) return;
            isExploded = true;

            var explosionParticle = coreManager.objectPoolManager.Get("EmpGrenadeExplosion");
            if (explosionParticle.TryGetComponent(out EmpExplosionParticle particle))
                particle.Initialize(transform.position, Quaternion.identity);
            coreManager.soundManager.PlaySFX(SfxType.GrenadeExplode, transform.position);
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, Radius, HittableLayer);
            foreach (var obj in colliders)
            {
                if (obj.gameObject.layer != LayerMask.NameToLayer("Bullet"))
                {
                    var rigidBody = obj.GetComponent<Rigidbody>();
                    if (rigidBody != null) rigidBody.AddExplosionForce(Force, transform.position, Radius);
                }

                if (((1 << obj.gameObject.layer) & HittableLayer.value) == 0) continue;
                if (obj.TryGetComponent(out IStunnable stunnable))
                    stunnable.OnStunned(StunDuration);
                if (obj.TryGetComponent(out IDamagable damagable))
                    damagable.OnTakeDamage(Mathf.CeilToInt(Damage));
            }
            Service.Log("Grenade exploded!");
            
            coreManager.objectPoolManager.Release(gameObject);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}