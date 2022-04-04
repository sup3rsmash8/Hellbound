using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    public sealed class PlayerControllerMono : ControllerMono
    {
        private PlayerController _playerController = null;

#if UNITY_EDITOR
        [SerializeField, Tooltip("The pawn currently being controlled. This will only accept MonoBehaviours that derive from IPawn!")]
        private MonoBehaviour _pawn;
#endif

        protected override Controller GetController()
        {
            if (!Application.isPlaying) 
                return null;

            if (_playerController == null)
            {
                _playerController = new PlayerController();
            }

            return _playerController;
        }

        private void Awake()
        {
            if (_playerController != null)
            {
                _playerController.CleanUp();
                _playerController = new PlayerController();//PlayerController.InitializeNewPlayerController();
            }

#if UNITY_EDITOR
            if (_pawn && _pawn is IPawn pawn)
            {
                PossessPawn(0, pawn);
            }
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_pawn)
            {
                UnpossessCurrentPawn(0);
                return;
            }

            IPawn asPawn = _pawn as IPawn;

            if (asPawn == null)
            {
                _pawn = null;

                UnpossessCurrentPawn(0);
                //
                Debug.LogError("A MonoBehaviour must derive from IPawn in order to be controlled by a PlayerController!");

                return;
            }

            PossessPawn(0, asPawn);
        }
#endif
    }
}

