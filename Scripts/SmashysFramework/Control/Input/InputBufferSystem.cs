using System;
using System.Collections;
using System.Collections.Generic;

namespace SmashysFramework
{
    /// <summary>
    /// An input system capable of buffering inputs until an action responds to it.
    /// <br>If this system is informed that a button has been pressed, but no actions are registered for it through this system,</br>
    /// <br>the button press will be remembered for a period of time until an action is registered</br>
    /// </summary>
    public class InputBufferSystem
    {
        public InputBufferSystem()
        {
            Dictionary<Buttons, InputBuffer> bufferMap = new Dictionary<Buttons, InputBuffer>();
            for (Buttons b = (Buttons)0; b < Buttons.Max; b++)
            {
                bufferMap.Add(b, new InputBuffer());
            }

            _buffers = bufferMap;
        }

        /// <summary>
        /// For how long an input remains buffered. Is global across all instances.
        /// </summary>
        public static float BufferTime { get; private set; } = 0.25f;

        /// <summary>
        /// Returns whether a bufferable button is currently pressed.
        /// </summary>
        public bool Pressed(Buttons button) => _buffers[button].Pressed;

        /// <summary>
        /// Returns whether a bufferable button is currently being buffered (not necessarily pressed).
        /// </summary>
        public bool Buffered(Buttons button) => _activeInputBuffers.Contains(_buffers[button]);

        private Dictionary<Buttons, InputBuffer> _buffers = new Dictionary<Buttons, InputBuffer>();

        private List<InputBuffer> _activeInputBuffers = new List<InputBuffer>();

        /// <summary>
        /// Assigns delegates to a buffer. onPress will return whether or not the buffer should go inactive after execution.
        /// <br>NOTE: Buffering works by calling onPress() every frame for the duration of the buffer until it returns true (releasing 
        /// the button will not stop this). Therefore, you shouldn't put code that affects values of any kind in cases where false CAN be 
        /// returned, unless that code is SUPPOSED TO be called every frame.</br>
        /// </summary>
        /// <param name="button">Which button should buffer the delegates.</param>
        /// <param name="isAssignment">Whether this is an assignment or unassignment.</param>
        /// <param name="actor">The actor that will be referenced in the callback parameters. Does nothing if isAssignement is false, the actor is nullified automatically, anyway.</param>
        /// <param name="onPress">The action that is called as soon as the buffer goes active and valid.</param>
        /// <param name="onHeld">The action that is called every frame the button is held, after onPress has been called.</param>
        /// <param name="onRelease">The action that is called the first frame the button is no longer held.</param>
        public void AssignInputBuffer(Buttons button, bool isAssignment, Actor actor = null, Func<Actor, bool> onPress = null, Action<Actor> onHeld = null, Action<Actor> onRelease = null)
        {
            if (isAssignment)
            {
                _buffers[button].SetActor(actor);
                _buffers[button].OnPress += onPress;
                _buffers[button].OnHold += onHeld;
                _buffers[button].OnReleased += onRelease;
            }
            else
            {
                _buffers[button].SetActor(null);
                if (onPress != null)
                    _buffers[button].OnPress -= onPress;

                if (onHeld != null)
                    _buffers[button].OnHold -= onHeld;

                if (onRelease != null)
                    _buffers[button].OnReleased -= onRelease;
            }
        }

        /// <summary>
        /// The update method for the system. Put this in your Update() method if running this in a MonoBehaviour (this isn't one). 
        /// </summary>
        /// <param name="deltaTime">Time.deltaTime.</param>
        public void UpdateBuffers(float deltaTime)
        {
            for (int i = 0; i < _activeInputBuffers.Count; i++)
            {
                if (_activeInputBuffers[i].bufferUpdate == null)
                {
                    _activeInputBuffers.RemoveAt(i);
                    i--;
                    continue;
                }

                _activeInputBuffers[i].bufferUpdate(deltaTime);
            }
        }

        /// <summary>
        /// Tells the buffer system that a button has been pressed or released.
        /// </summary>
        /// <param name="button">Which button has been pressed or released.</param>
        /// <param name="pressed">Is this a press or release?</param>
        public void OnInput(Buttons button, bool pressed)
        {
            if (pressed)
            {
                if (!_activeInputBuffers.Contains(_buffers[button]))
                {
                    _activeInputBuffers.Add(_buffers[button]);
                }
            }

            _buffers[button].Pressed = pressed;
        }

        // The subclass that handles the buffering of a button.
        public class InputBuffer
        {
            public event Func<Actor, bool> OnPress = delegate { return false; };
            public event Action<Actor> OnHold = delegate { };
            public event Action<Actor> OnReleased = delegate { };

            private Actor _actor;

            private float _timer;

            public delegate void BufferUpdate(float deltaTime);

            public BufferUpdate bufferUpdate;

            private bool _pressed;
            public bool Pressed
            {
                get => _pressed;
                set
                {
                    if (value && !_pressed)
                    {
                        bufferUpdate = BufferPhasePress;
                        _timer = BufferTime;
                    }

                    _pressed = value;
                }
            }

            private void BufferPhasePress(float deltaTime)
            {
                // Button will keep buffering until the button
                // is assigned a function, and that function returns true.
                if (OnPress == null || !OnPress(_actor))
                {
                    _timer -= deltaTime;

                    if (_timer <= 0)
                        bufferUpdate = null;
                }
                else
                {
                    if (Pressed)
                        bufferUpdate = BufferPhaseHold;
                    else
                        bufferUpdate = BufferPhaseRelease;
                }
            }

            private void BufferPhaseHold(float deltaTime)
            {
                OnHold?.Invoke(_actor);

                if (!Pressed)
                    bufferUpdate = BufferPhaseRelease;
            }

            private void BufferPhaseRelease(float deltaTime)
            {
                OnReleased?.Invoke(_actor);
                bufferUpdate = null;
            }

            public void SetActor(Actor newActor)
            {
                _actor = newActor;
            }
        }
    }
}

