using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// The state where regular jumps are performed.
        /// <br>StateEnter arguments:</br>
        /// <br>PS_Jump.JumpType argument[0]: The type of jump animation that will be made.</br>
        /// </summary>
        public sealed class PS_Jump : PS_Airborne
        {
            private static readonly IReadOnlyDictionary<JumpType, string> jumpTypeNames = new Dictionary<JumpType, string>
            {
                { JumpType.Regular, "Jump" },
                { JumpType.Super, "Jump_Super" },
                { JumpType.Back, "Jump_Back" }
            };

            /// <summary>
            /// For how long holding the jump button will increase the jump 
            /// height.
            /// </summary>
            public static readonly float jumpHoldDuration = 0.2f;

            protected override float TurningLerp(Player player) => player.ps_Jump_lockTurning ? 0 : 0.35f;

            protected override PerformConditions CanWallJump(Player user) => 
                user.SpeedYF < 9f ? PerformConditions.InFrontAndBehind : PerformConditions.Cannot;

            protected override PerformConditions CanGrabLedge(Player user) =>
                user.SpeedYF <= 0 ? PerformConditions.InFront : PerformConditions.Cannot;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user.Anim.Play(user.GetAnimationWithAttributeName(jumpTypeNames[(JumpType)stateEnterArguments[0]]), 0, 0);

                user.ps_Jump_jumpType = (JumpType)stateEnterArguments[0];

                switch (user.ps_Jump_jumpType)
                {
                    default:
                        user.SpeedYF = user._charSpdSettings.BaseJumpSpeed;
                        // Allows us to gain height while holding the button.
                        user.ps_Jump_IsHoldingButton = true;
                        user.ps_Jump_lockTurning = false;
                        break;

                    //case JumpType.Super:
                    //    user.SpeedYF = user._charSpdSettings.JumpSpeed * 2f;
                    //    user.ps_Jump_IsHoldingButton = false;
                    //    break;

                    case JumpType.Back:
                        user.ps_Jump_lockTurning = true;
                        user.SpeedYF = user._charSpdSettings.BaseJumpSpeed;
                        user.SpeedXZ *= -1.5f;
                        // Allows us to gain height while holding the button.
                        user.ps_Jump_IsHoldingButton = true;
                        break;
                }

                user._axisLagModifier = new Vector3(1, 1f, 1);

                user.Trans.position += user.GravitySpaceUp() * user._collisionDetector.CollisionCollider.contactOffset;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);

                HoldButtonToJump(user, 1);
            }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                user._axisLagModifier = new Vector3(1, 1, 1);
            }

            protected override void OnGroundStay(Actor user, HitResult result)
            {
                Player player = (Player)user;
                if (!player || player.Speed.y > 4f) return;

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
                
                if (user.ps_Jump_jumpType == PS_Jump.JumpType.Back && ControlStickDirectionDot(user) < 0)
                {
                    user._fwdRightSpeed = -user.Trans.InverseTransformVector(user.SpeedXZ); 
                    user.GravityEulerY += 180;
                }
            }

            public static void HoldButtonToJump(Player user, float multiplier)
            {
                // We gain height by constantly setting our y-speed
                // for a certain amount of time for as long as the button
                // is held.
                if (user.ps_Jump_IsHoldingButton)
                {
                    user.ps_Jump_IsHoldingButton = user._bufferSystem.Pressed(Buttons.A);

                    if (user._stateMachine.GetStateDuration() < jumpHoldDuration)
                        user.SpeedYF = user._charSpdSettings.BaseJumpSpeed * multiplier;
                }
                else
                {
                    user.ps_Jump_IsHoldingButton = false;
                }
            }

            public enum JumpType
            {
                Regular,
                Super,
                Back
            }
        }
    }
}
