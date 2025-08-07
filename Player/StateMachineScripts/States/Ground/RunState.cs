using System.Threading;
using UnityEngine.InputSystem;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Ground
{
    public class RunState : GroundState
    {
        public RunState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            stateMachine.MovementSpeedModifier = playerCondition.RunSpeedModifier;
            base.Enter();
            playerCondition.OnConsumeStamina(
                playerCondition.StatData.consumeRateOfStamina * playerCondition.StatData.interval, 
                playerCondition.StatData.interval);
        }

        public override void Update()
        {
            base.Update();
            if (playerCondition.CurrentStamina <= 0){ stateMachine.ChangeState(stateMachine.WalkState); }
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
            if (!playerCondition.IsPlayerHasControl) return;
            stateMachine.ChangeState(stateMachine.WalkState);
        }

        protected override void OnAimStarted(InputAction.CallbackContext context)
        {
            base.OnAimStarted(context);
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            stateMachine.ChangeState(stateMachine.WalkState);
        }

        protected override void OnFireStarted(InputAction.CallbackContext context)
        {
            base.OnFireStarted(context);
            if (playerCondition.IsSwitching || !playerCondition.IsPlayerHasControl) return;
            stateMachine.ChangeState(stateMachine.WalkState);
        }
    }
}