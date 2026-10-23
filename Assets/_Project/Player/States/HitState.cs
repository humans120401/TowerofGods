using RT.Player.Systems;
using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// 피격 경직. 전투방식설계서 2.1 · 4.2
    ///
    ///   경직 0.15s   입력 불가
    ///   무적 0.35s   경직보다 길다 → **대응 창 0.2초**가 생긴다
    ///
    /// 이 0.2초가 다단히트 설계의 핵심이다.
    /// 무적이 남아 있는 동안 패링이나 대시를 입력하면 뒷 타를 끊어낼 수 있다.
    ///
    ///   무적이 길면(0.8s)  12타 패턴이 사실상 1타가 되어 다단히트를 만들 이유가 사라진다
    ///   무적이 없으면      한 번 맞고 12타를 다 맞아 즉사한다
    ///   0.35s 는 "맞아도 대응하면 살아남는" 구간을 만드는 값이다
    ///
    /// 무적 해제는 PlayerBrain 이 타이머로 처리한다. 상태가 끝나도 무적은 남아야 하기 때문이다.
    /// </summary>
    public class HitState : PlayerState
    {
        private float _elapsed;

        public HitState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Hit;

        /// <summary>이미 맞는 중이면 또 끊기지 않는다. 무적이 막아 주기도 한다.</summary>
        public override bool InterruptedByHit => false;

        public override void Enter()
        {
            _elapsed = 0f;

            // 넉백 — 맞은 방향으로 밀려난다.
            float distance = Ctx.Stats.KnockbackDistance(Ctx.LastHitKnockback);

            if (distance > 0f)
            {
                // 경직 시간 동안 그 거리만큼 가도록 속도를 잡는다.
                float stun = Mathf.Max(0.01f, Ctx.Stats.hitStunDuration);
                Ctx.SetHorizontalVelocity(Ctx.LastHitDirection * (distance / stun));
            }
            else
            {
                // 넉백 '없음' 은 다단히트 중간 타격용이다.
                // 밀려나면 나머지 타가 자동으로 빗나가 설계가 깨진다 (설계서 4.2).
                Ctx.SetHorizontalVelocity(0f);
            }
        }

        public override PlayerStateId? Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (Ctx.IsDead) return PlayerStateId.Dead;

            if (_elapsed < Ctx.Stats.hitStunDuration) return null;

            // 경직이 끝나면 바로 움직일 수 있다. 무적은 아직 0.2초 더 남아 있다.
            return Ctx.IsGrounded ? PlayerStateId.Grounded : PlayerStateId.Airborne;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // 경직 중에는 이동 입력을 받지 않는다. 넉백 속도만 유지한다.
        }
    }

    /// <summary>사망. 더 이상 아무것도 하지 않는다.</summary>
    public class DeadState : PlayerState
    {
        public DeadState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Dead;

        public override bool InterruptedByHit => false;

        public override void Enter()
        {
            Ctx.SetHorizontalVelocity(0f);
            Ctx.Invincible.Hold(InvincibilitySource.Dead);   // 시체를 계속 때리지 않게
            Ctx.IsParryActive = false;
        }

        public override PlayerStateId? Tick(float deltaTime) => null;
    }
}
