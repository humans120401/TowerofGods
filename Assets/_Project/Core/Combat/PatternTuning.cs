namespace RT.Core.Combat
{
    /// <summary>
    /// DDA 가 패턴 하나에 거는 보정. 설계서 7.1
    ///
    /// **조정하는 건 이 다섯 개뿐이다.**
    ///   선딜 · 데미지 · 쿨타임 · 연타 수 · 연타 간격
    ///
    /// 절대 건드리지 않는 것 (설계서 7.1):
    ///   패링 신호 0.3s · 패링 판정 창 0.15s · 회피 무적 0.35s
    ///   플레이어 수치 전부 · 보스 체력 · 페이즈 전환 조건 · 패턴 선택 확률
    ///
    /// 왜 전역 난이도가 아니라 패턴 단위인가:
    ///   "이 사람은 돌진에 약하고 횡베기는 잘 피한다"가 실제 데이터다.
    ///   전체를 같이 낮추면 잘 피하던 패턴까지 쉬워져 학습이 사라진다.
    ///
    /// **RT.Core 에 둔 이유**: RT.Boss 와 RT.DDA 가 서로를 참조하면 안 되는데
    /// 둘 다 이 타입이 필요하다. 아무것도 참조하지 않는 Core 에 두면
    /// DDA 를 통째로 빼도 보스가 컴파일된다 → 대조군 빌드가 공짜다.
    ///
    /// UnityEngine 을 쓰지 않는다.
    /// </summary>
    public readonly struct PatternTuning
    {
        // 설계서 7.1 의 범위. 여기를 넘는 값은 생성자에서 잘린다.
        public const float TelegraphMin = 0.80f, TelegraphMax = 1.30f;
        public const float DamageMin = 0.80f, DamageMax = 1.20f;
        public const float CooldownMin = 0.80f, CooldownMax = 1.25f;
        public const int RepeatCountMin = -1, RepeatCountMax = 1;
        public const float RepeatIntervalMin = 0.90f, RepeatIntervalMax = 1.20f;

        /// <summary>선딜 배율. 작을수록 어렵다 (반응할 시간이 준다)</summary>
        public readonly float TelegraphScale;

        /// <summary>데미지 배율</summary>
        public readonly float DamageScale;

        /// <summary>쿨타임 배율. 작을수록 자주 나온다 = 어렵다</summary>
        public readonly float CooldownScale;

        /// <summary>연타 수 증감. 2타 이상인 패턴만 줄일 수 있다</summary>
        public readonly int RepeatCountDelta;

        /// <summary>연타 간격 배율. 작을수록 촘촘하다 = 어렵다</summary>
        public readonly float RepeatIntervalScale;

        /// <summary>보정 없음. 기획 값 그대로 (0단계)</summary>
        public static readonly PatternTuning Neutral = new PatternTuning(1f, 1f, 1f, 0, 1f);

        /// <summary>범위를 벗어난 값은 조용히 자른다. 바깥에서 잘못 넣어도 게임이 깨지지 않게.</summary>
        public PatternTuning(
            float telegraphScale,
            float damageScale,
            float cooldownScale,
            int repeatCountDelta,
            float repeatIntervalScale)
        {
            TelegraphScale = Clamp(telegraphScale, TelegraphMin, TelegraphMax);
            DamageScale = Clamp(damageScale, DamageMin, DamageMax);
            CooldownScale = Clamp(cooldownScale, CooldownMin, CooldownMax);
            RepeatIntervalScale = Clamp(repeatIntervalScale, RepeatIntervalMin, RepeatIntervalMax);

            RepeatCountDelta =
                repeatCountDelta < RepeatCountMin ? RepeatCountMin :
                repeatCountDelta > RepeatCountMax ? RepeatCountMax : repeatCountDelta;
        }

        /// <summary>보정이 전혀 없는가. 로그에서 0단계를 구분할 때 쓴다.</summary>
        public bool IsNeutral =>
            TelegraphScale == 1f && DamageScale == 1f && CooldownScale == 1f &&
            RepeatCountDelta == 0 && RepeatIntervalScale == 1f;

        private static float Clamp(float v, float min, float max) =>
            v < min ? min : v > max ? max : v;

        public override string ToString() =>
            $"선딜×{TelegraphScale:0.00} 피해×{DamageScale:0.00} 쿨×{CooldownScale:0.00} " +
            $"타수{RepeatCountDelta:+0;-0;+0} 간격×{RepeatIntervalScale:0.00}";
    }
}
