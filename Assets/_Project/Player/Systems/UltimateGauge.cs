namespace RT.Player.Systems
{
    /// <summary>
    /// 궁극기 게이지. 전투방식설계서 4.4
    ///
    ///   패링 성공      +15      주 수급 수단
    ///   보스에게 준 피해 누적 100당 +3
    ///   자연 충전      0.25/s   **전투 시작 30초 이후부터**
    ///   회피           없음      (v0.3 결정 — 회피는 수급 수단이 아니다)
    ///   피격           감소 없음
    ///
    /// 목표: 보스전 3분에 합계 190~220 → 전투당 2회 사용.
    ///
    /// 자연 충전에 30초 지연을 둔 이유는 "도망만 다녀도 게이지가 찬다"는 문제 때문이다.
    /// 대신 패링을 한 번도 못 해도 자연 충전 + 피해만으로 1회는 쓸 수 있다 (초보자 안전망).
    ///
    /// **UnityEngine을 쓰지 않는다.** 시간은 바깥에서 넣어 준다.
    /// </summary>
    public class UltimateGauge
    {
        private readonly int _max;
        private readonly int _parryGain;
        private readonly int _perHundredDamage;
        private readonly float _naturalRate;
        private readonly float _naturalDelay;

        private float _value;

        /// <summary>전투가 시작된 뒤 흐른 시간. 자연 충전 지연을 재는 데 쓴다.</summary>
        private float _battleTime;

        /// <summary>아직 게이지로 바뀌지 않은 누적 피해. 100을 넘을 때마다 변환한다.</summary>
        private float _pendingDamage;

        public UltimateGauge(
            int max,
            int parryGain,
            int perHundredDamage,
            float naturalRate,
            float naturalDelay)
        {
            _max = max < 1 ? 1 : max;
            _parryGain = parryGain;
            _perHundredDamage = perHundredDamage;
            _naturalRate = naturalRate;
            _naturalDelay = naturalDelay;
        }

        /// <summary>현재 게이지 (정수)</summary>
        public int Current => (int)_value;

        public int Max => _max;

        /// <summary>0~1. UI 게이지바용</summary>
        public float Ratio => _value / _max;

        /// <summary>궁극기를 쓸 수 있는가 — 가득 찼을 때만</summary>
        public bool IsFull => _value >= _max;

        public void Tick(float deltaTime)
        {
            _battleTime += deltaTime;

            // 자연 충전은 전투 시작 30초 뒤부터.
            if (_battleTime < _naturalDelay) return;

            Add(_naturalRate * deltaTime);
        }

        /// <summary>패링에 성공했다</summary>
        public void OnParrySuccess() => Add(_parryGain);

        /// <summary>보스에게 피해를 줬다. 누적 100마다 변환한다.</summary>
        public void OnDamageDealt(float damage)
        {
            if (damage <= 0f) return;

            _pendingDamage += damage;

            while (_pendingDamage >= 100f)
            {
                _pendingDamage -= 100f;
                Add(_perHundredDamage);
            }
        }

        /// <summary>궁극기를 쓴다. 가득 차 있지 않으면 아무 일도 없고 false를 돌려준다.</summary>
        public bool TryConsume()
        {
            if (!IsFull) return false;

            _value = 0f;
            return true;
        }

        /// <summary>보스전이 끝나면 게이지는 초기화된다 (설계서 4.4)</summary>
        public void ResetForNewBattle()
        {
            _value = 0f;
            _battleTime = 0f;
            _pendingDamage = 0f;
        }

        private void Add(float amount)
        {
            _value += amount;
            if (_value > _max) _value = _max;
            if (_value < 0f) _value = 0f;
        }
    }
}
