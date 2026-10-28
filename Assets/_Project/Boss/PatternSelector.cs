using System.Collections.Generic;
using RT.Data;

namespace RT.Boss
{
    /// <summary>
    /// 다음에 쓸 패턴을 고른다.
    ///
    /// 고르는 조건은 셋뿐이다 — **거리 · 쿨타임 · 페이즈**.
    /// 체력이나 플레이어의 실력은 보지 않는다.
    ///
    /// **패턴 선택 확률은 DDA 가 절대 건드리지 않는다** (설계서 7.1 고정 항목).
    /// DDA 가 "이 사람은 돌진에 약하니 돌진을 더 자주 내자"를 할 수 있게 되면
    /// 그건 난이도 조절이 아니라 괴롭히기가 되고, 패턴 실패율이라는 문맥 자체가 오염된다.
    /// DDA 는 쿨타임 배율로만 간접적으로 빈도에 영향을 준다.
    ///
    /// 같은 패턴이 연속으로 나오는 것만 막는다. 세 번 연속 같은 공격이면
    /// 플레이어가 배우는 게 아니라 지겨워진다.
    /// </summary>
    public class PatternSelector
    {
        private readonly List<BossPatternData> _candidates = new List<BossPatternData>();
        private readonly System.Random _random;

        /// <summary>직전에 쓴 패턴. 연속 선택을 막는 데 쓴다</summary>
        private string _lastPatternId;

        /// <summary>같은 패턴이 연속으로 몇 번 나왔나</summary>
        private int _repeatStreak;

        /// <summary>이 횟수만큼 연속되면 다른 걸 고른다</summary>
        private readonly int _maxStreak;

        public PatternSelector(int seed = 0, int maxStreak = 2)
        {
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
            _maxStreak = maxStreak < 1 ? 1 : maxStreak;
        }

        /// <summary>
        /// 지금 쓸 수 있는 패턴 중 하나를 고른다.
        /// </summary>
        /// <returns>쓸 게 없으면 null — 보스는 걸어서 접근한다</returns>
        public BossPatternData Pick(
            BossData boss, float distance, int phase, PatternCooldowns cooldowns)
        {
            if (boss == null || boss.patterns == null) return null;

            _candidates.Clear();

            foreach (var pattern in boss.patterns)
            {
                if (!IsUsable(pattern, distance, phase, cooldowns)) continue;

                // 너무 연속되면 후보에서 뺀다. 단 다른 후보가 아예 없으면 다시 넣는다.
                if (pattern.patternId == _lastPatternId && _repeatStreak >= _maxStreak) continue;

                _candidates.Add(pattern);
            }

            // 연속 제한 때문에 후보가 비었으면 제한을 풀고 다시 모은다.
            if (_candidates.Count == 0)
            {
                foreach (var pattern in boss.patterns)
                {
                    if (IsUsable(pattern, distance, phase, cooldowns)) _candidates.Add(pattern);
                }
            }

            if (_candidates.Count == 0) return null;

            var chosen = _candidates[_random.Next(_candidates.Count)];

            if (chosen.patternId == _lastPatternId) _repeatStreak++;
            else _repeatStreak = 1;

            _lastPatternId = chosen.patternId;
            return chosen;
        }

        private static bool IsUsable(
            BossPatternData pattern, float distance, int phase, PatternCooldowns cooldowns)
        {
            if (pattern == null) return false;
            if (!pattern.UsableInPhase(phase)) return false;
            if (!pattern.InRange(distance)) return false;
            if (cooldowns != null && !cooldowns.IsReady(pattern.patternId)) return false;

            return true;
        }

        /// <summary>층 진입 · 보스 리셋</summary>
        public void Reset()
        {
            _lastPatternId = null;
            _repeatStreak = 0;
        }
    }
}
