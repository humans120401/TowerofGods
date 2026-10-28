using NUnit.Framework;
using RT.Boss;

namespace RT.Tests
{
    /// <summary>
    /// 보스 패턴 쿨타임. 설계서 5장
    ///
    /// 쿨타임은 DDA 가 조정하는 다섯 값 중 하나라서 (×0.80~×1.25),
    /// 여기가 틀리면 난이도 조절이 통째로 어긋난다.
    /// </summary>
    public class PatternCooldownsTests
    {
        [Test]
        public void 쓴_적이_없으면_바로_쓸_수_있다()
        {
            var c = new PatternCooldowns();
            Assert.IsTrue(c.IsReady("거검휘두르기"));
        }

        [Test]
        public void 쓰면_쿨이_걸린다()
        {
            var c = new PatternCooldowns();
            c.Start("거검휘두르기", 10f);

            Assert.IsFalse(c.IsReady("거검휘두르기"));
            Assert.AreEqual(10f, c.Remaining("거검휘두르기"), 0.0001f);
        }

        [Test]
        public void 쿨이_0이면_아무것도_걸지_않는다()
        {
            // 일반 공격은 쿨타임이 없다 (설계서 5.1 — "쿨타임: 없음")
            var c = new PatternCooldowns();
            c.Start("일반공격1", 0f);

            Assert.IsTrue(c.IsReady("일반공격1"));
        }

        [Test]
        public void 다른_패턴은_영향을_받지_않는다()
        {
            var c = new PatternCooldowns();
            c.Start("거검휘두르기", 10f);

            Assert.IsTrue(c.IsReady("방패돌진"));
        }

        [Test]
        public void 시간이_지나면_다시_쓸_수_있다()
        {
            var c = new PatternCooldowns();
            c.Start("방패돌진", 14f);

            c.Tick(13.9f);
            Assert.IsFalse(c.IsReady("방패돌진"));

            c.Tick(0.2f);
            Assert.IsTrue(c.IsReady("방패돌진"));
            Assert.AreEqual(0f, c.Remaining("방패돌진"), 0.0001f, "음수로 가지 않아야 한다");
        }

        [Test]
        public void DDA가_쿨을_줄이면_더_자주_나온다()
        {
            // +2단계는 cooldownScale ×0.80 — 14s 가 11.2s 가 된다
            var c = new PatternCooldowns();
            c.Start("방패돌진", 14f * 0.8f);

            c.Tick(11.2f);
            Assert.IsTrue(c.IsReady("방패돌진"));
        }

        [Test]
        public void 층에_들어가면_전부_초기화된다()
        {
            // 설계서 5.2 — 층 진입 조건은 항상 같아야 한다
            var c = new PatternCooldowns();
            c.Start("A", 10f);
            c.Start("B", 20f);

            c.ResetAll();

            Assert.IsTrue(c.IsReady("A"));
            Assert.IsTrue(c.IsReady("B"));
        }

        [Test]
        public void 없는_패턴을_물어도_터지지_않는다()
        {
            var c = new PatternCooldowns();

            Assert.IsTrue(c.IsReady(null));
            Assert.AreEqual(0f, c.Remaining("없는패턴"), 0.0001f);

            Assert.DoesNotThrow(() => c.Start(null, 5f));
            Assert.DoesNotThrow(() => c.Tick(1f));
        }
    }
}
