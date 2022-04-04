using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    /// <summary>
    /// Interface that allows Controller classes to take control of an object.
    /// </summary>
    public interface IPawn
    {
        /// <summary>
        /// Can Controller classes possess this object right now?
        /// </summary>
        bool IsPossessable { get; }

        /// <summary>
        /// Is called when a Controller possesses this object. Initialize
        /// your control-related stuff here.
        /// </summary>
        void OnPossessed(Controller controller);

        /// <summary>
        /// Is called when a Controller stops possessing this object.
        /// </summary>
        void OnUnpossessed(Controller controller);

        /// <summary>
        /// Method that receives axis inputs from a Controller.
        /// </summary>
        void OnAxisInput(Axes axis, float value);

        /// <summary>
        /// Method that receives button inputs from a Controller.
        /// </summary>
        void OnButtonInput(Buttons button, bool pressed);
    }

    public enum ButtonPhase
    {
        Pressed,
        Held,
        Released
    }
}
