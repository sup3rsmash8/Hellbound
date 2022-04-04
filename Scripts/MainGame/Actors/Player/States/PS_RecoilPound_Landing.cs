using SmashysFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        [SerializeField]
        private CameraShake _landingShake;

        public sealed class PS_RecoilPound_Landing : PS_Grounded
        {
            // The amount of time in seconds after landing before
            // you can perform a recoil bounce.
            public static float jumpTime = 0.1f;

            public override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                base.OnStateEnter_ExecSub(user, previousState, stateEnterArguments);

                user.Anim.Play(hN_RecoilPound_Landing, 0, 0);

                user.OnAnimReachTransitionTime += OnAnimEnd;

                user.Speed = Vector3.zero;

                MG_PlayerClient client = MG_PlayerClient.GetClientThatControls(user);
                if (client)
                {
                    Cameras.MG_PlayerCameraSystem cameraSystem = client.GetCamera();
                    if (cameraSystem)
                    {
                        cameraSystem.AddCameraShake(user._landingShake);
                    }
                }
                //MainGame.Actors.Cameras.MG_PlayerCameraSystem.GetCamera(0).;
            }

            public override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                base.OnStateExit_ExecSub(user, nextState);

                user.OnAnimReachTransitionTime -= OnAnimEnd;
            }

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                return player.GetStateTime() > jumpTime + 0.05f && base.OnXButtonPressed_Exec(player);
            }

            protected override bool OnAButtonPressed_Exec(Player player)
            {
                if (player.GetStateTime() < jumpTime || player.GetStateTime() > jumpTime + 0.1f)
                    return false;

                player._stateMachine.ChangeState(playerStates[typeof(PS_RecoilPound_BounceJump)]);

                return true;
            }
        }
    }
}