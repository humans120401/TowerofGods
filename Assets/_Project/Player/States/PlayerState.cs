namespace RT.Player.States
{
    /// <summary>플레이어 상태 종류. 로그·애니메이션·텔레메트리에서 공통으로 쓴다.</summary>
    public enum PlayerStateId
    {
        Grounded,   // 지상 — 대기와 이동을 함께 다룬다
        Airborne,   // 공중 — 상승과 낙하를 함께 다룬다
        Dash,
        Attack,     // 검 콤보 1·2타
        Skill,      // Q · W · E
        Parry,
        Hit,        // 피격 경직
        Dead,
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

        public virtual void Enter() { }
        public virtual void Exit() { }

        /// <summary>매 프레임. 다음 상태를 돌려주고, 바꿀 게 없으면 null.</summary>
        public virtual PlayerStateId? Tick(float deltaTime) => null;

        /// <summary>물리 프레임. Rigidbody 조작은 여기서만.</summary>
        public virtual void FixedTick(float fixedDeltaTime) { }

        /// <summary>
        /// 피격당했을 때 이 상태가 끊기는가.
        /// E(처형 베기) 차지처럼 캔슬되는 것이 있어 상태마다 다르다.
        /// </summary>
        public virtual bool InterruptedByHit => true;

        // ── 상태들이 공통으로 쓰는 전환 판단 ──
        // Grounded / Airborne 에서 똑같이 쓰이므로 한 곳에 둔다.

        /// <summary>지금 대시로 넘어갈 수 있는가</summary>
        protected bool WantsDash =>
            Ctx.Input.DashPressed && !Ctx.DashBlocked && Ctx.Dash.CanDash;

        /// <summary>지금 패링으로 넘어갈 수 있는가</summary>
        protected bool WantsParry =>
            Ctx.Input.ParryPressed && !Ctx.ParryBlocked;

        /// <summary>지금 기본 공격으로 넘어갈 수 있는가</summary>
        protected bool WantsAttack =>
            Ctx.Input.AttackPressed && !Ctx.AttackBlocked;

        /// <summary>
        /// Q · W · E 입력을 확인한다.
        /// 쓸 수 있는 스킬이 눌렸으면 Ctx.PendingSkill 에 담고 true 를 돌려준다.
        /// SkillState 가 들어갈 때 그걸 읽는다.
        /// </summary>
        protected bool TryTakeSkillInput()
        {
            if (Ctx.AttackBlocked) return false;

            if (Ctx.Input.SkillQPressed && Ready(Data.SkillSlot.Q)) return true;
            if (Ctx.Input.SkillWPressed && Ready(Data.SkillSlot.W)) return true;
            if (Ctx.Input.SkillEPressed && Ready(Data.SkillSlot.E)) return true;

            return false;
        }

        private bool Ready(Data.SkillSlot slot)
        {
            var skill = Ctx.SkillOf(slot);

            if (skill == null) return false;
            if (!Ctx.Cooldowns.IsReady(slot)) return false;

            Ctx.PendingSkill = skill;
            return true;
        }
    }
}
