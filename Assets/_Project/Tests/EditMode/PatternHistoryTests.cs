using NUnit.Framework;
using RT.Boss;
using RT.Core.Combat;
using RT.Data;
using UnityEngine;

namespace RT.Tests
{
    /// <summary>
    /// 패턴 기록. 카로스의 환영이 여기서 복제할 패턴을 꺼낸다.
    ///
    /// 핵심은 **환영이 환영을 복제하지 않는 것**이다.
    /// 그게 막히지 않으면 환영이 기하급수로 늘어 전투가 터진다.
    /// 보스를 만들기 전에 잡아 두는 게 싸다.
    /// </summary>
    public class PatternHistoryTests
    {
        private static BossPatternData Pattern(
            string id, PatternKind kind = PatternKind.Melee, float damage = 18f)
        {
            var p = ScriptableObject.CreateInstance<BossPatternData>();
            p.patternId = id;
            p.kind = kind;
            p.damage = damage;
            return p;
        }

        private static PatternInvocation Invoke(BossPatternData p, bool isReplay = false)
        {
            var tuned = TunedPattern.Resolve(p.Baseline, PatternTuning.Neutral, 100f);
            return new PatternInvocation(p, tuned, Vector2.zero, 1f, Vector2.one, isReplay);
        }

        [Test]
        public void 비어_있으면_복제할_게_없다()
        {
            var h = new PatternHistory();
            Assert.IsFalse(h.TryGetReplayable(out _));
        }

        [Test]
        public void 가장_최근_공격을_꺼낸다()
        {
            var h = new PatternHistory();
            h.Record(Invoke(Pattern("횡베기")));
            h.Record(Invoke(Pattern("내려찍기")));

            Assert.IsTrue(h.TryGetReplayable(out var got));
            Assert.AreEqual("내려찍기", got.Pattern.patternId);
        }

        [Test]
        public void 환영은_환영을_복제하지_않는다()
        {
            // 이게 이 클래스의 존재 이유다.
            var h = new PatternHistory();
            h.Record(Invoke(Pattern("횡베기")));
            h.Record(Invoke(Pattern("횡베기"), isReplay: true));

            Assert.IsTrue(h.TryGetReplayable(out var got));
            Assert.IsFalse(got.IsReplay, "복제본을 또 복제하면 환영이 무한히 늘어난다");
        }

        [Test]
        public void 환영_패턴_자체도_건너뛴다()
        {
            var h = new PatternHistory();
            h.Record(Invoke(Pattern("횡베기")));
            h.Record(Invoke(Pattern("잔상", PatternKind.Replay)));

            Assert.IsTrue(h.TryGetReplayable(out var got));
            Assert.AreEqual("횡베기", got.Pattern.patternId);
        }

        [Test]
        public void 피해가_없는_기동은_복제하지_않는다()
        {
            // 퀵스텝을 복제해 봐야 플레이어가 대응할 게 없다.
            var h = new PatternHistory();
            h.Record(Invoke(Pattern("횡베기")));
            h.Record(Invoke(Pattern("퀵스텝", PatternKind.Movement, damage: 0f)));

            Assert.IsTrue(h.TryGetReplayable(out var got));
            Assert.AreEqual("횡베기", got.Pattern.patternId);
        }

        [Test]
        public void 정원을_넘으면_오래된_것부터_버린다()
        {
            var h = new PatternHistory(capacity: 3);

            for (int i = 0; i < 5; i++) h.Record(Invoke(Pattern($"p{i}")));

            Assert.AreEqual(3, h.Count, "전투가 길어져도 메모리가 늘지 않아야 한다");

            Assert.IsTrue(h.TryGet(0, out var newest));
            Assert.AreEqual("p4", newest.Pattern.patternId);
        }

        [Test]
        public void 복제본은_자리만_바뀌고_나머지는_원본_그대로다()
        {
            var original = Invoke(Pattern("횡베기"));
            var copy = original.AsReplayFrom(new Vector2(5f, 0f), -1f);

            Assert.IsTrue(copy.IsReplay);
            Assert.AreEqual(5f, copy.Origin.x, 0.0001f);
            Assert.AreEqual(-1f, copy.Facing, 0.0001f);

            Assert.AreEqual(
                original.Tuned.Damage, copy.Tuned.Damage, 0.0001f,
                "보정이 기록에 들어 있어야 원본이 나갔던 난이도 그대로 재생된다");

            Assert.AreEqual(
                original.TargetAtStart, copy.TargetAtStart,
                "겨냥 지점은 원본 그대로여야 같은 공격이 된다");
        }
    }
}
