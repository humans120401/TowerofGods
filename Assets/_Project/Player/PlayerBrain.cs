using RT.Core.Input;
using RT.Data;
using RT.Player.States;
using UnityEngine;

namespace RT.Player
{
    /// <summary>
    /// 플레이어를 조립하고 굴리는 유일한 MonoBehaviour.
    ///
    /// 하는 일:
    ///   1) 입력 폴링   2) 접지 판정   3) 하위 시스템 틱   4) 상태 기계 틱
    ///
    /// 게임 로직은 전부 상태 클래스와 시스템 클래스에 있다.
    /// 이 파일이 커지기 시작하면 뭔가 잘못 들어온 것이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Tower of Gods/플레이어/플레이어 브레인")]
    public class PlayerBrain : MonoBehaviour
    {
        [Header("필수 연결")]

        [Tooltip("모든 수치가 들어 있는 에셋. Create ▸ Tower of Gods ▸ 플레이어 스탯")]
        [SerializeField] private PlayerStats stats;

        [Tooltip("Q · W · E 순서로 넣는다. 비워 두면 그 스킬은 안 나간다")]
        [SerializeField] private SkillData[] skills = new SkillData[3];

        [Tooltip("입력 구현체. 비워 두면 같은 오브젝트에서 찾는다")]
        [SerializeField] private MonoBehaviour inputSource;

        [Header("판정 레이어")]

        [Tooltip("플레이어 공격이 맞출 대상")]
        [SerializeField] private LayerMask enemyLayers = ~0;

        [Header("접지 판정")]

        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.9f, 0.1f);

        [Header("디버그")]

        [SerializeField] private bool logStateChanges;
        [SerializeField] private bool drawGroundCheck = true;

        private Rigidbody2D _body;
        private IPlayerInput _input;
        private PlayerContext _ctx;
        private PlayerStateMachine _sm;
        private ParryState _parry;
        private SkillState _skill;

        public PlayerContext Context => _ctx;
        public PlayerStateId CurrentState => _sm?.CurrentId ?? PlayerStateId.Grounded;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();

            if (stats == null)
            {
                // 멈추지 않는다. 설정이 빠졌다고 아무것도 못 하면 원인을 찾기가 더 어렵다.
                stats = ScriptableObject.CreateInstance<PlayerStats>();

                Debug.LogWarning(
                    "[PlayerBrain] Stats 가 비어 있어 설계서 기본값으로 진행합니다.\n" +
                    "제대로 쓰려면 Player Brain ▸ Stats 칸에 " +
                    "Assets/_Project/Data/PlayerStats 를 끌어다 놓으세요.", this);
            }

            _input = ResolveInput();

            if (_input == null)
            {
                Debug.LogError(
                    "[PlayerBrain] IPlayerInput 구현체를 찾지 못했습니다. " +
                    "KeyboardInput 컴포넌트를 같은 오브젝트에 붙여 주세요.", this);
                enabled = false;
                return;
            }

            _body.freezeRotation = true;
            _body.gravityScale = stats.GravityScale;

            _ctx = new PlayerContext(_body, transform, stats, _input, skills)
            {
                EnemyLayers = enemyLayers,
            };

            _parry = new ParryState(_ctx);
            _skill = new SkillState(_ctx);

            _sm = new PlayerStateMachine();
            _sm.Register(new GroundedState(_ctx));
            _sm.Register(new AirborneState(_ctx));
            _sm.Register(new DashState(_ctx));
            _sm.Register(new AttackState(_ctx));
            _sm.Register(_skill);
            _sm.Register(_parry);
            _sm.Register(new HitState(_ctx));
            _sm.Register(new DeadState(_ctx));

            if (logStateChanges)
            {
                _sm.StateChanged += (from, to) => Debug.Log($"[Player] {from} → {to}", this);
            }

            _sm.Start(PlayerStateId.Grounded);
        }

        private IPlayerInput ResolveInput()
        {
            if (inputSource is IPlayerInput assigned) return assigned;

            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb is IPlayerInput found) return found;
            }

            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 순서가 중요하다. 점프 보조가 '이번 프레임의' 입력과 접지를 봐야 한다.
            _input.Poll(dt);

            _ctx.WasGrounded = _ctx.IsGrounded;
            _ctx.IsGrounded = CheckGrounded();

            _ctx.Invincible.Tick(dt);
            _ctx.Dash.Tick(dt, _ctx.DashRechargeMultiplier);
            _ctx.Jump.Tick(dt, _ctx.IsGrounded, _input.JumpPressed);
            _ctx.Cooldowns.Tick(dt);
            _ctx.Gauge.Tick(dt);

            _sm.Tick(dt);
        }

        private void FixedUpdate() => _sm.FixedTick(Time.fixedDeltaTime);

        /// <summary>
        /// PlayerHealth 가 피해를 넣은 뒤 부른다.
        ///
        /// 피격 무적은 경직보다 길다 — 경직 0.15s, 무적 0.35s.
        /// 그 차이 0.2초가 설계서 4.2 의 '대응 창'이다.
        /// 타이머는 Invincibility 가 들고 있으므로 상태가 바뀌어도 살아남는다.
        /// </summary>
        public void NotifyHit()
        {
            _ctx.Invincible.For(
                Systems.InvincibilitySource.Hit, _ctx.Stats.invincibleAfterHit);

            if (_ctx.IsDead)
            {
                _sm.Force(PlayerStateId.Dead);
                return;
            }

            // 지금 상태가 피격으로 끊기는 상태면 경직으로 보낸다.
            if (_sm.CurrentInterruptedByHit)
            {
                // E(처형 베기) 차지 중이었으면 쿨타임을 절반만 돌려준다.
                if (CurrentState == PlayerStateId.Skill) _skill.CancelByHit();

                _sm.Force(PlayerStateId.Hit);
            }
        }

        /// <summary>PlayerHealth 가 패링 성공을 알린다.</summary>
        public void NotifyParrySuccess()
        {
            _parry.Succeeded = true;
        }

        /// <summary>
        /// 층에 (다시) 들어간다. 설계서 5.2 — 층 진입 조건은 항상 같아야 한다.
        ///
        /// 체력 · 쿨타임 · 게이지 · 무적을 전부 되돌리고 지상 대기로 보낸다.
        /// 죽은 상태에서도 부를 수 있어야 하므로 상태를 강제로 바꾼다.
        /// 7단계 로비에서 쓰고, 그전까지는 디버그 창의 F2 가 쓴다.
        /// </summary>
        public void ResetForFloor()
        {
            _ctx.Health = _ctx.Stats.maxHealth;
            _ctx.Invincible.ClearAll();
            _ctx.IsParryActive = false;
            _ctx.Cooldowns.ResetAll();
            _ctx.Gauge.ResetForNewBattle();
            _ctx.PendingSkill = null;

            _sm.Force(PlayerStateId.Grounded);
        }

        private bool CheckGrounded()
        {
            Vector2 center = groundCheck != null
                ? (Vector2)groundCheck.position
                : (Vector2)transform.position;

            return Physics2D.OverlapBox(center, groundCheckSize, 0f, groundLayer) != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGroundCheck) return;

            Vector3 center = groundCheck != null ? groundCheck.position : transform.position;

            Gizmos.color = Application.isPlaying && _ctx != null && _ctx.IsGrounded
                ? Color.green : Color.red;

            Gizmos.DrawWireCube(center, new Vector3(groundCheckSize.x, groundCheckSize.y, 0f));
        }
    }
}
