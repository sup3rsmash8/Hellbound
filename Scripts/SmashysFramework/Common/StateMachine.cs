using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    /// <summary>
    /// A versatile, class-bound state machine system that allows
    /// the user to store the states however they want.
    /// </summary>
    /// <typeparam name="T">The class that will inherit this machine.</typeparam>
    public class StateMachine<T> where T : class
    {
        public StateMachine(T user, IState initialState = null, StateChangeType stateChangeType = StateChangeType.ChangeAtEndOfUpdate, params object[] stateEnterArguments)
        {
            User = user;

            if (initialState != null)
            {
                _stateChangeType = StateChangeType.ChangeImmediately;
                CurrentState = initialState;
                ChangeState(initialState, stateEnterArguments);
            }

            _stateChangeType = stateChangeType;
        }

        #region Properties
        private T _user;
        public T User { get => _user; private set => _user = value; }

        /// <summary>
        /// Event that calls when a state has been successfully changed. 
        /// This is called after StateEnter is called on the user.
        /// <br>arg0: The state that was transitioned into.</br>
        /// <br>arg1: The previous state. Note that this parameter CAN be null.</br>
        /// </summary>
        public event System.Action<IState, IState> OnStateChanged;

        private StateChangeType _stateChangeType;

        private float _stateChangeTimeStamp = 0;

        public float GetStateDuration() => Time.time - _stateChangeTimeStamp;

        /// <summary>
        /// The way this state machine changes states.
        /// <br>Note: Changing to 'ChangeImmediately' from 'ChangeAtEndOfUpdate' while a state change is pending will cause it to be executed immediately once the value's been set instead!</br>
        /// </summary>
        public StateChangeType _StateChangeType
        {
            get => _stateChangeType;
            private set
            {
                StateChangeType prev = _stateChangeType;
                _stateChangeType = value;

                if (prev == StateChangeType.ChangeAtEndOfUpdate && _stateChangeType == StateChangeType.ChangeImmediately)
                {
                    if (IsPendingStateChange)
                    {
                        ChangeStateImmediately(_pendingState, _pendingStateEnterArguments);
                    }
                }
            }
        }

        // State processing
        public virtual IState CurrentState { get; protected set; }
        public virtual IState PreviousState { get; protected set; }

        // Returns a valid value if the state is supposed to be called every frame.
        protected IUpdateState _currentStateAsUpdateState;
        // Returns a valid value if the state is supposed to be called every physics step.
        protected IFixedUpdateState _currentStateAsFixedUpdateState;

        // Pending stuff
        private object[] _pendingStateEnterArguments;
        private IState _pendingState;

        /// <summary>
        /// Returns whether we are pending a state change at the end of the frame
        /// from a previous ChangeState() call.
        /// </summary>
        public bool IsPendingStateChange => _pendingState != null;
        #endregion

        /// <summary>
        /// The update method. If using MonoBehaviours, put this in the Update() method (for actors, ActorUpdate(), and characters, CharacterUpdate()).
        /// </summary>
        /// <param name="deltaTime">Time.deltaTime.</param>
        public virtual void StateMachineUpdate(float deltaTime)
        {
            _currentStateAsUpdateState?.OnStateUpdate(User, deltaTime);

            ChangeToPendingState();
        }

        /// <summary>
        /// The fixed update method. If using MonoBehaviours, put this in the FixedUpdate() method.
        /// </summary>
        public virtual void StateMachineFixedUpdate()
        {
            _currentStateAsFixedUpdateState?.OnStateFixedUpdate(User);

            //ChangeToPendingState();
        }

        /// <summary>
        /// Changes the state executed by the state machine. 
        /// </summary>
        /// <param name="newState">The state that will be changed to.</param>
        /// <param name="stateEnterArguments">Arguments that the state may use upon OnStateEnter being called.</param>
        public void ChangeState(IState newState, params object[] stateEnterArguments)
        {
            if (_stateChangeType == StateChangeType.ChangeAtEndOfUpdate)
            {
                if (_pendingState != null) return;

                _pendingState = newState;
                _pendingStateEnterArguments = stateEnterArguments;
            }
            else
            {
                ChangeStateExec(newState, stateEnterArguments);
            }
        }

        /// <summary>
        /// Changes the state executed by the state machine immediately, even if the state machine is set to buffer.
        /// <br>Clears the state buffer in the process.</br>
        /// </summary>
        /// <param name="newState">The state that will be changed to.</param>
        /// <param name="stateEnterArguments">Arguments that the state may use upon OnStateEnter being called.</param>
        public void ChangeStateImmediately(IState newState, params object[] stateEnterArguments)
        {
            _pendingState = null;
            _pendingStateEnterArguments = null;
            ChangeStateExec(newState, stateEnterArguments);
        }

        protected virtual void ChangeStateExec(IState newState, params object[] stateEnterArguments)
        {
            CurrentState?.OnStateExit(User, newState);

            PreviousState = CurrentState;

            CurrentState = newState;

            _currentStateAsUpdateState = CurrentState as IUpdateState;
            _currentStateAsFixedUpdateState = CurrentState as IFixedUpdateState;

            _stateChangeTimeStamp = Time.time;

            CurrentState?.OnStateEnter(User, PreviousState, stateEnterArguments);

            OnStateChanged?.Invoke(CurrentState, PreviousState);
        }

        /// <summary>
        /// Executes the change to a pending state, if one is buffered.
        /// Intended to be called inside StateMachineUpdate().
        /// </summary>
        protected void ChangeToPendingState()
        {
            if (IsPendingStateChange && _stateChangeType == StateChangeType.ChangeAtEndOfUpdate)
            {
                ChangeStateExec(_pendingState, _pendingStateEnterArguments);
            }

            _pendingState = null;
        }

        #region Interfaces
        public interface IState
        {

            void OnStateEnter(T user, IState previousState, params object[] stateEnterArguments);
            void OnStateExit(T user, IState nextState);
        }

        public interface IUpdateState
        {
            void OnStateUpdate(T user, float deltaTime);
        }

        public interface IFixedUpdateState
        {
            void OnStateFixedUpdate(T user);
        }
        #endregion


    }

    /// <summary>
    /// Enum that specifies how a state machine should change states.
    /// </summary>
    public enum StateChangeType
    {
        /// <summary>
        /// Changes the state immediately as ChangeState() is called.
        /// </summary>
        ChangeImmediately,

        /// <summary>
        /// Pends the state change from when ChangeState() is called to the
        /// end of the Update() call.
        /// </summary>
        ChangeAtEndOfUpdate
    }
}
