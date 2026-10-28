using System.Collections.Generic;

namespace RT.Boss
{
    /// <summary>
    /// 보스 패턴의 쿨타임. 패턴 이름으로 관리한다.
    ///
    /// 플레이어 쪽 Cooldowns 와 달리 슬롯이 고정돼 있지 않다 —
    /// 보스마다 패턴 수가 다르고(아라쿠스 11개, 에텔로스 20개+) 이름도 다르다.
    ///
    /// **쿨타임은 DDA 가 조정하는 다섯 값 중 하나다** (cooldownScale ×0.80~×1.25).
    /// 짧아지면 센 패턴이 자주 나와 어려워진다. 그래서 거는 값은
    /// 기획 값이 아니라 보정을 통과한 TunedPattern.Cooldown 이다.
    ///
    /// UnityEngine 을 쓰지 않는다 — 시간은 바깥에서 넣어 준다.
    /// </summary>
    public class PatternCooldowns
    {
        private readonly Dictionary<string, float> _remaining = new Dictionary<string, float>();

        /// <summary>Tick 중에 딕셔너리를 바꿀 수 없어 키를 담아 두는 임시 목록</summary>
        private readonly List<string> _keys = new List<string>();

        public void Tick(float deltaTime)
        {
            if (_remaining.Count == 0) return;

            _keys.Clear();
            _keys.AddRange(_remaining.Keys);

            foreach (var key in _keys)
            {
                float v = _remaining[key] - deltaTime;

                if (v <= 0f) _remaining.Remove(key);
                else _remaining[key] = v;
            }
        }

        /// <summary>지금 쓸 수 있는가. 쿨이 아예 없는 패턴은 항상 true</summary>
        public bool IsReady(string patternId) => Remaining(patternId) <= 0f;

        public float Remaining(string patternId) =>
            patternId != null && _remaining.TryGetValue(patternId, out var v) ? v : 0f;

        /// <summary>썼다. 쿨이 0 이하면 아무것도 걸지 않는다 (일반 공격)</summary>
        public void Start(string patternId, float cooldown)
        {
            if (string.IsNullOrEmpty(patternId)) return;

            if (cooldown <= 0f)
            {
                _remaining.Remove(patternId);
                return;
            }

            _remaining[patternId] = cooldown;
        }

        /// <summary>층 진입·보스 리셋. 설계서 5.2 — 진입 조건은 항상 같아야 한다</summary>
        public void ResetAll() => _remaining.Clear();
    }
}
