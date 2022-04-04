using System.Collections;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        public class PS_WallJump_Jump : PS_Airborne
        {
            //user.SpeedYF = user._charSpdSettings.BaseJumpSpeed;
            protected override float TurningLerp(Player player) => Mathf.Clamp01(player._stateMachine.GetStateDuration());

            protected override PerformConditions CanWallJump(Player user) => 
                user.SpeedYF < 9.5f ? PerformConditions.InFront : PerformConditions.Cannot;

            protected override PerformConditions CanGrabLedge(Player user) =>
                user.SpeedYF <= 0 ? PerformConditions.InFront : PerformConditions.Cannot;

            protected override float AirAccelMultiplier(Player user) => 
                (user._stateMachine.GetStateDuration() <= 0.2f) ? 0.5f : 1;

            protected override float AirDecelMultiplier(Player user) => 
                (user._stateMachine.GetStateDuration() <= 0.2f) ? 0.5f : 1;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user.SpeedYF = user._charSpdSettings.BaseJumpSpeed;

                user.Anim.Play(hN_WallJump_Jump, 0, 0);

                user.Trans.position += user.GravitySpaceUp() * user._collisionDetector.CollisionCollider.contactOffset;

                user.ps_WallJump_Attach_JumpPosition = user.Trans.position;

                user.ps_Jump_IsHoldingButton = true;

                user._wallJumpJumpPart.gameObject.SetActive(false);
                user._wallJumpJumpPart.gameObject.SetActive(true);
                user._wallJumpJumpPart.Play(true);
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);

                // We gain height by constantly setting our y-speed
                // for a certain amount of time for as long as the button
                // is held.
                if (user.ps_Jump_IsHoldingButton)
                {
                    user.ps_Jump_IsHoldingButton = user._bufferSystem.Pressed(Buttons.A);

                    if (user._stateMachine.GetStateDuration() < PS_Jump.jumpHoldDuration)
                        user.SpeedYF = user._charSpdSettings.BaseJumpSpeed;
                }
                else
                {
                    user.ps_Jump_IsHoldingButton = false;
                }
            }

            protected override void OnStateFixedUpdate_Exec(Player user, ref Vector3 speed) { }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                user._wallJumpJumpPart.Stop(true);
            }

            protected override void OnGroundEnter(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 0) return;

                OnGroundLanded(player, result);
            }

            protected override void OnGroundStay(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 0) return;

                OnGroundLanded(player, result);
            }
        }
    }
}