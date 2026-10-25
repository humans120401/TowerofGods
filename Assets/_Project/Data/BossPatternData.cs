using RT.Core.Combat;
using UnityEngine;

namespace RT.Data
{
    /// <summary>어느 실행기가 이 패턴을 돌리는가. 설계서(ARCHITECTURE §3.1)</summary>
    public enum PatternKind
    {
        Melee,       // 근접 히트박스 — 횡베기 · 내려찍기 · 발차기
        Projectile,  // 투사체 — 방패 투척 · 탄막 (직선 · 유도 · 부채꼴 · 반사)
        Dash,        // 돌진 — 저지 가능 여부 포함
        Summon,      // 소환물 — 죄수 · 망자 · 분신
        Area,        // 장판 · 기둥 · 레이저 · 안전지대
        Replay,      // 이미 쓴 패턴을 지연·복제해 다시 실행 (카로스)
        Movement,    // 피해 없는 기동 — 퀵스텝
        Buff,        // 자기 강화 — 아라쿠스의 분노
    }

    /// <summary>어느 페이즈에서 쓸 수 있는가</summary>
    public enum PhaseMask
    {
        Both = 0,
        PhaseOneOnly = 1,
        PhaseTwoOnly = 2,
    }

    /// <summary>
    /// 보스 패턴 하나. 설계서 5장의 표 한 줄이 이 에셋 하나다.
    ///
    /// **여기 적힌 값은 전부 '기획 값'이다.** DDA 는 이 에셋을 고치지 않고,
    /// 실행 직전에 PatternTuning 을 곱해 TunedPattern 을 만든다.
    /// 에셋을 직접 바꾸면 보정이 누적돼 전투가 길어질수록 값이 표류한다.
    ///
    /// 만들기: Create ▸ Tower of Gods ▸ 보스 패턴
    /// </summary>
    [CreateAssetMenu(fileName = "Pattern", menuName = "Tower of Gods/보스 패턴", order = 10)]
    public class BossPatternData : ScriptableObject
    {
        [Header("정체")]

        [Tooltip("로그·텔레메트리에 쓰는 이름. DDA 가 패턴을 구분하는 키이기도 하다")]
        public string patternId = "일반공격1_거검횡베기";

        public PatternKind kind = PatternKind.Melee;

        [Tooltip("어느 페이즈에서 쓰는가")]
        public PhaseMask phase = PhaseMask.Both;

        [Header("타임라인 — 기획 값 (설계서 5장)")]

        [Tooltip("선딜(초). 첫 타가 나갈 때까지. 0이면 예고 없는 기동")]
        [Min(0f)] public float telegraph = 1.05f;

        [Tooltip("총 길이(초). 선딜 + 연타 구간 + 후딜")]
        [Min(0.05f)] public float totalDuration = 2.1f;

        [Tooltip("쿨타임(초). 0이면 쿨 없음 — 일반 공격")]
        [Min(0f)] public float cooldown;

        [Tooltip("타수. 1이면 단발")]
        [Min(1)] public int repeatCount = 1;

        [Tooltip("타와 타 사이(초). **피격 무적 0.35초보다 길어야 한다** (설계서 4.2)")]
        [Min(0f)] public float repeatInterval;

        [Header("피해")]

        [Tooltip("한 타당 피해. 보스 공격력 20 · 플레이어 체력 100 기준")]
        [Min(0f)] public float damage = 18f;

        [Tooltip("넉백 — 0 없음 / 1 약 / 2 중 / 3 강. **다단히트 중간 타는 0이어야 한다**")]
        [Range(0, 3)] public int knockbackLevel = 2;

        [Header("대응 수단 — 이 셋이 패턴의 성격을 정한다")]

        [Tooltip("패링으로 막히는가. 설계서 목표는 전체의 35~40%")]
        public bool parryable;

        [Tooltip("회피(대시·무적)를 뚫는가. 패링만 통하는 패턴을 만들 때 쓴다")]
        public bool ignoresDodge;

        [Tooltip("패링 성공 시 보스가 굳는 시간(초)")]
        [Min(0f)] public float parryStaggerTime = 0.6f;

        [Header("판정 — 점프로 넘을 수 있는가를 결정한다")]

        [Tooltip("판정 아래끝 높이(m)")]
        [Min(0f)] public float hitLow;

        [Tooltip("판정 위끝 높이(m). 플레이어 점프가 1.8m 이므로 2.0 이상이면 못 넘는다")]
        [Min(0f)] public float hitHigh = 2f;

        [Tooltip("판정 가로 범위(m). 보스 기준 전방 거리")]
        [Min(0f)] public float range = 1.5f;

        [Header("쓸 수 있는 거리")]

        [Tooltip("이 거리 이상일 때만 쓴다(m)")]
        [Min(0f)] public float minDistance;

        [Tooltip("이 거리 이하일 때만 쓴다(m)")]
        [Min(0f)] public float maxDistance = 1.5f;

        [Header("DDA")]

        [Tooltip("켜면 DDA 가 이 패턴을 건드리지 않는다. 연출·튜토리얼용 (설계서 7.1)")]
        public bool ddaExcluded;

        [Header("연출")]

        [Tooltip("예고 중 보스가 띄울 색. 패링 가능 패턴은 반드시 구분되게")]
        public Color telegraphColor = new Color(1f, 0.9f, 0.3f);

        public string animationState = "";

        // ── 파생 ──

        /// <summary>보정 전 기획 값. 실행기가 PatternTuning 과 합쳐 쓴다.</summary>
        public PatternBaseline Baseline => new PatternBaseline(
            telegraph, totalDuration, cooldown, damage,
            repeatCount, repeatInterval, ddaExcluded);

        /// <summary>플레이어 점프(1.8m)로 통째로 넘을 수 있는가. 설계서 5.1 판정 높이표</summary>
        public bool JumpEvadable => hitHigh < 1.8f;

        /// <summary>피해를 주는 패턴인가. 기동·버프는 아니다.</summary>
        public bool IsAttack =>
            kind != PatternKind.Movement && kind != PatternKind.Buff && damage > 0f;

        public bool UsableInPhase(int phase1Or2) => phase switch
        {
            PhaseMask.PhaseOneOnly => phase1Or2 == 1,
            PhaseMask.PhaseTwoOnly => phase1Or2 == 2,
            _ => true,
        };

        public bool InRange(float distance) =>
            distance >= minDistance && distance <= maxDistance;

        private void OnValidate()
        {
            if (maxDistance < minDistance) maxDistance = minDistance;

            // 다단히트인데 간격이 무적보다 짧으면 격타가 그냥 씹힌다 (설계서 4.2).
            // 아라쿠스 [방패 돌진]과 아디쿠스 [낫 전체회전]이 둘 다 걸렸던 함정이라
            // 에셋을 만들 때 바로 경고한다.
            if (repeatCount >= 2 && repeatInterval > 0f && repeatInterval <= 0.35f)
            {
                Debug.LogWarning(
                    $"[{patternId}] 연타 간격 {repeatInterval:0.00}s 가 피격 무적 0.35s 이하입니다.\n" +
                    "격타가 무적에 씹혀 DDA 의 연타 조정도 통하지 않습니다. 0.4s 이상을 권합니다.",
                    this);
            }
        }
    }
}
