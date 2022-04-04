using UnityEngine;
using SmashysFramework;

namespace MainGame.Actors.Player
{
    public partial class Player
    {
        /// <summary>
        /// when you f a l l i n g
        /// </summary>
        public sealed class PS_Fall : PS_Airborne
        {
            protected override float TurningLerp(Player player) => 1;

            protected override PerformConditions CanWallJump(Player user) => PerformConditions.InFront;

            protected override PerformConditions CanGrabLedge(Player user) =>
                user.SpeedYF <= 0 ? PerformConditions.InFrontAndBehind : PerformConditions.Cannot;

            protected override void OnStateEnter_ExecSub(Player user, StateMachine<Player>.IState previousState, params object[] stateEnterArguments)
            {
                if (!user) return;

                user.Anim.CrossFadeInFixedTime(user.GetAnimationWithAttributeName("Fall"), 0.2f, 0);
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
