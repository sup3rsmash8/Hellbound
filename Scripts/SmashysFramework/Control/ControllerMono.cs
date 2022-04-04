using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    /// <summary>
    /// A MonoBehaviour-ized wrapper for Controller objects.
    /// </summary>
    public abstract class ControllerMono : MonoBehaviour
    {
        /// <summary>
        /// Returns the controller nested inside this MB.
        /// </summary>
        /// <returns></returns>
        protected abstract Controller GetController();

        /// <summary>
        /// Possesses a pawn.
        /// </summary>
        public bool PossessPawn(int index, IPawn pawn)
        {
            return GetController()?.PossessPawn(pawn) ?? false;
        }

        /// <summary>
        /// Unpossesses the current pawn.
        /// </summary>
        public void UnpossessCurrentPawn(int index)
        {
            GetController()?.UnpossessCurrentPawn();
        }

        protected virtual void Update()
        {
            GetController()?.ControllerUpdate();
        }

        protected virtual void OnDestroy()
        {
            GetController()?.CleanUp();
        }
    }
}

