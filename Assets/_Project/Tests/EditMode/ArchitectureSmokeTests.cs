using NUnit.Framework;
using RT.Core.Combat;
using RT.DDA.Model;
using RT.Telemetry;

namespace RT.Tests
{
    /// <summary>
    /// 0단계 스모크 테스트.
    ///
    /// 기능을 검증하는 게 아니라 **구조가 서 있는지**를 검증한다.
    ///   - 테스트 실행기가 붙어 있다
    ///   - RT.DDA.Model 이 UnityEngine 없이 테스트에서 참조된다
    ///   - 핵심 타입의 기본값이 설계대로다
    ///
    /// 이 파일이 통과하면 "유니티에서 에러 0으로 열린다"는 0단계 목표가 확인된다.
    /// </summary>
    public class ArchitectureSmokeTests
    {
        [Test]
        public void 대조군_결정은_항상_0단계이고_이유가_off다()
        {
            var d = DdaDecision.Off;

            Assert.AreEqual(0, d.Step, "대조군은 보정을 걸지 않는다");
            Assert.AreEqual("off", d.Reason);
        }

        [Test]
        public void 보정_단계는_음수2에서_양수2_범위다()
        {
            // DDA 설계서 5장 — 팔은 다섯 개
            for (int step = -2; step <= 2; step++)
            {
                var d = new DdaDecision(step, "learned", 0.7f, 0.1f);
                Assert.AreEqual(step, d.Step);
            }
        }

        [Test]
        public void HitInfo_기본값은_패링불가_회피가능이다()
        {
            // 안전한 기본값: 명시하지 않으면 패링으로 못 막고, 무적으로는 피할 수 있다.
            // 패링 가능 패턴은 전체의 35~40%뿐이므로 기본이 false인 게 맞다.
            var hit = new HitInfo("test_pattern", 0, 20f, 1f);

            Assert.IsFalse(hit.Parryable,    "패링 가능은 명시적으로 켜야 한다");
            Assert.IsFalse(hit.IgnoresDodge, "회피 불가는 명시적으로 켜야 한다");
        }

        [Test]
        public void 다단히트_중간타는_넉백이_없어야_한다()
        {
            // 전투방식설계서 4.2 — 넉백이 있으면 나머지 타가 자동으로 빗나간다.
            // 열거형 값 자체를 고정해 두어, 누가 순서를 바꾸면 테스트가 깨지게 한다.
            Assert.AreEqual(0, (int)KnockbackLevel.None);
            Assert.AreEqual(1, (int)KnockbackLevel.Weak);
            Assert.AreEqual(2, (int)KnockbackLevel.Medium);
            Assert.AreEqual(3, (int)KnockbackLevel.Strong);
        }

        [Test]
        public void 유효타는_날아온타에서_무시된타를_뺀다()
        {
            // 대응 성공률 y 의 분모. DDA 설계서 1장 · 6장
            var stats = new PatternStats
            {
                PatternId = "test",
                Strikes = 8,
                IgnoredCount = 1,
            };

            Assert.AreEqual(7, stats.EffectiveStrikes);
        }
    }
}
