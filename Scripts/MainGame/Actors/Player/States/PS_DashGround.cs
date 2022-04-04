using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        public sealed class PS_DashGround : PS_Grounded
        {
            public static readonly float stickDotThreshold = -1f;
            public static readonly float dashCancelNormalizedTime = 0.25f;

            public override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                user.Anim.Play(hN_Dash, 0, 0);
                user.OnAnimReachTransitionTime += OnAnimEnd;

                user.GravityEulerY = FaceControlStickDirection(user).eulerAngles.y;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Vector3 transPosition, ref Quaternion gravityRotation, float deltaTime)
            {

            }

            protected override void OnStateFixedUpdate_Exec(Player user)
            {
                float deltaTime = user.MyDeltaTime(false);

                Vector3 speed = user.Speed;

                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                float controlStickDot = ControlStickDirectionDot(user);

                float friction = user.GetFrictionScale();
                float accel = user._charSpdSettings.Deceleration;

                speed = Vector3.MoveTowards(speed, Vector3.zero, accel * friction * deltaTime);

                user.Speed = speed;

                if (asi.shortNameHash == hN_Dash && asi.normalizedTime > dashCancelNormalizedTime)
                {
                    user._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);
                }
            }

            public override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                user.OnAnimReachTransitionTime -= OnAnimEnd;
            }

            protected override bool OnAButtonPressed_Exec(Player player)
            {
                const float normTimeThreshold = 0.15f;

                // Make sure that we're actually dashing before we allow a jump to be executed.
                AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);
                if (!(asi.shortNameHash == hN_Dash && asi.normalizedTime >= normTimeThreshold))
                    return false;

                PS_Jump.JumpType jumpType = ControlStickDirectionDot(player) >= 0 ? PS_Jump.JumpType.Super : PS_Jump.JumpType.Back;
                player._stateMachine.ChangeState(playerStates[typeof(PS_Jump)], jumpType);
                return true;
            }

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                return false;
                //const float normTimeThreshold = 0.2f;
                
                //// Make sure that we're actually dashing before we allow a jump to be executed.
                //AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);
                //if (!(asi.shortNameHash == hN_Dash && asi.normalizedTime >= normTimeThreshold))
                //    return false;

                //    return base.OnXButtonPressed_Exec(player);
            }

            protected override void OnGroundExit_Exec(Player player)
            {
                AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

                if (player._bufferSystem.Pressed(Buttons.A))
                {
                    PS_Jump.JumpType jumpType = ControlStickDirectionDot(player) >= 0 ? PS_Jump.JumpType.Super : PS_Jump.JumpType.Back;
                    player._stateMachine.ChangeState(playerStates[typeof(PS_Jump)], jumpType);
                }
                else
                {
                    if (asi.normalizedTime <= dashCancelNormalizedTime)
                    {
                        // No arguments, the incoming state will know that
                        // this is the state that preceeds it (and play the animation correctly).
                        player._stateMachine.ChangeState(playerStates[typeof(PS_DashAirborne)]);
                    }
                    else
                    {
                        base.OnGroundExit_Exec(player);
                    }
                }
            }

            protected override void OnAnimEnd(Character character, int hashedAnimName, int layer)
            {
                Player player = (Player)character;
                if (!player) return;

                if (hashedAnimName == hN_Dash)
                {
                    player._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);
                    player.FrictionModifier = 1;
                }
            }
        }
    }
}