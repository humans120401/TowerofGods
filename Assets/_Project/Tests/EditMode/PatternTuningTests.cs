using NUnit.Framework;
using RT.Core.Combat;

namespace RT.Tests
{
    /// <summary>
    /// DDA 가 패턴에 거는 보정과 안전장치. 설계서 7.1 · 7.6
    ///
    /// 설계서가 직접 적어 둔 검증 표를 그대로 테스트로 옮겼다.
    /// 나중에 수치를 만질 때 "설계서랑 어긋났다"를 바로 알 수 있다.
    ///
    /// 전부 UnityEngine 없이 돈다 — DDA 학습 코어와 같은 원칙이다.
    /// </summary>
    public class PatternTuningTests
    {
        // 설계서 7.2 — 팔(보정 단계) 표
        private static PatternTuning Arm(int step) => step switch
        {
            -2 => new PatternTuning(1.30f, 0.80f, 1.25f, -1, 1.20f),
            -1 => new PatternTuning(1.15f, 0.90f, 1.10f, 0, 1.10f),
            +1 => new PatternTuning(0.90f, 1.10f, 0.90f, 0, 0.95f),
            +2 => new PatternTuning(0.80f, 1.20f, 0.80f, +1, 0.90f),
            _ => PatternTuning.Neutral,
        };

        [Test]
        public void 범위를_벗어난_보정은_잘린다()
        {
            var t = new PatternTuning(99f, -5f, 99f, 7, 0f);

            Assert.AreEqual(PatternTuning.TelegraphMax, t.TelegraphScale, 0.0001f);
            Assert.AreEqual(PatternTuning.DamageMin, t.DamageScale, 0.0001f);
            Assert.AreEqual(PatternTuning.CooldownMax, t.CooldownScale, 0.0001f);
            Assert.AreEqual(PatternTuning.RepeatCountMax, t.RepeatCountDelta);
            Assert.AreEqual(PatternTuning.RepeatIntervalMin, t.RepeatIntervalScale, 0.0001f);
        }

        [Test]
        public void 보정이_없으면_기획_값_그대로다()
        {
            // 일반 공격 1 (거검 횡베기) — 설계서 5.1
            var baseline = new PatternBaseline(
                telegraph: 1.05f, totalDuration: 2.1f, cooldown: 0f, damage: 18f);

            var tuned = TunedPattern.Resolve(baseline, PatternTuning.Neutral, 100f);

            Assert.AreEqual(1.05f, tuned.Telegraph, 0.0001f);
            Assert.AreEqual(2.1f, tuned.TotalDuration, 0.0001f);
            Assert.AreEqual(18f, tuned.Damage, 0.0001f);
        }

        // ── 안전장치 ① 선딜 하한 0.4초 ──

