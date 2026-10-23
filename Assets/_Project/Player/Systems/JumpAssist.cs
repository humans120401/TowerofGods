namespace RT.Player.Systems
{
    /// <summary>
    /// 점프 관용 장치 — 코요테 타임과 점프 버퍼. 전투방식설계서 2.2
    ///
    ///   코요테 타임 0.1s  발판을 벗어난 뒤에도 0.1초 동안 점프를 받아준다
    ///   점프 버퍼  0.12s  착지 0.12초 전에 누른 점프를 착지 순간 실행한다
    ///
    /// 둘 다 "입력은 맞았는데 한 프레임 차이로 씹히는" 상황을 없앤다.
    /// 플레이어는 이게 있는 줄 모르지만, 없으면 조작이 뻣뻣하다고 느낀다.
    ///
    /// **UnityEngine을 쓰지 않는다.** 입력과 시간은 바깥에서 넣어 준다.
    /// </summary>
    public class JumpAssist
    {
        private readonly float _coyoteTime;
        private readonly float _bufferTime;

        private float _coyoteTimer;
        private float _bufferTimer;

        public JumpAssist(float coyoteTime, float jumpBufferTime)
        {
            _coyoteTime = coyoteTime < 0f ? 0f : coyoteTime;
            _bufferTime = jumpBufferTime < 0f ? 0f : jumpBufferTime;
        }

        /// <summary>코요테 유예가 남아 있다 (디버그·UI용)</summary>
        public bool HasCoyoteGrace => _coyoteTimer > 0f;

        /// <summary>버퍼에 점프 입력이 남아 있다 (디버그용)</summary>
        public bool HasBufferedJump => _bufferTimer > 0f;

        /// <summary>
        /// 프레임마다 호출한다.
        /// </summary>
        /// <param name="deltaTime">경과 시간</param>
        /// <param name="isGrounded">지금 지상에 있는가</param>
        /// <param name="jumpPressed">이번 프레임에 점프를 눌렀는가</param>
        public void Tick(float deltaTime, bool isGrounded, bool jumpPressed)
        {
            // 코요테 — 지상이면 가득 채우고, 공중이면 줄여 나간다.
            if (isGrounded) _coyoteTimer = _coyoteTime;
            else _coyoteTimer -= deltaTime;

            // 버퍼 — 누른 순간 채우고, 아니면 줄여 나간다.
            if (jumpPressed) _bufferTimer = _bufferTime;
            else _bufferTimer -= deltaTime;
        }

        /// <summary>
        /// 지금 점프를 발동해야 하는가. 조건이 맞으면 **소모하고** true를 돌려준다.
        ///
        /// 소모가 중요하다. 안 비우면 같은 입력 하나로 공중에서 한 번 더 뛴다.
        /// </summary>
        public bool TryConsumeJump()
        {
            if (_bufferTimer <= 0f) return false;
            if (_coyoteTimer <= 0f) return false;

            _bufferTimer = 0f;
            _coyoteTimer = 0f;
            return true;
        }

        /// <summary>
        /// 점프가 아닌 이유로 공중에 뜬 경우(대시·넉백 등) 코요테를 끊는다.
        /// 안 끊으면 대시 직후에 공중 점프가 나온다.
        /// </summary>
        public void CancelCoyote()
        {
            _coyoteTimer = 0f;
        }
    }
}
