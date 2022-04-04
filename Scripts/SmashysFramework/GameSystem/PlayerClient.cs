using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    /// <summary>
    /// Actor class that represents a player in the game world.
    /// Contains information about which character they're playing
    /// as, but can be expanded upon to add more player-specific stuff.
    /// </summary>
    public class PlayerClient : Actor
    {
        #region Statics
        private static int ClientCount = 0;

        #endregion

        protected virtual Controller InitializeController() => new PlayerController();

        [SerializeField]
        protected Actor _clientActor;

        /// <summary>
        /// Returns the actor this player is using.
        /// </summary>
        public Actor GetActor() => _clientActor;

        //[SerializeField]
        //private UISystem _uiSystem;

        protected Controller _controller = null;

        /// <summary>
        /// Sets the actor that player is playing as. Note that inputs
        /// may only be received by the actor if it is an IPawn.
        /// </summary>
        /// <param name="newActor"></param>
        public virtual void SetActor(Actor newActor)
        {
            // Don't client ourselves. duh
            if (newActor is PlayerClient)
                newActor = null;

            _clientActor = newActor;

            _controller.PossessPawn(_clientActor as IPawn);
        }

        protected override void ActorPostAwake()
        {
            base.ActorPostAwake();

            //DontDestroyOnLoad(gameObject);

            ClientCount++;

            _controller = InitializeController();

            if (_clientActor is IPawn acAsPa)
            {
                if (_controller.GetPawn() != acAsPa)
                {
                    _controller.PossessPawn(acAsPa);
                }
            }

            OnActorDestroy += Event_OnActorDestroy;
        }

        protected override void ActorUpdate(float deltaTime)
        {
            base.ActorUpdate(deltaTime);

            _controller?.ControllerUpdate();
        }

        /// <summary>
        /// Instantiates the player's dependencies, according to the Game Mode Configuration.
        /// </summary>
        public virtual void InitializePlayer(Vector3 position, Quaternion rotation)
        {
            GameModeConfiguration config = GameManager.GetGameModeConfiguration();
            if (config && config.PlayerPrefab)
            {
                SetActor(Instantiate(config.PlayerPrefab, position, rotation));
            }
        }

        /// <summary>
        /// Instantiates the player's dependencies.
        /// </summary>
        public virtual void InitializePlayer(Vector3 position, Quaternion rotation, int index, Actor playerObject)
        {
            GameModeConfiguration config = GameManager.GetGameModeConfiguration();
            if (config && playerObject)
            {
                SetActor(Instantiate(playerObject, position, rotation));
            }
        }

        /// <summary>
        /// PlayerClient: The controller unpossesses the pawn 
        /// and destroys the actor the player was playing as.
        /// </summary>
        protected override void ActorOnBeginDestroy()
        {
            base.ActorOnBeginDestroy();            
            _controller.CleanUp();

            if (_clientActor)
                Destroy(_clientActor.gameObject);

            SetActor(null);


            ClientCount--;

            OnActorDestroy -= Event_OnActorDestroy;
        }

        /// <summary>
        /// If the actor that was just destroyed was our player object,
        /// set our actor to null to successfully disconnect all refs
        /// to it.
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void Event_OnActorDestroy(Actor actor, ActorDestroyType destroyType)
        {
            if (actor == _clientActor)
            {
                SetActor(null);
            }
        }

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (IsPrefab(this) && _clientActor)
                {
                    _clientActor = null;
                }

                return;
            }

            if (_controller != null)
            {
                print("knull");
                // way too lazy to just make a custom editor
                // that can just call SetActor() :(
                if (_clientActor is IPawn acAsPa)
                {
                    if (_controller.GetPawn() != acAsPa)
                    {
                        _controller.PossessPawn(acAsPa);
                    }
                }
            }
            else
            {
                print("null");
            }
        }
    }
}