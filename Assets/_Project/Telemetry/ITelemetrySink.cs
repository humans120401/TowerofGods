using System.Collections.Generic;

namespace RT.Telemetry
{
    /// <summary>한 패턴에 대한 이번 전투의 집계. DDA 설계서 10장</summary>
    public class PatternStats
    {
        public string PatternId;

        /// <summary>날아온 타 수</summary>
        public int Strikes;

        /// <summary>무적 등으로 무시된 타 수</summary>
        public int IgnoredCount;

        public int ParrySuccess;
        public int DodgeCount;
        public int HitCount;

        /// <summary>이번 전투에 걸린 단계 (−2 ~ +2)</summary>
        public int DdaStep;

        /// <summary>단계를 고른 이유</summary>
        public string DdaReason;

        /// <summary>단계를 고를 때의 문맥 x1~x5 (x0는 항상 1이라 안 남김)</summary>
        public float[] Context;

        public float DdaPredicted;
        public float DdaUncertainty;

        /// <summary>실제 대응 성공률. 유효 타 3 미만이면 −1</summary>
        public float ResponseRate = -1f;

        /// <summary>구간 보상 r (DDA 설계서 7장)</summary>
        public float DdaReward;

        /// <summary>유효 타 = 날아온 타 − 무시된 타. 대응 성공률의 분모</summary>
        public int EffectiveStrikes => Strikes - IgnoredCount;
    }

    /// <summary>한 전투의 요약</summary>
    public class BattleSummary
    {
        public string BattleId;
        public string BossId;
        public int AttemptIndex;

        public bool Cleared;
        public float CombatTime;

        public int DamageDealt;
        public int DamageTaken;
        public int HitCount;
        public int ParryCount;

        /// <summary>off / linucb / rule / random</summary>
        public string DdaPolicy;

        public string PriorVersion;

        /// <summary>이번 전투에 3연패 보호가 켜졌는가</summary>
        public bool ProtectionApplied;
    }

    /// <summary>전투 하나의 기록 전체</summary>
    public class BattleReport
    {
        public BattleSummary Summary = new BattleSummary();
        public List<PatternStats> Patterns = new List<PatternStats>();
    }

    /// <summary>
    /// 전투 기록을 받아 어딘가에 남기는 곳.
    ///
    /// 구현체:
    ///   CsvSink       로컬 CSV. **서버가 없어도 항상 동작한다** (전시장 대비)
    ///   SupabaseSink  Postgres 업로드
    ///   CompositeSink 여러 Sink에 동시에 흘린다
    ///
    /// 규칙: **Sink가 실패해도 게임은 멈추지 않는다.** 예외를 밖으로 던지지 않는다.
    /// </summary>
    public interface ITelemetrySink
    {
        /// <summary>전투 하나가 끝났을 때 호출</summary>
        void WriteBattle(BattleReport report);

        /// <summary>남아 있는 것을 밀어낸다 (앱 종료 시)</summary>
        void Flush();
    }
}
