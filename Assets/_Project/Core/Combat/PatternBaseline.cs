namespace RT.Core.Combat
{
    /// <summary>
    /// 패턴 하나의 **기획 값**. 보정이 걸리기 전의 원본이다.
    ///
    /// 보스 데이터(ScriptableObject)에서 뽑아 와 여기에 담고,
    /// PatternTuning 과 합쳐 TunedPattern 으로 만든다.
    ///
    /// 원본을 따로 들고 다니는 이유:
    ///   보정은 **항상 기획 값에서 출발해야 한다.**
    ///   이미 보정된 값에 또 곱하면 전투가 길어질수록 값이 표류한다.
    ///
    /// UnityEngine 을 쓰지 않는다.
    /// </summary>
    public readonly struct PatternBaseline
    {
        /// <summary>입력부터 첫 타가 나갈 때까지(초). 0이면 예고 없는 기동(퀵스텝 등)</summary>
        public readonly float Telegraph;

        /// <summary>패턴 전체 길이(초). 선딜 + 연타 구간 + 후딜</summary>
        public readonly float TotalDuration;

        /// <summary>다음에 또 쓸 수 있을 때까지(초). 0이면 쿨 없음(일반 공격)</summary>
        public readonly float Cooldown;

        /// <summary>한 타당 피해</summary>
        public readonly float Damage;

        /// <summary>타수. 1이면 단발</summary>
        public readonly int RepeatCount;

        /// <summary>타와 타 사이(초). 단발이면 의미 없다</summary>
        public readonly float RepeatInterval;

        /// <summary>true 면 DDA 가 건드리지 않는다. 연출용·튜토리얼 패턴 (설계서 7.1)</summary>
        public readonly bool DdaExcluded;

        public PatternBaseline(
            float telegraph,
            float totalDuration,
            float cooldown,
            float damage,
            int repeatCount = 1,
            float repeatInterval = 0f,
            bool ddaExcluded = false)
        {
            Telegraph = telegraph < 0f ? 0f : telegraph;
            TotalDuration = totalDuration < 0.05f ? 0.05f : totalDuration;
            Cooldown = cooldown < 0f ? 0f : cooldown;
            Damage = damage < 0f ? 0f : damage;
            RepeatCount = repeatCount < 1 ? 1 : repeatCount;
            RepeatInterval = repeatInterval < 0f ? 0f : repeatInterval;
            DdaExcluded = ddaExcluded;
        }

        /// <summary>연타가 차지하는 시간. 5타 간격 0.4초면 1.6초다 (첫 타는 선딜에 포함).</summary>
        public float StrikeSpan => (RepeatCount - 1) * RepeatInterval;

        /// <summary>
        /// 후딜. 총 길이에서 선딜과 연타 구간을 뺀 나머지다.
        ///
        /// 이걸 따로 계산해 두면 DDA 가 선딜이나 타수를 바꿔도
        /// **후딜은 그대로 유지**되어 패턴의 성격이 보존된다.
        ///
        /// 검산: 방패 돌진 — 총 2.6 − 선딜 0.5 − 연타 1.6 = 후딜 0.5
        /// 설계서 §5.1 이 적어 둔 "후딜 0.5s" 와 일치한다.
        /// </summary>
        public float Recovery
        {
            get
            {
                float r = TotalDuration - Telegraph - StrikeSpan;
                return r < 0f ? 0f : r;
            }
        }
    }
}
