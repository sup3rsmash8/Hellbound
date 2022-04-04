using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    /// <summary>
    /// Directional conditions under which a certain action, 
    /// such as wall-jumping or ledge-climbing, can be performed.
    /// </summary>
    public enum PerformConditions
    {
        /// <summary>
        /// Action cannot be performed.
        /// </summary>
        Cannot = 0,
        /// <summary>
        /// Action can be performed if object is in front of the player.
        /// </summary>
        InFront,
        /// <summary>
        /// Action can be performed if object is behind the player.
        /// </summary>
        Behind,
        /// <summary>
        /// Action can be performed if object is in front of or behind the player.
        /// </summary>
        InFrontAndBehind
    }

    public partial class Player
    {
        #region HashedAnimProperties
        public static readonly int hN_Dash = Animator.StringToHash("Dash");
        public static readonly int hN_DashAir = Animator.StringToHash("Dash_Air");
        public static readonly int hN_WallJump_Attach = Animator.StringToHash("WallJump_Attach");
        public static readonly int hN_WallJump_Jump = Animator.StringToHash("WallJump_Jump");
        public static readonly int hN_Ledge_Grab = Animator.StringToHash("Ledge_Grab");
        public static readonly int hN_Ledge_Hold = Animator.StringToHash("Ledge_Hold");
        public static readonly int hN_Ledge_Getup_Normal = Animator.StringToHash("Ledge_Getup_Normal");
        public static readonly int hN_Ledge_Getup_Fast = Animator.StringToHash("Ledge_Getup_Fast");
        public static readonly int hN_Dash_Recoil_DualGuns = Animator.StringToHash("Dash_Recoil_DualGuns");
        public static readonly int hN_Dash_Air_Recoil_DualGuns = Animator.StringToHash("Dash_Air_Recoil_DualGuns");
        public static readonly int hN_RecoilJump = Animator.StringToHash("RecoilJump");
        public static readonly int hN_RecoilPound = Animator.StringToHash("RecoilPound");
        public static readonly int hN_RecoilPound_Landing = Animator.StringToHash("RecoilPound_Landing");
        public static readonly int hN_RecoilPound_BounceJump = Animator.StringToHash("RecoilPound_BounceJump");

        public static readonly int h_MoveScale = Animator.StringToHash("MoveScale");
        public static readonly int h_TiltScale = Animator.StringToHash("TiltScale");
        #endregion

        public abstract class PlayerState : StateMachine<Player>.IState
        {
            /// <summary>
            /// Returns the dot product of the control stick direction and
            /// the player's direction.
            /// </summary>
            public static float ControlStickDirectionDot(Player user) => Vector3.Dot(new Vector3(user.GetLeftAnalogInput().x, 0, user.GetLeftAnalogInput().y), user.GravityForward());

            /// <summary>
            /// Returns a rotation that represents the direction of the control stick
            /// relative to the character. If no input, returns the  character's current
            /// rotation.
            /// </summary>
            /// <param name="user"></param>
            /// <returns></returns>
            public static Quaternion FaceControlStickDirection(Player user)
            {
                Vector2 analog = user.GetLeftAnalogInput();
                if (analog == Vector2.zero)
                {
                    Vector3 fow = user.GravityRotation * Vector3.forward;
                    analog = new Vector2(fow.x, fow.z);
                }

                //return Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(analog.x, 0, analog.y).normalized).eulerAngles.y, 0);
                return Quaternion.LookRotation(new Vector3(analog.x, 0, analog.y).normalized);
            }

            public static float FaceControlStickDirectionY(Player user) => Quaternion.LookRotation(new Vector3(user.GetLeftAnalogInput().x, 0, user.GetLeftAnalogInput().y)).eulerAngles.y;

            public static Vector3 AnalogStickIn3DSpace(Player user) => new Vector3(user.GetLeftAnalogInput().x, 0, user.GetLeftAnalogInput().y);

            public void OnStateEnter(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user._stateChangeTimestamp = Time.time;

                user.RemoveWeaponSpecializedActions();
                
                user.AddWeaponSpecializedActions(this);

                OnStateEnter_Exec(user, previousState, stateEnterArguments);
            }

            public abstract void OnStateEnter_Exec(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments);

            public void OnStateExit(Player user, StateMachine<Player>.IState nextState)
            {
                if (!user) return;

                user.RemoveWeaponSpecializedActions();

                OnStateExit_Exec(user, nextState);
            }

            public abstract void OnStateExit_Exec(Player user, StateMachine<Player>.IState nextState);
        }

        public abstract class PS_Grounded : PlayerState, StateMachine<Player>.IUpdateState, StateMachine<Player>.IFixedUpdateState
        {
            public override sealed void OnStateEnter_Exec(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                user._bufferSystem.AssignInputBuffer(Buttons.A, true, user, OnButtonAPressed);

                user._bufferSystem.AssignInputBuffer(Buttons.X, true, user, OnButtonXPressed);

                user._collisionDetector.OnGroundExit += OnGroundExit;

                user.SpeedYF = 0;

                OnStateEnter_ExecSub(user, previousState, stateEnterArguments);

                // Safety measure in case state somehow bypasses the event call.
                if (!user.OnGround)
                    OnGroundExit_Exec(user);
            }

            public virtual void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments) { }

            public void OnStateUpdate(Player user, float deltaTime)
            {
                if (!user || !user._charSpdSettings) return;

                Vector3 transPosition = user.Trans.position;
                Quaternion gravityRotation = user.GravityRotation;

                OnStateUpdate_Exec(user, ref transPosition, ref gravityRotation, deltaTime);

                if (transPosition != user.Trans.position)
                {
                    user.Trans.position = transPosition;
                }

                //if (gravityRotation != user.GravityRotation)
                //{
                    user.GravityRotation = gravityRotation;
                //}
            }

            protected virtual void OnStateUpdate_Exec(Player user, ref Vector3 transPosition, ref Quaternion gravityRotation, float deltaTime) { }

            public void OnStateFixedUpdate(Player user)
            {
                if (!user || !user._charSpdSettings) return;

                OnStateFixedUpdate_Exec(user);
            }

            protected virtual void OnStateFixedUpdate_Exec(Player user) { }

            public override sealed void OnStateExit_Exec(Player user, StateMachine<Player>.IState nextState)
            {
                user._bufferSystem.AssignInputBuffer(Buttons.A, false, null, OnButtonAPressed); 
                user._bufferSystem.AssignInputBuffer(Buttons.X, false, null, OnButtonXPressed);
                user._collisionDetector.OnGroundExit -= OnGroundExit;

                OnStateExit_ExecSub(user, nextState);
            }

            public virtual void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState) { }

            private void OnGroundExit(Actor actor)
            {
                Player player = (Player)actor;
                if (!player) return;
                OnGroundExit_Exec(player);
            }

            protected virtual void OnGroundExit_Exec(Player player)
            {
                if (!player) return;
                player._stateMachine?.ChangeState(playerStates[typeof(PS_Fall)]);
            }

            private bool OnButtonAPressed(Actor actor)
            {
                Player player = (Player)actor;
                if (!player) return false;

                return OnAButtonPressed_Exec(player);
            }

            /// <summary>
            /// What's supposed to happen when you press the A-button.
            /// By default, you jump. This can be overridden for your own implementation.
            /// </summary>
            protected virtual bool OnAButtonPressed_Exec(Player player)
            {
                PS_Jump.JumpType jumpType = PS_Jump.JumpType.Regular;
                player._stateMachine.ChangeState(playerStates[typeof(PS_Jump)], jumpType);

                return true;
            }

            private bool OnButtonXPressed(Actor actor)
            {
                Player player = (Player)actor;
                if (!player) return false;

                return OnXButtonPressed_Exec(player);
            }

            /// <summary>
            /// What's supposed to happen when you press the X-button.
            /// By default, you dash. This can be overridden for your own implementation.
            /// </summary>
            protected virtual bool OnXButtonPressed_Exec(Player player)
            {
                player._stateMachine.ChangeState(playerStates[typeof(PS_DashGround)]);

                return true;
            }

            /// <summary>
            /// This method can be used as a callback for OnAnimReachTransitionTime. This
            /// virtual template exists purely to let the user know this exists, though 
            /// preferrably this is the method you should be using when assigning to the 
            /// event. Note that the user has to manually assign/unassign the event this
            /// method.
            /// </summary>
            /// <param name="character">The character, aka. the player. Cast this in code to access the Player.</param>
            /// <param name="hashedAnimName">The animation name, hashed.</param>
            /// <param name="layer">The animation layer. Almost always 0.</param>
            protected virtual void OnAnimEnd(Character character, int hashedAnimName, int layer)
            {
                Player player = (Player)character;
                if (!player) return;

                player._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);
            }
        }

        /// <summary>
        /// Base class for airborne states, most prominently 'PS_Jump' and 'PS_Fall'.
        /// </summary>
        public abstract class PS_Airborne : PlayerState, StateMachine<Player>.IUpdateState, StateMachine<Player>.IFixedUpdateState
        {
            /// <summary>
            /// If true, entering this state leaving the ground will cause 
            /// any vertical velocity to be preserved (for example, running off an 
            /// uphill will slightly propel the player extra upwards once airborne).
            /// </summary>
            protected virtual bool InheritYSpeedOnLeaveGround => true;
            /// <summary>
            /// The lerping value for turning towards the input direction.
            /// </summary>
            protected virtual float TurningLerp(Player player) => 1;

            /// <summary>
            /// After falling for a while, should wind sound effects be playing?
            /// </summary>
            protected virtual bool PlayWindFXOnFall(Player player) => true;

            /// <summary>
            /// Value that dampens the speed value horizontally if moving faster than CharacterSpeedSettings(CSS).TopAirSpeed. 
            /// 1 means the speed will be reduced at a rate of (CSS).AirDeceleration until it reaches (CSS).TopAirSpeed. 
            /// 0 means this will never happen.
            /// </summary>
            protected virtual float OverspeedDamp(Player user)
            {
                // Nothing needs to be done here if we are not
                // moving faster than we're allowed in the air
                if (user.SpeedXZ.sqrMagnitude < user._charSpdSettings.TopAirSpeed * user._charSpdSettings.TopAirSpeed)
                {
                    return 1;
                }

                // However, if we are, we will see an extra
                // reduction based on our control stick input.
                float result;
                if (user.GetLeftAnalogInput() == Vector2.zero)
                {
                    result = 1;
                }
                else
                {
                    float lAnalogDotProd = Mathf.Clamp01(ControlStickDirectionDot(user));

                    float dampRate = 1;

                    result = 1 + dampRate * (1 - lAnalogDotProd);
                }

                return result;
            }

            /// <summary>
            /// Multiplies the player's upwards speed when they touch a ceiling.
            /// </summary>
            protected virtual float CeilingHitRicochetMultiplier(Player player) => -0.5f;

            /// <summary>
            /// Returns a value that reduces or increases the horizontal top speed of the character.
            /// </summary>
            protected virtual float AirTopSpeedMultiplier(Player user) => 1;

            /// <summary>
            /// Returns a value that reduces or increases the air acceleration of the character.
            /// </summary>
            protected virtual float AirAccelMultiplier(Player user) => 1;

            /// <summary>
            /// Returns a value that reduces or increases the air deceleration of the character.
            /// </summary>
            protected virtual float AirDecelMultiplier(Player user) => 1;

            /// <summary>
            /// Returns a value that reduces or increases the gravity of the character.
            /// </summary>
            protected virtual float GravityMultiplier(Player user) => 1;

            /// <summary>
            /// Returns a value that reduces or increases the terminal velocity of the character.
            /// </summary>
            protected virtual float TerminalVelocityMultiplier(Player user) => 1;

            /// <summary>
            /// Returns whether or not the player can perform a wall jump in this state.
            /// </summary>
            protected virtual PerformConditions CanWallJump(Player user) => PerformConditions.Cannot;

            protected virtual PerformConditions CanGrabLedge(Player user) => PerformConditions.Cannot;

            public override sealed void OnStateEnter_Exec(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user || !user._charSpdSettings) return;

                user._bufferSystem.AssignInputBuffer(Buttons.X, true, user, OnButtonXPressed);

                user._collisionDetector.OnGroundEnter += OnGroundEnter;
                user._collisionDetector.OnGroundStay += OnGroundStay;
                user._collisionDetector.OnGround = false;

                user._collisionDetector.OnWallEnter += OnWallEnter;
                user._collisionDetector.OnWallStay += OnWallStay;

                user._collisionDetector.OnCeilingEnter += OnCeilingEnter;
                user._collisionDetector.OnCeilingStay += OnCeilingStay;

                if (InheritYSpeedOnLeaveGround)
                {
                    Vector3 delta = (user.DeltaNormal * user.Speed.magnitude);

                    //print($"({delta.x}, {delta.y}, {delta.z})");

                    Vector3 velocity = new Vector3(user.Speed.x, delta.y, user.Speed.z);
                    user.Speed = velocity;
                }

                OnStateEnter_ExecSub(user, previousState, stateEnterArguments);
            }

            protected virtual void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments) { }

            // Rotation is calculated here
            public void OnStateUpdate(Player user, float deltaTime)
            {
                if (!user || !user._charSpdSettings) return;

                Vector3 inputVector = Vector3Extensions.Vec2ToHorizontalVec3(user.GetLeftAnalogInput());
                // Cacheing in case I want to interchange this between
                // magnitude or sqrMagnitude (depending on what the game demands)
                float inputVectorMag = inputVector.sqrMagnitude;

                // cache
                Quaternion gravityRotation = user.GravityRotation;

                // try facing the input on our controller
                Quaternion targetRotation = inputVectorMag != 0 ?
                    Quaternion.LookRotation(inputVector.normalized) :
                    user.GravityRotation;

                float turnRate = 300 * TurningLerp(user);

                gravityRotation = Quaternion.RotateTowards(gravityRotation, targetRotation, turnRate * deltaTime);

                OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);

                user.GravityRotation = gravityRotation;
            }

            protected virtual void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime) 
            { 
                if (PlayWindFXOnFall(user) && user._fallAudio)
                {
                    const float threshold = 17f;
                    if (user.SpeedYF < -threshold)
                    {
                        float volume = Mathf.Clamp01(Mathf.Abs((float)(user.SpeedYF + threshold) / user._charSpdSettings.TerminalVelocity)) * 0.3f;
                        user._fallAudio.volume = volume;
                        
                        if (!user._fallAudio.gameObject.activeSelf)
                        {
                            user._fallAudio.gameObject.SetActive(true);
                            user._fallAudio.Play();
                        }

                    }
                    else if (user._fallAudio.gameObject.activeSelf)
                    {
                        user._fallAudio.gameObject.SetActive(false);
                    }
                }
            }

            // Mostly speed rate changes here
            public void OnStateFixedUpdate(Player user)
            {
                if (!user || !user._charSpdSettings) return;

                float deltaTime = user.MyDeltaTime(true);

                Vector3 speed = user.Speed;

                Vector3 inputVector = Vector3Extensions.Vec2ToHorizontalVec3(user.GetLeftAnalogInput());

                // target direction is the same as the control stick,
                // target speed is airtopspeed.
                Vector3 targetXZSpeed = inputVector *
                    user._charSpdSettings.TopAirSpeed * AirTopSpeedMultiplier(user);// * inputVectorMag;

                // just in case someone on the dev team decides to be funni
                // (tho im the only person on the devteam... but who knows, cant trust myself now can i? · ◡ ·)
                targetXZSpeed.y = 0;

                // set acceleration value for horziontal speed
                float airAccel;

                if (targetXZSpeed.sqrMagnitude == 0 || (user.SpeedXZ.sqrMagnitude > targetXZSpeed.sqrMagnitude && Vector3.Dot(user.SpeedXZ, targetXZSpeed) > 0))
                {
                    // if we move faster than our allowed speed, decel
                    // according to overspeeddamp, otherwise airdeceleration.
                    float decel = user._charSpdSettings.AirDeceleration;
                    //user.XZSpeed.magnitude > user._charSpdSettings.TopAirSpeed * AirTopSpeedMultiplier(user) ?
                    //    OverspeedDamp(user) :
                    //    ;

                    decel *= AirDecelMultiplier(user);

                    airAccel = decel;
                }
                else
                {
                    // if slower than target speed, set to the 
                    // airacceleration specified in the scriptableobject
                    airAccel = user._charSpdSettings.AirAcceleration * AirAccelMultiplier(user);
                }

                airAccel *= OverspeedDamp(user);

                speed = Vector3.MoveTowards(speed, new Vector3(targetXZSpeed.x, speed.y, targetXZSpeed.z), airAccel * OverspeedDamp(user) * deltaTime);
                speed.y -= user._charSpdSettings.Gravity * GravityMultiplier(user) * deltaTime;

                OnStateFixedUpdate_Exec(user, ref speed);

                user.Speed = speed;
            }

            protected virtual void OnStateFixedUpdate_Exec(Player user, ref Vector3 speed) { }

            public override sealed void OnStateExit_Exec(Player user, StateMachine<Player>.IState nextState)
            {
                user._collisionDetector.OnGroundEnter -= OnGroundEnter;
                user._collisionDetector.OnGroundStay -= OnGroundStay;

                user._collisionDetector.OnWallEnter -= OnWallEnter;
                user._collisionDetector.OnWallStay -= OnWallStay;

                user._collisionDetector.OnCeilingEnter -= OnCeilingEnter;
                user._collisionDetector.OnCeilingStay -= OnCeilingStay;

                user._bufferSystem.AssignInputBuffer(Buttons.X, false, user, OnButtonXPressed);

                OnStateExit_ExecSub(user, nextState);
            }

            protected virtual void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState) { }

            protected virtual void OnGroundEnter(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 0) return;

                OnGroundLanded(player, result);
            }

            protected virtual void OnGroundStay(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 4f) return;

                OnGroundLanded(player, result);
            }

            protected virtual void OnWallEnter(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player) return;

                TestWallJump(player, result);

                TestLedgeGrab(player);
            }

            protected virtual void OnWallStay(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player) return;

                TestWallJump(player, result);

                TestLedgeGrab(player);
            }

            private void TestLedgeGrab(Player player)
            {
                if (CanLedgeGrab(player, CanGrabLedge(player), out RaycastHit wallHit, out RaycastHit surfHit) &&
                    Time.time > player.ps_Ledge_Grab_LedgeTimestamp)
                {
                    //if (!player._stateMachine.IsPendingStateChange)
                    player._stateMachine.ChangeState(playerStates[typeof(PS_Ledge_Grab)], wallHit, surfHit);
                }
            }

            private void TestWallJump(Player player, HitResult result)
            {
                //print("normal " + (HasWallJumpedCondition(player) && !player._stateMachine.IsPendingStateChange));
                //print("angle " + (Vector3.Angle(result.normal, player.ps_WallJump_Attach_WallNormal) > player.WallJumpHorizontalArc));
                if (Vector3.Angle(result.normal, player.ps_WallJump_Attach_WallNormal) > player.WallJumpHorizontalArc)
                print("angle angle " + Vector3.Angle(result.normal, player.ps_WallJump_Attach_WallNormal)); //

                // Prevent us from jumping the same wall (or walls with similar angles).
                // Positive infinity indicates we haven't wall jumped since we last left the ground.
                if (HasWallJumpedCondition(player) && !player._stateMachine.IsPendingStateChange ||
                    Vector3.Angle(result.normal, player.ps_WallJump_Attach_WallNormal) > player.WallJumpHorizontalArc)
                {
                    PerformConditions wallJumpableState = CanWallJump(player);

                    if (wallJumpableState != PerformConditions.Cannot &&
                        result.NormalAngle(player.GravitySpaceUp()) >= player.WallJumpSteepnessLimit)
                    {
                        LayerMask lm = player._collisionDetector.CollMask;

                        bool didHitWallM;
                        RaycastHit mHit;
                        if (wallJumpableState == PerformConditions.InFrontAndBehind)
                        {
                            didHitWallM = Physics.Raycast
                            (WallCheckRayMid(player, out float dist, true),
                            out mHit, dist * 1.25f, lm) || Physics.Raycast
                            (WallCheckRayMid(player, out dist, false),
                            out mHit, dist * 1.25f, lm);

                            if (!didHitWallM)
                            {
                                didHitWallM = Physics.Raycast
                                (WallCheckRayBottom(player, out dist, true),
                                out mHit, dist * 1.25f, lm) || Physics.Raycast
                                (WallCheckRayBottom(player, out dist, false),
                                out mHit, dist * 1.25f, lm);
                            }
                        }
                        else if (wallJumpableState == PerformConditions.InFront)
                        {
                            didHitWallM = Physics.Raycast
                            (WallCheckRayMid(player, out float dist, false),
                            out mHit, dist * 1.25f, lm);

                            if (!didHitWallM)
                            {
                                didHitWallM = Physics.Raycast
                                (WallCheckRayBottom(player, out dist, false),
                                out mHit, dist * 1.25f, lm);
                            }
                        }
                        else
                        {
                            didHitWallM = Physics.Raycast
                            (WallCheckRayMid(player, out float dist, true),
                            out mHit, dist * 1.25f, lm);

                            if (!didHitWallM)
                            {
                                didHitWallM = Physics.Raycast
                                (WallCheckRayBottom(player, out dist, true),
                                out mHit, dist * 1.25f, lm);
                            }
                        }

                        didHitWallM = didHitWallM && Vector3.Angle(player.GravitySpaceTRS.GetColumn(1), mHit.normal) <= 100;

                        //print(Vector3.Angle(player.GravitySpaceTRS.GetColumn(1), mHit.normal));

                        Vector3 controlStick = AnalogStickIn3DSpace(player);

                        bool inWallRange = (IsInWallJumpRange(player, result.normal) ||
                            wallJumpableState >= PerformConditions.Behind && IsInWallJumpRange(player, -result.normal)) &&
                            controlStick.sqrMagnitude > 0;

                        if (didHitWallM && inWallRange)
                        {
                            if (player.SpeedYF > 0)
                            {
                                player.SpeedYF *= 0.3f;
                            }

                            player._stateMachine.ChangeState(playerStates[typeof(PS_WallJump_Attach)], result.normal);
                        }
                    }
                }
            }

            private bool HasWallJumpedCondition(Player player)
            {
                const float heightLossOffset = 0.75f;

                if (!player.ps_Airborne_HasWallJumped)
                    return true;

                // We can still wall jump off the same/similarly
                // angled walls if we're below it.
                Vector3 inversePreviousWallJumpPosition = 
                    player.Trans.InverseTransformPoint(player.ps_WallJump_Attach_JumpPosition);

                return inversePreviousWallJumpPosition.y > heightLossOffset;
            }

            protected virtual void OnCeilingEnter(Actor user, HitResult result)
            {
                Player player = (Player)user;

                OnCeiling(player, result);
            }

            protected virtual void OnCeilingStay(Actor user, HitResult result)
            {
                Player player = (Player)user;

                OnCeiling(player, result);
            }

            private void OnCeiling(Player user, HitResult result)
            {
                Player player = (Player)user;

                if (player.SpeedYF > 0)
                {
                    player.ps_Jump_IsHoldingButton = false;

                    player.SpeedYF *= CeilingHitRicochetMultiplier(player);
                }
            }

            /// <summary>
            /// Is called every frame ground is detected. By default, you enter the
            /// PS_IdleMove state playing the landing animation. This can be overridden 
            /// for your own implementation.
            /// </summary>
            /// <param name="user"></param>
            /// <param name="result"></param>
            protected virtual void OnGroundLanded(Player user, HitResult result)
            {
                user.SpeedYF = 0;

                user.ps_Airborne_HasWallJumped = false;

                user._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Landing);
            }

            private bool OnButtonXPressed(Actor actor)
            {
                Player player = (Player)actor;
                if (!player) return false;

                return OnXButtonPressed_Exec(player);
            }
            
            /// <summary>
            /// What's supposed to happen when you press the X-button.
            /// By default, you dash. This can be overridden for your own implementation.
            /// </summary>
            protected virtual bool OnXButtonPressed_Exec(Player player)
            {
                player._stateMachine.ChangeState(playerStates[typeof(PS_DashAirborne)]);

                return true;
            }
        }
    }
}
