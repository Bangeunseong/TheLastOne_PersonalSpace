using System.Threading;
using UnityEngine.InputSystem;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Ground
{
    public class CrouchWalkState : GroundState
    {
        public CrouchWalkState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            stateMachine.MovementSpeedModifier = playerCondition.CrouchSpeedModifier;
            base.Enter();
            StartAnimation(stateMachine.Player.AnimationData.CrouchParameterHash);
        }

        public override void Exit()
        {
            base.Exit();
            StopAnimation(stateMachine.Player.AnimationData.CrouchParameterHash);
        }
        
        protected override void OnCrouchStarted(InputAction.CallbackContext context)
        {
            base.OnCrouchStarted(context);
            if (!playerCondition.IsPlayerHasControl) return;
            playerCondition.OnCrouch(false, 0.1f);
            stateMachine.ChangeState(stateMachine.WalkState);
        }
    }
}