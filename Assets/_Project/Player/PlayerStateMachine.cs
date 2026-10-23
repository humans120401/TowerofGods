using System;
using System.Collections.Generic;
using RT.Player.States;

namespace RT.Player
{
    /// <summary>
    /// 플레이어 상태 기계.
    ///
    /// 상태는 전환을 '요청'만 하고(Tick의 반환값), 실제 교체는 여기서 한다.
    /// 상태가 서로를 직접 부르지 않으므로 의존이 생기지 않는다.
    /// </summary>
    public class PlayerStateMachine
    {
        private readonly Dictionary<PlayerStateId, PlayerState> _states =
            new Dictionary<PlayerStateId, PlayerState>();

        private PlayerState _current;

        /// <summary>상태가 바뀔 때마다 (이전, 다음)으로 호출된다. 디버그·애니메이션용.</summary>
        public event Action<PlayerStateId, PlayerStateId> StateChanged;

        public PlayerStateId CurrentId => _current?.Id ?? PlayerStateId.Grounded;

        /// <summary>지금 상태가 피격으로 끊기는가. PlayerBrain 이 본다.</summary>
        public bool CurrentInterruptedByHit => _current == null || _current.InterruptedByHit;

        /// <summary>
        /// 바깥에서 강제로 상태를 바꾼다. 피격·사망처럼 상태가 거부할 수 없는 경우에만 쓴다.
        /// 보통의 전환은 Tick 의 반환값으로 한다.
        /// </summary>
        public void Force(PlayerStateId next) => Transition(next);

        public void Register(PlayerState state)
        {
            _states[state.Id] = state;
        }

        public void Start(PlayerStateId id)
        {
            _current = _states[id];
            _current.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (_current == null) return;

            var next = _current.Tick(deltaTime);

            if (next.HasValue && next.Value != _current.Id)
            {
                Transition(next.Value);
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            _current?.FixedTick(fixedDeltaTime);
        }

        private void Transition(PlayerStateId next)
        {
            if (!_states.TryGetValue(next, out var target)) return;

            var from = _current.Id;

            _current.Exit();
            _current = target;
            _current.Enter();

            StateChanged?.Invoke(from, next);
        }
    }
}
