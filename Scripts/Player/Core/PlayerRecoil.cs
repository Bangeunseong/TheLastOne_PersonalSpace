using System;
using _1.Scripts.Manager.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerRecoil : MonoBehaviour
    {
        [Header("Recoil Pivot")] 
        [SerializeField] private float recoilReturnSpeed = 5f;
        [SerializeField] private float recoilSnappiness = 10f;
        
        // Fields
        private CoreManager coreManager;
        private Player player;
        private Vector3 targetRotation;
        private Vector3 velocity;
        
        // Properties
        public Vector3 CurrentRotation { get; private set; }

        private void Start()
        {
            coreManager = CoreManager.Instance;
            player = coreManager.gameManager.Player;
        }

        private void Update()
        {
            CurrentRotation = Vector3.Lerp(CurrentRotation, targetRotation, Time.unscaledDeltaTime * recoilSnappiness);
            targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, Time.unscaledDeltaTime * recoilReturnSpeed);
            
            player.Pov.m_HorizontalAxis.Value += CurrentRotation.y * Time.unscaledDeltaTime;
            player.Pov.m_VerticalAxis.Value += CurrentRotation.x * Time.unscaledDeltaTime;
        }

        public void ApplyRecoil(float recoilX = -2f, float recoilY = 2f)
        {
            targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), 0f);
        }
    }
}