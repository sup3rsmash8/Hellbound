using System.Collections;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// The state in which the player sticks to a wall during a wall jump.
        /// <br>argument0: Vector3 wallNormal (The normal of the wall touched with which rotation will be aligned)</br>
        /// </summary>
        public class PS_WallJump_Attach : PS_Airborne
        {

            //public static bool WallJumpAttachConditions(Player player, RaycastHit )
            //{
            //    Vector3 origin = player._collisionDetector.CollisionCollider.transform.position;
            //}

            // The speed the player slides across walls horizontally.
            // Not gravity.
            public static readonly float wallSlideMoveSpeed = 0.2f;

            // The rate at which the conserved impact speed is lost.
            public const float wallTouchSpeedLossRate = 0.1f;

            protected override float TurningLerp(Player player)
            {
                return 0;
            }

            protected override PerformConditions CanGrabLedge(Player user) =>
                user.SpeedYF <= 0 ? PerformConditions.Behind : PerformConditions.Cannot;

            protected override float TerminalVelocityMultiplier(Player user)
            {
                return 0.1f;
            }

            public Vector3 AnalogStickOnWall(Player player)
            {
                return player.GetLeftAnalogInput() * 
                    (1 - Vector3.Dot(AnalogStickIn3DSpace(player), 
                    player.ps_WallJump_Attach_WallNormal));
            }

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                base.OnStateEnter_ExecSub(user, previousState, stateEnterArguments);
                if (!user) return;

                user._collisionDetector.OnWallExit += OnWallExit;

                user.Anim.Play(hN_WallJump_Attach, 0, 0);

                user._bufferSystem.AssignInputBuffer(Buttons.A, true, user, OnButtonAPressed);

                user.ps_WallJump_Attach_WallNormal = (Vector3)stateEnterArguments[0];

                user.GravityEulerY =
                    Quaternion.LookRotation(user.ps_WallJump_Attach_WallNormal).eulerAngles.y;

                user.SpeedXZ = Vector3.ProjectOnPlane(user.SpeedXZ, user.ps_WallJump_Attach_WallNormal);//user.ps_WallJump_Attach_WallNormal * 10;

                SnapToWall(user);

                if (user._bufferSystem.Buffered(Buttons.A))
                {
                    if (previousState is PS_DashAirborne)
                        user.ps_WallJump_Attach_wallTouchSpeed = 20;
                    else
                        user.ps_WallJump_Attach_wallTouchSpeed = user._charSpdSettings.TopAirSpeed * 2f;
                }
                else
                {
                    user.ps_WallJump_Attach_wallTouchSpeed = user._charSpdSettings.TopAirSpeed;
                }

                if (user._wallJumpAttachPart)
                {
                    user._wallJumpAttachPart.gameObject.SetActive(false);
                    user._wallJumpAttachPart.gameObject.SetActive(true);
                    user._wallJumpAttachPart.Play(true);
                }

                if (user._wallSlipSound)
                    user._wallSlipSound.Play();
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);

                //user.GravityEulerY =
                //    Quaternion.LookRotation(user.ps_WallJump_Attach_WallNormal).eulerAngles.y;

                SnapToWall(user);
            }

            protected override void OnStateFixedUpdate_Exec(Player user, ref Vector3 speed)
            {
                Vector3 xzSpeed = new Vector3(speed.x, 0, speed.z);

                Vector3 targetSpeed = AnalogStickOnWall(user) * AirTopSpeedMultiplier(user);
                    //- user.ps_WallJump_Attach_WallNormal * 2f;

                xzSpeed = Vector3.MoveTowards(xzSpeed, targetSpeed, wallSlideMoveSpeed);

                speed.x = xzSpeed.x;
                speed.z = xzSpeed.z;

                //speed -= user.ps_WallJump_Attach_WallNormal * 1f;
            }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                base.OnStateExit_ExecSub(user, nextState);

                user._collisionDetector.OnWallExit -= OnWallExit;

                user._bufferSystem.AssignInputBuffer(Buttons.A, false, user, OnButtonAPressed);

                if (nextState is PS_WallJump_Jump)
                {
                    // When wall-jumping, we need the player to
                    // angle himself in the direction of the
                    // control stick. So first step is to get
                    // the stick coordinates in his local space.
                    Vector3 stickFWD = Quaternion.Inverse(user.GravityRotation) * AnalogStickIn3DSpace(user);

                    float limit = user.WallJumpBounceOffArc;

                    float dotAngle = Vector3.Angle(Vector3.forward, stickFWD) * Mathf.Sign(stickFWD.x);

                    if (stickFWD.magnitude != 0 && ControlStickDirectionDot(user) > -0.7071f)
                    {
                        // Player's direction should always be on one side
                        // of the wall plane, so we need to reverse the
                        // angle if it is past the wall. (180d becomes 0).
                        if (dotAngle > 90)
                        {
                            dotAngle = 180 - dotAngle;
                        }
                        else if (dotAngle < -90)
                        {
                            dotAngle = -180 + dotAngle;
                        }

                        // Oh, and the angle should always stay within the
                        // given arc.
                        user.GravityEulerY += Mathf.Clamp(dotAngle, -limit, limit);
                    }

                    //print("dotAngle unlimited: " + user.GravityEulerY + dotAngle);
                    //print("dotAngle limited: " + user.GravityEulerY + Mathf.Clamp(dotAngle, -limit, limit));

                    if (ControlStickDirectionDot(user) < -0.707f)
                        user.ps_WallJump_Attach_wallTouchSpeed *= 0.87f;

                    user.SpeedXZ = user.GravityForward() * user.ps_WallJump_Attach_wallTouchSpeed;
                }
                else
                {
                    user.SpeedXZ += user.ps_WallJump_Attach_WallNormal;
                }

                if (user._wallJumpAttachPart)
                    user._wallJumpAttachPart.Stop(true);
                
                if (user._wallSlipSound)
                    user._wallSlipSound.Stop();
            }

            private void SnapToWall(Player user)
            {
                const float offset = 0.05f;

                if (TestSnapToWall(user, offset, out RaycastHit hit))
                {
                    if (user._collisionDetector is CharacterCollisionModule ccm)
                    {
                        // When we touch a wall, we want to snap the player
                        // to the hit point. But due to bugs during development,
                        // we do not quite want to touch the wall itself.

                        // First get the hit point in local space.
                        Vector3 invPt = user.Trans.InverseTransformPoint(hit.point);

                        // Then we push ourselves outwards in our forward direction.
                        Vector3 radOffset = Vector3.forward * (ccm.ScaledCapsuleRadius);

                        // Apply in world space
                        user.Trans.position = user.Trans.TransformPoint(new Vector3(invPt.x, 0, invPt.z) + radOffset);
                    }

                }
                else
                {
                    Eject(user);
                }
            }

            private bool TestSnapToWall(Player user, float offset, out RaycastHit hit)
            {
                CharacterCollisionModule ccm = user._collisionDetector as CharacterCollisionModule;

                if (ccm)
                {
                    float distance = ccm.ScaledCapsuleRadius + offset;

                    if (Physics.Raycast(Player.WallCheckRayBottom(user, out _, true), out hit, distance, ccm.CollMask) ||
                        Physics.Raycast(Player.WallCheckRayMid(user, out _, true), out hit, distance, ccm.CollMask))
                    {
                        return true;
                    }
                }

                hit = new RaycastHit();

                return false;
            }

            // Cannot dash in this state.
            protected override bool OnXButtonPressed_Exec(Player player) { return false; }

            private bool OnButtonAPressed(Actor user)
            {
                Player player = (Player)user;
                if (!player || player._stateMachine.IsPendingStateChange)
                    return false;

                player.ps_Airborne_HasWallJumped = true;

                player._stateMachine.ChangeState(playerStates[typeof(PS_WallJump_Jump)]);

                return true;
            }

            protected override void OnGroundLanded(Player user, HitResult result)
            {
                base.OnGroundLanded(user, result);

                user.SpeedXZ = Vector3.zero;
            }

            protected override void OnWallEnter(Actor user, HitResult result)
            {
                base.OnWallEnter(user, result);

                OnWall(user, result);
            }

            protected override void OnWallStay(Actor user, HitResult result)
            {
                base.OnWallStay(user, result);

                OnWall(user, result);
            }

            private void OnWallExit(Actor user)
            {
                //Eject(user as Player);
            }

            private void OnWall(Actor user, HitResult result)
            {
                return;

                #region DeleteThis
                //Player player = (Player)user;

                //CharacterCollisionModule ccm =
                //    player._collisionDetector as CharacterCollisionModule;

                //// Send a raycast behind to see if any wall is there.
                //bool didHitWallM = Physics.Raycast(WallCheckRayMid(player, out float dist, true),
                //    out RaycastHit mHit, dist * 1.25f, player._collisionDetector.CollMask);

                //if (didHitWallM /*&& Vector3.Angle(player.GravitySpaceUp(), mHit.normal) <= 91*/)
                //{
                //    player.ps_WallJump_Attach_WallNormal = mHit.normal;

                //    //Vector3 snapPoint = player.GravitySpaceTRS.inverse.MultiplyPoint3x4(mHit.point);

                //    //snapPoint.y -= ccm.ScaledCapsuleHeight * 0.5f;

                //    //player.Trans.position = player.GravitySpaceTRS.MultiplyPoint3x4(snapPoint) +
                //    //    mHit.normal * ccm.ScaledCapsuleRadius;

                //    player.GravityEulerY =
                //        Quaternion.LookRotation(player.ps_WallJump_Attach_WallNormal).eulerAngles.y;
                //}
                //else
                //{
                //    Eject(player);
                //}
                #endregion
            }

            private void Eject(Player player)
            {
                player._stateMachine.ChangeState(playerStates[typeof(PS_Fall)]);
            }
        }
    }
}