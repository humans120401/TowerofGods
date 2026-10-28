using RT.Core.Combat;
using RT.Data;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>보스의 상태. 플레이어 쪽 PlayerStateId 와 같은 역할이다.</summary>
    public enum BossStateId
    {
        Idle,        // 쓸 패턴이 없다. 접근하거나 기다린다
        Telegraph,   // 패턴 시전 중 (선딜 · 판정 · 후딜 전부 포함)
        Staggered,   // 패링당해 굳었다 — 플레이어의 딜 타임
        PhaseShift,  // 페이즈 전환 연출 중
        Dead,
    }

    /// <summary>
    /// 보스를 조립하고 굴리는 유일한 MonoBehaviour.
    ///
    /// 하는 일: 패턴 고르기 → 러너에 넘기기 → 쿨타임 돌리기. 그게 전부다.
    /// 패턴이 **무엇을 하는지는 실행기**가, **언제 무엇이 나가는지는 러너**가 안다.
    /// 그래서 보스 다섯을 올려도 이 파일은 거의 그대로다 — 늘어나는 건 에셋뿐이다.
    ///
    /// DDA 는 아직 붙지 않았다. 6단계에서 Tuning 을 넣어 주는 쪽만 갈아 끼우면 된다.
    /// 지금은 PatternTuning.Neutral — 즉 기획 값 그대로 돈다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Tower of Gods/보스/보스 브레인")]
    public class BossBrain : MonoBehaviour
    {
        [Header("필수 연결")]

        [Tooltip("보스 수치와 패턴 목록. Create ▸ Tower of Gods ▸ 보스")]
        [SerializeField] private BossData data;

        [Tooltip("때릴 대상. 비워 두면 PlayerHealth 가 붙은 오브젝트를 찾는다")]
        [SerializeField] private Transform target;

        [Tooltip("플레이어가 있는 레이어")]
        [SerializeField] private LayerMask playerLayers = ~0;

        [Header("실행기 프리팹")]

        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject summonPrefab;
        [SerializeField] private GameObject areaPrefab;

        [Header("한 방 상한 기준")]

        [Tooltip("플레이어 최대 체력. DDA 의 '한 방 30% 상한'을 재는 기준이다")]
        [SerializeField][Min(1)] private int playerMaxHealth = 100;

        [Header("디버그")]

        [SerializeField] private bool logPatterns = true;

        private Rigidbody2D _body;
        private BossContext _ctx;
        private PatternRunner _runner;
        private PatternSelector _selector;
        private PatternCooldowns _cooldowns;

        private BossStateId _state = BossStateId.Idle;
        private float _stateTimer;

        public BossContext Context => _ctx;
        public BossStateId State => _state;
        public PatternRunner Runner => _runner;

        /// <summary>지금 패링 신호가 켜져 있는가. 연출이 본다</summary>
        public bool ParrySignalOn => _runner != null && _runner.SignalOn;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();

            if (data == null)
            {
                Debug.LogError(
                    "[BossBrain] 보스 데이터가 비어 있습니다. " +
                    "Boss Brain ▸ Data 에 보스 에셋을 넣어 주세요.", this);
                enabled = false;
                return;
            }

            _ctx = new BossContext(transform, _body, data)
            {
                Target = target,
                PlayerLayers = playerLayers,
                ProjectilePrefab = projectilePrefab,
                SummonPrefab = summonPrefab,
                AreaPrefab = areaPrefab,
                Executors = PatternExecutorSet.Default(),
            };

            _ctx.Parried = Stagger;

            _runner = new PatternRunner(_ctx);
            _selector = new PatternSelector();
            _cooldowns = new PatternCooldowns();

            BossDataValidator.Check(data, this);
        }

        private void Update()
        {
            if (_ctx == null) return;

            float dt = Time.deltaTime;

            _cooldowns.Tick(dt);

            if (_stateTimer > 0f) _stateTimer -= dt;

            switch (_state)
            {
                case BossStateId.Idle: TickIdle(dt); break;
                case BossStateId.Telegraph: TickPattern(dt); break;
                case BossStateId.Staggered: TickTimed(dt); break;
                case BossStateId.PhaseShift: TickTimed(dt); break;
            }
        }

        /// <summary>쓸 패턴을 찾는다. 없으면 걸어서 접근한다.</summary>
        private void TickIdle(float dt)
        {
            if (_stateTimer > 0f)
            {
                Approach();
                return;
            }

            _ctx.FaceTarget();

            var pattern = _selector.Pick(
                data, _ctx.DistanceToTarget, _ctx.Phase, _cooldowns);

            if (pattern == null)
            {
                Approach();
                return;
            }

            StartPattern(pattern);
        }

        private void StartPattern(BossPatternData pattern)
        {
            // DDA 는 6단계에서 붙는다. 지금은 보정 없이 기획 값 그대로다.
            var invocation = _ctx.Invoke(pattern, PatternTuning.Neutral, playerMaxHealth);

            var executor = _ctx.Executors.For(pattern);

            if (executor == null)
            {
                Debug.LogWarning(
                    $"[{data.displayName}] '{pattern.patternId}' ({pattern.kind}) 을 " +
                    "돌릴 실행기가 없습니다. 건너뜁니다.", this);

                _cooldowns.Start(pattern.patternId, 1f);
                return;
            }

            _cooldowns.Start(pattern.patternId, invocation.Tuned.Cooldown);
            _runner.Begin(in invocation, executor);

            _state = BossStateId.Telegraph;

            if (logPatterns)
            {
                Debug.Log(
                    $"[{data.displayName}] {invocation} " +
                    $"(거리 {_ctx.DistanceToTarget:0.0}m, {_ctx.Phase}P)", this);
            }
        }

        private void TickPattern(float dt)
        {
            if (_runner.Tick(dt)) return;

            _state = BossStateId.Idle;
            _stateTimer = data.betweenPatterns;
        }

        private void TickTimed(float dt)
        {
            Stop();

            if (_stateTimer > 0f) return;

            _ctx.Staggered = false;
            _state = BossStateId.Idle;
        }

        private void Approach()
        {
            if (!data.approachWhenIdle || !_ctx.HasTarget || _body == null)
            {
                Stop();
                return;
            }

            _ctx.FaceTarget();

            // 이미 붙어 있으면 더 밀고 들어가지 않는다. 겹쳐 서면 판정이 이상해진다.
            if (_ctx.DistanceToTarget < 1.2f)
            {
                Stop();
                return;
            }

            _body.linearVelocity = new Vector2(
                _ctx.Facing * data.moveSpeed, _body.linearVelocity.y);
        }

        private void Stop()
        {
            if (_body == null) return;

            _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
        }

        // ── 바깥에서 들어오는 사건 ──

        /// <summary>패링당했다. 시전 중이던 패턴은 끊긴다 — 그게 패링의 보상이다.</summary>
        public void Stagger(float seconds)
        {
            if (_state == BossStateId.Dead) return;

            _runner.Cancel();
            Stop();

            _ctx.Staggered = true;
            _state = BossStateId.Staggered;
            _stateTimer = Mathf.Max(0.1f, seconds);
        }

        /// <summary>BossHealth 가 페이즈 전환을 알린다.</summary>
        public void OnPhaseChanged(int phase)
        {
            if (_state == BossStateId.Dead) return;

            _runner.Cancel();
            _cooldowns.ResetAll();
            _selector.Reset();

            _state = BossStateId.PhaseShift;
            _stateTimer = data.phaseTransitionTime;
        }

        public void OnDied()
        {
            _runner.Cancel();
            Stop();

            _state = BossStateId.Dead;
            enabled = false;
        }

        /// <summary>층 진입 · 재도전. 설계서 5.2 — 진입 조건은 항상 같아야 한다.</summary>
        public void ResetForFloor()
        {
            _ctx.Health = data.maxHealth;
            _ctx.Phase = 1;
            _ctx.Staggered = false;
            _ctx.History.Clear();

            _runner.Cancel();
            _cooldowns.ResetAll();
            _selector.Reset();

            _state = BossStateId.Idle;
            _stateTimer = 0f;
            enabled = true;
        }
    }
}
