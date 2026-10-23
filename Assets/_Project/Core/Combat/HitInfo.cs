namespace RT.Core.Combat
{
    /// <summary>넉백 4단계. 전투방식설계서 4.2</summary>
    public enum KnockbackLevel
    {
        /// <summary>0m — 다단히트 중간 타격. 넉백이 있으면 나머지 타가 자동으로 빗나간다</summary>
        None = 0,

        /// <summary>0.5m — 일반 공격, 콤보 마무리</summary>
        Weak = 1,

        /// <summary>1m — 대부분의 스킬 패턴 기본값</summary>
        Medium = 2,

        /// <summary>2m + 다운 — 페이즈 전환기, 대형 범위기. 기상 0.6s 추가</summary>
        Strong = 3,
    }

    /// <summary>피격 판정 결과</summary>
    public enum HitResult
    {
        /// <summary>실제로 맞았다</summary>
        Hit,

        /// <summary>패링으로 막았다 — 피해 0, 게이지 +15, 보스 경직</summary>
        Parried,

        /// <summary>무적 등으로 흘렸다 — 피해 0, 보상 없음</summary>
        Ignored,
    }

    /// <summary>
    /// 보스 → 플레이어로 전달되는 한 타의 정보.
    ///
    /// **Parryable 과 IgnoresDodge 가 핵심이다.**
    /// 팀 프로젝트에서는 이 두 값이 보스 쪽에 있었는데 플레이어가 받을 통로가 없어서,
    /// 패링 한 번이면 모든 공격이 막히고 회피불가 공격이 무적에 막혔다.
    /// 처음부터 한 구조체에 담아 넘긴다.
    /// </summary>
    public readonly struct HitInfo
    {
        /// <summary>어느 패턴에서 나온 타인가 (텔레메트리·DDA 집계 키)</summary>
        public readonly string PatternId;

        /// <summary>그 패턴 안에서 몇 번째 타인가 (0부터)</summary>
        public readonly int HitIndex;

        /// <summary>피해량. 보스 공격력 × 계수 × DDA 배율이 이미 적용된 최종값</summary>
        public readonly float Damage;

        /// <summary>넉백 방향 (x = -1 또는 1)</summary>
        public readonly float Direction;

        public readonly KnockbackLevel Knockback;

        /// <summary>패링으로 막을 수 있는 타인가. SPEC 기준 보스 패턴의 35~40%만 true</summary>
        public readonly bool Parryable;

        /// <summary>대시·피격 무적을 뚫는가. 설계상 '회피 불가' 패턴</summary>
        public readonly bool IgnoresDodge;

        /// <summary>패링 성공 시 보스가 경직되는 시간(초)</summary>
        public readonly float ParryStaggerTime;

        public HitInfo(
            string patternId,
            int hitIndex,
            float damage,
            float direction,
            KnockbackLevel knockback = KnockbackLevel.Weak,
            bool parryable = false,
            bool ignoresDodge = false,
            float parryStaggerTime = 0.6f)
        {
            PatternId = patternId;
            HitIndex = hitIndex;
            Damage = damage;
            Direction = direction;
            Knockback = knockback;
            Parryable = parryable;
            IgnoresDodge = ignoresDodge;
            ParryStaggerTime = parryStaggerTime;
        }
    }

    /// <summary>
    /// 보스 공격을 받을 수 있는 대상. 플레이어가 구현한다.
    ///
    /// 받는 쪽이 Hit / Parried / Ignored 를 판정해 돌려준다.
    /// 보스는 그 결과를 보고 경직에 걸릴지 정한다.
    /// 보스는 이 인터페이스만 알고 RT.Player 를 참조하지 않는다.
    /// </summary>
    public interface IAttackReceiver
    {
        HitResult ReceiveHit(in HitInfo info);
    }

    /// <summary>
    /// 플레이어 공격을 받을 수 있는 대상. 보스·허수아비가 구현한다.
    ///
    /// IAttackReceiver 와 방향이 반대다.
    /// 이쪽은 결과를 돌려주지 않는다 — 보스가 패링하는 경우(에텔로스)는
    /// 보스 쪽에서 따로 처리하고, 플레이어는 "때렸다"까지만 안다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>이미 죽었는가. 죽은 대상은 판정에서 제외한다.</summary>
        bool IsDead { get; }

        void TakeDamage(in HitInfo info);
    }
}
