namespace RT.Boss
{
    /// <summary>
    /// 패턴 하나를 타임라인대로 돌린다. 진행 상태는 전부 여기 있다.
    ///
    ///   0 ─── 선딜 ───┬─ 1타 ─┬─ 2타 ─ … ─ n타 ─── 후딜 ─── 끝
    ///                 └ 간격 ─┘
    ///
    /// 실행기가 상태를 갖지 않는 이유가 이 분리다.
    /// 러너를 여러 개 두면 같은 패턴을 **겹쳐서** 돌릴 수 있다 —
    /// 카로스의 환영과 파네스의 분신이 그걸 요구한다.
    ///
    /// **패링 신호 0.3초는 러너가 책임진다** (설계서 4.1).
    /// 패링 가능 패턴은 판정 0.3초 전에 반드시 시각 신호를 낸다.
    /// DDA 가 선딜을 ×0.80 까지 줄여도 하한 0.4초가 있어 신호 여유가 남는다.
    /// </summary>
    public class PatternRunner
    {
        /// <summary>패링 가능 패턴이 판정 몇 초 전에 신호를 내는가 (설계서 4.1 — 고정값)</summary>
        public const float ParrySignalLead = 0.3f;

        private readonly BossContext _ctx;

        private PatternExecutor _executor;
        private PatternInvocation _invocation;
        private float _elapsed;
        private int _strikesDone;
        private bool _signalled;

        public PatternRunner(BossContext ctx)
        {
            _ctx = ctx;
        }

        public bool IsRunning { get; private set; }

        public PatternInvocation Current => _invocation;

        /// <summary>몇 타가 실제로 나갔나. 텔레메트리의 strikes 가 된다</summary>
        public int StrikesDone => _strikesDone;

        /// <summary>패링 신호가 켜져 있는가. 연출이 이걸 보고 번쩍인다</summary>
        public bool SignalOn { get; private set; }

        public void Begin(in PatternInvocation invocation, PatternExecutor executor)
        {
            _invocation = invocation;
            _executor = executor;
            _elapsed = 0f;
            _strikesDone = 0;
            _signalled = false;
            SignalOn = false;
            IsRunning = true;

            _ctx.History.Record(in _invocation);
            _executor?.OnBegin(_ctx, in _invocation);
        }

        /// <summary>한 프레임 진행. 끝났으면 false 를 돌려준다.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning) return false;

            _elapsed += deltaTime;

            var tuned = _invocation.Tuned;

            UpdateParrySignal(tuned.Telegraph);

            // 판정 — 선딜이 끝나는 순간 1타, 그 뒤로는 간격마다 한 타씩.
            while (_strikesDone < tuned.RepeatCount)
            {
                float strikeAt = tuned.Telegraph + _strikesDone * tuned.RepeatInterval;

                if (_elapsed < strikeAt) break;

                _executor?.OnStrike(_ctx, in _invocation, _strikesDone);
                _strikesDone++;
            }

            _executor?.OnTick(_ctx, in _invocation, _elapsed);

            if (_elapsed < tuned.TotalDuration) return true;

            Finish();
            return false;
        }

        /// <summary>
        /// 패링 가능 패턴은 첫 판정 0.3초 전부터 신호를 켠다.
        ///
        /// 신호가 없으면 패링은 외우기(암기)가 되고, 처음 보는 패턴은 무조건 맞는다.
        /// 그러면 "보고 반응한다"는 설계가 "몇 번 죽어 보고 외운다"로 바뀐다.
        /// </summary>
        private void UpdateParrySignal(float telegraph)
        {
            if (_invocation.Pattern == null || !_invocation.Pattern.parryable) return;

            bool shouldBeOn = _elapsed >= telegraph - ParrySignalLead && _strikesDone == 0;

            if (shouldBeOn && !_signalled) _signalled = true;

            SignalOn = shouldBeOn;
        }

        /// <summary>패링 경직 · 페이즈 전환 · 사망으로 중간에 끊겼다.</summary>
        public void Cancel()
        {
            if (!IsRunning) return;

            _executor?.OnCancel(_ctx, in _invocation);
            Stop();
        }

        private void Finish()
        {
            _executor?.OnEnd(_ctx, in _invocation);
            Stop();
        }

        private void Stop()
        {
            IsRunning = false;
            SignalOn = false;
            _executor = null;
        }
    }
}
