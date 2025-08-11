using System.Threading;
using _1.Scripts.UI.Inventory;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Air
{
    public class AirState : BaseState
    {
        public AirState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            stateMachine.MovementSpeedModifier = playerCondition.AirSpeedModifier;
            base.Enter();
            StartAnimation(stateMachine.Player.AnimationData.AirParameterHash);
            
            // Cancel Crouch
            playerCondition.OnCrouch(false, 0.1f);
            
            // Hide Inventory UI
            coreManager.uiManager.HideUI<InventoryUI>();
        }

        public override void Exit()
        {
            base.Exit();
            StopAnimation(stateMachine.Player.AnimationData.AirParameterHash);
        }
    }
}