namespace RT.Core.Combat
{
    /// <summary>
    /// 보정을 적용하고 안전장치까지 통과한 **실제로 실행될 수치**.
    ///
    /// 보스 실행기는 이 구조체만 본다. DDA 가 있든 없든 똑같이 동작한다 —
    /// DDA 가 없으면 PatternTuning.Neutral 이 들어와 기획 값이 그대로 나온다.
    ///
    /// 안전장치 (설계서 7.6 — 적용 순서가 중요하다):
    ///   1) 선딜은 배율을 곱한 뒤에도 **0.4초 밑으로 내려가지 않는다**
    ///   2) 한 방 피해는 **플레이어 최대 체력의 30%** 에서 자른다
    ///   3) 타수는 1 밑으로 내려가지 않고, 2타 이상인 패턴만 줄일 수 있다
    ///   4) ddaExcluded 패턴은 아예 건드리지 않는다
    ///
    /// 1)이 없으면 "보이지도 않는데 맞는" 패턴이 나오고,
    /// 2)가 없으면 운 나쁜 한 방에 즉사해서 학습이 아니라 운이 된다.
    ///
    /// UnityEngine 을 쓰지 않는다.
    /// </summary>
    public readonly struct TunedPattern
    {
        /// <summary>선딜이 아무리 줄어도 이 밑으로는 안 간다 (설계서 7.6)</summary>
        public const float MinTelegraph = 0.4f;

        /// <summary>한 방 피해 상한 — 플레이어 최대 체력 대비 (설계서 7.6)</summary>
        public const float MaxSingleHitRatio = 0.3f;

        public readonly float Telegraph;
        public readonly float TotalDuration;
        public readonly float Cooldown;

        /// <summary>한 타당 피해. 상한이 이미 적용돼 있다</summary>
        public readonly float Damage;

        public readonly int RepeatCount;
        public readonly float RepeatInterval;

        /// <summary>선딜이 하한에 걸려 잘렸는가. 로그·검증용</summary>
        public readonly bool TelegraphClamped;

        /// <summary>피해가 상한에 걸려 잘렸는가. 로그·검증용</summary>
        public readonly bool DamageClamped;

        private TunedPattern(
            float telegraph, float totalDuration, float cooldown,
            float damage, int repeatCount, float repeatInterval,
            bool telegraphClamped, bool damageClamped)
        {
            Telegraph = telegraph;
            TotalDuration = totalDuration;
            Cooldown = cooldown;
            Damage = damage;
            RepeatCount = repeatCount;
            RepeatInterval = repeatInterval;
            TelegraphClamped = telegraphClamped;
            DamageClamped = damageClamped;
        }

        /// <summary>패턴 전체가 주는 피해. 밸런스 검증에 쓴다.</summary>
        public float TotalDamage => Damage * RepeatCount;

        /// <summary>
        /// 기획 값 + 보정 → 실행 값.
        /// </summary>
        /// <param name="playerMaxHealth">한 방 상한(30%)을 재는 기준</param>
        public static TunedPattern Resolve(
            in PatternBaseline baseline,
            in PatternTuning tuning,
            float playerMaxHealth)
        {
            if (baseline.DdaExcluded) return Untouched(baseline);

            // ── 선딜 ──
            // 예고가 없는 기동(퀵스텝)은 0 그대로 둔다. 하한을 씌우면 없던 예고가 생긴다.
            float telegraph = baseline.Telegraph;
            bool telegraphClamped = false;

            if (telegraph > 0f)
            {
                telegraph *= tuning.TelegraphScale;

                if (telegraph < MinTelegraph)
                {
                    telegraph = MinTelegraph;
                    telegraphClamped = true;
                }
            }

            // ── 타수 ──
            // 단발 패턴을 0타로 만들 수는 없고, 늘리는 것도 의미가 없다.
            int repeatCount = baseline.RepeatCount;

            if (baseline.RepeatCount >= 2)
            {
                repeatCount += tuning.RepeatCountDelta;
                if (repeatCount < 1) repeatCount = 1;
            }

            float repeatInterval = baseline.RepeatInterval * tuning.RepeatIntervalScale;

            // ── 피해 ──
            float damage = baseline.Damage * tuning.DamageScale;
            float cap = playerMaxHealth * MaxSingleHitRatio;
            bool damageClamped = playerMaxHealth > 0f && damage > cap;

            if (damageClamped) damage = cap;

            // ── 총 길이 ──
            // 후딜은 기획 값 그대로 두고, 앞쪽(선딜 + 연타 구간)만 다시 쌓는다.
            float total = telegraph + (repeatCount - 1) * repeatInterval + baseline.Recovery;

            return new TunedPattern(
                telegraph,
                total,
                baseline.Cooldown * tuning.CooldownScale,
                damage,
                repeatCount,
                repeatInterval,
                telegraphClamped,
                damageClamped);
        }

        /// <summary>보정 없이 기획 값 그대로.</summary>
        public static TunedPattern Untouched(in PatternBaseline baseline) =>
            new TunedPattern(
                baseline.Telegraph,
                baseline.TotalDuration,
                baseline.Cooldown,
                baseline.Damage,
                baseline.RepeatCount,
                baseline.RepeatInterval,
                false,
                false);

        public override string ToString() =>
            $"선딜 {Telegraph:0.00}s 총 {TotalDuration:0.00}s 쿨 {Cooldown:0.0}s " +
            $"피해 {Damage:0.#}×{RepeatCount}";
    }
}
