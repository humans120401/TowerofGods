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
