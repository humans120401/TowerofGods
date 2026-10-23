using RT.Core.Input;
using RT.Data;
using RT.Player.States;
using UnityEngine;

namespace RT.Player
{
    /// <summary>
    /// 플레이어를 조립하고 굴리는 유일한 MonoBehaviour.
    ///
    /// 하는 일은 네 가지뿐이다:
    ///   1) 입력을 폴링한다
    ///   2) 접지 판정을 한다
    ///   3) 하위 시스템(대시 스택·점프 보조)을 틱한다
    ///   4) 상태 기계를 틱한다
    ///
    /// 게임 로직은 전부 상태 클래스와 시스템 클래스에 있다.
    /// 이 파일이 커지기 시작하면 뭔가 잘못 들어온 것이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("역전의 탑/플레이어/플레이어 브레인")]
    public class PlayerBrain : MonoBehaviour
    {
        [Header("필수 연결")]

        [Tooltip("모든 수치가 들어 있는 에셋. Create ▸ 역전의 탑 ▸ 플레이어 스탯")]
        [SerializeField] private PlayerStats stats;

        [Tooltip("입력 구현체. 비워 두면 같은 오브젝트에서 찾는다")]
        [SerializeField] private MonoBehaviour inputSource;

        [Header("접지 판정")]

        [Tooltip("발밑 기준점. 비워 두면 콜라이더 아래쪽을 쓴다")]
        [SerializeField] private Transform groundCheck;

        [Tooltip("지면으로 볼 레이어")]
        [SerializeField] private LayerMask groundLayer = ~0;

        [Tooltip("접지 판정 상자 크기")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.12f);

        [Header("디버그")]

        [SerializeField] private bool logStateChanges;
        [SerializeField] private bool drawGroundCheck = true;

        private Rigidbody2D _body;
        private IPlayerInput _input;
        private PlayerContext _ctx;
        private PlayerStateMachine _sm;

        /// <summary>디버그 화면·UI가 읽는다</summary>
        public PlayerContext Context => _ctx;

        /// <summary>디버그 화면·UI가 읽는다</summary>
        public PlayerStateId CurrentState => _sm?.CurrentId ?? PlayerStateId.Grounded;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();

            if (stats == null)
            {
                Debug.LogError(
                    "[PlayerBrain] Stats 가 비어 있습니다. " +
                    "Create ▸ 역전의 탑 ▸ 플레이어 스탯 으로 만들어 연결해 주세요.", this);
                enabled = false;
                return;
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

            // 2D 플랫포머 기본 설정 — 넘어지지 않게 회전을 막는다.
            _body.freezeRotation = true;
            _body.gravityScale = stats.GravityScale;

            _ctx = new PlayerContext(_body, transform, stats, _input);
            _sm = new PlayerStateMachine();

            _sm.Register(new GroundedState(_ctx));
            _sm.Register(new AirborneState(_ctx));
            _sm.Register(new DashState(_ctx));

            if (logStateChanges)
            {
                _sm.StateChanged += (from, to) =>
                    Debug.Log($"[Player] {from} → {to}", this);
            }

            _sm.Start(PlayerStateId.Grounded);
        }

        private IPlayerInput ResolveInput()
        {
            if (inputSource is IPlayerInput assigned) return assigned;

            // 같은 오브젝트에 붙은 구현체를 찾는다.
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb is IPlayerInput found) return found;
            }

            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 순서가 중요하다.
            //   입력 → 접지 → 점프 보조 → 상태
            // 점프 보조가 '이번 프레임의' 입력과 접지를 봐야 하기 때문이다.
            _input.Poll(dt);

            _ctx.WasGrounded = _ctx.IsGrounded;
            _ctx.IsGrounded = CheckGrounded();

            _ctx.Dash.Tick(dt, _ctx.DashRechargeMultiplier);
            _ctx.Jump.Tick(dt, _ctx.IsGrounded, _input.JumpPressed);

            _sm.Tick(dt);
        }

        private void FixedUpdate()
        {
            _sm.FixedTick(Time.fixedDeltaTime);
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

            Vector3 center = groundCheck != null
                ? groundCheck.position
                : transform.position;

            Gizmos.color = Application.isPlaying && _ctx != null && _ctx.IsGrounded
                ? Color.green
                : Color.red;

            Gizmos.DrawWireCube(center, new Vector3(groundCheckSize.x, groundCheckSize.y, 0f));
        }
    }
}
