using UnityEngine;

namespace RT.Data
{
    /// <summary>
    /// 플레이어의 모든 수치. 전투방식설계서 v0.4 2.1 기본 스탯이 출처다.
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
        [Header("체력 · 공격력")]

        [Tooltip("최대 체력. 보스 일반 공격(20) 5대면 사망")]
        [Min(1)] public int maxHealth = 100;

        [Tooltip("기본 공격력. 모든 스킬 계수의 기준이 된다")]
        [Min(1)] public int baseAttackPower = 10;

        [Header("이동")]

        [Tooltip("지상 이동속도(m/s). 보스방 20m를 약 2.9초에 횡단한다")]
        [Min(0.1f)] public float moveSpeed = 7f;

        [Tooltip("공중 이동속도(m/s). 설계서 기준 지상의 90%")]
        [Min(0.1f)] public float airMoveSpeed = 6.3f;

        [Tooltip("지상에서 목표 속도에 도달하는 시간(초). 0이면 즉시")]
        [Min(0f)] public float groundAccelTime = 0.06f;

        [Tooltip("지상에서 멈추는 데 걸리는 시간(초)")]
        [Min(0f)] public float groundDecelTime = 0.05f;

        [Tooltip("공중에서 방향을 바꾸는 데 걸리는 시간(초). 지상보다 느리게 두면 공중 제어가 묵직해진다")]
        [Min(0f)] public float airAccelTime = 0.12f;

        [Header("점프")]

        [Tooltip("점프 최고 높이(m). 설계서 기준 1.8 — 보스 지면 판정 2m는 못 넘고 허리 높이는 넘는다")]
        [Min(0.1f)] public float jumpHeight = 1.8f;

        [Tooltip("최고점까지 걸리는 시간(초). 설계서 기준 0.333 (착지까지 0.66)")]
        [Min(0.05f)] public float jumpApexTime = 0.3333f;

        [Tooltip("하강할 때 중력 배율. 1이면 설계서 그대로, 올리면 더 빨리 떨어져 경쾌해진다")]
        [Min(1f)] public float fallGravityMultiplier = 1f;

        [Tooltip("낙하 최고 속도(m/s). 너무 빠르면 바닥을 뚫는다")]
        [Min(1f)] public float maxFallSpeed = 25f;

        [Tooltip("점프 버튼을 떼면 상승 속도에 곱하는 값 — 가변 점프")]
        [Range(0f, 1f)] public float variableJumpCut = 0.5f;

        [Header("기동 보조 (전투방식설계서 2.2)")]

        [Tooltip("발판을 벗어난 뒤에도 점프를 받아주는 시간(초)")]
        [Min(0f)] public float coyoteTime = 0.1f;

        [Tooltip("착지 직전에 누른 점프를 기억하는 시간(초)")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("대시 (전투방식설계서 2.3 — 스택형)")]

        [Tooltip("대시 거리(m)")]
        [Min(0.1f)] public float dashDistance = 4f;

        [Tooltip("대시 지속 시간(초)")]
        [Min(0.05f)] public float dashDuration = 0.2f;

        [Tooltip("대시 시작부터 무적인 시간(초). 지속(0.2)보다 짧다 — 끝 0.05초는 맞는다")]
        [Min(0f)] public float dashInvincibleTime = 0.15f;

        [Tooltip("보유 가능한 대시 스택 수")]
        [Min(1)] public int dashMaxStacks = 2;

        [Tooltip("스택 1개가 차는 데 걸리는 시간(초)")]
        [Min(0.1f)] public float dashRechargeTime = 2.5f;

        [Tooltip("연속 대시 사이의 최소 간격(초). 두 스택을 같은 순간에 쏟지 못하게 한다")]
        [Min(0f)] public float dashMinInterval = 0.3f;

        [Header("피격 (2단계에서 사용)")]

        [Tooltip("피격 경직(초). 입력 불가")]
        [Min(0f)] public float hitStunDuration = 0.15f;

        [Tooltip("피격 후 무적(초). 경직보다 길어 '대응 창'이 생긴다")]
        [Min(0f)] public float invincibleAfterHit = 0.35f;

        // ── 파생값 ──────────────────────────────────────────────
        // 설계서는 '높이 1.8m, 정점 0.33s'로 말한다. 기획자가 다루기 쉬운 단위다.
        // 물리 엔진이 필요한 건 초기 속도와 중력이므로 여기서 환산한다.
        //
        //   h = v₀t/2      →  v₀ = 2h/t
        //   h = gt²/2      →  g  = 2h/t²
        //
        // 설계서 값(1.8m, 0.333s)을 넣으면 v₀ = 10.8, g = 32.4가 나온다.

        /// <summary>점프 시작 속도(m/s)</summary>
        public float JumpVelocity => 2f * jumpHeight / jumpApexTime;

        /// <summary>중력 가속도(m/s², 양수)</summary>
        public float Gravity => 2f * jumpHeight / (jumpApexTime * jumpApexTime);

        /// <summary>
        /// Rigidbody2D.gravityScale에 넣을 값.
        /// Unity 기본 중력(-9.81)에 곱해져 위 Gravity가 되도록 환산한다.
        /// </summary>
        public float GravityScale
        {
            get
            {
                float unityGravity = Mathf.Abs(Physics2D.gravity.y);
                return unityGravity < 0.001f ? 0f : Gravity / unityGravity;
            }
        }

        /// <summary>대시 속도(m/s) = 거리 ÷ 지속</summary>
        public float DashSpeed => dashDistance / dashDuration;
    }
}
