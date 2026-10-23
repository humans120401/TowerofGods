using UnityEngine;

namespace RT.Data
{
    /// <summary>어느 키에 묶인 스킬인가</summary>
    public enum SkillSlot { Q, W, E }

    /// <summary>판정 모양</summary>
    public enum HitboxShape { Box, Circle }

    /// <summary>
    /// 스킬 하나의 수치. 전투방식설계서 3.2
    ///
    /// 설계서가 정한 세 스킬은 역할이 겹치지 않는다 — 접근 · 광역 · 한 방.
    ///
    ///   Q 돌진 베기  쿨 6s   180%  전방 3m 돌진 후 베기   거리 좁히기
    ///   W 회전 베기  쿨 10s  120%  자기 중심 반경 2.5m    광역 · 포위 탈출
    ///   E 처형 베기  쿨 15s  300%  전방 2.5m, 차지 0.7s   그로기 구간 전용
    ///
    /// 만들기: Create ▸ Tower of Gods ▸ 스킬
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillData",
        menuName = "Tower of Gods/스킬",
        order = 1)]
    public class SkillData : ScriptableObject
    {
        [Header("정체")]

        public SkillSlot slot = SkillSlot.Q;

        [Tooltip("로그·UI에 쓰는 이름")]
        public string displayName = "돌진 베기";

        [Header("비용")]

        [Tooltip("쿨타임(초). 마나가 아니라 쿨로만 관리한다 (설계서 3.2)")]
        [Min(0f)] public float cooldown = 6f;

        [Header("타임라인")]

        [Tooltip("선딜(초). 입력부터 판정이 나갈 때까지")]
        [Min(0f)] public float windup = 0.2f;

        [Tooltip("총 길이(초). 입력부터 다음 입력이 가능할 때까지")]
        [Min(0.05f)] public float totalDuration = 0.7f;

        [Header("판정")]

        [Tooltip("계수. 1.8 = 공격력의 180%")]
        [Min(0f)] public float damageCoefficient = 1.8f;

        public HitboxShape shape = HitboxShape.Box;

        [Tooltip("Box일 때 크기(m)")]
        public Vector2 boxSize = new Vector2(3f, 1.8f);

        [Tooltip("Circle일 때 반경(m)")]
        [Min(0.1f)] public float radius = 2.5f;

        [Tooltip("판정을 몸 중심에서 앞으로 밀어내는 거리(m). Circle이면 0이 보통")]
        public float forwardOffset = 1.5f;

        [Tooltip("넉백 세기 — 0 없음 / 1 약 / 2 중 / 3 강")]
        [Range(0, 3)] public int knockbackLevel = 0;

        [Header("이동")]

        [Tooltip("시전과 함께 앞으로 나아가는 거리(m). Q는 3, 나머지는 0")]
        [Min(0f)] public float dashDistance = 3f;

        [Tooltip("시전 중 이동 입력을 받는가. E(차지)는 false")]
        public bool canMoveWhileCasting = false;

        [Tooltip("시전 중 무적인가. Q는 '이동 중 무적 없음'이 설계 의도다")]
        public bool invincibleWhileCasting = false;

        [Header("캔슬")]

        [Tooltip("피격당하면 취소되는가. E(처형 베기)만 true")]
        public bool cancelledOnHit = false;

        [Tooltip("피격 취소 시 쿨타임을 이 비율만 소모한다. E는 0.5 — 실수 비용을 줄인다")]
        [Range(0f, 1f)] public float cancelCooldownRatio = 1f;

        [Header("연출")]

        public string animationState = "";
        public GameObject vfxPrefab;

        /// <summary>히트스톱 세기를 계수로 가늠한다. 큰 기술일수록 길게 멈춘다.</summary>
        public bool IsHeavy => damageCoefficient >= 2f;
    }
}
