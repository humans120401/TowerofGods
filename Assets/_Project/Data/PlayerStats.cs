using UnityEngine;

namespace RT.Data
{
    /// <summary>
    /// 플레이어의 모든 수치. 전투방식설계서 v0.4 가 출처다.
    ///
    /// **코드에 매직 넘버를 두지 않는다.** 밸런싱은 전부 이 에셋에서 한다.
    /// DDA가 조정하는 값(보스 패턴)과 고정하는 값(여기)의 경계가 파일 단위로 드러난다.
    ///
    /// 만들기: Project 창 우클릭 ▸ Create ▸ Tower of Gods ▸ 플레이어 스탯
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerStats",
        menuName = "Tower of Gods/플레이어 스탯",
        order = 0)]
    public class PlayerStats : ScriptableObject
    {
        [Header("체력 · 공격력 (설계서 2.1)")]

        [Tooltip("최대 체력. 보스 일반 공격(20) 5대면 사망")]
        [Min(1)] public int maxHealth = 100;

        [Tooltip("기본 공격력. 모든 스킬 계수의 기준이 된다")]
        [Min(1)] public int baseAttackPower = 10;

        [Header("이동 (설계서 2.1)")]

        [Tooltip("지상 이동속도(m/s). 보스방 20m를 약 2.9초에 횡단한다")]
        [Min(0.1f)] public float moveSpeed = 7f;

        [Tooltip("공중 이동속도(m/s). 설계서 기준 지상의 90%")]
        [Min(0.1f)] public float airMoveSpeed = 6.3f;

        [Tooltip("지상에서 목표 속도에 도달하는 시간(초). 0이면 즉시")]
        [Min(0f)] public float groundAccelTime = 0.06f;

        [Tooltip("지상에서 멈추는 데 걸리는 시간(초)")]
        [Min(0f)] public float groundDecelTime = 0.05f;

        [Tooltip("공중에서 방향을 바꾸는 데 걸리는 시간(초)")]
        [Min(0f)] public float airAccelTime = 0.12f;

        [Header("점프 (설계서 2.2)")]

        [Tooltip("점프 최고 높이(m). 보스 지면 판정 2m는 못 넘고 허리 높이는 넘는다")]
        [Min(0.1f)] public float jumpHeight = 1.8f;

        [Tooltip("최고점까지 걸리는 시간(초)")]
        [Min(0.05f)] public float jumpApexTime = 0.3333f;

        [Tooltip("하강할 때 중력 배율. 1이면 설계서 그대로")]
        [Min(1f)] public float fallGravityMultiplier = 1f;

        [Tooltip("낙하 최고 속도(m/s)")]
        [Min(1f)] public float maxFallSpeed = 25f;

        [Tooltip("점프 버튼을 떼면 상승 속도에 곱하는 값 — 가변 점프")]
        [Range(0f, 1f)] public float variableJumpCut = 0.5f;

        [Tooltip("발판을 벗어난 뒤에도 점프를 받아주는 시간(초)")]
        [Min(0f)] public float coyoteTime = 0.1f;

        [Tooltip("착지 직전에 누른 점프를 기억하는 시간(초)")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("대시 (설계서 2.3 — 스택형)")]

        [Min(0.1f)] public float dashDistance = 4f;
        [Min(0.05f)] public float dashDuration = 0.2f;

        [Tooltip("대시 시작부터 무적인 시간(초). 지속(0.2)보다 짧다 — 끝 0.05초는 맞는다")]
        [Min(0f)] public float dashInvincibleTime = 0.15f;

        [Min(1)] public int dashMaxStacks = 2;
        [Min(0.1f)] public float dashRechargeTime = 2.5f;
        [Min(0f)] public float dashMinInterval = 0.3f;

        [Header("기본 공격 — 검 콤보 (설계서 3.1)")]

        [Tooltip("1타 선딜(초)")]
        [Min(0f)] public float attackWindup = 0.15f;

        [Tooltip("1타 총 길이(초). 입력부터 다음 입력 가능까지")]
        [Min(0.05f)] public float attackTotalDuration = 0.35f;

        [Tooltip("타당 계수. 1.0 = 공격력의 100%. 2타 합쳐서 200%")]
        [Min(0f)] public float attackCoefficient = 1.0f;

        [Tooltip("판정 박스 크기(m). 설계서 1.6 × 1.8")]
        public Vector2 attackHitboxSize = new Vector2(1.6f, 1.8f);

        [Tooltip("판정 박스를 몸 중심에서 앞으로 밀어내는 거리(m)")]
        public float attackHitboxOffset = 0.9f;

        [Tooltip("1타 후 이 시간 안에 다시 누르면 2타로 이어진다")]
        [Min(0f)] public float comboWindow = 0.4f;

        [Header("패링 (설계서 4.1)")]

        [Tooltip("누른 뒤 판정이 열리기까지의 지연(초)")]
        [Min(0f)] public float parryInputDelay = 0.05f;

        [Tooltip("판정이 열려 있는 시간(초). 소울류 평균 0.1~0.2 중 관대한 쪽")]
        [Min(0.01f)] public float parryActiveTime = 0.15f;

        [Tooltip("실패했을 때의 후딜(초). 유일한 페널티 — 없으면 계속 누르는 게 최적이 된다")]
        [Min(0f)] public float parryFailRecovery = 0.35f;

        [Header("피격 (설계서 2.1 · 4.2)")]

        [Tooltip("피격 경직(초). 입력 불가. 다단히트 대응을 위해 짧게")]
        [Min(0f)] public float hitStunDuration = 0.15f;

        [Tooltip("피격 후 무적(초). 경직보다 길어 '대응 창' 0.2초가 생긴다")]
        [Min(0f)] public float invincibleAfterHit = 0.35f;

        [Tooltip("넉백 거리(m) — 없음 / 약 / 중 / 강 순서")]
        public float[] knockbackDistances = { 0f, 0.5f, 1f, 2f };

        [Header("궁극기 게이지 (설계서 4.4)")]

        [Min(1)] public int gaugeMax = 100;

        [Tooltip("패링 성공 1회당 획득. 주 수급 수단")]
        [Min(0)] public int gaugeParryGain = 15;

        [Tooltip("보스에게 준 피해 누적 100당 획득")]
        [Min(0)] public int gaugeDamagePer100 = 3;

        [Tooltip("초당 자연 충전량")]
        [Min(0f)] public float gaugeNaturalRate = 0.25f;

        [Tooltip("자연 충전이 시작되는 시점(초). 도망만 다녀도 차는 문제를 막는다")]
        [Min(0f)] public float gaugeNaturalDelay = 30f;

        [Tooltip("궁극기 시전 중 무적(초)")]
        [Min(0f)] public float ultimateInvincibleTime = 0.5f;

        [Header("히트스톱 (손맛)")]

        [Tooltip("약공격이 맞았을 때 멈추는 시간(초)")]
        [Min(0f)] public float hitStopLight = 0.05f;

        [Tooltip("강공격·스킬이 맞았을 때 멈추는 시간(초)")]
        [Min(0f)] public float hitStopHeavy = 0.12f;

        [Tooltip("패링 성공 시 멈추는 시간(초). 가장 길게 줘서 쾌감을 만든다")]
        [Min(0f)] public float hitStopParry = 0.15f;

        // ── 파생값 ──────────────────────────────────────────────
        // 설계서는 '높이 1.8m, 정점 0.33s'로 말한다. 기획자가 다루기 쉬운 단위다.
        // 물리 엔진이 필요한 건 초기 속도와 중력이므로 여기서 환산한다.
        //
        //   h = v₀t/2  →  v₀ = 2h/t        h = gt²/2  →  g = 2h/t²
        //
        // 설계서 값(1.8m, 0.333s)을 넣으면 v₀ = 10.8, g = 32.4가 나온다.

        public float JumpVelocity => 2f * jumpHeight / jumpApexTime;

        public float Gravity => 2f * jumpHeight / (jumpApexTime * jumpApexTime);

        public float GravityScale
        {
            get
            {
                float unityGravity = Mathf.Abs(Physics2D.gravity.y);
                return unityGravity < 0.001f ? 0f : Gravity / unityGravity;
            }
        }

        public float DashSpeed => dashDistance / dashDuration;

        /// <summary>패링 판정이 닫히는 시점 (누른 순간 기준)</summary>
        public float ParryActiveEnd => parryInputDelay + parryActiveTime;

        /// <summary>패링 실패 시 상태가 끝나는 시점</summary>
        public float ParryTotalDuration => ParryActiveEnd + parryFailRecovery;

        /// <summary>넉백 단계(0~3)를 거리(m)로 바꾼다</summary>
        public float KnockbackDistance(int level)
        {
            if (knockbackDistances == null || knockbackDistances.Length == 0) return 0f;
            return knockbackDistances[Mathf.Clamp(level, 0, knockbackDistances.Length - 1)];
        }
    }
}
