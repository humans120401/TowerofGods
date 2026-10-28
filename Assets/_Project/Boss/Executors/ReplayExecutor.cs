using RT.Data;
using UnityEngine;

namespace RT.Boss.Executors
{
    /// <summary>
    /// 이미 쓴 패턴을 그대로 다시 튼다. **카로스 전용이지만 지금 만들어야 한다.**
    ///
    /// 카로스의 환영·환각·잔상은 새 패턴이 아니라
    /// "조금 전에 보스가 한 행동을 지연·복제해서 재생하는 것"이다 (ARCHITECTURE §3.1).
    /// 패턴 실행이 **재생 가능한 데이터**(PatternInvocation)로 설계돼 있지 않으면
    /// 나중에 못 만든다. 그래서 보스가 하나도 없는 지금 자리를 잡아 둔다.
    ///
    /// 동작: 기록에서 다시 틀 만한 가장 최근 패턴을 꺼내,
    /// **원래 실행기에게 그 기록을 그대로 넘긴다.** 여기서는 아무것도 직접 하지 않는다.
    /// 보정(TunedPattern)도 기록에 들어 있으므로 원본이 나갔던 난이도 그대로 재생된다.
    ///
    /// 환영 자신은 기록에서 건너뛴다 — 안 그러면 환영이 환영을 복제해 끝없이 늘어난다.
    /// </summary>
    public class ReplayExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Replay;

        /// <summary>보스 자리에서 얼마나 떨어진 곳에서 재생할까(m). 0이면 제자리</summary>
        private readonly float _offset;

        public ReplayExecutor(float offset = 0f)
        {
            _offset = offset;
        }

        public override void OnBegin(BossContext ctx, in PatternInvocation inv)
        {
            if (TryResolve(ctx, inv, out var source, out var executor))
            {
                executor.OnBegin(ctx, source);
            }
        }

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            if (!TryResolve(ctx, inv, out var source, out var executor))
            {
                // 복제할 게 아직 없다. 전투 초반에는 정상이다.
                return;
            }

            executor.OnStrike(ctx, source, hitIndex);
        }

        public override void OnTick(BossContext ctx, in PatternInvocation inv, float elapsed)
        {
            if (TryResolve(ctx, inv, out var source, out var executor))
            {
                executor.OnTick(ctx, source, elapsed);
            }
        }

        public override void OnEnd(BossContext ctx, in PatternInvocation inv)
        {
            if (TryResolve(ctx, inv, out var source, out var executor))
            {
                executor.OnEnd(ctx, source);
            }
        }

        /// <summary>
        /// 다시 틀 기록과 그 기록을 돌릴 실행기를 찾는다.
        ///
        /// 재생되는 기록은 **원본 그대로가 아니라** 이번 환영의 위치·방향으로 옮긴 사본이다.
        /// 그래서 같은 공격이 다른 자리에서 나간다 — 그게 환영이다.
        /// </summary>
        private bool TryResolve(
            BossContext ctx, in PatternInvocation inv,
            out PatternInvocation source, out PatternExecutor executor)
        {
            executor = null;

            if (!ctx.History.TryGetReplayable(out var recorded))
            {
                source = default;
                return false;
            }

            Vector2 origin = inv.Origin + new Vector2(inv.Facing * _offset, 0f);
            source = recorded.AsReplayFrom(origin, inv.Facing);

            executor = ctx.Executors?.For(recorded.Pattern);
            return executor != null;
        }
    }

    /// <summary>
    /// 피해 없는 기동. 퀵스텝 · 후퇴 · 위치 재조정.
    /// 거리를 벌려 근접 패턴을 못 쓰게 만드는 것이 목적이다.
    /// </summary>
    public class MovementExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Movement;

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            // 판정이 없다. 이동은 OnTick 이 한다.
        }

        public override void OnTick(BossContext ctx, in PatternInvocation inv, float elapsed)
        {
            if (ctx.Body == null) return;

            // 플레이어 반대쪽으로 물러난다. 범위는 에셋의 range 를 거리로 쓴다.
            float duration = Mathf.Max(0.05f, inv.Tuned.TotalDuration);
            float speed = inv.Pattern.range / duration;

            ctx.Body.linearVelocity = new Vector2(
                -ctx.DirectionToTarget * speed, ctx.Body.linearVelocity.y);
        }

        public override void OnEnd(BossContext ctx, in PatternInvocation inv)
        {
            if (ctx.Body != null)
            {
                ctx.Body.linearVelocity = new Vector2(0f, ctx.Body.linearVelocity.y);
            }
        }
    }

    /// <summary>
    /// 자기 강화. 아라쿠스의 분노(속도 1.5배).
    /// 판정이 없고, 거는 효과는 BossContext 의 수치를 바꾸는 것으로 끝난다.
    /// </summary>
    public class BuffExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Buff;

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            Debug.Log($"[{ctx.Data?.displayName}] {inv.Pattern.patternId} 발동", ctx.Transform);
        }

        public override void OnTick(BossContext ctx, in PatternInvocation inv, float elapsed)
        {
            if (ctx.Body != null)
            {
                ctx.Body.linearVelocity = new Vector2(0f, ctx.Body.linearVelocity.y);
            }
        }
    }
}
