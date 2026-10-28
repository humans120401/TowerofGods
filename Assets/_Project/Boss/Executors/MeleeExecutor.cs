using RT.Data;
using UnityEngine;

namespace RT.Boss.Executors
{
    /// <summary>
    /// 근접 판정. 횡베기 · 내려찍기 · 발차기 · 회전베기.
    ///
    /// 판정 높이(hitLow ~ hitHigh)를 그대로 쓰므로 설계서 5.1 의 판정 높이표가
    /// 코드가 아니라 **에셋에서** 점프 회피 가부를 정한다.
    ///   거검 횡베기 0.5~2.0m → 점프로 못 넘는다 → 패링하거나 거리를 벌려야 한다
    ///   방패 내려찍기 0~1.0m → 점프로 넘는다
    ///
    /// 시전 중에는 제자리에 선다. 근접기가 플레이어를 쫓아오면
    /// "거리를 벌려 피한다"는 대응 수단이 사라진다.
    /// </summary>
    public class MeleeExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Melee;

        public override void OnBegin(BossContext ctx, in PatternInvocation inv)
        {
            // 시전 시작할 때 방향을 굳힌다. 선딜 중에 돌아보면 등 뒤로 돌아가는 대응이 죽는다.
            ctx.Body.linearVelocity = new Vector2(0f, ctx.Body.linearVelocity.y);
        }

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            BossAttack.Forward(ctx, inv, hitIndex);
        }

        public override void OnTick(BossContext ctx, in PatternInvocation inv, float elapsed)
        {
            // 근접기는 시전 내내 제자리다.
            if (ctx.Body != null)
            {
                ctx.Body.linearVelocity = new Vector2(0f, ctx.Body.linearVelocity.y);
            }
        }
    }

    /// <summary>
    /// 자기 중심 범위기. 낫 전체회전처럼 앞뒤를 같이 친다.
    ///
    /// Melee 와 나눈 이유: 전방 박스와 원은 "뒤로 돌아가면 피할 수 있는가"가 다르다.
    /// 그게 플레이어의 대응 수단을 바꾸므로 같은 실행기로 묶으면 안 된다.
    /// </summary>
    public class RadialExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Melee;

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            BossAttack.Circle(ctx, inv, hitIndex, inv.Origin, inv.Pattern.range);
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
