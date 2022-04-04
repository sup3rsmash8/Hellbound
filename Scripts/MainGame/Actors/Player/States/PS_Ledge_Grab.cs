using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// The state where the player grabs a ledge. Arguments are used to
        /// snap the player to the proper ledge position.
        /// <br>argument0: RaycastHit wallHit</br>
        /// <br>argument1: RaycastHit surfaceHit</br>
        /// </summary>
        public class PS_Ledge_Grab : PS_Airborne
        {
            protected override float TurningLerp(Player player) => 0;

            protected override float AirAccelMultiplier(Player user) => 0;
            
            protected override float AirDecelMultiplier(Player user) => 0;

            protected override float GravityMultiplier(Player user) => 0;

            protected override PerformConditions CanGrabLedge(Player user) => PerformConditions.Cannot;

            protected override PerformConditions CanWallJump(Player user) => PerformConditions.Cannot;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                base.OnStateEnter_ExecSub(user, previousState, stateEnterArguments);

                user._bufferSystem.AssignInputBuffer(Buttons.A, true, user, OnButtonAPressed);

                RaycastHit wallHit = (RaycastHit)stateEnterArguments[0];
                RaycastHit surfHit = (RaycastHit)stateEnterArguments[1];

                user.Anim.Play(hN_Ledge_Grab, 0, 0);

                // Not a good practice, but for the sake of GetLedgeGrabPosition,
                // we need to make sure that the collider is in local identity.
                Transform colliderTrans = user._collisionDetector.CollisionCollider.transform;
                colliderTrans.localPosition = Vector3.zero;
                colliderTrans.localScale = Vector3.one;

                user.Trans.position = GetLedgeGrabPosition(user, wallHit, surfHit);
                user.GravityEulerY = Quaternion.LookRotation(-wallHit.normal).eulerAngles.y;
                user.Speed = Vector3.zero;
                user.ps_Ledge_Grab_grabbedTrans = surfHit.collider.transform;
                user.ps_Ledge_Grab_platformMatrix = user.ps_Ledge_Grab_grabbedTrans.transform.localToWorldMatrix;

                user.ps_Airborne_HasWallJumped = false;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);

                if (!LedgeHangOn(user, ref gravityRotation))
                {
                    user._stateMachine.ChangeState(playerStates[typeof(PS_Fall)]);
                }
                else if (user._stateMachine.GetStateDuration() > user._ledgeGrabInactionableTime)
                {
                    // If pushing forward on the control stick, climb up ledge.
                    if (user.GetLeftAnalogInput().sqrMagnitude > 0.2f && ControlStickDirectionDot(user) > 0)
                    {
                        if (!user._stateMachine.IsPendingStateChange)
                        user._stateMachine.ChangeState(playerStates[typeof(PS_Ledge_Getup)], LedgeGetupType.Normal);
                    }
                }
            }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                base.OnStateExit_ExecSub(user, nextState);

                user._bufferSystem.AssignInputBuffer(Buttons.A, false, user, OnButtonAPressed);

                user.ps_Ledge_Grab_LedgeTimestamp = Time.time + user._ledgeGrabWindDownTime;
            }

            protected bool OnButtonAPressed(Actor actor)
            {
                Player player = (Player)actor;
                if (!player) return false;

                //if (player._stateMachine.GetStateDuration() > 0.3f)
                //{
                    player._stateMachine.ChangeState(playerStates[typeof(PS_Ledge_Getup)], LedgeGetupType.Fast);
                    return true;
                //}

                //return false;
            }

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                return false;
            }

            /// <summary>
            /// Updates the player's position such that they remain in tact with the ledge they're
            /// currently grabbing. Returns true if all conditions for remaining on a ledge are met -
            /// that is, if CanLedgeGrab is true, if the player isn't pulling back on the control
            /// stick, and the ledge transform isn't null.
            /// </summary>
            public static bool LedgeHangOn(Player user, ref Quaternion gravityRotation)
            {
                // Let go if we're pulling back on the control stick.
                // Keep hanging on if we're in neutral.
                if (user._stateMachine.GetStateDuration() < user._ledgeGrabInactionableTime || 
                    (user.GetLeftAnalogInput().sqrMagnitude < 0.2f || ControlStickDirectionDot(user) >= 0))
                {
                    if (CanLedgeGrab(user, PerformConditions.InFront, out RaycastHit wallHit, out RaycastHit surfaceHit))
                    {
                        if (surfaceHit.collider != user.ps_Ledge_Grab_grabbedTrans)
                        {
                            user.ps_Ledge_Grab_grabbedTrans = surfaceHit.collider.transform;
                        }

                        if (user.ps_Ledge_Grab_grabbedTrans)
                        {
                            // If non-static, update the position to be relative to
                            // the ledge's, in case the platform is moving.
                            if (!user.ps_Ledge_Grab_grabbedTrans.gameObject.isStatic)
                            {
                                Matrix4x4 invPlatMatrix = user.ps_Ledge_Grab_platformMatrix.inverse;

                                Vector3 gravityRot = gravityRotation.eulerAngles;

                                Vector3 inversePos = invPlatMatrix.MultiplyPoint3x4(user.Trans.position);
                                Quaternion inverseRot = invPlatMatrix.rotation * Quaternion.Euler(gravityRot);

                                user.ps_Ledge_Grab_platformMatrix = user.ps_Ledge_Grab_grabbedTrans.transform.localToWorldMatrix;

                                user.Trans.position = user.ps_Ledge_Grab_platformMatrix.MultiplyPoint3x4(inversePos);
                                gravityRotation = user.ps_Ledge_Grab_platformMatrix.rotation * inverseRot;

                                user.GravityRotation = Quaternion.Euler(gravityRot.x, user.GravityRotation.eulerAngles.y, gravityRot.z);
                            }

                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}