using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// The ground pound state.
        /// </summary>
        public sealed class PS_RecoilPound : PS_Airborne
        {
            public static readonly float multThreshold = 0.55f; // Frame ~21/38

            protected override float TurningLerp(Player player)
            {
                //AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

                //// @TODO: Hash the animation names
                //if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime < actionableThreshold)
                //{
                //    return 0;
                //}

                //return base.TurningLerp(player);
                return 0;
            }

            protected override float AirAccelMultiplier(Player user)
            {
                return 0;
            }

            protected override PerformConditions CanWallJump(Player user)
            {
                return PerformConditions.Cannot;
            }

            protected override PerformConditions CanGrabLedge(Player user) => PerformConditions.Cannot;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user.Anim.Play(hN_RecoilPound, 0, 0);

                //user.OnAnimReachTransitionTime += OnAnimEnd;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);
            }

            protected override void OnStateFixedUpdate_Exec(Player user, ref Vector3 speed)
            {
                base.OnStateFixedUpdate_Exec(user, ref speed);

                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                if (asi.shortNameHash == hN_RecoilPound && asi.normalizedTime < multThreshold)
                {
                    float horMult;
                    if (ControlStickDirectionDot(user) > 0 || AnalogStickIn3DSpace(user).sqrMagnitude == 0)
                        horMult = 0.91f;
                    else
                        horMult = 0.65f;

                    speed.x *= horMult;
                    speed.z *= horMult;

                    speed.y *= speed.y > 0 ? 0.85f : 0.65f;
                }
            }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                base.OnStateExit_ExecSub(user, nextState);

                //user.OnAnimReachTransitionTime -= OnAnimEnd;
            }

            protected override void OnGroundLanded(Player user, HitResult result)
            {
                user.SpeedYF = 0;

                user.ps_Airborne_HasWallJumped = false;

                user._stateMachine.ChangeState(playerStates[typeof(PS_RecoilPound_Landing)]);
            }

            //protected override void OnGroundEnter(Actor user, HitResult result)
            //{
            //    Player player = (Player)user;
            //    if (!player || player.Speed.y > 0) return;

            //    OnGroundLanded(player, result);
            //}

            //protected override void OnGroundStay(Actor user, HitResult result)
            //{
            //    Player player = (Player)user;
            //    if (!player || player.Speed.y > 0) return;

            //    OnGroundLanded(player, result);
            //}

            protected override bool OnXButtonPressed_Exec(Player player)
            {
                //AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

                //// @TODO: Hash the animation names
                //if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime > actionableThreshold && player.SpeedYF <= 7.5f)
                //{
                //    return base.OnXButtonPressed_Exec(player);
                //}

                return false;
            }

            //private void OnAnimEnd(Character character, int hashedAnimName, int layer)
            //{
            //    Player player = (Player)character;
            //    if (!player) return;

            //    AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

            //    // @TODO: Hash the animation names
            //    if (hashedAnimName == hN_RecoilJump)
            //    {
            //        player._stateMachine.ChangeState(playerStates[typeof(PS_Fall)]);
            //        player.FrictionModifier = 1;
            //    }
            //}
        }
    }
}
