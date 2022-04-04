using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// The state where the player climbs up a ledge.
        /// <br>argument0: LedgeGetupType ledgeGetupType</br>
        /// </summary>
        public class PS_Ledge_Getup : PS_Grounded /*StateMachine<Player>.IUpdateState*//*, StateMachine<Player>.IFixedUpdateState*/
        {
            public override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                LedgeGetupType ledgeGetupType = (LedgeGetupType)stateEnterArguments[0];

                int hashAnimName;
                switch (ledgeGetupType)
                {
                    default:
                    case LedgeGetupType.Normal:
                        hashAnimName = hN_Ledge_Getup_Normal;
                        break;

                    case LedgeGetupType.Fast:
                        hashAnimName = hN_Ledge_Getup_Fast;
                        break;
                }

                user.Anim.Play(hashAnimName, 0, 0);

                user.OnAnimReachTransitionTime += OnAnimReachTransitionTime;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Vector3 transPosition, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref transPosition, ref gravityRotation, deltaTime);                
                
                bool hangOn = PS_Ledge_Grab.LedgeHangOn(user, ref gravityRotation);

                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);
                if (!hangOn || (asi.shortNameHash == hN_Ledge_Getup_Normal || asi.shortNameHash == hN_Ledge_Getup_Fast))
                {
                    if (asi.normalizedTime > 0.6f)
                    {
                        user.SpeedYF = -0.1f;
                    }
                    //user._stateMachine.ChangeState(playerStates[typeof(PS_Fall)]);
                }
            }

            public override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                user.OnAnimReachTransitionTime -= OnAnimReachTransitionTime;
            }

            protected void OnAnimReachTransitionTime(Character character, int hashAnimName, int layer)
            {
                if (character is Player player)
                {
                    if (hashAnimName == hN_Ledge_Getup_Normal || hashAnimName == hN_Ledge_Getup_Fast)
                    {
                        if (player.OnGround)
                        {
                            player._stateMachine.ChangeState(playerStates[typeof(PS_IdleMove)], PS_IdleMove.StateEnterTypes.Neutral);
                        }
                        else
                        {
                            player._stateMachine.ChangeState(playerStates[typeof(PS_Fall)]);
                        }
                        
                    }
                }
            }

            protected override bool OnAButtonPressed_Exec(Player player)
            {
                // Jump only possible when on ground
                return player.OnGround && base.OnAButtonPressed_Exec(player);
            }

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                // Dash only possible when on ground
                return player.OnGround && base.OnXButtonPressed_Exec(player);
            }
        }

        public enum LedgeGetupType
        {
            Normal = 0,
            Fast,
        }
    }
}