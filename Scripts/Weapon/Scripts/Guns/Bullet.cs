using System;
using _1.Scripts.Interfaces.Common;
using _1.Scripts.Manager.Core;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Guns
{
    public class Bullet : MonoBehaviour
    {
        [Header("Components")] 
        [SerializeField] private Rigidbody rigidBody;
        [SerializeField] private TrailRenderer trailRenderer;
        
        [Header("Bullet Presets")] 
        [SerializeField] private float appliedForce;
        [SerializeField] private float maxMoveDistance;
        [SerializeField] private Vector3 initializedPosition;
        [SerializeField] private int damage;
        [SerializeField] private LayerMask hittableLayer;
        [SerializeField] private Vector3 direction;
        
        [Header("Bullet Settings")]
        [SerializeField] private float drag;

        private bool isAlreadyReached;
        private bool alreadyAppliedDamage;

        private void Awake()
        {
            if (!rigidBody) rigidBody = this.TryGetComponent<Rigidbody>();
            if (!trailRenderer) trailRenderer = this.TryGetComponent<TrailRenderer>();
        }

        private void Reset()
        {
            if (!rigidBody) rigidBody = this.TryGetComponent<Rigidbody>();
            if (!trailRenderer) trailRenderer = this.TryGetComponent<TrailRenderer>();
        }

        private void OnEnable()
        {
            isAlreadyReached = false;
            rigidBody.useGravity = false;
            rigidBody.drag = 0f;
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (isAlreadyReached) return;
            if (!((transform.position - initializedPosition).magnitude > maxMoveDistance)) return;
            isAlreadyReached = true;
            CoreManager.Instance.objectPoolManager.Release(gameObject);
        }

        private void OnDisable()
        {
            trailRenderer.Clear();
        }

        public void Initialize(Vector3 position, Vector3 dir, float maxDistance, float force, int dealtDamage, LayerMask hitLayer)
        {
            transform.position = position;
            initializedPosition = position;
            transform.rotation = Quaternion.LookRotation(dir);
            
            damage = dealtDamage;
            appliedForce = force; 
            maxMoveDistance = maxDistance;
            direction = dir;
            hittableLayer = hitLayer;

            rigidBody.AddForce(direction.normalized * appliedForce, ForceMode.Impulse);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & hittableLayer) != 0)
            {
                // Debug.Log(other.gameObject.layer);
                if (other.TryGetComponent(out IDamagable damagable)){ damagable.OnTakeDamage(damage); }
                // else if()
                CoreManager.Instance.objectPoolManager.Release(gameObject);
            }
        }
    }
}