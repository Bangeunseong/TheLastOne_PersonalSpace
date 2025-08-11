using System;
using Unity.Collections;
using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.StateMachineScripts
{
    public interface IState
    {
        public void Enter();
        public void HandleInput();
        public void Update();
        public void PhysicsUpdate();
        public void LateUpdate();
        public void Exit();
        public string ToString();
    }
    
    [Serializable] public abstract class StateMachine
    {
        [field: Header("Current State")]
        [field: SerializeField, ReadOnly] public string CurrentStateName { get; protected set; }
        public IState CurrentState { get; protected set; }

        public void ChangeState(IState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentStateName = CurrentState.ToString();
            CurrentState?.Enter();
        }

        public void HandleInput()
        {
            CurrentState?.HandleInput();
        }

        public void Update()
        {
            CurrentState?.Update();
        }

        public void LateUpdate()
        {
            CurrentState?.LateUpdate();
        }

        public void PhysicsUpdate()
        {
            CurrentState?.PhysicsUpdate();
        }
    }
}