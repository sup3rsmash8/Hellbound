//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    /// <summary>
    /// Base for any class that can control IPawns. 
    /// </summary>
    public abstract class Controller// : MonoBehaviour
    {
        #region InputVariables
        // Previous
        private Vector2 _pLAnalogStick;
        private Vector2 _pRAnalogStick;

        private bool[] _pButtons = new bool[(int)Buttons.Max];

        // Current
        private Vector2 _lAnalogStick;
        private Vector2 _rAnalogStick;

        private bool[] _buttons = new bool[(int)Buttons.Max];
        #endregion

        private IPawn _currentPawn = null;

        /// <summary>
        /// The pawn of this controller.
        /// </summary>
        public IPawn GetPawn()
        {
            return _currentPawn;
            //protected set
            //{
            //    _currentPawns = value;
            //    //if (!(value is Object pawnAsObj) || pawnAsObj)
            //    //{

            //    //}
            //    //else
            //    //{
            //    //    _currentPawn = null;
            //    //}
            //}
        }

        /// <summary>
        /// The Controller class itself is not a MonoBehaviour, but expects
        /// one to run this method inside its Update() function.
        /// </summary>
        public void ControllerUpdate()
        {
            if (!Statics.AssertIsPlaying("Controllers may only work in Play Mode!"))
                return;

            IPawn current = GetPawn();

            if (current != null)
            {
                // Store previous values
                _pLAnalogStick = _lAnalogStick;
                _pRAnalogStick = _rAnalogStick;
                // _buttons and _pButtons are meant to be of the same length
                // as each other, so this won't ever result in exceptions.
                System.Array.Copy(_buttons, _pButtons, _buttons.Length);

                // Process the inputs.
                ProcessInputs(ref _lAnalogStick, ref _rAnalogStick, ref _buttons);

                // Send the inputs if there's a difference
                // between current and previous values
                for (int i = 0; i < _buttons.Length; i++)
                {
                    if (_buttons[i] != _pButtons[i])
                    {
                        Buttons button = (Buttons)i;
                        current.OnButtonInput(button, _buttons[i]);
                    }
                }

                // CPUs may have a potential advantage over players in that
                // they can register values beyond our physical range, so we
                // just need to ensure that we always play by the same rules.
                _lAnalogStick = Vector2.ClampMagnitude(_lAnalogStick, 1f);
                _rAnalogStick = Vector2.ClampMagnitude(_rAnalogStick, 1f);

                if (_lAnalogStick.x != _pLAnalogStick.x || _lAnalogStick.y != _pLAnalogStick.y)
                {
                    current.OnAxisInput(Axes.LAnalogX, _lAnalogStick.x);
                    current.OnAxisInput(Axes.LAnalogY, _lAnalogStick.y);
                }

                if (_rAnalogStick.x != _pRAnalogStick.x || _rAnalogStick.y != _pRAnalogStick.y)
                {
                    current.OnAxisInput(Axes.RAnalogX, _rAnalogStick.x);
                    current.OnAxisInput(Axes.RAnalogY, _rAnalogStick.y);
                }
            }
        }

        /// <summary>
        /// Central method where the controller sets the button/axis values.
        /// </summary>
        protected abstract void ProcessInputs(ref Vector2 lAnalogStick, ref Vector2 rAnalogStick, ref bool[] buttons);

        /// <summary>
        /// Possesses a pawn, taking control of it. Returns
        /// whether or not the possession was successful.
        /// </summary>
        public virtual bool PossessPawn(IPawn pawn)
        {
            if (!Statics.AssertIsPlaying("Controllers may only work in Play Mode!"))
                return false;

            ResetAllInputs();

            // If we're possessing an already possessed pawn,
            // don't bother repossessing. (HAHAHAHAHA)
            if (pawn == GetPawn() && pawn != null)
                return true;

            if (GetPawn() != null)
            {
                GetPawn().OnUnpossessed(this);
            }

            _currentPawn = pawn;

            if (GetPawn() != null && GetPawn().IsPossessable)
            {
                GetPawn().OnPossessed(this);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets all inputs to their default value (analogs will be neutralized,
        /// and buttons will be unpressed).
        /// </summary>
        protected virtual void ResetAllInputs()
        {
            _lAnalogStick = Vector2.zero;
            _rAnalogStick = Vector2.zero;

            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i] = false;
            }
        }

        /// <summary>
        /// If the controller is currently possessing a pawn, unpossesses it.
        /// </summary>
        public void UnpossessCurrentPawn()
        {
#if UNITY_EDITOR
            if (!Statics.AssertIsPlaying(string.Empty))
                return;
#endif
            PossessPawn(null);
        }

        /// <summary>
        /// Cleans up the controller for disposal. You should call this whenever you're done using a controller (like in OnDestroy()).
        /// </summary>
        public virtual void CleanUp()
        {
            // Possess null to properly detach the pawn.
            UnpossessCurrentPawn();
        }
    }

    //public struct ButtonStates
    //{
    //    public ButtonStates()
    //    {
    //        buttons = new bool[(int)Buttons.Max];
    //    }

    //    public bool[] buttons;
    //}



    //                void OnAButton(InputAction.CallbackContext context);
    //void OnBButton(InputAction.CallbackContext context);
    //void OnXButton(InputAction.CallbackContext context);
    //void OnYButton(InputAction.CallbackContext context);
    //void OnZButton(InputAction.CallbackContext context);
    //void OnLTrigger(InputAction.CallbackContext context);
    //void OnRTrigger(InputAction.CallbackContext context);
    //void OnControlStickX(InputAction.CallbackContext context);
    //void OnControlStickY(InputAction.CallbackContext context);
    //void OnCStickX(InputAction.CallbackContext context);
    //void OnCStickY(InputAction.CallbackContext context);
    //void OnDPadUp(InputAction.CallbackContext context);
    //void OnDPadDown(InputAction.CallbackContext context);
    //void OnDPadLeft(InputAction.CallbackContext context);
    //void OnDPadRight(InputAction.CallbackContext context);
}