        [Test]
        public void 방패_돌진_선딜은_가장_어려운_단계에서_정확히_하한이다()
        {
            // 설계서 5.1 DDA 검증 ① — "0.50s × 0.80 = 0.40s, 정확히 경계"
            var baseline = new PatternBaseline(
                telegraph: 0.5f, totalDuration: 2.6f, cooldown: 14f,
                damage: 7f, repeatCount: 5, repeatInterval: 0.4f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(0.4f, tuned.Telegraph, 0.0001f);
            Assert.IsFalse(
                tuned.TelegraphClamped,
                "경계에 '걸린' 게 아니라 '닿은' 것이다. 여기서 선딜을 더 줄이면 잘린다");
        }

        [Test]
        public void 선딜이_하한_밑으로_가면_잘린다()
        {
            var baseline = new PatternBaseline(
                telegraph: 0.45f, totalDuration: 1.5f, cooldown: 5f, damage: 10f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(TunedPattern.MinTelegraph, tuned.Telegraph, 0.0001f);
            Assert.IsTrue(tuned.TelegraphClamped);
        }

        [Test]
        public void 예고가_없는_기동에는_하한을_씌우지_않는다()
        {
            // 퀵스텝 — 선딜 없음. 하한을 씌우면 없던 예고가 생겨 버린다.
            var baseline = new PatternBaseline(
                telegraph: 0f, totalDuration: 0.4f, cooldown: 6f, damage: 0f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(0f, tuned.Telegraph, 0.0001f);
        }

        // ── 안전장치 ② 한 방 피해 상한 30% ──

        [Test]
        public void 거검_휘두르기는_상한에_걸려_30으로_잘린다()
        {
            // 설계서 5.1 — "130% × ×1.20 = 156% = 31.2 → 30 으로 절단"
            var baseline = new PatternBaseline(
                telegraph: 0.8f, totalDuration: 1.6f, cooldown: 10f, damage: 26f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(30f, tuned.Damage, 0.0001f);
            Assert.IsTrue(tuned.DamageClamped);
        }

        [Test]
        public void 돌진_기믹도_상한에_걸린다()
        {
            // 설계서 5.1 — "150% × 1.20 = 180% = 36 → 30 으로 절단"
            var baseline = new PatternBaseline(
                telegraph: 1.1f, totalDuration: 1.5f, cooldown: 0f, damage: 30f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(30f, tuned.Damage, 0.0001f);
        }

        [Test]
        public void 일반_공격_1은_상한에_걸리지_않는다()
        {
            // 설계서 5.1 — "나머지는 상한에 걸리지 않는다"
            var baseline = new PatternBaseline(
                telegraph: 1.05f, totalDuration: 2.1f, cooldown: 0f, damage: 18f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(21.6f, tuned.Damage, 0.0001f);
            Assert.IsFalse(tuned.DamageClamped);
        }

        // ── 안전장치 ③ 타수 ──

        [Test]
        public void 단발_패턴은_타수가_늘지_않는다()
        {
            var baseline = new PatternBaseline(
                telegraph: 1f, totalDuration: 2f, cooldown: 5f, damage: 10f);

            Assert.AreEqual(1, TunedPattern.Resolve(baseline, Arm(+2), 100f).RepeatCount);
            Assert.AreEqual(1, TunedPattern.Resolve(baseline, Arm(-2), 100f).RepeatCount);
        }

        [Test]
        public void 다단히트는_한_타씩_늘고_준다()
        {
            var baseline = new PatternBaseline(
                telegraph: 0.5f, totalDuration: 2.6f, cooldown: 14f,
                damage: 7f, repeatCount: 5, repeatInterval: 0.4f);

            Assert.AreEqual(6, TunedPattern.Resolve(baseline, Arm(+2), 100f).RepeatCount);
            Assert.AreEqual(4, TunedPattern.Resolve(baseline, Arm(-2), 100f).RepeatCount);
        }

        [Test]
        public void 연타_간격은_무적_0_35초보다_길게_유지된다()
        {
            // 설계서 5.1 의 핵심 수정 — 간격이 무적보다 짧으면 격타가 그냥 씹힌다.
            // 0.4s 로 고친 이유가 이것이고, 가장 어려운 단계에서도 지켜져야 한다.
            var baseline = new PatternBaseline(
                telegraph: 0.5f, totalDuration: 2.6f, cooldown: 14f,
                damage: 7f, repeatCount: 5, repeatInterval: 0.4f);

            float interval = TunedPattern.Resolve(baseline, Arm(+2), 100f).RepeatInterval;

            Assert.AreEqual(0.36f, interval, 0.0001f);
            Assert.Greater(
                interval, 0.35f,
                "간격이 피격 무적보다 짧으면 DDA 의 연타 조정이 아무 의미가 없다");
        }

        // ── 총 길이 ──

        [Test]
        public void 후딜은_보정과_무관하게_유지된다()
        {
            // 방패 돌진: 총 2.6 − 선딜 0.5 − 연타 1.6 = 후딜 0.5 (설계서 5.1)
            var baseline = new PatternBaseline(
                telegraph: 0.5f, totalDuration: 2.6f, cooldown: 14f,
                damage: 7f, repeatCount: 5, repeatInterval: 0.4f);

            Assert.AreEqual(0.5f, baseline.Recovery, 0.0001f);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            // +2단계는 타수가 5 → 6 이 되므로 간격이 5번 들어간다.
            // 선딜 0.40 + 0.36×5 (=1.80) + 후딜 0.50 = 2.70
            Assert.AreEqual(6, tuned.RepeatCount);
            Assert.AreEqual(2.7f, tuned.TotalDuration, 0.0001f);
        }

        [Test]
        public void 연출용_패턴은_DDA가_건드리지_않는다()
        {
            var baseline = new PatternBaseline(
                telegraph: 2.5f, totalDuration: 5f, cooldown: 0f,
                damage: 0f, ddaExcluded: true);

            var tuned = TunedPattern.Resolve(baseline, Arm(+2), 100f);

            Assert.AreEqual(2.5f, tuned.Telegraph, 0.0001f);
            Assert.AreEqual(5f, tuned.TotalDuration, 0.0001f);
        }
    }
}
