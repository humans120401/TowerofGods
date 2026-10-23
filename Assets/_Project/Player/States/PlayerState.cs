namespace RT.Player.States
{
    /// <summary>플레이어 상태 종류. 로그·애니메이션·텔레메트리에서 공통으로 쓴다.</summary>
    public enum PlayerStateId
    {
        Grounded,   // 지상 — 대기와 이동을 함께 다룬다
        Airborne,   // 공중 — 상승과 낙하를 함께 다룬다
        Dash,
        // 2단계에서 추가: Attack, Skill, Parry, Hit, Stun, Dead
    }

    /// <summary>
    /// 상태 하나의 뼈대.
    ///
    /// 규칙:
    ///   Tick      — 매 프레임. 입력 판단과 상태 전환은 여기서 한다.
    ///   FixedTick — 물리 프레임. Rigidbody를 건드리는 건 여기서만 한다.
    ///
    /// 상태 전환은 Tick이 돌려주는 값으로 한다.
    /// null을 돌려주면 "그대로 유지"다. 상태가 직접 상태 기계를 호출하지 않는다.
    /// </summary>
    public abstract class PlayerState
    {
        protected readonly PlayerContext Ctx;

        protected PlayerState(PlayerContext ctx)
        {
            Ctx = ctx;
        }

        public abstract PlayerStateId Id { get; }

        /// <summary>이 상태로 들어올 때 한 번</summary>
        public virtual void Enter() { }

        /// <summary>이 상태에서 나갈 때 한 번</summary>
        public virtual void Exit() { }

        /// <summary>
        /// 매 프레임. 다음 상태를 돌려주고, 바꿀 게 없으면 null.
        /// </summary>
        public virtual PlayerStateId? Tick(float deltaTime) => null;

        /// <summary>물리 프레임. Rigidbody 조작은 여기서만.</summary>
        public virtual void FixedTick(float fixedDeltaTime) { }
    }
}
