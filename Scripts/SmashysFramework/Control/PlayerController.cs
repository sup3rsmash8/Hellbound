using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmashysFramework
{
    //using Input;
    

    //[DisallowMultipleComponent]
    public class PlayerController : Controller, GameInput.IGameActions
    {
        /// <summary>
        /// Spawns and returns a new PlayerController. Automatically registers
        /// to the InputManager for input callbacks in the process. Cameras can
        /// be passed as arguments to let them influence analog stick inputs sent
        /// to pawns.
        /// </summary>
        public PlayerController(CameraSystem cameraSystem = null, bool replaceOccupiedSlots = false /* <-- For later implementation */)
        {
#if UNITY_EDITOR
            if (!Statics.AssertIsPlaying("PlayerControllers should only be created in Play Mode."))
            {
                return;
            }
#endif

            for (int i = 0; i < Statics.MaxPlayerCount; i++)
            {
                if (GameManager.InputManager.SetCallback(i, this))
                {
                    return;
                }
            }

            Debug.LogWarning("PlayerController could not be registered for input callbacks: All existing slots are currently occupied; this PlayerController will not receive any inputs.");
        }

        ~PlayerController()
        {
            CleanUp();
        }

        // Since input callbacks might be asynchronous to MonoBehaviour
        // updates, and for the sake of consistency between player and AI,
        // player inputs are registered in buffers that are then read
        // inside ProcessInputs() when it is called in Update().
        #region InputVariables
        private Vector2 _bufferLAnalogStick;
        private Vector2 _bufferRAnalogStick;

        private bool[] _bufferButtons = new bool[(int)Buttons.Max];
        #endregion

        #region IGameActions
        // Buffer method called in the interface
        private void OnButton(InputAction.CallbackContext context, Buttons button)
        {
            // L-trigger and R-trigger are axes, but are treated like
            // buttons, so we just gotta do a threshold check for 'em
            if (button == Buttons.LT || button == Buttons.RT)
            {
                _bufferButtons[(int)button] = context.ReadValue<float>() > 0.05f;
            }
            else
            {
                _bufferButtons[(int)button] = context.ReadValueAsButton();
            }
        }

        // Buffer method called in the interface
        private void OnAxis(InputAction.CallbackContext context, ref float axis)
        {
            float value = context.ReadValue<float>();
            axis = value;
        }

        public void OnButtonA(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.A;
            OnButton(context, button);
        }

        public void OnButtonB(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.B;
            OnButton(context, button);
        }

        public void OnButtonLB(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.LB;
            OnButton(context, button);
        }

        public void OnButtonRB(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.RB;
            OnButton(context, button);
        }

        public void OnButtonX(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.X;
            OnButton(context, button);
        }

        public void OnButtonY(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.Y;
            OnButton(context, button);
        }

        public void OnDPadDown(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.DDown;
            OnButton(context, button);
        }

        public void OnDPadLeft(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.DLeft;
            OnButton(context, button);
        }

        public void OnDPadRight(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.DRight;
            OnButton(context, button);
        }

        public void OnDPadUp(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.DUp;
            OnButton(context, button);
        }

        public void OnLAnalogX(InputAction.CallbackContext context)
        {
            OnAxis(context, ref _bufferLAnalogStick.x);
        }

        public void OnLAnalogY(InputAction.CallbackContext context)
        {
            OnAxis(context, ref _bufferLAnalogStick.y);
        }

        public void OnLTrigger(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.LT;
            OnButton(context, button);
        }

        public void OnRAnalogX(InputAction.CallbackContext context)
        {
            OnAxis(context, ref _bufferRAnalogStick.x);
        }

        public void OnRAnalogY(InputAction.CallbackContext context)
        {
            OnAxis(context, ref _bufferRAnalogStick.y);
        }

        public void OnRTrigger(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.RT;
            OnButton(context, button);
        }

        public void OnStart(InputAction.CallbackContext context)
        {
            Buttons button = Buttons.Start;
            OnButton(context, button);
        }
        #endregion

        private CameraSystem _cameraSystem;
        private bool _cameraInfluencesLInput = true;
        private bool _cameraInfluencesRInput = false;

        /// <summary>
        /// Sets the camera system that is currently influencing
        /// the analog stick inputs.
        /// </summary>
        public CameraSystem GetCameraSystem() => _cameraSystem;

        /// <summary>
        /// Sets the camera system influencing
        /// the analog stick inputs. Set null for no influence.
        /// </summary>
        /// <param name="newSystem"></param>
        public void SetCameraSystem(CameraSystem newSystem, bool inflLeftAnalog, bool inflRightAnalog)
        {
            _cameraSystem = newSystem;
            SetCameraAnalogInfluence(inflLeftAnalog, inflRightAnalog);
        }

        /// <summary>
        /// Sets which analog sticks can be influenced by the referenced camera system.
        /// </summary>
        /// <param name="inflLeftAnalog">Should the left analog stick be influenced by the camera rotation?</param>
        /// <param name="inflRightAnalog">Should the right analog stick be influenced by the camera rotation?</param>
        public void SetCameraAnalogInfluence(bool inflLeftAnalog, bool inflRightAnalog)
        {
            _cameraInfluencesLInput = inflLeftAnalog;
            _cameraInfluencesRInput = inflRightAnalog;
        }

        protected override void ProcessInputs(ref Vector2 lAnalogStick, ref Vector2 rAnalogStick, ref bool[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = _bufferButtons[i];
            }

            lAnalogStick = _bufferLAnalogStick;
            rAnalogStick = _bufferRAnalogStick;

            // Alter control stick inputs depending on 
            // camera rotation, if non-null.
            if (_cameraSystem)
            {
                if (_cameraInfluencesLInput)
                {
                    // Create a 3D vector of the analog input, let the camera 
                    // quat influence it, then extract the result's X and Z-axes.
                    Vector3 lAnalogAs3DVec = _cameraSystem.GetRelativeYRotation() * new Vector3(lAnalogStick.x, 0, lAnalogStick.y);
                    lAnalogStick = new Vector2(lAnalogAs3DVec.x, lAnalogAs3DVec.z);
                }

                if (_cameraInfluencesRInput)
                {
                    // Create a 3D vector of the analog input, let the camera 
                    // quat influence it, then extract the result's X and Z-axes.
                    Vector3 rAnalogAs3DVec = _cameraSystem.GetRelativeYRotation() * new Vector3(rAnalogStick.x, 0, rAnalogStick.y);
                    rAnalogStick = new Vector2(rAnalogAs3DVec.x, rAnalogAs3DVec.z);
                }
            }
        }

        public override void CleanUp()
        {
            base.CleanUp();

            GameManager.InputManager.UnassignCallbacksIfControlling(this);
        }
    }

}
