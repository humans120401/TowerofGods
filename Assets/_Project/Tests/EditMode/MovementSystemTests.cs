using NUnit.Framework;
using RT.Player.Systems;

namespace RT.Tests
{
    /// <summary>
    /// 대시 스택과 점프 보조의 규칙을 검증한다.
    ///
    /// 두 클래스 모두 UnityEngine을 쓰지 않게 만들어 둬서,
    /// 플레이 모드를 켜지 않고 시간만 넣어 가며 그대로 돌릴 수 있다.
    /// "2.5초 뒤에 차는지"를 손으로 세어 보지 않아도 된다.
    /// </summary>
    public class DashStacksTests
    {
        // 설계서 2.3 — 스택 2 / 충전 2.5s / 연속 간격 0.3s
        private static DashStacks Make() => new DashStacks(2, 2.5f, 0.3f);

        [Test]
        public void 시작할때_스택이_가득_차_있다()
        {
            var d = Make();
            Assert.AreEqual(2, d.Current);
            Assert.IsTrue(d.CanDash);
        }

        [Test]
        public void 한번_쓰면_스택이_하나_줄어든다()
        {
            var d = Make();
            Assert.IsTrue(d.TryConsume());
            Assert.AreEqual(1, d.Current);
        }

        [Test]
        public void 연속_사용에는_최소_간격이_필요하다()
        {
            var d = Make();

            Assert.IsTrue(d.TryConsume(), "첫 대시는 나간다");
            Assert.IsFalse(d.CanDash, "0.3초 전에는 스택이 남아 있어도 못 쓴다");

            d.Tick(0.29f);
            Assert.IsFalse(d.CanDash, "0.29초는 아직 모자라다");

            d.Tick(0.02f);
            Assert.IsTrue(d.CanDash, "0.31초면 두 번째 대시가 나간다");
        }

        [Test]
        public void 스택을_다_쓰면_못_쓴다()
        {
            var d = Make();

            d.TryConsume();
            d.Tick(0.31f);
            d.TryConsume();

            Assert.AreEqual(0, d.Current);

            d.Tick(1.0f);
            Assert.IsFalse(d.CanDash, "간격은 지났지만 스택이 없다");
        }

        [Test]
        public void 충전시간이_지나면_한_칸_찬다()
        {
            var d = Make();
            d.TryConsume();

            d.Tick(2.49f);
            Assert.AreEqual(1, d.Current, "2.49초에는 아직");

            d.Tick(0.02f);
            Assert.AreEqual(2, d.Current, "2.5초에 한 칸 찬다");
        }

        [Test]
        public void 최대치를_넘어서_차지_않는다()
        {
            var d = Make();
            d.Tick(100f);
            Assert.AreEqual(2, d.Current);
        }

        [Test]
        public void 충전배율이_낮으면_느리게_찬다()
        {
            // 에텔로스 전기 피격 디버프 — 충전 속도 절반
            var d = Make();
            d.TryConsume();

            d.Tick(2.5f, 0.5f);
            Assert.AreEqual(1, d.Current, "절반 속도면 2.5초로는 모자라다");

            d.Tick(2.5f, 0.5f);
            Assert.AreEqual(2, d.Current, "합쳐서 5초면 찬다");
        }
    }

    public class JumpAssistTests
    {
        // 설계서 2.2 — 코요테 0.1s / 버퍼 0.12s
        private static JumpAssist Make() => new JumpAssist(0.1f, 0.12f);

        [Test]
        public void 지상에서_누르면_바로_점프한다()
        {
            var j = Make();
            j.Tick(0.016f, isGrounded: true, jumpPressed: true);

            Assert.IsTrue(j.TryConsumeJump());
        }

        [Test]
        public void 같은_입력으로_두_번_점프하지_않는다()
        {
            var j = Make();
            j.Tick(0.016f, isGrounded: true, jumpPressed: true);

            Assert.IsTrue(j.TryConsumeJump());
            Assert.IsFalse(j.TryConsumeJump(), "소모됐으므로 두 번째는 안 나간다");
        }

        [Test]
        public void 발판을_벗어난_직후에도_점프가_된다()
        {
            var j = Make();

            // 지상에 있다가
            j.Tick(0.016f, isGrounded: true, jumpPressed: false);

            // 걸어서 떨어진 뒤 0.05초 지나 누른다
            j.Tick(0.05f, isGrounded: false, jumpPressed: false);
            j.Tick(0.001f, isGrounded: false, jumpPressed: true);

            Assert.IsTrue(j.TryConsumeJump(), "코요테 0.1초 안이면 받아준다");
        }

        [Test]
        public void 코요테_시간이_지나면_점프가_안_된다()
        {
            var j = Make();

            j.Tick(0.016f, isGrounded: true, jumpPressed: false);
            j.Tick(0.15f, isGrounded: false, jumpPressed: true);

            Assert.IsFalse(j.TryConsumeJump(), "0.1초를 넘겼다");
        }

        [Test]
        public void 착지_직전에_누른_점프가_착지_순간_나간다()
        {
            var j = Make();

            // 공중에서 미리 누른다
            j.Tick(0.016f, isGrounded: false, jumpPressed: true);
            Assert.IsFalse(j.TryConsumeJump(), "아직 공중이라 안 나간다");

            // 0.08초 뒤 착지
            j.Tick(0.08f, isGrounded: true, jumpPressed: false);
            Assert.IsTrue(j.TryConsumeJump(), "버퍼 0.12초 안이라 착지하자마자 나간다");
        }

        [Test]
        public void 너무_일찍_누른_점프는_버려진다()
        {
            var j = Make();

            j.Tick(0.016f, isGrounded: false, jumpPressed: true);
            j.Tick(0.2f, isGrounded: true, jumpPressed: false);

            Assert.IsFalse(j.TryConsumeJump(), "버퍼 0.12초를 넘겼다");
        }

        [Test]
        public void 코요테를_끊으면_공중_점프가_안_된다()
        {
            // 대시로 공중에 뜬 직후 — 코요테를 안 끊으면 공중 점프가 한 번 더 나온다
            var j = Make();

            j.Tick(0.016f, isGrounded: true, jumpPressed: false);
            j.CancelCoyote();
            j.Tick(0.001f, isGrounded: false, jumpPressed: true);

            Assert.IsFalse(j.TryConsumeJump());
        }
    }
}
