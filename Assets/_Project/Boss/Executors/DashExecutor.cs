using RT.Data;
using UnityEngine;

namespace RT.Boss.Executors
{
    /// <summary>
    /// 돌진. 방패 돌진(다단히트) · 2페이즈 돌진 기믹.
    ///
    /// 설계서 5.1 이 고친 바로 그 패턴이 여기 걸린다 —
    /// 원본은 8타 간격 0.25s 였는데 **피격 무적 0.35s 보다 짧아 격타가 씹혔다.**
    /// 5타 간격 0.4s 로 바꿔야 전부 적중하고 DDA 의 연타 조정도 실제로 먹는다.
    /// 그 간격은 에셋(BossPatternData.repeatInterval)에 있고,
    /// 여기서는 PatternRunner 가 그 간격마다 OnStrike 를 불러 준다.
    ///
    /// 시전 중 계속 전진하므로 **직선을 벗어나면 피할 수 있다** —
    /// 점프로 넘거나(판정 높이 1.6m), 측면으로 빠지거나.
    /// 돌진 기믹은 충돌 오브젝트로만 저지된다.
    /// </summary>
    public class DashExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Dash;

        public override void OnBegin(BossContext ctx, in PatternInvocation inv)
        {
            // 선딜 동안은 멈춰 서서 예고한다. 바로 튀어나가면 반응할 시간이 없다.
            Stop(ctx);
        }

        public override void OnTick(BossContext ctx, in PatternInvocation inv, float elapsed)
        {
            if (ctx.Body == null) return;

            // 선딜 중에는 제자리.
            if (elapsed < inv.Tuned.Telegraph)
            {
                Stop(ctx);
                return;
            }

            // 연타 구간이 끝나면 후딜 — 멈춰서 틈을 준다.
            float strikeEnd = inv.Tuned.Telegraph
                              + (inv.Tuned.RepeatCount - 1) * inv.Tuned.RepeatInterval;

            if (elapsed > strikeEnd)
            {
                Stop(ctx);
                return;
            }

            float speed = ctx.Data != null ? ctx.Data.dashSpeed : 10f;
            ctx.Body.linearVelocity = new Vector2(inv.Facing * speed, ctx.Body.linearVelocity.y);
        }

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            // 돌진은 몸통이 판정이다. 기록된 출발점이 아니라 **지금 위치**에서 쏜다.
            var p = inv.Pattern;

            float height = Mathf.Max(0.1f, p.hitHigh - p.hitLow);
            float width = Mathf.Max(0.5f, ctx.Data != null ? ctx.Data.height * 0.5f : 1.2f);

            Vector2 center = ctx.Position + new Vector2(
                inv.Facing * width * 0.5f,
                p.hitLow + height * 0.5f - (ctx.Data != null ? ctx.Data.height * 0.5f : 1.2f));

            BossAttack.Box(ctx, inv, hitIndex, center, new Vector2(width, height));
        }

        public override void OnEnd(BossContext ctx, in PatternInvocation inv) => Stop(ctx);

        public override void OnCancel(BossContext ctx, in PatternInvocation inv) => Stop(ctx);

        private static void Stop(BossContext ctx)
        {
            if (ctx.Body == null) return;

            ctx.Body.linearVelocity = new Vector2(0f, ctx.Body.linearVelocity.y);
        }
    }
}
