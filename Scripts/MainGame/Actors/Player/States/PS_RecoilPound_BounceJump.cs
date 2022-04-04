using SmashysFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        public sealed class PS_RecoilPound_BounceJump : PS_Airborne
        {
            protected override float TurningLerp(Player player) => 0;

            protected override float OverspeedDamp(Player user)
            {
                return base.OverspeedDamp(user);
            }

            protected override PerformConditions CanWallJump(Player user) =>
                user.SpeedYF < 0 ? PerformConditions.InFrontAndBehind : PerformConditions.Cannot;

            protected override PerformConditions CanGrabLedge(Player user) =>
                user.SpeedYF <= 0 ? PerformConditions.InFront : PerformConditions.Cannot;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user.Anim.Play(hN_RecoilPound_BounceJump, 0, 0);
                user.Anim.speed = 1.5f;

                user._axisLagModifier = new Vector3(1, 1f, 1);

                user.Trans.position += user.GravitySpaceUp() * user._collisionDetector.CollisionCollider.contactOffset;

                Vector3 inputVector = Vector3Extensions.Vec2ToHorizontalVec3(user.GetLeftAnalogInput());

                if (ControlStickDirectionDot(user) < 0)
                    inputVector *= 0.5f;

                user.SpeedYF = user._charSpdSettings.BaseJumpSpeed * 1.3f;
                user.ps_Jump_IsHoldingButton = true;
                user.SpeedXZ += inputVector * user._charSpdSettings.TopAirSpeed * 3f;

                user.Trans.position += user.GravitySpaceUp() * 0.1f;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);

                PS_Jump.HoldButtonToJump(user, 1.3f);
            }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                user._axisLagModifier = new Vector3(1, 1, 1);
                user.Anim.speed = 1f;
            }

            protected override void OnGroundEnter(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 4f) return;

                if (player.GetStateTime() < 0.1f)
                    return;

                OnGroundLanded(player, result);
            }

            protected override void OnGroundStay(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 4f) return;

                if (player.GetStateTime() < 0.1f)
                    return;

                OnGroundLanded(player, result);
            }

            protected override void OnWallEnter(Actor user, HitResult result)
            {
                base.OnWallEnter(user, result);

                Player player = (Player)user;
                if (!player) return;

                Vector3 projSpeed = Vector3.ProjectOnPlane(player.SpeedXZ, result.normal);

                player.Speed = new Vector3(projSpeed.x, player.Speed.y, projSpeed.z);
                //Vector3.ProjectOnPlane(player.Speed, result.normal);
            }

            protected override void OnGroundLanded(Player user, HitResult result)
            {
                base.OnGroundLanded(user, result);

                //if (user.ps_Jump_jumpType == PS_Jump.JumpType.Back && ControlStickDirectionDot(user) < 0)
                //{
                //    user._fwdRightSpeed = -user.Trans.InverseTransformVector(user.SpeedXZ);
                //    user.GravityEulerY += 180;
                //}
            }

        }
    }
}