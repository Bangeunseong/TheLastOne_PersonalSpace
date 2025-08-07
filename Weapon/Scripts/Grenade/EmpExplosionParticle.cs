using System.Collections;
using _1.Scripts.Manager.Core;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Grenade
{
    public class EmpExplosionParticle : MonoBehaviour
    {
        [field: Header("Particle System")]
        [field: SerializeField] public ParticleSystem ParticleSystem { get; private set; }
        
        private CoreManager coreManager;
        
        private void Awake()
        {
            if (!ParticleSystem) ParticleSystem = this.TryGetComponent<ParticleSystem>();
        }

        private void Reset()
        {
            if (!ParticleSystem) ParticleSystem = this.TryGetComponent<ParticleSystem>();
        }

        private void Start()
        {
            coreManager = CoreManager.Instance;
        }

        public void Initialize(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            ParticleSystem.Play();
            StartCoroutine(Release());
        }

        private IEnumerator Release()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (!ParticleSystem.IsAlive()) break;
            }
            coreManager.objectPoolManager.Release(gameObject);
        }
    }
}