using NUnit.Framework;
using RT.Data;
using RT.Player.Systems;

namespace RT.Tests
{
    /// <summary>
    /// 쿨타임. 전투방식설계서 3.2 · 3.3
    ///
    /// 마나가 없으므로 쿨타임이 스킬 성격을 정하는 유일한 수단이다.
    /// </summary>
    public class CooldownsTests
    {
        [Test]
        public void 시작할때_전부_준비되어_있다()
        {
            var c = new Cooldowns();

            Assert.IsTrue(c.IsReady(SkillSlot.Q));
            Assert.IsTrue(c.IsReady(SkillSlot.W));
            Assert.IsTrue(c.IsReady(SkillSlot.E));
        }

        [Test]
        public void 쓰면_쿨타임이_걸린다()
        {
            var c = new Cooldowns();
            c.Start(SkillSlot.Q, 6f);

            Assert.IsFalse(c.IsReady(SkillSlot.Q));
            Assert.AreEqual(6f, c.Remaining(SkillSlot.Q), 0.0001f);
        }

        [Test]
        public void 다른_슬롯은_영향을_받지_않는다()
        {
            var c = new Cooldowns();
            c.Start(SkillSlot.Q, 6f);

            Assert.IsTrue(c.IsReady(SkillSlot.W));
            Assert.IsTrue(c.IsReady(SkillSlot.E));
        }

        [Test]
        public void 시간이_지나면_준비된다()
        {
            var c = new Cooldowns();
            c.Start(SkillSlot.W, 10f);

            c.Tick(9.9f);
            Assert.IsFalse(c.IsReady(SkillSlot.W));

            c.Tick(0.2f);
            Assert.IsTrue(c.IsReady(SkillSlot.W));
            Assert.AreEqual(0f, c.Remaining(SkillSlot.W), 0.0001f, "음수로 가지 않아야 한다");
        }

        [Test]
        public void 피격_캔슬은_쿨타임을_절반만_먹는다()
        {
            // 설계서 3.3 — E(처형 베기)가 차지 중 피격당하면 15s 가 아니라 7.5s
            var c = new Cooldowns();
            c.StartPartial(SkillSlot.E, 15f, 0.5f);

            Assert.AreEqual(
                7.5f, c.Remaining(SkillSlot.E), 0.0001f,
                "실수 비용을 줄이는 게 설계 의도다. 전체 쿨이 돌면 E를 아무도 안 쓴다");
        }

        [Test]
        public void 층에_들어가면_전부_초기화된다()
        {
            // 설계서 5.2 — 층 진입 조건은 항상 같아야 한다
            var c = new Cooldowns();
            c.Start(SkillSlot.Q, 6f);
            c.Start(SkillSlot.W, 10f);
            c.Start(SkillSlot.E, 15f);

            c.ResetAll();

            Assert.IsTrue(c.IsReady(SkillSlot.Q));
            Assert.IsTrue(c.IsReady(SkillSlot.W));
            Assert.IsTrue(c.IsReady(SkillSlot.E));
        }
    }
}
