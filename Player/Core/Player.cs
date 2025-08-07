using _1.Scripts.Entity.Scripts.Player.Data;
using _1.Scripts.Entity.Scripts.Player.StateMachineScripts;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Subs;
using _1.Scripts.UI.InGame.HUD;
using Cinemachine;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerCondition), typeof(PlayerInteraction))]
    [RequireComponent(typeof(PlayerInput), typeof(PlayerGravity), typeof(PlayerRecoil))]
    [RequireComponent(typeof(PlayerInventory), typeof(PlayerWeapon), typeof(PlayerDamageReceiver))]
    
    public class Player : MonoBehaviour
    {
        [field: Header("Components")]
        [field: SerializeField] public Animator Animator { get; private set; }
        [field: SerializeField] public CharacterController Controller { get; private set; }
        [field: SerializeField] public PlayerInput PlayerInput { get; private set; }
        [field: SerializeField] public PlayerCondition PlayerCondition { get; private set; }
        [field: SerializeField] public PlayerDamageReceiver PlayerDamageReceiver { get; private set; }
        [field: SerializeField] public PlayerWeapon PlayerWeapon { get; private set; }
        [field: SerializeField] public PlayerInteraction PlayerInteraction { get; private set; }
        [field: SerializeField] public PlayerGravity PlayerGravity { get; private set; }
        [field: SerializeField] public PlayerRecoil PlayerRecoil { get; private set; }
        [field: SerializeField] public PlayerInventory PlayerInventory { get; private set; }
        
        [field: Header("Camera Components")]
        [field: SerializeField] public Transform MainCameraTransform { get; private set; }
        [field: SerializeField] public Transform CameraPivot { get; private set; }
        [field: SerializeField] public Transform CameraPoint { get; private set; }
        [field: SerializeField] public Transform IdlePivot { get; private set; }
        [field: SerializeField] public Transform CrouchPivot { get; private set; }
        [field: SerializeField] public CinemachineVirtualCamera FirstPersonCamera { get; private set; } // 플레이 전용
        [field: SerializeField] public CinemachineInputProvider InputProvider { get; private set; }
        [field: SerializeField] public CinemachinePOV Pov { get; private set; }
        
        [field: Header("Camera Settings")]
        [field: SerializeField] public float OriginalFoV { get; private set; }
        [field: SerializeField] public float ZoomFoV { get; private set; } = 40f;
        [field: SerializeField] public float TransitionTime { get; private set; } = 0.3f;
        
        [field: Header("Animation Data")] 
        [field: SerializeField] public AnimationData AnimationData { get; private set; } 
        
        [field: Header("StateMachine")] 
        [field: SerializeField] public PlayerStateMachine StateMachine { get; private set; }

        private CoreManager coreManager;
        
        // Properties
        public Camera Cam { get; private set; }
        public Vector3 OriginalOffset { get; private set; }
        public float OriginalHeight { get; private set; }

        private void Awake()
        {
            if (!Animator) Animator = this.TryGetComponent<Animator>();
            if (!Controller) Controller = this.TryGetComponent<CharacterController>();
            if (!PlayerCondition) PlayerCondition = this.TryGetComponent<PlayerCondition>();
            if (!PlayerDamageReceiver) PlayerDamageReceiver = this.TryGetComponent<PlayerDamageReceiver>();
            if (!PlayerWeapon) PlayerWeapon = this.TryGetComponent<PlayerWeapon>();
            if (!PlayerInteraction) PlayerInteraction = this.TryGetComponent<PlayerInteraction>();
            if (!PlayerInput) PlayerInput = this.TryGetComponent<PlayerInput>();
            if (!PlayerGravity) PlayerGravity = this.TryGetComponent<PlayerGravity>();
            if (!PlayerRecoil) PlayerRecoil = this.TryGetChildComponent<PlayerRecoil>();
            if (!PlayerInventory) PlayerInventory = this.TryGetChildComponent<PlayerInventory>();
            
            if (!CameraPivot) CameraPivot = this.TryGetChildComponent<Transform>("CameraPivot");
            if (!CameraPoint) CameraPoint = this.TryGetChildComponent<Transform>("CameraPoint");
            if (!IdlePivot) IdlePivot = this.TryGetChildComponent<Transform>("IdlePivot");
            if (!CrouchPivot) CrouchPivot = this.TryGetChildComponent<Transform>("CrouchPivot");
            
            if (!FirstPersonCamera) FirstPersonCamera = GameObject.Find("FirstPersonCamera")?.GetComponent<CinemachineVirtualCamera>();
            if (!InputProvider) InputProvider = FirstPersonCamera?.GetComponent<CinemachineInputProvider>();
            if (!Pov) Pov = FirstPersonCamera?.GetCinemachineComponent<CinemachinePOV>();
            
            AnimationData.Initialize();
        }

        private void Reset()
        {
            if (!Animator) Animator = this.TryGetComponent<Animator>();
            if (!Controller) Controller = this.TryGetComponent<CharacterController>();
            if (!PlayerCondition) PlayerCondition = this.TryGetComponent<PlayerCondition>();
            if (!PlayerDamageReceiver) PlayerDamageReceiver = this.TryGetComponent<PlayerDamageReceiver>();
            if (!PlayerWeapon) PlayerWeapon = this.TryGetComponent<PlayerWeapon>();
            if (!PlayerInteraction) PlayerInteraction = this.TryGetComponent<PlayerInteraction>();
            if (!PlayerInput) PlayerInput = this.TryGetComponent<PlayerInput>();
            if (!PlayerGravity) PlayerGravity = this.TryGetComponent<PlayerGravity>();
            if (!PlayerRecoil) PlayerRecoil = this.TryGetChildComponent<PlayerRecoil>();
            if (!PlayerInventory) PlayerInventory = this.TryGetChildComponent<PlayerInventory>();
            
            if (!CameraPivot) CameraPivot = this.TryGetChildComponent<Transform>("CameraPivot");
            if (!CameraPoint) CameraPoint = this.TryGetChildComponent<Transform>("CameraPoint");
            if (!IdlePivot) IdlePivot = this.TryGetChildComponent<Transform>("IdlePivot");
            if (!CrouchPivot) CrouchPivot = this.TryGetChildComponent<Transform>("CrouchPivot");
            
            if (!FirstPersonCamera) FirstPersonCamera = GameObject.Find("FirstPersonCamera")?.GetComponent<CinemachineVirtualCamera>();
            if (!InputProvider)InputProvider = FirstPersonCamera?.GetComponent<CinemachineInputProvider>();
            if (!Pov) Pov = FirstPersonCamera?.GetCinemachineComponent<CinemachinePOV>();
            
            AnimationData.Initialize();
        }

        // Start is called before the first frame update
        private void Start()
        {
            coreManager = CoreManager.Instance;
            
            FirstPersonCamera.Follow = CameraPoint;
            Cam = Camera.main;
            OriginalOffset = Controller.center;
            OriginalHeight = Controller.height;
            MainCameraTransform = Cam?.transform;
            OriginalFoV = FirstPersonCamera.m_Lens.FieldOfView;
            
            // Core Component 선언 -> Save Data에 영향을 받는 것들에게만 적용
            PlayerCondition.Initialize(coreManager.gameManager.SaveData);
            PlayerDamageReceiver.Initialize();
            PlayerInventory.Initialize(coreManager.gameManager.SaveData);
            PlayerWeapon.Initialize(coreManager.gameManager.SaveData);
            
            coreManager.uiManager.GetUI<InGameUI>().UpdateStateUI();
            coreManager.uiManager.GetUI<WeaponUI>().Refresh(false);
            
            StateMachine = new PlayerStateMachine(this);
            StateMachine.ChangeState(StateMachine.IdleState);
            
            coreManager.SaveData_QueuedAsync();
        }

        private void FixedUpdate()
        {
            StateMachine.PhysicsUpdate();
        }

        // Update is called once per frame
        private void Update()
        {
            StateMachine.HandleInput();
            StateMachine.Update();
            
            PlayerCondition.OnAttack();
        }

        private void LateUpdate()
        {
            StateMachine.LateUpdate();
        }

        /// <summary>
        /// Play Foot Step Sound
        /// </summary>
        /// <param name="animationEvent"></param>
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                CoreManager.Instance.soundManager.PlaySFX(SfxType.PlayerFootStep, transform.position);
            }
        }

        /// <summary>
        /// Play Land Sound
        /// </summary>
        /// <param name="animationEvent"></param>
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                CoreManager.Instance.soundManager.PlaySFX(SfxType.PlayerLand, transform.position);
            }
        }
    }
}
