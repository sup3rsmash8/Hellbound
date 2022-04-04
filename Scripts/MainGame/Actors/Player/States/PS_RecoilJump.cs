using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        public static readonly float actionableThreshold = 0.35f;
            
        /// <summary>
        /// when you f a l l i n g
        /// </summary>
        public sealed class PS_RecoilJump : PS_Airborne
        {
            protected override float TurningLerp(Player player)
            {
                AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

                // @TODO: Hash the animation names
                if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime < actionableThreshold)
                {
                    return 0;
                }

                return base.TurningLerp(player);
            }

            protected override float AirAccelMultiplier(Player user)
            {
                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                // @TODO: Hash the animation names
                if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime < actionableThreshold)
                {
                    return base.AirAccelMultiplier(user) * 0.3f;
                }

                return base.AirAccelMultiplier(user);
            }

            protected override PerformConditions CanWallJump(Player user)
            {
                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                // Prevent wall jumping during the beginning of the
                // recoil jump to avoid cancelling it
                if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime < actionableThreshold + 0.1f)
                {
                    return PerformConditions.Cannot;
                }

                return PerformConditions.InFront;
            }

            protected override PerformConditions CanGrabLedge(Player user) =>
                user.SpeedYF <= 0 ? PerformConditions.InFrontAndBehind : PerformConditions.Cannot;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user.Anim.Play(hN_RecoilJump, 0, 0);

                user.OnAnimReachTransitionTime += OnAnimEnd;
            }

            protected override void OnStateUpdate_Exec(Player user, ref Quaternion gravityRotation, float deltaTime)
            {
                base.OnStateUpdate_Exec(user, ref gravityRotation, deltaTime);
            }

            protected override void OnStateFixedUpdate_Exec(Player user, ref Vector3 speed)
            {
                base.OnStateFixedUpdate_Exec(user, ref speed);

                AnimatorStateInfo asi = user.Anim.GetCurrentAnimatorStateInfo(0);

                if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime < actionableThreshold
                    && speed.y < 0)
                {
                    speed.y *= 0.85f;
                }
            }

            protected override void OnStateExit_ExecSub(Player user, StateMachine<Player>.IState nextState)
            {
                base.OnStateExit_ExecSub(user, nextState);

                user.OnAnimReachTransitionTime -= OnAnimEnd;
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
                AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

                // @TODO: Hash the animation names
                if (asi.shortNameHash == hN_RecoilJump && asi.normalizedTime > actionableThreshold && player.SpeedYF <= 7.5f)
                {
                    return base.OnXButtonPressed_Exec(player);
                }

                return false;
            }

            private void OnAnimEnd(Character character, int hashedAnimName, int layer)
            {
                Player player = (Player)character;
                if (!player) return;

                AnimatorStateInfo asi = player.Anim.GetCurrentAnimatorStateInfo(0);

                // @TODO: Hash the animation names
                if (hashedAnimName == hN_RecoilJump)
                {
                    player._stateMachine.ChangeState(playerStates[typeof(PS_Fall)]);
                    player.FrictionModifier = 1;
                }
            }
        }
    }
}
