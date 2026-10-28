using RT.Data;
using UnityEngine;

namespace RT.Boss.Executors
{
    /// <summary>
    /// 소환. 아디쿠스의 죄수·망자, 파네스의 분신.
    ///
    /// 소환물은 보스가 아니라 **독립된 오브젝트**다. 보스가 다음 패턴으로 넘어가도 남는다.
    /// 그래서 소환 패턴의 난이도는 "한 번에 몇 마리"가 아니라
    /// "얼마나 자주 소환되는가"(쿨타임)로 조절된다 — DDA 의 cooldownScale 이 먹는 자리다.
    ///
    /// 소환 위치는 플레이어 주변이 아니라 **보스 주변**이다.
    /// 플레이어 발밑에 바로 띄우면 반응할 시간이 없다.
    /// </summary>
    public class SummonExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Summon;

        /// <summary>한 번에 몇 마리</summary>
        private readonly int _count;

        /// <summary>보스로부터 떨어뜨릴 거리(m)</summary>
        private readonly float _spacing;

        public SummonExecutor(int count = 2, float spacing = 2f)
        {
            _count = count < 1 ? 1 : count;
            _spacing = spacing <= 0f ? 2f : spacing;
        }

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            if (ctx.SummonPrefab == null)
            {
                Debug.LogWarning(
                    $"[{inv.Pattern.patternId}] 소환 프리팹이 없어 소환하지 못했습니다. " +
                    "Boss Brain ▸ Summon Prefab 을 채워 주세요.", ctx.Transform);
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                // 보스 양옆으로 번갈아 둔다. 한쪽에 몰면 그쪽만 피하면 끝난다.
                float side = (i % 2 == 0) ? 1f : -1f;
                float step = (i / 2 + 1) * _spacing;

                Vector2 at = ctx.Position + new Vector2(side * step, 0f);

                Object.Instantiate(ctx.SummonPrefab, at, Quaternion.identity);
            }
        }
    }
}
