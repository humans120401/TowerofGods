using RT.Data;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>
    /// 보스 데이터가 설계서의 규칙을 지키는지 본다. 전투 시작할 때 한 번 돈다.
    ///
    /// 에셋의 OnValidate 는 **인스펙터에서 건드릴 때만** 돌아서,
    /// 코드로 만든 에셋이나 불러온 에셋은 그냥 통과한다. 그래서 여기서 다시 본다.
    ///
    /// 경고만 하고 막지는 않는다. 만들다 만 보스로도 일단 돌려 봐야 하기 때문이다.
    /// </summary>
    public static class BossDataValidator
    {
        /// <summary>피격 무적(설계서 4.2). 연타 간격이 이보다 짧으면 격타가 씹힌다</summary>
        private const float InvincibleAfterHit = 0.35f;

        /// <summary>패링 가능 비율 목표 (설계서 4.1)</summary>
        private const float ParryRatioMin = 0.30f;
        private const float ParryRatioMax = 0.50f;

        public static void Check(BossData data, Object context = null)
        {
            if (data == null) return;

            CheckIntervals(data, context);
            CheckParryRatio(data, context);
        }

        /// <summary>
        /// 다단히트 간격이 피격 무적보다 짧으면 격타가 하나씩 씹힌다.
        ///
        /// 아라쿠스 [방패 돌진] 0.25s, 아디쿠스 [낫 전체회전] — 둘 다 걸렸던 함정이다.
        /// 더 나쁜 건 **DDA 의 연타 조정이 통째로 무의미해진다**는 것이다.
        /// repeatIntervalScale 최대(×1.20)로도 0.3s 라 여전히 무적 안이다.
        /// </summary>
        private static void CheckIntervals(BossData data, Object context)
        {
            if (data.patterns == null) return;

            foreach (var p in data.patterns)
            {
                if (p == null) continue;
                if (p.repeatCount < 2 || p.repeatInterval <= 0f) continue;
                if (p.repeatInterval > InvincibleAfterHit) continue;

                Debug.LogWarning(
                    $"[{data.displayName}] '{p.patternId}' 연타 간격 " +
                    $"{p.repeatInterval:0.00}s 가 피격 무적 {InvincibleAfterHit:0.00}s 이하입니다.\n" +
                    "격타가 무적에 씹히고 DDA 연타 조정도 통하지 않습니다. 0.4s 이상을 권합니다.",
                    context);
            }
        }

        /// <summary>
        /// 패링 가능 비율. 설계서 4.1 의 목표는 35~40% 다.
        ///
        /// 너무 낮으면 패링을 배울 기회가 없고(아라쿠스 초안이 12.5%였다),
        /// 너무 높으면 패링 하나로 전투가 끝나 다른 대응 수단이 죽는다.
        /// </summary>
        private static void CheckParryRatio(BossData data, Object context)
        {
            int attacks = data.AttackPatternCount;
            if (attacks == 0) return;

            float ratio = data.ParryableRatio;

            if (ratio >= ParryRatioMin && ratio <= ParryRatioMax) return;

            Debug.LogWarning(
                $"[{data.displayName}] 패링 가능 비율 {ratio * 100f:0.#}% " +
                $"(공격 패턴 {attacks}개). 설계서 4.1 목표는 35~40% 입니다.\n" +
                (ratio < ParryRatioMin
                    ? "너무 낮으면 플레이어가 패링을 배울 기회가 없습니다."
                    : "너무 높으면 패링 하나로 전투가 끝나 다른 대응 수단이 죽습니다."),
                context);
        }
    }
}
