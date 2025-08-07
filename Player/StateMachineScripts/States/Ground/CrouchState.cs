using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Ground
{
    public class CrouchState : GroundState
    {
        public CrouchState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            stateMachine.MovementSpeedModifier = 0f;
            playerCondition.OnCrouch(true, 0.1f);
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
            stateMachine.ChangeState(stateMachine.IdleState);
        }
        
        public override void Update()
        {
            base.Update();

            if (stateMachine.MovementDirection == Vector2.zero) return;
            stateMachine.ChangeState(stateMachine.CrouchWalkState);
        }
    }
}