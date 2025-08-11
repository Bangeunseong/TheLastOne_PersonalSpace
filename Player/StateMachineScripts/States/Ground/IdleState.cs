using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Ground
{
    public class IdleState : GroundState
    {
        public IdleState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            // stateMachine.MovementSpeedModifier = 0f;
            base.Enter();
            
            playerCondition.OnRecoverStamina(
                playerCondition.StatData.recoverRateOfStamina_Idle * playerCondition.StatData.interval, 
                playerCondition.StatData.interval);
        }

        public override void Exit()
        {
            base.Exit();
            
            playerCondition.CancelStaminaTask();
        }
        
        public override void Update()
        {
            base.Update();

            if (stateMachine.MovementDirection == Vector2.zero) return;
            stateMachine.ChangeState(stateMachine.WalkState);
        }
        
        protected override void OnCrouchStarted(InputAction.CallbackContext context)
        {
            base.OnCrouchStarted(context);
            if (!playerCondition.IsPlayerHasControl) return;
            stateMachine.ChangeState(stateMachine.CrouchState);
        }
    }
}