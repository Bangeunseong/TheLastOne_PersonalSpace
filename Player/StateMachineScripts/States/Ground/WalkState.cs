using System.Threading;
using UnityEngine.InputSystem;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Ground
{
    public class WalkState : GroundState
    {
        public WalkState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            stateMachine.MovementSpeedModifier = playerCondition.WalkSpeedModifier;
            base.Enter();
            
            // Start Stamina Recovery Coroutine 
            playerCondition.OnRecoverStamina(
                playerCondition.StatData.recoverRateOfStamina_Walk * playerCondition.StatData.interval,
                playerCondition.StatData.interval);
        }

        public override void Exit()
        {
            base.Exit();
            playerCondition.CancelStaminaTask();
        }

        protected override void OnCrouchStarted(InputAction.CallbackContext context)
        {
            base.OnCrouchStarted(context);
            if (!playerCondition.IsPlayerHasControl) return;
            stateMachine.ChangeState(stateMachine.CrouchState);
        }

        protected override void OnRunStarted(InputAction.CallbackContext context)
        {
            base.OnRunStarted(context);
            if (!playerCondition.IsPlayerHasControl || playerCondition.IsAiming || playerCondition.IsAttacking) return;
            stateMachine.ChangeState(stateMachine.RunState);
        }
    }
}