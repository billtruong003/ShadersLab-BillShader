// Path: Assets/Scripts/LocomotionModular/StateMachine/LocomotionState.cs

using System;
using UnityEngine;

namespace ModularTopDown.Locomotion
{
    public abstract class LocomotionState
    {
        public static event Action<CharacterFXProfile.FXType, Vector3> OnFXRequest;

        protected readonly LocomotionStateMachine stateMachine;
        protected readonly ILocomotionInput input;
        protected readonly CharacterLocomotion locomotion;
        protected readonly CharacterAnimator animator;

        public LocomotionState(LocomotionStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
            this.input = stateMachine.Input;
            this.locomotion = stateMachine.Locomotion;
            this.animator = stateMachine.Animator;
        }

        protected void RequestFX(CharacterFXProfile.FXType type)
        {
            OnFXRequest?.Invoke(type, stateMachine.transform.position);
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }
}