using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// The dashing state in mid-air. Entering this state out of PS_DashGround will automatically preserve
        /// the normalizedTime from the previous animation (they're supposed to sync).
        /// </summary>
        public sealed class PS_DashAirborne : PS_Airborne
        {
            public static readonly float dashCancelAirNormalizedTime = 0.125f;

            // Heeeeeavy
            protected override float AirAccelMultiplier(Player user) => 0.075f;

            protected override PerformConditions CanWallJump(Player user) => PerformConditions.InFront;

            protected override PerformConditions CanGrabLedge(Player user) => PerformConditions.InFront;

            protected override float OverspeedDamp(Player user)
            {
                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                // While actually dashing, we don't want to
                // cancel our momentum.
                if (asi.shortNameHash == hN_DashAir && asi.normalizedTime <= dashCancelAirNormalizedTime)
                {
                    return 1;
                }
                
                return base.OverspeedDamp(user);
            }

            protected override float GravityMultiplier(Player user)
            {
                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                // No downwards momentum while dashing ;)
                if (asi.shortNameHash == hN_DashAir && asi.normalizedTime <= dashCancelAirNormalizedTime)
                {
                    return 0;
                }

                return base.GravityMultiplier(user);
            }

            protected override float TurningLerp(Player player) => 0.05f;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                base.OnStateEnter_ExecSub(user, previousState, stateEnterArguments);

                // Set the normalized time of the air dash animation.
                // If we were already dashing when entering this state,
                // that animation's time will be passed to this animation.
                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);
                float normalizedTime;
                if (previousState is PS_DashGround && asi.shortNameHash == hN_Dash)
                {
                    normalizedTime = asi.normalizedTime;
                }
                else
                {
                    normalizedTime = 0;

                    user.SpeedXZ = Vector3.zero;

                    user.GravityEulerY = FaceControlStickDirection(user).eulerAngles.y;
                }

                user.SpeedYF = 0;

                user.Anim.Play(hN_DashAir, 0, normalizedTime);
            }

            protected override void OnStateFixedUpdate_Exec(Player user, ref Vector3 speed)
            {
                base.OnStateFixedUpdate_Exec(user, ref speed);

                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                // The animator can be a funni boi sometimes, so we're just
                // making sure that what should play here plays here.
                if (asi.shortNameHash == hN_DashAir && asi.normalizedTime > dashCancelAirNormalizedTime)
                {
                    // Airbrake our dash if holding RT
                    if (user._bufferSystem.Pressed(Buttons.RT))
                    {
                        DashCancel(user, ref speed);
                    }
                }
            }

            protected override void OnGroundLanded(Player user, HitResult result)
            {
                //0,0666667
                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                // We hardcode dashing speed if we land on the ground
                // before dashing so we don't come to a complete stop.
                if (asi.shortNameHash == hN_DashAir && asi.normalizedTime <= 0.1f)
                {
                    user.SpeedXZ = user.GravityForward() * 20f;
                }

                base.OnGroundLanded(user, result);
            }

            private void DashCancel(Player user, ref Vector3 speed)
            {
                if (!user) return;

                const float brakingMult = 0.9f;

                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);
                if (asi.normalizedTime >= PS_DashGround.dashCancelNormalizedTime 
                    /*&& user.SpeedXZ.sqrMagnitude > 0.1f*/)
                {
                    speed.x *= brakingMult;
                    speed.z *= brakingMult;
                }
            }

            // Empty because we're not supposed to be able to initiate
            // a new dash in this state
            protected override bool OnXButtonPressed_Exec(Player player)
            {
                return false;
            }
        }
    }
}