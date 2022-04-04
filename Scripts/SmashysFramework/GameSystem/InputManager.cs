using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace SmashysFramework
{
    using Util = UnityEngine.InputSystem.Utilities;
    public partial class GameManager : MonoSingleton<GameManager>
    {
        public sealed class InputManager// : GameManagerSubComponent
        {
            // Initializes the manager in the constructor.
            public InputManager()
            {
                if (_inputSlots != null)
                    return;

                _inputSlots = new InputSlot[Statics.MaxPlayerCount];
                for (int i = 0; i < _inputSlots.Length; i++)
                {
                    _inputSlots[i] = new InputSlot();
                }
            }

            private static InputManager Inst { get { return _inputManager; } }

            //protected override bool IsInitialized => _inputManager != null;

            private static InputSlot[] _inputSlots = null;

            /// <summary>
            /// Binds an object to an input slot, allowing to take control of it. 
            /// Returns whether the callback setting was successful or not.
            /// </summary>
            /// <param name="inputSlot">The player index that will take control.</param>
            /// <param name="actions">The interface that will be bound. Set to null to unassign instead.</param>
            public static bool SetCallback(int inputSlot, GameInput.IGameActions actions)
            {
                if (!Statics.AssertIsPlaying("Callbacks may only be set during Play Mode!"))
                    return false;

                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                {
                    return false;
                }

                if (actions == null)
                {
                    _inputSlots[inputSlot].UnsetCallbacks();
                    return true;
                }

                return _inputSlots[inputSlot].SetCallbacks(actions);
            }

            /// <summary>
            /// Unoccupies an input slot so that other objects can be controlled.
            /// </summary>
            /// <param name="inputSlot">The input slot to free.</param>
            /// <returns></returns>
            public static bool UnassignCallback(int inputSlot)
            {
                if (!Statics.AssertIsPlaying("Callbacks may only be set during Play Mode!"))
                {
                    return false;
                }

                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                {
                    return false;
                }
                
                return SetCallback(inputSlot, null);
            }

            /// <summary>
            /// Unoccupies a slot if the object being controlled is the same as referenced in 'actions'.
            /// </summary>
            /// <param name="inputSlot">The slot to unoccupy, if it is currently controlling 'actions'.</param>
            /// <param name="actions">The object to check if the slot is controlling.</param>
            public static bool UnassignCallbacksIfControlling(int inputSlot, GameInput.IGameActions actions)
            {
                if (!Statics.AssertIsPlaying("Callbacks may only be unset during Play Mode!"))
                {
                    return false;
                }

                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                {
                    return false;
                }

                return _inputSlots[inputSlot].UnsetCallbacksIfControlling(actions);
            }

            /// <summary>
            /// <br>Unoccupies a slot if 'actions' is being controlled by one.</br>
            /// <br>This overload will automatically look through all existing slots to see if they're assigned this object.</br>
            /// <br>Returns the index of the slot that was occupying this object. If none, returns -1 instead.</br>
            /// </summary>
            /// <param name="actions">The object to check if any of the slots are controlling.</param>
            public static int UnassignCallbacksIfControlling(GameInput.IGameActions actions)
            {
                if (!Statics.AssertIsPlaying("Callbacks may only be unset during Play Mode!"))
                {
                    return -1;
                }

                if (actions != null && Inst != null)
                {
                    for (int i = 0; i < _inputSlots.Length; i++)
                    {
                        if (_inputSlots[i].UnsetCallbacksIfControlling(actions))
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }

            /// <summary>
            /// Returns if an input slot is currently controlling this object.
            /// </summary>
            /// <param name="inputSlot">The index of the slot.</param>
            /// <param name="actions">The object to check if it's being controlled.</param>
            /// <returns></returns>
            public static bool IsControlling(int inputSlot, GameInput.IGameActions actions)
            {
                if (!Statics.AssertIsPlaying("This may only be called during Play Mode!"))
                {
                    return false;
                }

                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                {
                    return false;
                }

                return _inputSlots[inputSlot].IsControlling(actions);
            }

            /// <summary>
            /// Returns if an input slot is currently controlling an object at all.
            /// </summary>
            /// <param name="inputSlot">The index of the slot.</param>
            /// <returns></returns>
            public static bool IsSet(int inputSlot)
            {
                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                {
                    return false;
                }

                return _inputSlots[inputSlot].IsSet;
            }

            /// <summary>
            /// Retrieves the value of an axis of a controller.
            /// </summary>
            /// <param name="inputSlot"></param>
            /// <param name="axis"></param>
            /// <returns></returns>
            public static float GetAxis(int inputSlot, Axes axis)
            {
                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                    return 0;

                InputSlot slot = _inputSlots[inputSlot];

                if (slot != null)
                {
                    return slot.GetAxis(axis);
                }

                return 0;
            }

            /// <summary>
            /// Retrieves whether a button on a controller is pressed or not.
            /// </summary>
            /// <param name="inputSlot"></param>
            /// <param name="button"></param>
            /// <returns></returns>
            public static bool GetButton(int inputSlot, Buttons button)
            {
                if (inputSlot >= Statics.MaxPlayerCount || inputSlot < 0 || Inst == null)
                    return false;

                InputSlot slot = _inputSlots[inputSlot];

                if (slot != null)
                {
                    return slot.GetButton(button);
                }

                return false;
            }

            private class InputSlot
            {

                public InputSlot()
                {
                    if (!Statics.AssertIsPlaying("InputSlots may only be constructed during Play Mode!"))
                    {
                        return;
                    }

                    InputSystem.onDeviceChange += OnDeviceChange;

                    InputDevice device = FindAppropriateDevice();
                    Util.ReadOnlyArray<InputDevice> inputDevices = new Util.ReadOnlyArray<InputDevice>(new InputDevice[] { device });

                    _input = new GameInput();
                    _input.Enable();

                    _input.devices = device != null ? inputDevices : null;

                    AddBoundInputDevice(device);
                }

                ~InputSlot()
                {
                    if (_input != null)
                    {
                        _input.Disable();
                        _input.Dispose();
                        RemoveBoundInputDevice(_input.devices.Value[0]);
                    }

                    InputSystem.onDeviceChange -= OnDeviceChange;
                }

                #region Statics
                // NOTE: Layouts appear in the order they're prioritized.
                // In other words, Xbox controllers are prioritized over keyboards.
                private static readonly int[] _hashedInputDeviceLayouts = new int[]
                {
                    Statics.StringToHash("Xbox Controller"),
                    Statics.StringToHash("Keyboard"),
                };

                private static List<InputDevice> _boundInputDevices = new List<InputDevice>();

                private static int StringToHash(string name)
                {
                    return Animator.StringToHash(name);
                }

                // Retrieves the next unoccupied input device.
                private static InputDevice FindAppropriateDevice()
                {
                    Util.ReadOnlyArray<InputDevice> inputDevices = InputSystem.devices;

                    int supportedDeviceCount = InputSystem.settings.supportedDevices.Count;

                    for (int i = 0; i < _hashedInputDeviceLayouts.Length; i++)
                    {
                        for (int j = 0; j < inputDevices.Count; j++)
                        {
                            if (_boundInputDevices.Contains(inputDevices[j]))
                                continue;

                            int hashedName = Statics.StringToHash(inputDevices[j].displayName);
                            if (hashedName == _hashedInputDeviceLayouts[i])
                            {
                                return inputDevices[j];
                            }
                        }
                    }

                    Debug.Log("FindAppropriateDevice(): All supported devices that are connected are currently in use - returning null.");
                    return null;
                }

                // Helper method to properly add devices to _boundInputDevices.
                private static void AddBoundInputDevice(InputDevice device)
                {
                    if (!_boundInputDevices.Contains(device) && device != null)
                    {
                        _boundInputDevices.Add(device);
                    }
                }

                // Helper method to properly remove devices from _boundInputDevices.
                private static void RemoveBoundInputDevice(InputDevice device)
                {
                    if (device != null)
                    {
                        _boundInputDevices.Remove(device);
                    }
                }
                #endregion

                // The input class.
                private GameInput _input;

                // For comparison purposes.
                private GameInput.IGameActions _currentCallbacks;

                /// <summary>
                /// Is this slot currently bound to a callback?
                /// </summary>
                public bool IsSet => _currentCallbacks != null;

                public bool IsControlling(GameInput.IGameActions actions)
                {
                    if (IsSet)
                        return _currentCallbacks == actions;

                    return false;
                }

                public bool SetCallbacks(GameInput.IGameActions actions)
                {
                    if (!IsSet)
                    {
                        _input.Game.SetCallbacks(actions);
                        _currentCallbacks = actions;
                        return true;
                    }

                    return false;
                }

                public void UnsetCallbacks()
                {
                    if (IsSet)
                    {
                        _input.Game.SetCallbacks(null);
                        _currentCallbacks = null;
                    }
                }

                public bool UnsetCallbacksIfControlling(GameInput.IGameActions actions)
                {
                    if (_currentCallbacks == null)
                        return actions == null;

                    if (_currentCallbacks == actions)
                    {
                        UnsetCallbacks();
                        return true;
                    }

                    return false;
                }

                private void OnDeviceChange(InputDevice device, InputDeviceChange change)
                {
                    if (!_input.devices.HasValue || device != _input.devices.Value[0])
                        return;

                    switch (change)
                    {
                        case InputDeviceChange.Disconnected:
                            _input.Disable();
                            RemoveBoundInputDevice(device);
                            break;

                        //case InputDeviceChange.Reconnected:
                        //    _input.Disable();
                        //    _boundInputDevices.Remove(device);
                        //    break;
                    }
                }

                public float GetAxis(Axes axis)
                {
                    InputAction action;
                    switch (axis)
                    {
                        default:
                            return 0;

                        case Axes.LAnalogX:
                            action = _input.Game.LAnalogX;
                            break;

                        case Axes.LAnalogY:
                            action = _input.Game.LAnalogY;
                            break;

                        case Axes.RAnalogX:
                            action = _input.Game.RAnalogX;
                            break;

                        case Axes.RAnalogY:
                            action = _input.Game.RAnalogY;
                            break;
                    }

                    float result = action.ReadValue<float>();

                    return result;
                }

                public bool GetButton(Buttons button)
                {
                    InputAction action;
                    switch (button)
                    {
                        default:
                            return false;

                        case Buttons.A:
                            action = _input.Game.ButtonA;
                            break;

                        case Buttons.B:
                            action = _input.Game.ButtonB;
                            break;

                        case Buttons.X:
                            action = _input.Game.ButtonX;
                            break;

                        case Buttons.Y:
                            action = _input.Game.ButtonY;
                            break;

                        case Buttons.LB:
                            action = _input.Game.ButtonLB;
                            break;

                        case Buttons.RB:
                            action = _input.Game.ButtonRB;
                            break;

                        case Buttons.DDown:
                            action = _input.Game.DPadDown;
                            break;

                        case Buttons.DUp:
                            action = _input.Game.DPadUp;
                            break;

                        case Buttons.DLeft:
                            action = _input.Game.DPadLeft;
                            break;

                        case Buttons.DRight:
                            action = _input.Game.DPadRight;
                            break;
                    }

                    bool result = action.triggered;

                    return result;
                }
            }
        }
    }

}

