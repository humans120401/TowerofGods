using RT.Core.Combat;
using UnityEngine;

namespace RT.Game
{
    /// <summary>
    /// 연습용 허수아비. 2단계에서 전투를 시험하는 상대다.
    ///
    /// 두 가지를 한다:
    ///   1) 맞아 준다 — 피해를 받고 숫자를 띄운다
    ///   2) 때린다   — 주기적으로 공격해 패링을 연습시킨다
    ///
    /// **(패링 가능 × 회피 불가) 네 조합을 돌아가며 쏜다** — DummyAttack 참고.
    /// 이게 2단계의 핵심 검증이다.
    /// 패링 불가 공격을 패링했는데 막히면 설계가 안 지켜지고 있는 것이다.
    ///
    /// 보스가 아니다. 3단계의 보스 프레임워크와는 별개이고, 그때 가서 지워도 된다.
    /// </summary>
    [AddComponentMenu("Tower of Gods/디버그/연습용 허수아비")]
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [Header("체력")]
        [SerializeField] private int maxHealth = 2000;

        [Tooltip("켜면 죽지 않는다. 계속 때려 보기 좋다")]
        [SerializeField] private bool invincible = true;

        [Header("공격")]

        [Tooltip("끄면 가만히 서서 맞기만 한다")]
        [SerializeField] private bool attacks = true;

        [Tooltip("공격 간격(초)")]
        [SerializeField][Min(0.5f)] private float interval = 3f;

        [Tooltip("공격 예고 시간(초). 이 동안 색이 바뀐다")]
        [SerializeField][Min(0.1f)] private float telegraph = 0.8f;

        [Tooltip("피해량. 설계서 기준 보스 일반 공격 = 20")]
        [SerializeField][Min(1)] private float damage = 20f;

        [Tooltip("판정 범위(m)")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(2.5f, 2.5f);

        [Tooltip("플레이어가 있는 레이어")]
        [SerializeField] private LayerMask playerLayers = ~0;

        [Header("무엇을 쏠 것인가")]

        [Tooltip("켠 것만 순서대로 돌아가며 쏜다. 넷 다 켜 두는 게 기본")]
        [SerializeField] private bool[] enabledKinds = { true, true, true, true };

        private int _health;
        private float _timer;
        private bool _telegraphing;
        private int _cycle = -1;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private DummyAttack _next;

        public bool IsDead => !invincible && _health <= 0;

        private void Awake()
        {
            _health = maxHealth;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;

            _timer = interval;
        }

        public void TakeDamage(in HitInfo info)
        {
            if (IsDead) return;

            _health -= Mathf.Max(1, Mathf.RoundToInt(info.Damage));

            Debug.Log(
                $"[허수아비] {info.PatternId} 피격 — 피해 {info.Damage:0.#}, " +
                $"남은 체력 {Mathf.Max(0, _health)}", this);
        }

        private void Update()
        {
            if (!attacks) return;

            _timer -= Time.deltaTime;

            if (!_telegraphing && _timer <= telegraph)
            {
                BeginTelegraph();
            }

            if (_timer <= 0f)
            {
                Strike();
                _timer = interval;
                _telegraphing = false;
                if (_sr != null) _sr.color = _baseColor;
            }
        }

        private void BeginTelegraph()
        {
            _telegraphing = true;
            _next = PickNextAttack();

            // 색으로 공격의 성격을 알린다.
            // 설계서 4.1: 패링 가능 패턴은 판정 0.3초 전에 반드시 시각 신호를 낸다.
            if (_sr != null) _sr.color = _next.TelegraphColor;
        }

        /// <summary>켜 둔 종류를 순서대로 하나씩. 무작위보다 검증하기 쉽다.</summary>
        private DummyAttack PickNextAttack()
        {
            var all = DummyAttack.All;

            for (int tried = 0; tried < all.Length; tried++)
            {
                _cycle = (_cycle + 1) % all.Length;

                if (IsEnabled(_cycle)) return all[_cycle];
            }

            // 전부 꺼 뒀으면 가장 기본인 패링 가능 공격을 쓴다.
            return all[0];
        }

        private bool IsEnabled(int index) =>
            enabledKinds == null ||
            index >= enabledKinds.Length ||
            enabledKinds[index];

        private void Strike()
        {
            var hits = Physics2D.OverlapBoxAll(
                transform.position, hitboxSize, 0f, playerLayers);

            foreach (var col in hits)
            {
                var receiver = col.GetComponentInParent<IAttackReceiver>();
                if (receiver == null) continue;

                float dir = Mathf.Sign(col.transform.position.x - transform.position.x);
                if (Mathf.Approximately(dir, 0f)) dir = 1f;

                var info = new HitInfo(
                    patternId: _next.Label,
                    hitIndex: 0,
                    damage: damage,
                    direction: dir,
                    knockback: KnockbackLevel.Medium,
                    parryable: _next.Parryable,
                    ignoresDodge: _next.IgnoresDodge,
                    parryStaggerTime: 0.6f);

                receiver.ReceiveHit(in info);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawWireCube(transform.position, hitboxSize);
        }
    }
}
