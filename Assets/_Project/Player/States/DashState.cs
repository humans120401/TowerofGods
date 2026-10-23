using RT.Player.Systems;
using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// 대시 상태. 전투방식설계서 2.3
    ///
    ///   거리 4m / 지속 0.2s  →  속도 20m/s
    ///   무적은 앞 0.15초만. **끝 0.05초는 맞는다.**
    ///
    /// 그 0.05초가 설계의 핵심이다. 대시로 모든 걸 흘릴 수 있으면
    /// 거리를 읽을 이유가 없어지고, 패링을 쓸 이유도 사라진다.
    /// </summary>
    public class DashState : PlayerState
    {
        private float _elapsed;
        private float _originalGravityScale;

        public DashState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Dash;

        public override void Enter()
        {
            // 스택을 못 쓰면 들어오지 말았어야 한다. 방어적으로 한 번 더 확인한다.
            if (!Ctx.Dash.TryConsume())
            {
                _elapsed = Ctx.Stats.dashDuration;  // 즉시 끝나게
                return;
            }

            _elapsed = 0f;

            // 입력이 있으면 그 방향, 없으면 바라보는 방향.
            float h = Ctx.Input.Horizontal;
            Ctx.DashDirection = Mathf.Abs(h) > 0.01f ? Mathf.Sign(h) : Ctx.Facing;

            // 무적은 대시 길이(0.2s)보다 짧게(0.15s) 건다. 타이머가 저절로 꺼 준다.
            Ctx.Invincible.For(InvincibilitySource.Dash, Ctx.Stats.dashInvincibleTime);

            // 대시 중에는 중력을 끈다. 공중 대시가 아래로 처지지 않게.
            _originalGravityScale = Ctx.Body.gravityScale;
            Ctx.Body.gravityScale = 0f;

            // 대시로 공중에 뜬 경우 코요테를 끊는다.
            // 안 끊으면 대시 직후에 공중 점프가 한 번 더 나온다.
            if (!Ctx.IsGrounded) Ctx.Jump.CancelCoyote();
        }

        public override PlayerStateId? Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            // 무적 해제는 타이머가 알아서 한다 (설계서 2.3 — 끝 0.05초는 맞는다).

            if (_elapsed >= Ctx.Stats.dashDuration)
            {
                return Ctx.IsGrounded
                    ? PlayerStateId.Grounded
                    : PlayerStateId.Airborne;
            }

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // 대시는 등속이다. 수직 속도는 0으로 묶어 직선으로 나간다.
            Ctx.Body.linearVelocity = new Vector2(
                Ctx.DashDirection * Ctx.Stats.DashSpeed,
                0f);
        }

        public override void Exit()
        {
            // **자기 것만** 끈다.
            // 대시 중 회피 불가 공격에 맞아 피격 무적이 켜져 있을 수 있는데,
            // 그걸 같이 끄면 다단히트의 뒷 타를 전부 맞는다.
            Ctx.Invincible.Release(InvincibilitySource.Dash);

            Ctx.Body.gravityScale = _originalGravityScale <= 0f
                ? Ctx.Stats.GravityScale
                : _originalGravityScale;
        }
    }
}
