using RT.Core.Combat;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>
    /// 보스 판정을 한 번 쏘고 결과를 돌려준다. 실행기들이 공유하는 도구다.
    ///
    /// 플레이어 쪽 AttackResolver 와 같은 방식 — 콜라이더를 켰다 껐다 하지 않고
    /// **판정 프레임에 한 번만 겹침 검사**를 한다. 타이밍 버그가 생길 자리가 없다.
    ///
    /// **HitInfo 를 만드는 곳이 여기 하나뿐이라는 게 중요하다.**
    /// 패링 가능 · 회피 불가 플래그가 패턴 에셋에서 그대로 실려 나가므로,
    /// 실행기가 그 두 값을 빠뜨릴 방법이 없다. 팀 프로젝트가 무너진 지점이다.
    /// </summary>
    public static class BossAttack
    {
        private static readonly Collider2D[] Buffer = new Collider2D[8];

        /// <summary>
        /// 박스 판정. 판정 높이(hitLow~hitHigh)를 그대로 써서
        /// 점프로 넘을 수 있는 패턴과 없는 패턴이 실제로 갈린다.
        /// </summary>
        /// <returns>맞은 결과. 아무도 못 맞혔으면 Ignored</returns>
        public static HitResult Box(
            BossContext ctx, in PatternInvocation inv, int hitIndex,
            Vector2 center, Vector2 size)
        {
            int count = Physics2D.OverlapBoxNonAlloc(
                center, size, 0f, Buffer, ctx.PlayerLayers);

            return Deliver(ctx, inv, hitIndex, count);
        }

        /// <summary>원 판정. 자기 중심 범위기에 쓴다.</summary>
        public static HitResult Circle(
            BossContext ctx, in PatternInvocation inv, int hitIndex,
            Vector2 center, float radius)
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                center, radius, Buffer, ctx.PlayerLayers);

            return Deliver(ctx, inv, hitIndex, count);
        }

        /// <summary>
        /// 패턴의 범위·판정 높이를 그대로 써서 보스 전방에 박스를 쏜다.
        /// 근접 패턴 대부분이 이걸 쓴다.
        /// </summary>
        public static HitResult Forward(
            BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            var p = inv.Pattern;

            float height = Mathf.Max(0.1f, p.hitHigh - p.hitLow);
            float width = Mathf.Max(0.1f, p.range);

            // 판정은 보스 발밑 기준 높이로 잡는다. hitLow 0 이면 바닥에 붙는다.
            Vector2 center = inv.Origin + new Vector2(
                inv.Facing * (width * 0.5f),
                p.hitLow + height * 0.5f - ctx.Data.height * 0.5f);

            return Box(ctx, inv, hitIndex, center, new Vector2(width, height));
        }

        /// <summary>
        /// 겹친 대상에게 HitInfo 를 넘긴다.
        ///
        /// 넉백은 **다단히트 중간 타에서 0** 이어야 한다 (설계서 4.2).
        /// 밀려나면 나머지 타가 자동으로 빗나가 다단히트 설계가 통째로 무너진다.
        /// 마지막 타에서만 패턴이 정한 넉백을 준다.
        /// </summary>
        private static HitResult Deliver(
            BossContext ctx, in PatternInvocation inv, int hitIndex, int count)
        {
            var p = inv.Pattern;
            var result = HitResult.Ignored;

            bool isLastHit = hitIndex >= inv.Tuned.RepeatCount - 1;

            var knockback = isLastHit
                ? (KnockbackLevel)Mathf.Clamp(p.knockbackLevel, 0, 3)
                : KnockbackLevel.None;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;

                var receiver = col.GetComponentInParent<IAttackReceiver>();
                if (receiver == null) continue;

                float dir = col.transform.position.x < inv.Origin.x ? -1f : 1f;

                var info = new HitInfo(
                    patternId: p.patternId,
                    hitIndex: hitIndex,
                    damage: inv.Tuned.Damage,
                    direction: dir,
                    knockback: knockback,
                    parryable: p.parryable,
                    ignoresDodge: p.ignoresDodge,
                    parryStaggerTime: p.parryStaggerTime);

                var one = receiver.ReceiveHit(in info);

                // 패링이 가장 중요한 결과다. 하나라도 패링당하면 보스가 굳는다.
                if (one == HitResult.Parried)
                {
                    result = HitResult.Parried;
                    ctx.Parried?.Invoke(info.ParryStaggerTime);
                }
                else if (one == HitResult.Hit && result != HitResult.Parried) result = HitResult.Hit;
            }

            return result;
        }
    }
}
