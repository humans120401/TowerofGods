namespace RT.Player.Systems
{
    /// <summary>
    /// 스택형 대시의 충전·소모 규칙. 전투방식설계서 2.3
    ///
    ///   스택 2개 · 각 2.5초에 하나씩 충전 · 연속 사용 최소 간격 0.3초
    ///
    /// 설계서가 스태미너를 명시적으로 뺐다. 관리할 게이지가 3개(체력·스태미너·궁극기)가 되고,
    /// 패링에 스태미너 비용을 매기면 "패링을 장려하는 설계"와 정면으로 충돌하기 때문이다.
    /// 스택형은 "급할 때 2번 연속 구르고 이후 5초를 감수한다"는 선택을 만든다.
    ///
    /// **UnityEngine을 쓰지 않는다.** 시간은 바깥에서 넣어 준다.
    /// 덕분에 EditMode 테스트로 충전·간격 규칙을 그대로 검증할 수 있다.
    /// </summary>
    public class DashStacks
    {
        private readonly int _maxStacks;
        private readonly float _rechargeTime;
        private readonly float _minInterval;

        private int _stacks;
        private float _rechargeTimer;

        /// <summary>마지막 대시로부터 흐른 시간. 처음에는 충분히 큰 값으로 둬서 바로 쓸 수 있게 한다.</summary>
        private float _sinceLastDash;

        public DashStacks(int maxStacks, float rechargeTime, float minInterval)
        {
            _maxStacks = maxStacks < 1 ? 1 : maxStacks;
            _rechargeTime = rechargeTime < 0.01f ? 0.01f : rechargeTime;
            _minInterval = minInterval < 0f ? 0f : minInterval;

            _stacks = _maxStacks;
            _rechargeTimer = _rechargeTime;
            _sinceLastDash = float.MaxValue / 2f;
        }

        /// <summary>현재 보유 스택</summary>
        public int Current => _stacks;

        /// <summary>최대 스택</summary>
        public int Max => _maxStacks;

        /// <summary>다음 스택이 찰 때까지 남은 비율 0~1 (UI용)</summary>
        public float RechargeProgress =>
            _stacks >= _maxStacks ? 1f : 1f - (_rechargeTimer / _rechargeTime);

        /// <summary>
        /// 지금 대시를 쓸 수 있는가.
        /// 스택이 있어야 하고, 직전 대시로부터 최소 간격이 지나 있어야 한다.
        /// </summary>
        public bool CanDash => _stacks > 0 && _sinceLastDash >= _minInterval;

        /// <summary>프레임마다 호출한다.</summary>
        public void Tick(float deltaTime, float rechargeMultiplier = 1f)
        {
            _sinceLastDash += deltaTime;

            if (_stacks >= _maxStacks)
            {
                // 꽉 찼으면 타이머를 되감아 둔다.
                // 안 그러면 한 칸 쓰자마자 즉시 하나가 차 버린다.
                _rechargeTimer = _rechargeTime;
                return;
            }

            // rechargeMultiplier 는 디버프용.
            // 에텔로스 전기 피격 시 충전을 느리게 하는 식으로 쓴다 (IPlayerConstraint).
            if (rechargeMultiplier <= 0f) return;

            _rechargeTimer -= deltaTime * rechargeMultiplier;

            while (_rechargeTimer <= 0f && _stacks < _maxStacks)
            {
                _stacks++;
                _rechargeTimer += _rechargeTime;
            }

            if (_stacks >= _maxStacks) _rechargeTimer = _rechargeTime;
        }

        /// <summary>
        /// 스택을 하나 쓴다. 쓸 수 없는 상태면 아무것도 하지 않고 false를 돌려준다.
        /// </summary>
        public bool TryConsume()
        {
            if (!CanDash) return false;

            _stacks--;
            _sinceLastDash = 0f;
            return true;
        }

        /// <summary>층 진입 시 초기화. 설계서 5.2 — 층 진입 조건은 항상 같아야 한다.</summary>
        public void ResetFull()
        {
            _stacks = _maxStacks;
            _rechargeTimer = _rechargeTime;
            _sinceLastDash = float.MaxValue / 2f;
        }
    }
}
