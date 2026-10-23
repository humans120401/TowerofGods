using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// 지상 상태 — 대기와 이동을 함께 다룬다.
    ///
    /// 둘을 나누지 않은 이유: 전환 조건이 "수평 입력이 있나" 하나뿐이라
    /// 상태를 쪼개면 같은 코드가 두 벌이 된다. 애니메이션은 속도를 보고 고르면 된다.
    ///
    /// 전환 우선순위가 중요하다.
    ///   대시 > 패링 > 스킬 > 공격 > 점프
    /// 대시와 패링을 앞에 둔 이유는 둘 다 '살아남는' 행동이기 때문이다.
    /// 공격하려다 맞는 것보다 피하려다 공격을 놓치는 쪽이 낫다.
    /// </summary>
    public class GroundedState : PlayerState
    {
        public GroundedState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Grounded;

        public override PlayerStateId? Tick(float deltaTime)
        {
            var input = Ctx.Input;

            Ctx.UpdateFacing(input.Horizontal);

            if (WantsDash) return PlayerStateId.Dash;
            if (WantsParry) return PlayerStateId.Parry;
            if (TryTakeSkillInput()) return PlayerStateId.Skill;
            if (WantsAttack) return PlayerStateId.Attack;

            // 점프 — 버퍼와 코요테는 JumpAssist 가 이미 판단해 뒀다.
            if (Ctx.Jump.TryConsumeJump())
            {
                Ctx.SetVerticalVelocity(Ctx.Stats.JumpVelocity);
                return PlayerStateId.Airborne;
            }

            // 발판에서 걸어 나간 경우. 코요테 유예는 JumpAssist 가 들고 있다.
            if (!Ctx.IsGrounded) return PlayerStateId.Airborne;

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
                    current, target,
                    (Ctx.Stats.moveSpeed / time) * fixedDeltaTime);

            Ctx.SetHorizontalVelocity(next);
        }
    }
}
