namespace RT.Player.States
{
    /// <summary>
    /// 패링. 전투방식설계서 4.1
    ///
    /// 타임라인 (누른 순간이 0초):
    ///   0 ~ 0.05   입력 지연 — 아직 판정이 열리지 않는다
    ///   0.05 ~ 0.20  판정 열림 (0.15초)
    ///   0.20 ~ 0.55  실패 후딜 (0.35초)
    ///
    /// **패링은 '막는 기술'이 아니라 '자원을 버는 기술'이다.**
    /// 성공하면 피해 0 · 넉백 0 · 궁극기 게이지 +15 · 보스 경직.
    /// 게이지를 버는 거의 유일한 수단이라, 패링 기회가 곧 성장 속도다.
    ///
    /// 실패 후딜 0.35초가 유일한 페널티다.
    /// 이게 없으면 "계속 누르고 있기"가 최적 전략이 되어 설계가 무너진다.
    ///
    /// 실제 성공 판정은 PlayerHealth 가 한다 (맞아 봐야 알 수 있으므로).
    /// 이 상태는 **판정 창을 열고 닫는 일**만 한다.
    /// </summary>
    public class ParryState : PlayerState
    {
        private float _elapsed;

        /// <summary>이번 패링이 성공했는가. PlayerHealth 가 켜 준다.</summary>
        public bool Succeeded;

        public ParryState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Parry;

        /// <summary>
        /// 패링에 실패한 상태로 맞으면 경직으로 끊긴다.
        /// 성공한 경우에는 PlayerHealth 가 애초에 피해 처리를 하지 않으므로 여기까지 오지 않는다.
        /// </summary>
        public override bool InterruptedByHit => true;

        public override void Enter()
        {
            _elapsed = 0f;
            Succeeded = false;
            Ctx.IsParryActive = false;
        }

        public override PlayerStateId? Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            // 판정 창을 열고 닫는다.
            Ctx.IsParryActive =
                _elapsed >= Ctx.Stats.parryInputDelay &&
                _elapsed < Ctx.Stats.ParryActiveEnd;

            // 성공했으면 후딜 없이 바로 복귀한다. 성공에 벌을 줄 이유가 없다.
            if (Succeeded)
            {
                return Ctx.IsGrounded ? PlayerStateId.Grounded : PlayerStateId.Airborne;
            }

            // 허공에 쳤으면 후딜까지 버틴다.
            if (_elapsed < Ctx.Stats.ParryTotalDuration) return null;

            return Ctx.IsGrounded ? PlayerStateId.Grounded : PlayerStateId.Airborne;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // 패링 중에는 지상에서 제자리에 선다.
            // 움직이면서 막을 수 있으면 위치 선정이라는 판단이 사라진다.
            if (Ctx.IsGrounded) Ctx.SetHorizontalVelocity(0f);
        }

        public override void Exit()
        {
            Ctx.IsParryActive = false;
        }
    }
}
