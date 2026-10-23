namespace RT.Core.Input
{
    /// <summary>
    /// 플레이어 입력의 유일한 창구.
    ///
    /// **플레이어 로직은 UnityEngine.Input을 직접 쓰지 않는다.** 반드시 이 인터페이스를 거친다.
    ///
    /// 왜:
    ///   1) 사람 말고도 플레이할 수 있어야 한다.
    ///      DDA 웜 스타트 초기값을 만들려면 사람 12명 × 10전이 필요한데, 그게 일정 병목이다.
    ///      입력이 인터페이스면 초보/중수/고수 페르소나 에이전트가 수천 판을 돌릴 수 있다.
    ///   2) 입력을 기록·재생하면 버그를 정확히 재현할 수 있다.
    ///   3) 테스트에서 가짜 입력을 주입할 수 있다.
    ///
    /// 구현체: KeyboardInput (사람) · ReplayInput (재생) · AgentInput (에이전트)
    ///
    /// 설계 규칙:
    ///   - Pressed 는 "이번 프레임에 눌렸다"(엣지), Held 는 "지금 눌려 있다"(레벨).
    ///   - 구현체는 프레임마다 Poll()에서 상태를 갱신하고, 그 프레임 동안 값을 고정한다.
    ///     같은 프레임에 두 번 읽어도 같은 값이 나와야 한다.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>좌우 이동. -1 ~ 1</summary>
        float Horizontal { get; }

        /// <summary>이번 프레임에 점프를 눌렀다</summary>
        bool JumpPressed { get; }

        /// <summary>점프를 누르고 있다 (가변 점프 판정용)</summary>
        bool JumpHeld { get; }

        /// <summary>이번 프레임에 점프를 뗐다 (가변 점프 감쇠 시점)</summary>
        bool JumpReleased { get; }

        /// <summary>이번 프레임에 대시를 눌렀다</summary>
        bool DashPressed { get; }

        /// <summary>이번 프레임에 패링을 눌렀다</summary>
        bool ParryPressed { get; }

        /// <summary>이번 프레임에 기본 공격을 눌렀다</summary>
        bool AttackPressed { get; }

        /// <summary>이번 프레임에 Q(돌진 베기)를 눌렀다</summary>
        bool SkillQPressed { get; }

        /// <summary>이번 프레임에 W(회전 베기)를 눌렀다</summary>
        bool SkillWPressed { get; }

        /// <summary>이번 프레임에 E(처형 베기)를 눌렀다</summary>
        bool SkillEPressed { get; }

        /// <summary>이번 프레임에 궁극기를 눌렀다</summary>
        bool UltimatePressed { get; }

        /// <summary>이번 프레임에 포션을 눌렀다</summary>
        bool PotionPressed { get; }

        /// <summary>
        /// 프레임당 한 번 호출해 입력 상태를 갱신한다.
        /// 이걸 부르는 쪽은 플레이어가 아니라 상위(PlayerBrain)다.
        /// </summary>
        void Poll(float deltaTime);
    }
}
