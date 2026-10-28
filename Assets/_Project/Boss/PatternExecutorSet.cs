using System.Collections.Generic;
using RT.Boss.Executors;
using RT.Data;

namespace RT.Boss
{
    /// <summary>
    /// 패턴 종류 → 실행기 짝짓기. 보스 하나가 하나씩 들고 있다.
    ///
    /// **왜 static 전역이 아닌가**: 보스마다 같은 종류를 다르게 쓴다.
    /// 아라쿠스의 투사체는 직선 한 발이고 파네스는 부채꼴 다섯 발이다.
    /// 전역 하나를 공유하면 파네스를 만들 때 아라쿠스가 같이 바뀐다.
    ///
    /// 실행기는 상태가 없으므로 같은 인스턴스를 여러 패턴이 공유해도 안전하다.
    /// </summary>
    public class PatternExecutorSet
    {
        private readonly Dictionary<PatternKind, PatternExecutor> _byKind =
            new Dictionary<PatternKind, PatternExecutor>();

        /// <summary>patternId 로 지정한 예외. 같은 종류 안에서 하나만 다를 때 쓴다</summary>
        private readonly Dictionary<string, PatternExecutor> _byPattern =
            new Dictionary<string, PatternExecutor>();

        /// <summary>어떤 종류도 처리하는 기본 묶음. 보스별로 Set 으로 갈아 끼운다.</summary>
        public static PatternExecutorSet Default()
        {
            var set = new PatternExecutorSet();

            set.Set(PatternKind.Melee, new MeleeExecutor());
            set.Set(PatternKind.Projectile, new ProjectileExecutor());
            set.Set(PatternKind.Dash, new DashExecutor());
            set.Set(PatternKind.Summon, new SummonExecutor());
            set.Set(PatternKind.Area, new AreaExecutor());
            set.Set(PatternKind.Replay, new ReplayExecutor());
            set.Set(PatternKind.Movement, new MovementExecutor());
            set.Set(PatternKind.Buff, new BuffExecutor());

            return set;
        }

        public PatternExecutorSet Set(PatternKind kind, PatternExecutor executor)
        {
            _byKind[kind] = executor;
            return this;
        }

        /// <summary>이 패턴만 다른 실행기를 쓴다. 예: 회전베기만 RadialExecutor</summary>
        public PatternExecutorSet SetForPattern(string patternId, PatternExecutor executor)
        {
            if (!string.IsNullOrEmpty(patternId)) _byPattern[patternId] = executor;
            return this;
        }

        /// <summary>이 패턴을 돌릴 실행기. 없으면 null</summary>
        public PatternExecutor For(BossPatternData pattern)
        {
            if (pattern == null) return null;

            if (_byPattern.TryGetValue(pattern.patternId, out var special)) return special;

            return _byKind.TryGetValue(pattern.kind, out var byKind) ? byKind : null;
        }
    }
}
