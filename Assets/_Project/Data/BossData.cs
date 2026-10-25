using UnityEngine;

namespace RT.Data
{
    /// <summary>
    /// 보스 하나의 수치와 패턴 목록. 설계서 5장
    ///
    /// 보스마다 이 에셋 하나. 패턴은 BossPatternData 에셋들을 참조한다.
    /// 코드에 보스를 하드코딩하지 않으므로, 5종을 만들어도 클래스는 늘지 않는다.
    ///
    /// 만들기: Create ▸ Tower of Gods ▸ 보스
    /// </summary>
    [CreateAssetMenu(fileName = "Boss", menuName = "Tower of Gods/보스", order = 11)]
    public class BossData : ScriptableObject
    {
        [Header("정체")]

        public string bossId = "arakus";
        public string displayName = "아라쿠스";

        [Tooltip("몇 층 보스인가. 1 아라쿠스 / 2 파네스 / 3 아디쿠스 / 4 에텔로스 / 5 카로스")]
        [Min(1)] public int floor = 1;

        [Header("수치")]

        [Tooltip("체력. 설계서 5.0 제안값 2000")]
        [Min(1)] public int maxHealth = 2000;

        [Tooltip("공격력. 패턴의 피해는 이 값 기준으로 적어 둔다 (설계서 5.1 은 20 기준)")]
        [Min(1)] public float attackPower = 20f;

        [Tooltip("평상시 이동속도(m/s)")]
        [Min(0f)] public float moveSpeed = 3f;

        [Tooltip("돌진 속도(m/s). 설계서 5.1 제안값 10")]
        [Min(0f)] public float dashSpeed = 10f;

        [Tooltip("보스 키(m). 판정 높이의 기준")]
        [Min(0.5f)] public float height = 2.4f;

        [Header("페이즈")]

        [Tooltip("이 체력 비율 아래로 내려가면 2페이즈. 아라쿠스는 0.6 (설계서 5.1)")]
        [Range(0f, 1f)] public float phaseTwoThreshold = 0.6f;

        [Tooltip("페이즈가 바뀔 때 굳는 시간(초). 이 동안은 패턴을 쓰지 않는다")]
        [Min(0f)] public float phaseTransitionTime = 1.5f;

        [Header("행동")]

        [Tooltip("패턴과 패턴 사이 기본 간격(초). 0이면 끝나자마자 다음 패턴")]
        [Min(0f)] public float betweenPatterns = 0.8f;

        [Tooltip("쓸 수 있는 패턴이 하나도 없을 때 플레이어 쪽으로 걸어가는가")]
        public bool approachWhenIdle = true;

        [Header("패턴 — 설계서 5장의 표 한 줄이 에셋 하나")]

        public BossPatternData[] patterns = new BossPatternData[0];

        [Header("계승 궁극기 — 처치하면 플레이어가 얻는다 (설계서 2.6)")]

        [Tooltip("비워 두면 계승이 없다")]
        public string inheritedUltimateId = "";

        // ── 파생 ──

        /// <summary>2페이즈로 넘어갈 체력(절대값)</summary>
        public int PhaseTwoHealth => Mathf.RoundToInt(maxHealth * phaseTwoThreshold);

        /// <summary>
        /// 패링 가능한 공격 패턴의 비율. 설계서 4.1 의 목표는 35~40% 다.
        /// 비율이 무너지면 패링이 장식이 되거나 모든 걸 패링으로 넘기게 된다.
        /// </summary>
        public float ParryableRatio
        {
            get
            {
                int attacks = 0, parryable = 0;

                foreach (var p in patterns)
                {
                    if (p == null || !p.IsAttack) continue;

                    attacks++;
                    if (p.parryable) parryable++;
                }

                return attacks == 0 ? 0f : (float)parryable / attacks;
            }
        }

        /// <summary>공격 패턴 수 (기동·버프 제외)</summary>
        public int AttackPatternCount
        {
            get
            {
                int n = 0;

                foreach (var p in patterns)
                {
                    if (p != null && p.IsAttack) n++;
                }

                return n;
            }
        }
    }
}
