using System.Collections.Generic;
using RT.Data;

namespace RT.Player.Systems
{
    /// <summary>
    /// 스킬 Q · W · E 의 쿨타임을 한 곳에서 관리한다.
    ///
    /// 설계서 3.2 — 마나가 아니라 쿨타임제다.
    /// 자원을 늘리지 않고 쿨 길이로 스킬의 성격을 구분한다 (6s / 10s / 15s).
    ///
    /// 캔슬 규칙 (설계서 3.3):
    ///   - 대시로 캔슬해도 쿨타임은 그대로 돌아간다
    ///   - 단 E는 차지 중 피격당하면 쿨타임의 절반만 소모한다 (실수 비용 완화)
    ///
    /// **UnityEngine을 쓰지 않는다.**
    /// </summary>
    public class Cooldowns
    {
        /// <summary>돌릴 슬롯 목록. 매 프레임 새 배열을 만들지 않으려고 한 번만 둔다.</summary>
        private static readonly SkillSlot[] AllSlots =
            { SkillSlot.Q, SkillSlot.W, SkillSlot.E };

        private readonly Dictionary<SkillSlot, float> _remaining =
            new Dictionary<SkillSlot, float>
            {
                { SkillSlot.Q, 0f },
                { SkillSlot.W, 0f },
                { SkillSlot.E, 0f },
            };

        public void Tick(float deltaTime)
        {
            foreach (var slot in AllSlots)
            {
                float v = _remaining[slot] - deltaTime;
                _remaining[slot] = v < 0f ? 0f : v;
            }
        }

        public bool IsReady(SkillSlot slot) => Remaining(slot) <= 0f;

        public float Remaining(SkillSlot slot) =>
            _remaining.TryGetValue(slot, out var v) ? v : 0f;

        /// <summary>0~1. 1이면 다 찼다. UI용</summary>
        public float Progress(SkillSlot slot, float fullCooldown)
        {
            if (fullCooldown <= 0f) return 1f;
            return 1f - (Remaining(slot) / fullCooldown);
        }

        /// <summary>스킬을 썼다. 전체 쿨타임을 건다.</summary>
        public void Start(SkillSlot slot, float cooldown)
        {
            _remaining[slot] = cooldown < 0f ? 0f : cooldown;
        }

        /// <summary>
        /// 시전이 취소됐다. ratio 만큼만 쿨타임을 건다.
        /// E가 차지 중 피격당하면 0.5 로 부른다.
        /// </summary>
        public void StartPartial(SkillSlot slot, float cooldown, float ratio)
        {
            float v = cooldown * ratio;
            _remaining[slot] = v < 0f ? 0f : v;
        }

        /// <summary>층 진입 시 초기화. 설계서 5.2 — 층 진입 조건은 항상 같아야 한다.</summary>
        public void ResetAll()
        {
            foreach (var slot in AllSlots) _remaining[slot] = 0f;
        }
    }
}
