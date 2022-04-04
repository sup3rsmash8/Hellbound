using System.Collections;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        private float ps_Idle_Move_tiltPreviousEulerY = 0;
        private float ps_Idle_Move_tiltMaxDeltaAngle = 20;
        /// <summary>
        /// The state where the player stands still or moves.
        /// <br>StateEnter arguments:</br>
        /// <br>PS_IdleMove.StateEnterTypes argument[0]: The way the character acts when landing on the ground.</br>
        /// </summary>
        public sealed class PS_IdleMove : PS_Grounded
        {
            public override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (previousState is PS_DashGround)
                {
                    // Update the running animation intensity to match current speed
                    user.Anim.SetFloat(h_MoveScale, user._charSpdSettings.GetTopSpeedRatio(user.SpeedXZ.magnitude));
                }
                else
                {
                    // Update the running animation intensity to match current speed
                    user.Anim.SetFloat(h_MoveScale, user.GetLeftAnalogInput().sqrMagnitude);
                }

                // State enter arguments 
                if (stateEnterArguments?.Length > 0)
                {
                    #region argument[0]
                    StateEnterTypes stateEnterType = (StateEnterTypes)stateEnterArguments[0];
                    switch (stateEnterType)
                    {
                        default:
                        case StateEnterTypes.Neutral:
                            int name = user.Anim.GetFloat(h_MoveScale) <= 0 ?
                                user.GetAnimationWithAttributeName("Idle") :
                                user.GetAnimationWithAttributeName("Move");

                            user.Anim.CrossFadeInFixedTime(name, 0.15f, 0);
                            break;

                        case StateEnterTypes.Landing:
                            user.Anim.Play(user.GetAnimationWithAttributeName("Landing"), 0, 0);
                            break;
                    }
                    #endregion
                }

                // Convert current speed to fwdrightSpeed (which is local)
                user._fwdRightSpeed = user.Trans.InverseTransformVector(user.SpeedXZ);

                if (user.GetLeftAnalogInput().sqrMagnitude <= 0)
                {
                    user.ps_IdleMove_StopMoveOnNeutral = false;
                    user._fwdRightSpeed *= 0.66f;
                }

                // Tilt layer
                user.Anim.SetFloat(h_TiltScale, 0);
                user.Anim.SetLayerWeight(1, 1);
            }

            protected override void OnStateUpdate_Exec(Player user, ref Vector3 transPosition, ref Quaternion gravityRotation, float deltaTime)
            {
                // How fast we are moving compared to our top speed
                // (we are gonna use this for turn speed and animation)
                float topSpdRatio = 
                    user._charSpdSettings.GetTopSpeedRatio(user.SpeedXZ.magnitude);

                Vector2 input = user.GetLeftAnalogInput();

                // turnRatio is made to make turning
                // heavier the faster we move.
                float turnRatio = Mathf.Clamp(1 - topSpdRatio * 0.7f, 0.3f, 1);
                float rotationSpeed = 45;
                float turnSpeed = (rotationSpeed - (rotationSpeed * 0.875f * Mathf.Clamp(topSpdRatio, 0.02f, 1))) * deltaTime;
                //float turnSpeed = (25 * turnRatio) * deltaTime;

                // Adjust our running animation. If we're not overspeeding,
                // it is regulated by the input magnitude.
                float movScaleDest = input.sqrMagnitude <= topSpdRatio && topSpdRatio > 1 ? topSpdRatio : input.sqrMagnitude;

                user.Anim.SetFloat(h_MoveScale, movScaleDest, 0.15f, deltaTime);

                if (input.sqrMagnitude > 0)
                {
                    //print(Quaternion.Lerp(gravityRotation, FaceControlStickDirection(user), turnSpeed * deltaTime).eulerAngles.y);
                    //Quaternion lerp = Quaternion.Lerp(gravityRotation, FaceControlStickDirection(user), turnSpeed * deltaTime);

                    gravityRotation = Quaternion.Lerp(gravityRotation, FaceControlStickDirection(user), turnSpeed);
                    //print("defacto " + b);
                }

                // Stop dash wake if no longer overspeeding ("Naruto-running").
                if (user._dashWakePart && user._dashWakePart.isPlaying && user.Anim.GetFloat(h_MoveScale) <= 1.2f)
                {
                    user._dashWakePart.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                HandleTilt(user);
            }

            protected override void OnStateFixedUpdate_Exec(Player user)
            {
                if (!user || !user._charSpdSettings) return;

                float deltaTime = user.MyDeltaTime(true);

                Vector2 input = user.GetLeftAnalogInput();

                //float topSpdRatio = user._charSpdSettings.GetTopSpeedRatio(user.XZSpeed.magnitude);

                float topSpd = user._charSpdSettings.TopSpeed * input.sqrMagnitude * input.sqrMagnitude;
                float accel = user._fwdRightSpeed.magnitude < topSpd ? 
                    user._charSpdSettings.Acceleration : user._charSpdSettings.Deceleration;
                float friction = user.GetFrictionScale();

                user._fwdRightSpeed.z = Mathf.MoveTowards(user._fwdRightSpeed.z, topSpd, accel * deltaTime);

                Vector3 targetSpd = user.GravityRotation * Vector3.forward * user._fwdRightSpeed.z;

                if (friction == 1)
                    user.Speed = targetSpd;
                else if (user.Speed != targetSpd)
                    user.Speed = Vector3.Lerp(user.Speed, targetSpd, friction);

                const float stopMoveThreshold = 0.7f;
                if ((input.sqrMagnitude < 0.25f || ControlStickDirectionDot(user) < 0) && user.Anim.GetFloat(h_MoveScale) > stopMoveThreshold) // ...the input is almost neutral, and the run animation blend is above the threshold (the stop running animation should only play if you let the control stick snap back)
                {
                    user.ps_IdleMove_StopMoveOnNeutral = false;
                    //user._stateMachine.ChangeState(playerStates[typeof(PS_StopRun)]);
                }
                else if (!user.ps_IdleMove_StopMoveOnNeutral && (user.Anim.GetFloat(h_MoveScale) < input.sqrMagnitude || input.sqrMagnitude > 0.7f))
                {
                    user.ps_IdleMove_StopMoveOnNeutral = true;
                }
            }

            public override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                user.ps_IdleMove_StopMoveOnNeutral = false;
                user.Anim.SetFloat(h_TiltScale, 0);

                user.Anim.SetLayerWeight(1, 0);
            }

            private void HandleTilt(Player player)
            {
                float moveScale = player.Anim.GetFloat(h_MoveScale);

                float tiltTargetWeight;
                if (moveScale < 0.75f)
                {
                    tiltTargetWeight = 0;
                }
                else
                {
                    tiltTargetWeight = 1;
                }

                player.Anim.SetLayerWeight(1, Mathf.Lerp(player.Anim.GetLayerWeight(1), tiltTargetWeight, 10 * Time.deltaTime));

                float currentDelta = 
                    (float)(player.GravityRotation.eulerAngles.y - player.ps_Idle_Move_tiltPreviousEulerY) / 
                    player.ps_Idle_Move_tiltMaxDeltaAngle;

                
                currentDelta = Mathf.Clamp(currentDelta * 10, -1, 1);

                player.Anim.SetFloat(h_TiltScale, currentDelta, 0.1f, Time.deltaTime);

                player.ps_Idle_Move_tiltPreviousEulerY = player.GravityRotation.eulerAngles.y;
            }

            protected override bool OnAButtonPressed_Exec(Player player)
            {
                PS_Jump.JumpType jumpType;
                if (player.DeFactoSpeedXZ.magnitude >= 2)
                {
                    jumpType = PS_Jump.JumpType.Super;
                }
                else
                {
                    jumpType = PS_Jump.JumpType.Regular;
                }

                player._stateMachine.ChangeState(playerStates[typeof(PS_Jump)], jumpType);

                return true;
            }

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                if (player.SpeedXZ.magnitude > player._charSpdSettings.TopSpeed + 1 && player._stateMachine.PreviousState == playerStates[typeof(PS_DashGround)])
                    return false;

                return base.OnXButtonPressed_Exec(player);
            }

            public enum StateEnterTypes
            {
                Neutral, // Just change into the regular stance 
                Landing // Perform the landing animation 
            }


        }

        public class PS_StopRun : PS_Grounded
        {
            public override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                user.Anim.CrossFadeInFixedTime(user.GetAnimationWithAttributeName("Move_StopRun"), 0.08f, 0);
                user.OnAnimReachTransitionTime += OnAnimEnd;
            }

            protected override void OnStateFixedUpdate_Exec(Player user)
            {
                float deltaTime = user.MyDeltaTime(true);

                float sqrSpdThreshold = Mathf.Pow(2, 2);

                if (user.SpeedXZ.sqrMagnitude > 0)
                {
                    float friction = user.GetFrictionScale();
                    float decel = user._charSpdSettings.Deceleration * 0.6f;

                    user.SpeedXZ = Vector3.MoveTowards(user.SpeedXZ, Vector3.zero, decel * friction * deltaTime);
                }

                if (user.SpeedXZ.sqrMagnitude < sqrSpdThreshold)
                {
                    if (ControlStickDirectionDot(user) < -0.85f)
                    {
                        user.GravityEulerY += 120;
                        user.Speed = FaceControlStickDirection(user) * Vector3.forward * user._charSpdSettings.TopAirSpeed;
                    }
                    user._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);

                    //if (ControlStickDirectionDot(user) < 0f)
                    //{

                    //}
                    //else
                    //{
                    //    AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                    //    int animName = user.GetAnimationWithAttributeName("Move_StopRun_Stop");

                    //    if (asi.shortNameHash != animName)
                    //    {
                    //        user.Anim.Play(animName, 0, 0);
                    //    }
                    //}
                }

                // If we're making input on the analog stick, enter our
                // idle/move state once the player's speed according to
                // the analog stick is higher than the player's actual speed.
                Vector3 speedInput = Vector3Extensions.Vec2ToHorizontalVec3(user.GetLeftAnalogInput() * user._charSpdSettings.TopSpeed);
                if (speedInput.sqrMagnitude != 0)
                {
                    if (speedInput.sqrMagnitude > user.SpeedXZ.sqrMagnitude && ControlStickDirectionDot(user) > 0)
                    {
                        
                        user._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);
                    }
                }


            }

            protected override bool OnAButtonPressed_Exec(Player player)
            {
                PS_Jump.JumpType jumpType;
                print(ControlStickDirectionDot(player));

                if (ControlStickDirectionDot(player) < 0)
                {
                    jumpType = PS_Jump.JumpType.Back;
                }
                else if (player.DeFactoSpeedXZ.magnitude >= 2)
                {
                    jumpType = PS_Jump.JumpType.Super;
                }
                else
                {
                    jumpType = PS_Jump.JumpType.Regular;
                }

                player._stateMachine.ChangeState(playerStates[typeof(PS_Jump)], jumpType);

                return true;
            }

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                if (player.SpeedXZ.magnitude > player._charSpdSettings.TopSpeed * 2f && player._stateMachine.PreviousState == playerStates[typeof(PS_DashGround)])
                    return false;

                return base.OnXButtonPressed_Exec(player);
            }

            public override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                base.OnStateExit_ExecSub(user, nextState);
                user.OnAnimReachTransitionTime -= OnAnimEnd;
            }

            protected override void OnAnimEnd(Character character, int hashedAnimName, int layer)
            {
                Player player = (Player)character;
                if (!player) return;

                if (hashedAnimName == player.GetAnimationWithAttributeName("Move_StopRun_Stop"))
                {

                    player._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);
                    player.FrictionModifier = 1;
                }
            }
        }
    }
}
