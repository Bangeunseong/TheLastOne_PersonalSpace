using System;
using _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Air;
using _1.Scripts.Entity.Scripts.Player.StateMachineScripts.States.Ground;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts
{
    using Player = _1.Scripts.Entity.Scripts.Player.Core.Player;
    
    [Serializable] public class PlayerStateMachine : StateMachine
    {
        // Sub States
        public IdleState IdleState { get; private set; }
        public WalkState WalkState { get; private set; }
        public RunState RunState { get; private set; }
        public CrouchState CrouchState { get; private set; }
        public CrouchWalkState CrouchWalkState { get; private set; }
        public FallState FallState { get; private set; }
        public JumpState JumpState { get; private set; }
        
        // Properties
        public Player Player { get; }
        public Vector2 MovementDirection { get; set; }
        public float MovementSpeed { get; private set; }
        public float RotationDamping { get; private set; }
        public float MovementSpeedModifier { get; set; }
        public float JumpHeight { get; set; }
        public Transform MainCameraTransform { get; set; }

        public PlayerStateMachine(Player player)
        {
            Player = player;
            JumpHeight = player.PlayerCondition.JumpForce;
            MainCameraTransform = player.MainCameraTransform;
            MovementSpeed = player.PlayerCondition.Speed;
            RotationDamping = player.PlayerCondition.RotationDamping;

            IdleState = new IdleState(this);
            WalkState = new WalkState(this);
            RunState = new RunState(this);
            CrouchState = new CrouchState(this);
            CrouchWalkState = new CrouchWalkState(this);

            FallState = new FallState(this);
            JumpState = new JumpState(this);
        }
    }
}