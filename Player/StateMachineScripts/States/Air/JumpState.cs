namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Air
{
    public class JumpState : AirState
    {
        public JumpState(PlayerStateMachine machine) : base(machine)
        {
        }
        
        public override void Enter()
        {
            stateMachine.Player.PlayerGravity.Jump(stateMachine.JumpHeight);
            
            base.Enter();
            StartAnimation(stateMachine.Player.AnimationData.JumpParameterHash);
        }

        public override void Exit()
        {
            base.Exit();
            StopAnimation(stateMachine.Player.AnimationData.JumpParameterHash);
        }

        public override void Update()
        {
            base.Update();
            if(stateMachine.Player.Controller.velocity.y <= 0) 
                stateMachine.ChangeState(stateMachine.FallState);
        }
    }
}