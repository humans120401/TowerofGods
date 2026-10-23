using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// 지상 상태 — 대기와 이동을 함께 다룬다.
    ///
    /// 둘을 나누지 않은 이유: 전환 조건이 "수평 입력이 있나" 하나뿐이라
    /// 상태를 쪼개면 같은 코드가 두 벌이 된다. 애니메이션은 속도를 보고 고르면 된다.
    /// </summary>
    public class GroundedState : PlayerState
    {
        public GroundedState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Grounded;

        public override PlayerStateId? Tick(float deltaTime)
        {
            var input = Ctx.Input;

            Ctx.UpdateFacing(input.Horizontal);

            // 대시 — 지상·공중 공통. 제약(카로스 회피 봉인)이 걸려 있으면 막힌다.
            if (input.DashPressed && !Ctx.DashBlocked && Ctx.Dash.CanDash)
            {
                return PlayerStateId.Dash;
            }

            // 점프 — 버퍼와 코요테를 JumpAssist가 이미 판단해 뒀다.
            if (Ctx.Jump.TryConsumeJump())
            {
                Ctx.SetVerticalVelocity(Ctx.Stats.JumpVelocity);
                return PlayerStateId.Airborne;
            }

            // 발판에서 걸어 나간 경우.
            // 코요테 유예는 JumpAssist가 들고 있으므로 여기서는 상태만 바꾼다.
            if (!Ctx.IsGrounded)
            {
                return PlayerStateId.Airborne;
            }

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            float target = Ctx.Input.Horizontal * Ctx.CurrentMoveSpeed;
            float current = Ctx.Body.linearVelocity.x;

            // 가속과 감속을 따로 둔다.
            // 멈출 때가 더 빨라야 조작이 또렷하게 느껴진다.
            bool stopping = Mathf.Abs(target) < 0.01f;

            float time = stopping
                ? Ctx.Stats.groundDecelTime
                : Ctx.Stats.groundAccelTime;

            float next = time <= 0f
                ? target
                : Mathf.MoveTowards(
                    current,
                    target,
                    (Ctx.Stats.moveSpeed / time) * fixedDeltaTime);

            Ctx.SetHorizontalVelocity(next);
        }
    }
}
