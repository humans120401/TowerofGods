using NUnit.Framework;
using RT.Player.Systems;

namespace RT.Tests
{
    /// <summary>
    /// 무적 관리의 규칙을 검증한다.
    ///
    /// 이 테스트가 존재하는 이유가 명확하다.
    /// 무적을 bool 하나로 두면 "대시 중 회피 불가 공격에 맞음" 상황에서
    /// 대시가 끝날 때 피격 무적까지 지워 버린다.
    /// 그러면 설계서 4.2 의 대응 창 0.2초가 사라지고 다단히트에 즉사한다.
    ///
    /// 눈으로는 절대 못 잡는 버그다 — 0.05초 차이로 체력만 더 깎인다.
    /// </summary>
    public class InvincibilityTests
    {
        [Test]
        public void 아무도_켜지_않았으면_무적이_아니다()
        {
            var inv = new Invincibility();
            Assert.IsFalse(inv.IsActive);
        }

        [Test]
        public void 시간_무적은_지나면_저절로_꺼진다()
        {
            var inv = new Invincibility();
            inv.For(InvincibilitySource.Hit, 0.35f);

            Assert.IsTrue(inv.IsActive);

            inv.Tick(0.3f);
            Assert.IsTrue(inv.IsActive, "0.3초만 지났으면 아직 남아 있어야 한다");

            inv.Tick(0.1f);
            Assert.IsFalse(inv.IsActive);
        }

        [Test]
        public void 유지_무적은_풀어_줄_때까지_켜져_있다()
        {
            var inv = new Invincibility();
            inv.Hold(InvincibilitySource.Skill);

            inv.Tick(100f);
            Assert.IsTrue(inv.IsActive, "시간이 지나도 Hold 는 안 꺼진다");

            inv.Release(InvincibilitySource.Skill);
            Assert.IsFalse(inv.IsActive);
        }

        [Test]
        public void 대시가_끝나도_피격_무적은_남는다()
        {
            // 이게 핵심 회귀 테스트다.
            var inv = new Invincibility();

            // 대시 시작 — 무적 0.15초
            inv.For(InvincibilitySource.Dash, 0.15f);

            // 대시 중 회피 불가 공격에 맞음 — 피격 무적 0.35초
            inv.For(InvincibilitySource.Hit, 0.35f);

            // 상태가 Hit 으로 강제 전환되면서 DashState.Exit 이 돌아간다
            inv.Release(InvincibilitySource.Dash);

            Assert.IsTrue(
                inv.IsActive,
                "대시 무적만 꺼져야 한다. 피격 무적까지 꺼지면 다단히트 뒷 타를 전부 맞는다");

            Assert.AreEqual(
                0f, inv.Remaining(InvincibilitySource.Dash), 0.0001f);

            Assert.AreEqual(
                0.35f, inv.Remaining(InvincibilitySource.Hit), 0.0001f);
        }

        [Test]
        public void 더_짧은_무적이_긴_것을_밀어내지_않는다()
        {
            var inv = new Invincibility();

            inv.For(InvincibilitySource.Hit, 0.35f);
            inv.For(InvincibilitySource.Hit, 0.1f);   // 다단히트 2타

            Assert.AreEqual(
                0.35f, inv.Remaining(InvincibilitySource.Hit), 0.0001f,
                "짧은 쪽이 긴 쪽을 덮으면 무적이 짧아져 뒷 타를 맞는다");
        }

        [Test]
        public void 경직보다_무적이_길어_대응_창이_생긴다()
        {
            // 설계서 4.2 — 경직 0.15s, 무적 0.35s → 대응 창 0.2s
            var inv = new Invincibility();
            inv.For(InvincibilitySource.Hit, 0.35f);

            inv.Tick(0.15f);   // 경직이 끝나는 시점

            Assert.IsTrue(inv.IsActive, "경직이 끝나도 무적은 남아야 한다");
            Assert.AreEqual(0.2f, inv.Remaining(InvincibilitySource.Hit), 0.0001f);
        }

        [Test]
        public void 전부_끄면_전부_꺼진다()
        {
            var inv = new Invincibility();
            inv.For(InvincibilitySource.Hit, 1f);
            inv.Hold(InvincibilitySource.Dead);

            inv.ClearAll();

            Assert.IsFalse(inv.IsActive);
        }
    }
}
