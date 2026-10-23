using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// 공중 상태 — 상승과 낙하를 함께 다룬다.
    ///
    /// 여기서 하는 일 세 가지:
    ///   1) 가변 점프 — 버튼을 떼면 상승 속도를 깎는다
    ///   2) 하강 중력 — 내려올 때 중력을 더 세게 걸 수 있다 (fallGravityMultiplier)
    ///   3) 낙하 속도 상한 — 너무 빠르면 바닥을 뚫는다
    /// </summary>
    public class AirborneState : PlayerState
    {
        public AirborneState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Airborne;

        public override PlayerStateId? Tick(float deltaTime)
        {
            var input = Ctx.Input;

            Ctx.UpdateFacing(input.Horizontal);

            if (WantsDash) return PlayerStateId.Dash;
            if (WantsParry) return PlayerStateId.Parry;

            // 공중에서도 스킬·공격이 나간다.
            // 설계서는 Q·E를 '지상에서만'으로 두진 않았으므로 일단 허용하고,
            // 플레이해 보고 공중 난사가 문제가 되면 그때 막는다.
            if (TryTakeSkillInput()) return PlayerStateId.Skill;
            if (WantsAttack) return PlayerStateId.Attack;

            // 코요테 유예로 뒤늦게 나가는 점프.
            // 발판을 막 벗어났을 때 여기로 들어오므로, 여전히 점프할 수 있어야 한다.
            if (Ctx.Jump.TryConsumeJump())
            {
                Ctx.SetVerticalVelocity(Ctx.Stats.JumpVelocity);
                return null;
            }

            // 가변 점프 — 떼는 순간 아직 올라가는 중이면 상승 속도를 깎는다.
            if (input.JumpReleased && Ctx.Body.linearVelocity.y > 0f)
            {
                Ctx.SetVerticalVelocity(
                    Ctx.Body.linearVelocity.y * Ctx.Stats.variableJumpCut);
            }

            if (Ctx.IsGrounded)
            {
                return PlayerStateId.Grounded;
            }

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // 수평 — 공중에서는 더 느리게 반응한다. 공중 제어가 지상만큼 날카로우면 가볍게 느껴진다.
            float target = Ctx.Input.Horizontal * Ctx.CurrentMoveSpeed;
            float current = Ctx.Body.linearVelocity.x;

            float next = Ctx.Stats.airAccelTime <= 0f
                ? target
                : Mathf.MoveTowards(
                    current,
                    target,
                    (Ctx.Stats.airMoveSpeed / Ctx.Stats.airAccelTime) * fixedDeltaTime);

            Ctx.SetHorizontalVelocity(next);

            // 하강 중력 — 올라갈 때와 내려올 때를 다르게 줄 수 있다.
            // 설계서 그대로 가려면 fallGravityMultiplier를 1로 둔다.
            float scale = Ctx.Stats.GravityScale;

            if (Ctx.Body.linearVelocity.y < 0f)
            {
                scale *= Ctx.Stats.fallGravityMultiplier;
            }

            Ctx.Body.gravityScale = scale;

            // 낙하 상한 — 터널링(한 프레임에 바닥을 지나쳐 버리는 것) 방지
            if (Ctx.Body.linearVelocity.y < -Ctx.Stats.maxFallSpeed)
            {
                Ctx.SetVerticalVelocity(-Ctx.Stats.maxFallSpeed);
            }
        }

        public override void Exit()
        {
            // 다음 상태가 중력을 건드리지 않아도 되도록 원래대로 돌려놓는다.
            Ctx.Body.gravityScale = Ctx.Stats.GravityScale;
        }
    }
}
