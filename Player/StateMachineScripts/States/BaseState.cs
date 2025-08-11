using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Interfaces.Player;
using _1.Scripts.Item.Items;
using _1.Scripts.Manager.Core;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.Grenade;
using _1.Scripts.Weapon.Scripts.Guns;
using _1.Scripts.Weapon.Scripts.Hack;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States
{
    public class BaseState : IState
    {
        protected readonly PlayerStateMachine stateMachine;
        protected readonly PlayerCondition playerCondition;
        protected readonly PlayerWeapon playerWeapon;
        protected readonly CoreManager coreManager;
        
        private float speed;
        private float smoothVelocity = 10f;
        private Vector3 recoilEuler;
        
        public BaseState(PlayerStateMachine machine)
        {
            stateMachine = machine;
            coreManager = CoreManager.Instance;
            playerCondition = stateMachine.Player.PlayerCondition;
            playerWeapon = stateMachine.Player.PlayerWeapon;
        }
        
        public virtual void Enter()
        {
            AddInputActionCallbacks();
        }

        public virtual void HandleInput()
        {
            if (coreManager.gameManager.IsGamePaused || !playerCondition.IsPlayerHasControl) { stateMachine.MovementDirection = Vector2.zero; return; }
            ReadMovementInput();
        }

        public virtual void PhysicsUpdate()
        {
            
        }

        public virtual void Update()
        {
            Move();
        }

        public virtual void LateUpdate()
        {
            var baseForward = stateMachine.Player.MainCameraTransform.forward;
            var baseRot = Quaternion.LookRotation(baseForward);
            
            var rotatedForward = baseRot * Vector3.forward;
            Rotate(rotatedForward);
        }

        public virtual void Exit()
        {
            RemoveInputActionCallbacks();
        }
        
        protected void StartAnimation(int animatorHash)
        {
            stateMachine.Player.Animator.SetBool(animatorHash, true);
        }

        protected void StopAnimation(int animatorHash)
        {
            stateMachine.Player.Animator.SetBool(animatorHash, false);
        }
        
        protected virtual void ReadMovementInput()
        {
            stateMachine.MovementDirection =
                stateMachine.Player.PlayerInput.PlayerActions.Move.ReadValue<Vector2>();
        }
        
        private void Move()
        {
            var movementDirection = GetMovementDirection();
            Move(movementDirection);
        }

        private Vector3 GetMovementDirection()
        {
            var forward = stateMachine.MainCameraTransform.forward;
            var right = stateMachine.MainCameraTransform.right;
            
            forward.y = 0;
            right.y = 0;
            
            forward.Normalize();
            right.Normalize();
            
            return (forward * stateMachine.MovementDirection.y + right * stateMachine.MovementDirection.x).normalized;
        }

        private void Move(Vector3 direction)
        {
            var targetSpeed = GetMovementSpeed();
            var currentHorizontalSpeed = new Vector3(stateMachine.Player.Controller.velocity.x, 0f, stateMachine.Player.Controller.velocity.z).magnitude * Time.timeScale;

            if (currentHorizontalSpeed < targetSpeed - 0.1f || currentHorizontalSpeed > targetSpeed + 0.1f)
            {
                speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.unscaledDeltaTime * smoothVelocity);
                speed = Mathf.Round(speed * 1000f) / 1000f;
            }
            else speed = targetSpeed;
            
            // Set Animator Speed Parameter (Only Applied to Activated Animator)
            if (playerWeapon.WeaponAnimators[playerCondition.EquippedWeaponIndex].isActiveAndEnabled)
                playerWeapon.WeaponAnimators[playerCondition.EquippedWeaponIndex]
                    .SetFloat(stateMachine.Player.AnimationData.SpeedParameterHash, speed);
            stateMachine.Player.Animator.SetFloat(stateMachine.Player.AnimationData.SpeedParameterHash, speed);
            
            stateMachine.Player.Controller.Move(direction * (speed * Time.unscaledDeltaTime) + stateMachine.Player.PlayerGravity.ExtraMovement * Time.unscaledDeltaTime);
        }
        
        private float GetMovementSpeed()
        {
            var movementSpeed = stateMachine.MovementSpeed * stateMachine.MovementSpeedModifier * playerCondition.SkillSpeedMultiplier * playerCondition.ItemSpeedMultiplier * playerCondition.WeightSpeedMultiplier;
            return movementSpeed;
        }

        private void Rotate(Vector3 direction)
        {
            if (direction == Vector3.zero) return;
            
            var unitTransform = stateMachine.Player.transform;
            var cameraPivotTransform = stateMachine.Player.CameraPivot; 
            
            var unitDirection = new Vector3(direction.x, 0, direction.z);
            var targetRotation = Quaternion.LookRotation(unitDirection);
            unitTransform.rotation = Quaternion.Slerp(unitTransform.rotation, targetRotation, stateMachine.RotationDamping * Time.unscaledDeltaTime);
            
            var cameraTargetRotation = Quaternion.LookRotation(direction);
            cameraPivotTransform.rotation = cameraTargetRotation;
        }
        
        private void AddInputActionCallbacks()
        {
            var playerInput = stateMachine.Player.PlayerInput;
            playerInput.PlayerActions.Move.canceled += OnMoveCanceled;
            playerInput.PlayerActions.Jump.started += OnJumpStarted;
            playerInput.PlayerActions.Run.started += OnRunStarted;
            playerInput.PlayerActions.Crouch.started += OnCrouchStarted;
            playerInput.PlayerActions.Reload.started += OnReloadStarted;
            playerInput.PlayerActions.Interact.started += OnInteractStarted;
            playerInput.PlayerActions.Aim.started += OnAimStarted;
            playerInput.PlayerActions.Aim.canceled += OnAimCanceled;
            playerInput.PlayerActions.Fire.started += OnFireStarted;
            playerInput.PlayerActions.Fire.canceled += OnFireCanceled;
            playerInput.PlayerActions.SwitchWeapon.performed += OnSwitchByScroll;
            playerInput.PlayerActions.SwitchToMain.started += OnSwitchToMain;
            playerInput.PlayerActions.SwitchToSub.started += OnSwitchToSecondary;
            playerInput.PlayerActions.SwitchToBomb.started += OnSwitchToGrenade;
            playerInput.PlayerActions.SwitchToHack.started += OnSwitchToHackGun;
            playerInput.PlayerActions.Focus.started += OnFocusStarted;
            playerInput.PlayerActions.Instinct.started += OnInstinctStarted;
            playerInput.PlayerActions.ItemAction.started += OnItemActionStarted;
            playerInput.PlayerActions.ItemAction.canceled += OnItemActionCanceled;
            playerInput.PlayerActions.ToggleInventory.started += OnInventoryToggled;
        }
        
        private void RemoveInputActionCallbacks()
        {
            var playerInput = stateMachine.Player.PlayerInput;
            playerInput.PlayerActions.Move.canceled -= OnMoveCanceled;
            playerInput.PlayerActions.Jump.started -= OnJumpStarted;
            playerInput.PlayerActions.Run.started -= OnRunStarted;
            playerInput.PlayerActions.Crouch.started -= OnCrouchStarted;
            playerInput.PlayerActions.Reload.started -= OnReloadStarted;
            playerInput.PlayerActions.Interact.started -= OnInteractStarted;
            playerInput.PlayerActions.Aim.started -= OnAimStarted;
            playerInput.PlayerActions.Aim.canceled -= OnAimCanceled;
            playerInput.PlayerActions.Fire.started -= OnFireStarted;
            playerInput.PlayerActions.Fire.canceled -= OnFireCanceled;
            playerInput.PlayerActions.SwitchWeapon.performed -= OnSwitchByScroll;
            playerInput.PlayerActions.SwitchToMain.started -= OnSwitchToMain;
            playerInput.PlayerActions.SwitchToSub.started -= OnSwitchToSecondary;
            playerInput.PlayerActions.SwitchToBomb.started -= OnSwitchToGrenade;
            playerInput.PlayerActions.SwitchToHack.started -= OnSwitchToHackGun;
            playerInput.PlayerActions.Focus.started -= OnFocusStarted;
            playerInput.PlayerActions.Instinct.started -= OnInstinctStarted;
            playerInput.PlayerActions.ItemAction.started -= OnItemActionStarted;
            playerInput.PlayerActions.ItemAction.canceled -= OnItemActionCanceled;
            playerInput.PlayerActions.ToggleInventory.started -= OnInventoryToggled;
        }
        
        /* - 기본동작 관련 메소드 - */
        protected virtual void OnMoveCanceled(InputAction.CallbackContext context) { if (!playerCondition.IsPlayerHasControl) return; }

        protected virtual void OnJumpStarted(InputAction.CallbackContext context) { }

        protected virtual void OnRunStarted(InputAction.CallbackContext context) { }

        protected virtual void OnCrouchStarted(InputAction.CallbackContext context) { }
        /* -------------------- */
        
        /* - Aim 관련 메소드 - */
        protected virtual void OnAimStarted(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            playerCondition.OnAim(true, stateMachine.Player.ZoomFoV, stateMachine.Player.TransitionTime);
        }
        protected virtual void OnAimCanceled(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            playerCondition.OnAim(false, stateMachine.Player.OriginalFoV, stateMachine.Player.TransitionTime);
        }
        /* ----------------- */
        
        /* - Fire & Reload 관련 메소드 - */
        protected virtual void OnFireStarted(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            playerCondition.IsAttacking = true;
        }
        protected virtual void OnFireCanceled(InputAction.CallbackContext context)
        {
            playerCondition.IsAttacking = false;
            switch (playerWeapon.Weapons[playerCondition.EquippedWeaponIndex])
            {
                case Gun gun: gun.IsAlreadyPlayedEmpty = false; break;
                case GrenadeLauncher gL: gL.IsAlreadyPlayedEmpty = false; break;
                case HackGun hackGun: hackGun.IsAlreadyPlayedEmpty = false; break;
            }
        }
        protected virtual void OnReloadStarted(InputAction.CallbackContext context)
        {
            if (!playerCondition.IsPlayerHasControl) return;
        }
        /* ---------------------------- */
        
        /* - Weapon Switch 관련 메소드 - */
        private void OnSwitchToMain(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            
            if (!playerWeapon.AvailableWeapons.TryGetValue(WeaponType.Rifle, out var value)) return;
            if (!value) return;
            playerCondition.OnSwitchWeapon(WeaponType.Rifle, 0.25f);
        }
        private void OnSwitchToSecondary(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            
            if (playerWeapon.AvailableWeapons.TryGetValue(WeaponType.Pistol, out var pistol))
            {
                if (pistol) { playerCondition.OnSwitchWeapon(WeaponType.Pistol, 0.25f); return; }
                if (!playerWeapon.AvailableWeapons.TryGetValue(WeaponType.SniperRifle, out var sniperRifle)) return;
                if (sniperRifle) 
                    playerCondition.OnSwitchWeapon(WeaponType.SniperRifle, 0.25f);
            } else if (playerWeapon.AvailableWeapons.TryGetValue(WeaponType.SniperRifle, out var sniperRifle))
            {
                if (sniperRifle)
                    playerCondition.OnSwitchWeapon(WeaponType.SniperRifle, 0.25f);
            }
        }
        private void OnSwitchToGrenade(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;

            if (!playerWeapon.AvailableWeapons.TryGetValue(WeaponType.GrenadeLauncher, out var value)) return;
            if (!value) return;
            playerCondition.OnSwitchWeapon(WeaponType.GrenadeLauncher, 0.25f);
        }
        private void OnSwitchToHackGun(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;

            if (!playerWeapon.AvailableWeapons.TryGetValue(WeaponType.HackGun, out var value)) return;
            if (!value) return;
            playerCondition.OnSwitchWeapon(WeaponType.HackGun, 0.25f);
        }
        private void OnSwitchByScroll(InputAction.CallbackContext context)
        {
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            
            var value = context.ReadValue<Vector2>();
            WeaponType nextIndex = GetAvailableWeaponIndex(value.y, playerCondition.EquippedWeaponIndex);
            playerCondition.OnSwitchWeapon(nextIndex, 0.25f);
        }
        private WeaponType GetAvailableWeaponIndex(float direction, WeaponType currentIndex)
        {
            int count = playerWeapon.Weapons.Count;
            
            var dir = direction < 0f ? 1 : direction > 0f ? -1 : 0;
            if (dir == 0) return WeaponType.Punch;

            int nextIndex = (int)currentIndex;
            for (var i = 0; i < count; i++)
            {
                nextIndex = (nextIndex + dir + count) % count;
                if (!playerWeapon.AvailableWeapons.TryGetValue((WeaponType)nextIndex, out var value)) continue;
                if (!value) continue;
                return (WeaponType)nextIndex;
            }
            return WeaponType.Punch;
        }
        /* --------------------------- */
        
        /* - Interact 관련 메소드 - */
        protected virtual void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (!playerCondition.IsPlayerHasControl) return;

            IInteractable interactable = stateMachine.Player.PlayerInteraction.Interactable;
            switch (interactable)
            {
                case DummyWeapon gun:
                    gun.OnInteract(stateMachine.Player.gameObject);
                    break;
                case DummyItem item:
                    item.OnInteract(stateMachine.Player.gameObject);
                    break;
                case null: break;
                default: interactable.OnInteract(stateMachine.Player.gameObject); break;
            }
        }
        /* ---------------------- */
        
        /* - Skill 관련 메소드 - */
        protected virtual void OnFocusStarted(InputAction.CallbackContext context)
        {
            if (!playerCondition.IsPlayerHasControl) return;
            if (!playerCondition.OnConsumeFocusGauge()) return;
        }
        protected virtual void OnInstinctStarted(InputAction.CallbackContext context)
        {
            if (!playerCondition.IsPlayerHasControl) return;
            if (!playerCondition.OnConsumeInstinctGauge()) return;
        }
        /* -------------------- */
        
        /* - Item 관련 메소드 - */
        protected virtual void OnItemActionStarted(InputAction.CallbackContext context)
        {
            if (!playerCondition.IsPlayerHasControl) return;
            stateMachine.Player.PlayerInventory.OnItemActionStarted();
        }

        protected virtual void OnItemActionCanceled(InputAction.CallbackContext context)
        {
            if (!playerCondition.IsPlayerHasControl) return;
            stateMachine.Player.PlayerInventory.OnItemActionCanceled();
        }
        /* ------------------- */

        /* - Inventory UI Toggle - */
        protected virtual void OnInventoryToggled(InputAction.CallbackContext context) { }
        /* ----------------------- */
    }
}