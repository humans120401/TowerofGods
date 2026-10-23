using NUnit.Framework;
using RT.Player.Systems;

namespace RT.Tests
{
    /// <summary>
    /// 궁극기 게이지. 전투방식설계서 4.4
    ///
    /// 설계 목표는 "보스전 3분에 2회 사용"이다.
    /// 수급 수단이 패링에 크게 쏠려 있으므로, 숫자가 틀어지면
    /// 패링을 못 하는 플레이어는 궁극기를 한 번도 못 쓰게 된다.
    /// </summary>
    public class UltimateGaugeTests
    {
        // 설계서 4.4 — 최대 100 / 패링 +15 / 피해 100당 +3 / 자연 0.25per초, 30초 뒤부터
        private static UltimateGauge Make() => new UltimateGauge(100, 15, 3, 0.25f, 30f);

        [Test]
        public void 시작할때_비어_있다()
        {
            var g = Make();
            Assert.AreEqual(0, g.Current);
            Assert.IsFalse(g.IsFull);
        }

        [Test]
        public void 패링_성공으로_15씩_찬다()
        {
            var g = Make();
            g.OnParrySuccess();
            Assert.AreEqual(15, g.Current);
        }

        [Test]
        public void 패링_일곱번이면_가득_찬다()
        {
            // 15 × 7 = 105 → 100 에서 멈춘다
            var g = Make();
            for (int i = 0; i < 7; i++) g.OnParrySuccess();

            Assert.IsTrue(g.IsFull);
            Assert.AreEqual(100, g.Current, "최대치를 넘지 않아야 한다");
        }

        [Test]
        public void 피해_100당_3씩_찬다()
        {
            var g = Make();

            g.OnDamageDealt(100f);
            Assert.AreEqual(3, g.Current);

            g.OnDamageDealt(250f);
            Assert.AreEqual(9, g.Current, "150 누적 + 250 = 350 → 3회 변환 = 9");
        }

        [Test]
        public void 자연_충전은_30초_전에는_없다()
        {
            var g = Make();

            g.Tick(29f);
            Assert.AreEqual(0, g.Current, "30초 전에는 도망만 다녀도 게이지가 차지 않아야 한다");

            g.Tick(5f);
            Assert.Greater(g.Current, 0, "30초를 넘기면 차기 시작한다");
        }

        [Test]
        public void 궁극기를_쓰면_게이지가_비워진다()
        {
            var g = Make();
            for (int i = 0; i < 7; i++) g.OnParrySuccess();

            Assert.IsTrue(g.TryConsume());
            Assert.AreEqual(0, g.Current);
            Assert.IsFalse(g.TryConsume(), "비어 있으면 쓸 수 없다");
        }

        [Test]
        public void 가득_차지_않으면_쓸_수_없다()
        {
            var g = Make();
            for (int i = 0; i < 6; i++) g.OnParrySuccess();   // 90

            Assert.IsFalse(g.TryConsume());
            Assert.AreEqual(90, g.Current, "실패한 사용은 게이지를 건드리지 않아야 한다");
        }

        [Test]
        public void 패링을_한번도_못해도_3분에_한번은_쓸_수_있다()
        {
            // 설계 의도 — 초보자 안전망.
            // 자연 충전만으로: (180 - 30) × 0.25 = 37.5
            // 피해로: 보스 체력 3000 중 2000 정도를 넣는다고 보면 2000/100 × 3 = 60
            // 합계 ≈ 97.5 → 거의 한 번. 패링을 한 번만 해도 넘는다.
            var g = Make();

            g.Tick(180f);
            g.OnDamageDealt(2000f);

            Assert.GreaterOrEqual(
                g.Current, 90,
                "패링 없이도 1회에 가까워야 한다 — 아니면 초보자는 궁극기를 평생 못 본다");
        }
    }
}
