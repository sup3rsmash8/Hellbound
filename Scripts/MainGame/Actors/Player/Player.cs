using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    using Weapon;
    using Level;
    public partial class Player : Character, IPawn, IWeaponCarrier, ILevelEnterInitializable//, IKillable
    {
        public static readonly IReadOnlyDictionary<System.Type, PlayerState> playerStates = new Dictionary<System.Type, PlayerState>
        {
            { typeof(PS_IdleMove), new PS_IdleMove() },
            { typeof(PS_StopRun), new PS_StopRun() },
            { typeof(PS_Fall), new PS_Fall() },
            { typeof(PS_Jump), new PS_Jump() },
            { typeof(PS_DashGround), new PS_DashGround() },
            { typeof(PS_DashAirborne), new PS_DashAirborne() },
            { typeof(PS_WallJump_Attach), new PS_WallJump_Attach() },
            { typeof(PS_WallJump_Jump), new PS_WallJump_Jump() },
            { typeof(PS_Ledge_Grab), new PS_Ledge_Grab() },
            { typeof(PS_Ledge_Getup), new PS_Ledge_Getup() },
            { typeof(PS_RecoilJump), new PS_RecoilJump() },
            { typeof(PS_RecoilPound), new PS_RecoilPound() },
            { typeof(PS_RecoilPound_Landing), new PS_RecoilPound_Landing() },
            { typeof(PS_RecoilPound_BounceJump), new PS_RecoilPound_BounceJump() },
        };

        #region Cache
        //private WeaponHandler _weaponHandler = null;

        // This variable is used during PS_IdleMove to define the speed
        // on the local forward and right axes.
        private Vector3 _fwdRightSpeed = Vector3.zero;

        // The game time when the state was last changed.
        private float _stateChangeTimestamp = 0;

        // An extra factor to IFocable.AxisLag intended to be modified
        // by states for smoother camera movement (for instance when jumping,
        // the camera should slightly lag behind on the y-axis).
        private Vector3 _axisLagModifier = Vector3.one;
        #endregion

        #region Properties
        // Where all states are processed (idle, move, jump, dash, etc.)
        private StateMachine<Player> _stateMachine = null;

        public StateMachine<Player>.IState GetCurrentState() =>
            _stateMachine.CurrentState;

        protected override bool ExecuteOnDeathFloorReachedEvent()
        {
            return true;
        }

        #region WeaponSpecialActions
        private List<WeaponSpecialAction> _currentSpecialActions = new List<WeaponSpecialAction>();

        private void AddWeaponSpecializedActions(PlayerState state)
        {
            foreach (WeaponSpecialAction action in _currentSpecialActions)
            {
                if (action.IsCurrentStateAccepted(state.GetType()))
                {
                    _bufferSystem.AssignInputBuffer(action.GetButtonToPress(), true, this,
                        action.GetButtonPressAction(), action.GetButtonHoldAction(), action.GetButtonReleaseAction());
                }
            }
        }

        private void RemoveWeaponSpecializedActions()
        {
            foreach (WeaponSpecialAction action in _currentSpecialActions)
            {
                _bufferSystem.AssignInputBuffer(action.GetButtonToPress(), false, this,
                    action.GetButtonPressAction(), action.GetButtonHoldAction(), action.GetButtonReleaseAction());
            }
        }
        #endregion

        #region StateSpecific

        #region PS_IdleMove
        // Flag that determines if the character should come
        // to a stop using the stopping animation when letting
        // go of the control stick.
        private bool ps_IdleMove_StopMoveOnNeutral = false;
        #endregion

        #region PS_Airborne
        private bool ps_Airborne_HasWallJumped = false;
        #endregion

        #region PS_Jump
        // Flag that determines if a jump can acquire more height
        // from holding the jump button.
        private bool ps_Jump_IsHoldingButton = false;

        // Sets turning speed to 0 during jumping. Used most prominently
        // during back jumps.
        private bool ps_Jump_lockTurning = false;

        // What type of jump that is being currently performed.
        private PS_Jump.JumpType ps_Jump_jumpType = PS_Jump.JumpType.Regular;
        #endregion

        #region PS_WallJump_Attach
        private float ps_WallJump_Attach_wallTouchSpeed = 0;
        private Vector3 ps_WallJump_Attach_WallNormal = Vector3.forward;
        private Vector3 ps_WallJump_Attach_JumpPosition = Vector3.zero;
        #endregion

        #region PS_Ledge_Grab
        /// <summary>
        /// The object of the ledge's transform.
        /// </summary>
        private Transform ps_Ledge_Grab_grabbedTrans = null;

        /// <summary>
        /// The platform's world-to-local matrix.
        /// </summary>
        private Matrix4x4 ps_Ledge_Grab_platformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        private float ps_Ledge_Grab_LedgeTimestamp = 0;
        #endregion

        #endregion

        #region Input
        private InputBufferSystem _bufferSystem = null;

        // The current input on the left analog stick.
        private Vector2 _leftAnalogInput;
        
        /// <summary>
        /// Retrieves the current input on the left analog stick.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetLeftAnalogInput()
        {
            return _leftAnalogInput;
        }

        private Vector2 _rightAnalogInput;

        /// <summary>
        /// Retrieves the current input on the right analog stick.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetRightAnalogInput()
        {
            return _rightAnalogInput;
        }

        private event System.Action<Vector2> _onAnalogStickLeft;
        private event System.Action<Vector2> _onAnalogStickRight;
        private event System.Action<Buttons, bool> _onButton;
        //private event System.Action<Buttons, float, bool> _onTriggerButton;
        #endregion

        #region IPawn
        private Controller _controller = null;

        private bool _isPossessed = false;
        public bool IsPossessable => !_isPossessed;

        public void OnPossessed(Controller controller)
        {
            if (controller != null && (Object)controller.GetPawn() != this)
            {
                _isPossessed = true;
            }
        }

        public void OnUnpossessed(Controller controller)
        {
            IPawn thisPawn = this;
            if (controller != null && controller.GetPawn() == thisPawn)
            {
                _isPossessed = false;
            }
        }

        public void OnAxisInput(Axes axis, float value)
        {
            switch (axis)
            {
                case Axes.LAnalogX:
                    _leftAnalogInput.x = value;
                    break;

                case Axes.LAnalogY:
                    _leftAnalogInput.y = value;
                    _onAnalogStickLeft?.Invoke(_leftAnalogInput);
                    break;

                case Axes.RAnalogX:
                    _rightAnalogInput.x = value;
                    break;

                case Axes.RAnalogY:
                    _rightAnalogInput.y = value;
                    _onAnalogStickRight?.Invoke(_rightAnalogInput);
                    break;
            }
        }

        public void OnButtonInput(Buttons button, bool phase)
        {
            _bufferSystem?.OnInput(button, phase);
        }
        #endregion

        #region IFocable
        [Header("Focable")]
        [SerializeField, Tooltip("If cameras should focus on a non-volatile point, these are the local coordinates they will focus on.")]
        private Vector3 _focusPoint;

        [SerializeField, Tooltip("If cameras should focus on a volatile point, put that point in here. If null, defaults to 'Focus Point'.")]
        private Transform _focusTransform;

        public override Vector3 FocusPoint
        {
            get
            {
                Vector3 focusPt = _focusTransform ? _focusTransform.position :
                    Trans.position + Trans.rotation * _focusPoint;

                return focusPt;
            }
        }


        //public override Vector3 AxisLag
        //{
        //    get
        //    {
        //        return new Vector3(_cameraAxisLag.x * _axisLagModifier.x, 
        //            _cameraAxisLag.y * _axisLagModifier.y, 
        //            _cameraAxisLag.z * _axisLagModifier.z);
        //    }
        //}
        #endregion

        #region IWeaponCarrier

        [SerializeField]
        private Transform _primaryWeaponHand;
        public Transform PrimaryHand => _primaryWeaponHand;

        [SerializeField]
        private Transform _secondaryWeaponHand;
        public Transform SecondaryHand => _secondaryWeaponHand;


        private Weapon _primaryWeapon = null;
        public Weapon PrimaryWeapon
        {
            get => _primaryWeapon;
            set => _primaryWeapon = value;
        }

        private Weapon _secondaryWeapon = null;
        public Weapon SecondaryWeapon
        {
            get => _secondaryWeapon;
            set => _secondaryWeapon = value;
        }

        public void OnWeaponWield(Weapon weapon)
        {
            _currentSpecialActions.AddRange(weapon.GetAllSpecialActions());
            AddWeaponSpecializedActions(GetCurrentState() as PlayerState);
        }

        public void OnWeaponUnwield(Weapon weapon)
        {
            RemoveWeaponSpecializedActions();
            _currentSpecialActions.Clear();
        }
        #endregion

        #region Anim
        /// <summary>
        /// Returns the animation name, but with the current alteration suffix at the end ("_Standard", "_TwoHanded", etc.)
        /// </summary>
        /// <param name="baseName">The base animation name.</param>
        private int GetAnimationWithAttributeName(string baseName)
        {
            string suffix;

            Weapon wpn = PrimaryWeapon;

            if (wpn)
            {
                suffix = wpn.AnimSuffix;
            }
            else
            {
                suffix = "_Standard";
            }

            return Animator.StringToHash(baseName + suffix);
        }
        #endregion

        [Space]

        #region IKillable
        [SerializeField, Range(0, 100)]
        private float _health = 100;
        public float Health => _health;
        #endregion

        #region MechanicSettings

        [Header("Mechanic settings")]

        [Space]

        [SerializeField, Range(1, 90), Tooltip("How steep a wall has to be at least to wall jump it.")]
        private float _wallJumpSteepnessLimit = 80;
        public float WallJumpSteepnessLimit => _wallJumpSteepnessLimit;

        [SerializeField, Range(1, 90), Tooltip("The minimum allowed horizontal difference in angles for a wall jump to occur.")]
        private float _wallJumpHorizontalArc = 45;
        public float WallJumpHorizontalArc => _wallJumpHorizontalArc;

        [SerializeField, Range(1, 90), Tooltip("The arc within which the player can wall jump, relative to the wall.")]
        private float _wallJumpBounceOffArc = 45;
        public float WallJumpBounceOffArc => _wallJumpBounceOffArc;

        [Space]

        [Header("Ledge settings")]
        [SerializeField, Min(0), Tooltip("The amount of time after grabbing a ledge that must pass before grabbing another.")]
        private float _ledgeGrabWindDownTime = 0.2f;

        [SerializeField, Min(0), Tooltip("The amount of time that must pass before the player can climb/let go of a ledge after grabbing one.")]
        private float _ledgeGrabInactionableTime = 0.3f;

        [Space]

        [Header("Event Particle Systems")]
        [SerializeField, Tooltip("The particle system that leaves trails behind the player during and after a dash.")]
        private ParticleSystem _dashWakePart = null;

        [SerializeField, Tooltip("The particle system that plays when slipping down a wall.")]
        private ParticleSystem _wallJumpAttachPart = null;

        [SerializeField, Tooltip("The audio source that plays when slipping down a wall.")]
        private AudioSource _wallSlipSound = null;

        [SerializeField, Tooltip("The particle system that plays when slipping down a wall.")]
        private ParticleSystem _wallJumpJumpPart = null;

        [SerializeField, Tooltip("The audio source that plays when the player has been falling for a long time.")]
        private AudioSource _fallAudio = null;
        #endregion

        #endregion

#if UNITY_EDITOR
        [SerializeField]
        private Weapon _weaponPrefab;
#endif

        /// <summary>
        /// Spawns the weapon the player is currently holding, according to the quest manager.
        /// Used primarily for when the player is spawned into the scene.
        /// </summary>
        /// <returns></returns>
        private Weapon GetCurrentQuestWeaponPrefab()
        {
#if UNITY_EDITOR
            return _weaponPrefab;
#endif
        }

        #region ILevelInitializable
        public void OnLevelEnter(MG_PlayerStart playerStart, MG_PlayerStart.PlayerStartType playerStartType)
        {
            GravityEulerY = playerStart.transform.eulerAngles.y;
        }
        #endregion

        protected override void ActorPostAwake()
        {
            _bufferSystem = new InputBufferSystem();

            _stateMachine = new StateMachine<Player>(this, playerStates[typeof(PS_IdleMove)], StateChangeType.ChangeAtEndOfUpdate, PS_IdleMove.StateEnterTypes.Neutral);

            _stateMachine.OnStateChanged += OnStateChanged;

            Weapon weaponPrefab = GetCurrentQuestWeaponPrefab();
            
            if (weaponPrefab)
            {
                Weapon weapon = Instantiate(weaponPrefab);
                if (weapon)
                {
                    weapon.Wield(this);
                }
            }

            // Event Particle systems
            #region EventParticleSystems
            if (!_dashWakePart)
            {
                // Try find the particle system with this name.
                Transform particleTrans = Trans.Find("Part_Player_Dash_Wake");
                if (particleTrans)
                {
                    _dashWakePart = particleTrans.GetComponent<ParticleSystem>();
                }
            }

            if (!_wallJumpAttachPart)
            {
                // Try find the particle system with this name.
                Transform trans = Trans.Find("Part_Player_WallJump_Attach");
                if (trans)
                {
                    _wallJumpAttachPart = trans.GetComponent<ParticleSystem>();
                }

                if (!_wallSlipSound)
                {
                    trans = _wallJumpAttachPart.transform.Find("Audio_Wall_Slip");
                    if (trans)
                    {
                        _wallSlipSound = trans.GetComponent<AudioSource>();
                    }
                }
            }

            if (!_wallJumpJumpPart)
            {
                // Try find the particle system with this name.
                Transform particleTrans = Trans.Find("Part_Player_WallJump_Wake");
                if (particleTrans)
                {
                    _wallJumpJumpPart = particleTrans.GetComponent<ParticleSystem>();
                }
            }

            if (!_fallAudio)
            {
                Transform trans = _wallJumpAttachPart.transform.Find("Audio_Fall_Wind");
                if (trans)
                {
                    _fallAudio = trans.GetComponent<AudioSource>();
                }
            }
            #endregion
        }

        protected override void CharacterUpdate(float deltaTime)
        {
            _stateMachine.StateMachineUpdate(deltaTime);

            if (MyDeFactoTimeScale > 0)
                _bufferSystem.UpdateBuffers(deltaTime);

#if UNITY_EDITOR
            //if (Input.GetKeyDown(KeyCode.U))
            //{
            //    PrimaryWeapon = null;
            //    SecondaryWeapon = null;
            //}
            //else if (Input.GetKeyDown(KeyCode.I))
            //{
            //    WeaponGun[] weapons = FindObjectsOfType<WeaponGun>();

            //    if (weapons.Length > 0)
            //    {
            //        PrimaryWeapon = weapons[0];
            //    }

            //    //if (weapons.Length > 1)
            //    //{
            //    //    SecondaryWeapon = weapons[1];
            //    //}

            //    //print(weapons.Length);
            //}
#endif
        }

        private void FixedUpdate()
        {
            _stateMachine.StateMachineFixedUpdate();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw focus point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(FocusPoint, 0.1f);
        }

        protected override void OnDeathFloorReached()
        {
            Trans.position = new Vector3(Trans.position.x, MG_LevelManager.DeathFloorHeight, Trans.position.z);
        }

#if UNITY_EDITOR
        //private void OnValidate()
        //{
        //    if (!Application.isPlaying)
        //        return;

        //    if (_pPrimaryWeapon)
        //    {
        //        _pPrimaryWeapon.Drop();
        //    }

        //    if (_pSecondaryWeapon)
        //    {
        //        _pSecondaryWeapon.Drop();
        //    }

        //    if (_primaryWeapon && !_primaryWeapon.IsPickedUp())
        //    {
        //        _primaryWeapon.PickUp(this, PickedUpAs.Primary);
        //    }

        //    if (_secondaryWeapon && !_secondaryWeapon.IsPickedUp())
        //    {
        //        _secondaryWeapon.PickUp(this, PickedUpAs.Secondary);
        //    }

        //    _pPrimaryWeapon = _primaryWeapon;
        //    _pSecondaryWeapon = _secondaryWeapon;
        //}
#endif

        #region StateRelated
        // This callback helps reset values that might've been
        // changed that shouldn't last between states. Example
        // of this is the friction modifier, which is modified
        // during the dash state to maximize dashing distance,
        // and must be set back to 1 so we don't slip all around.
        private void OnStateChanged(StateMachine<Player>.IState currentState, StateMachine<Player>.IState previousState)
        {
            PlayerState psCurrentState = (PlayerState)currentState;
            PlayerState psPreviousState = (PlayerState)previousState;

            if (_dashWakePart && !(currentState is PS_IdleMove))
            {
                _dashWakePart.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            FrictionModifier = 1;
        }

        // Section containing helpful functions for
        // calculation of speed or other values
        #region StateHelperFunctions
        /// <summary>
        /// Returns the amount of seconds that have passed since the player entered the new state.
        /// </summary>
        /// <returns></returns>
        public float GetStateTime() => Time.time - _stateChangeTimestamp;

        /// <summary>
        /// Adds the player's current gravity to the provided vector.
        /// </summary>
        public Vector3 AddGravity(Vector3 speed, float deltaTime)
        {
            float gravity = _charSpdSettings ? _charSpdSettings.Gravity : 0;

            return speed + Vector3.down * gravity * deltaTime;
        }

        #region WallJump
        /// <summary>
        /// Returns the middle wall ray used for checking walls.
        /// </summary>
        public static Ray WallCheckRayTop(Player player, out float distance, bool checkBackwards = false)
        {
            const float postRadiusOffset = 0.15f;

            CharacterCollisionModule ccm = player._collisionDetector as CharacterCollisionModule;

            distance = ccm.ScaledCapsuleRadius + postRadiusOffset;

            return new Ray(ccm.CapsuleTopPoint(false),
                player.GravityForward() * (checkBackwards ? -1 : 1));
        }

        public static Ray WallCheckRayMid(Player player, out float distance, bool checkBackwards = false)
        {
            const float postRadiusOffset = 0.15f;

            CharacterCollisionModule ccm = player._collisionDetector as CharacterCollisionModule;

            distance = ccm.ScaledCapsuleRadius + postRadiusOffset;

            return new Ray(ccm.CapsuleCenter(),
                player.GravityForward() * (checkBackwards ? -1 : 1));
        }

        public static Ray WallCheckRayBottom(Player player, out float distance, bool checkBackwards = false)
        {
            const float postRadiusOffset = 0.15f;

            CharacterCollisionModule ccm = player._collisionDetector as CharacterCollisionModule;

            distance = ccm.ScaledCapsuleRadius + postRadiusOffset;

            return new Ray(ccm.CapsuleBottomPoint(false),
                player.GravityForward() * (checkBackwards ? -1 : 1));
        }

        public static bool IsInWallJumpRange(Player player, Vector3 wallNormal)
        {
            return Vector3.Angle(player.GravityForward(), -wallNormal) < player.WallJumpHorizontalArc;
        }
        #endregion

        #region Ledge
        /// <summary>
        /// Returns the ledge grab ray that checks if 
        /// there is a surface above the ledge being grabbed.
        /// </summary>
        public static Ray LedgeSurfaceCheck(Player player, PerformConditions condition, out float distance)
        {
            CharacterCollisionModule ccm = player._collisionDetector as CharacterCollisionModule;

            Vector3 fwdOffset = player.GravityForward() * (ccm.ScaledCapsuleRadius + 0.2f);

            if (condition == PerformConditions.Behind)
            {
                fwdOffset *= -1;
            }

            Vector3 start = ccm.CapsuleTopPoint() + player.GravityUp() * 0.3f + fwdOffset;

            distance = ccm.ScaledCapsuleHeight * 0.6666667f;

            return new Ray(start, -player.GravitySpaceUp());
        }

        /// <summary>
        /// Returns the ledge grab ray that checks if 
        /// there is a wall in front of us.
        /// </summary>
        public static Ray LedgeWallCheck(Player player, PerformConditions condition, out float distance)
        {
            CharacterCollisionModule ccm = player._collisionDetector as CharacterCollisionModule;

            Vector3 dir = player.GravityForward();

            if (condition == PerformConditions.Behind)
            {
                dir *= -1;
            }

            Vector3 start = ccm.CapsuleTopPoint(false);

            const float extraOffset = 0.25f;

            distance = ccm.ScaledCapsuleRadius + extraOffset;

            return new Ray(start, dir);
        }

        /// <summary>
        /// Returns if the player currently meets the requirements 
        /// for a ledge grab to be performed. Used as part of actually
        /// checking if the player should go into the ledge grab state.
        /// </summary>
        public static bool CanLedgeGrab(Player player, PerformConditions condition, out RaycastHit wallHit, out RaycastHit surfaceHit)
        {
            if (condition == PerformConditions.Cannot)
            {
                wallHit = new RaycastHit();
                surfaceHit = new RaycastHit();
                return false;
            }

            LayerMask lm = player._collisionDetector.CollMask;

            if (condition == PerformConditions.InFrontAndBehind)
            {
                return Check(out wallHit, out surfaceHit, true) || Check(out wallHit, out surfaceHit, false);
            }
            else
            {
                return Check(out wallHit, out surfaceHit, condition == PerformConditions.Behind);
            }

            bool Check(out RaycastHit wHit, out RaycastHit surfHit, bool behind = false)
            {
                PerformConditions cond = behind ? 
                    PerformConditions.Behind : PerformConditions.InFront;

                Ray surfaceCheck = LedgeSurfaceCheck(player, cond, out float distance);
                Ray wallCheck = LedgeWallCheck(player, cond, out float distance2);

                Debug.DrawRay(surfaceCheck.origin, surfaceCheck.direction, Color.red);
                Debug.DrawRay(wallCheck.origin, wallCheck.direction, Color.blue);

                bool cond1 = Physics.Raycast(surfaceCheck, out surfHit, distance, lm);
                bool cond2 = Physics.Raycast(wallCheck, out wHit, distance2, lm);
                if (cond1 && cond2)
                {
                    // ~20 degrees
                    const float cosSurfaceLim = 0.342f;
                    // ~85 degrees
                    const float cosWallLim = 0.9962f;

                    float surfAngle = Vector3.Dot(player.GravitySpaceUp(), surfHit.normal);

                    if (surfAngle >= cosWallLim)
                    {
                        surfAngle = Vector3.Dot(player.GravitySpaceUp(), wHit.normal);

                        if (surfAngle < cosSurfaceLim)
                        {
                            // Do a capsule check to see if there is collision 
                            // where we're about to climb up.
                            if (player._collisionDetector is CharacterCollisionModule ccm)
                            {
                                const float offset = 0.1f;
                                if (ccm.CheckMyCapsule(surfHit.point + player.GravitySpaceUp() * (ccm.ScaledCapsuleHeight + offset)))
                                    return false;
                            }

                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the world space position the player would be at when grabbing a ledge, based on two RaycastHits.
        /// </summary>
        static Vector3 GetLedgeGrabPosition(Player player, RaycastHit wallHit, RaycastHit surfHit)
        {
            CharacterCollisionModule ccm = player._collisionDetector as CharacterCollisionModule;

            Matrix4x4 invPlayerMatrix = player.Trans.worldToLocalMatrix;

            Vector3 wallPoint = invPlayerMatrix.MultiplyPoint3x4(wallHit.point);
            Vector3 wallNormal = invPlayerMatrix.MultiplyVector(wallHit.normal);
            Vector3 surfPoint = invPlayerMatrix.MultiplyPoint3x4(surfHit.point);

            Vector3 invertedResult = new Vector3(wallPoint.x, 0, wallPoint.z) + wallNormal * ccm.ScaledCapsuleRadius;
            invertedResult.y = surfPoint.y - ccm.ScaledCapsuleHeight;

            return player.Trans.localToWorldMatrix.MultiplyPoint3x4(invertedResult);
        }
        #endregion

        #endregion

        #endregion

        #region AnimationEvents
        public void AnimEvent_AttackPrimaryWeapon()
        {
            Weapon weapon = PrimaryWeapon;

            if (weapon)
            {
                weapon.StartAttack();
            }
        }

        public void AnimEvent_StopAttackPrimaryWeapon()
        {
            Weapon weapon = PrimaryWeapon;

            if (weapon)
            {
                weapon.StopAttack();
            }
        }

        public void AnimEvent_AttackSecondaryWeapon()
        {
            Weapon weapon = SecondaryWeapon;

            if (weapon)
            {
                weapon.StartAttack();
            }
        }

        public void AnimEvent_StopAttackSecondaryWeapon()
        {
            Weapon weapon = SecondaryWeapon;

            if (weapon)
            {
                weapon.StopAttack();
            }
        }

        public void AnimEvent_SetPrimaryWeaponShootType(int type)
        {
            Weapon weapon = SecondaryWeapon;

            AnimEvent_SetGunShootType(weapon, type);
        }

        public void AnimEvent_SetSecondaryWeaponShootType(int type)
        {
            Weapon weapon = SecondaryWeapon;

            AnimEvent_SetGunShootType(weapon, type);
        }

        private void AnimEvent_SetGunShootType(Weapon weapon, int type)
        {
            if (weapon && weapon is WeaponGun gun)
            {
                ShootType shootType;
                switch (type)
                {
                    default:
                    case 0: shootType = ShootType.Normal; break;
                    case 1: shootType = ShootType.Heavy; break;
                }

                gun.SetShootType(shootType);
            }
        }
        #endregion
    }
}
