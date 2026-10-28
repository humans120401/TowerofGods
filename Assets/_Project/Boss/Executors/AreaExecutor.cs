using RT.Data;
using UnityEngine;

namespace RT.Boss.Executors
{
    /// <summary>
    /// 장판 · 기둥 · 레이저 · 안전지대. 에텔로스의 맵 전역 패턴이 주로 쓴다.
    ///
    /// 근접·투사체와 결정적으로 다른 점: **보스에게서 멀리 떨어진 곳에 판정이 생긴다.**
    /// 그래서 "보스에게서 멀어지면 안전하다"는 기본 전략이 통하지 않고,
    /// 플레이어가 맵을 읽어야 한다.
    ///
    /// 겨냥 지점은 시전 시작 시점의 플레이어 위치다. 선딜 동안 움직이면 피해진다 —
    /// 그게 이 패턴의 대응 수단이므로 발사 시점 위치를 쓰면 안 된다.
    ///
    /// 프리팹이 없으면 판정만 한 번 쏜다. 아트가 없는 지금은 그것으로 충분히 시험된다.
    /// </summary>
    public class AreaExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Area;

        /// <summary>겨냥 지점을 기준으로 몇 개를 늘어놓을까. 기둥 패턴은 여러 개</summary>
        private readonly int _count;

        /// <summary>개와 개 사이(m)</summary>
        private readonly float _spacing;

        public AreaExecutor(int count = 1, float spacing = 2.5f)
        {
            _count = count < 1 ? 1 : count;
            _spacing = spacing <= 0f ? 2.5f : spacing;
        }

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            var p = inv.Pattern;

            float height = Mathf.Max(0.1f, p.hitHigh - p.hitLow);
            float width = Mathf.Max(0.5f, p.range);

            for (int i = 0; i < _count; i++)
            {
                // 가운데를 겨냥 지점에 두고 좌우로 펼친다.
                float offset = (i - (_count - 1) * 0.5f) * _spacing;

                Vector2 center = new Vector2(
                    inv.TargetAtStart.x + offset,
                    inv.TargetAtStart.y + p.hitLow + height * 0.5f);

                if (ctx.AreaPrefab != null)
                {
                    Object.Instantiate(ctx.AreaPrefab, center, Quaternion.identity);
                }

                BossAttack.Box(ctx, inv, hitIndex, center, new Vector2(width, height));
            }
        }
    }
}
