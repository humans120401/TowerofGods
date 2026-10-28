using System;
using RT.Core.Combat;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>
    /// 보스가 맞는 쪽. 체력과 페이즈 전환을 맡는다.
    ///
    /// **페이즈 전환 조건은 DDA 가 건드리지 않는다** (설계서 7.1 고정 항목).
    /// 체력 60%에서 2페이즈로 넘어가는 건 난이도가 아니라 보스의 정체성이다.
    /// 이게 흔들리면 "같은 보스를 상대했다"는 전제가 깨져 실험 결과도 비교할 수 없다.
    /// </summary>
    [RequireComponent(typeof(BossBrain))]
    [AddComponentMenu("Tower of Gods/보스/보스 체력")]
    public class BossHealth : MonoBehaviour, IDamageable
    {
        [Header("디버그")]
        [SerializeField] private bool logHits;

        private BossBrain _brain;

        /// <summary>(현재 체력, 최대 체력)</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>페이즈가 바뀌었다. 인자는 새 페이즈(2)</summary>
        public event Action<int> PhaseChanged;

        public event Action Died;

        private void Awake()
        {
            _brain = GetComponent<BossBrain>();
        }

        public bool IsDead
        {
            get
            {
                var ctx = _brain != null ? _brain.Context : null;
                return ctx == null || ctx.IsDead;
            }
        }

        public void TakeDamage(in HitInfo info)
        {
            var ctx = _brain != null ? _brain.Context : null;
            if (ctx == null || ctx.IsDead) return;

            int damage = Mathf.Max(1, Mathf.RoundToInt(info.Damage));
            ctx.Health = Mathf.Max(0, ctx.Health - damage);

            HealthChanged?.Invoke(ctx.Health, ctx.Data.maxHealth);

            if (logHits)
            {
                Debug.Log(
                    $"[{ctx.Data.displayName}] {info.PatternId} 피격 — " +
                    $"피해 {info.Damage:0.#}, 남은 체력 {ctx.Health}", this);
            }

            if (ctx.Health <= 0)
            {
                _brain.OnDied();
                Died?.Invoke();
                return;
            }

            TryEnterPhaseTwo(ctx);
        }

        /// <summary>
        /// 체력이 기준 아래로 내려가면 2페이즈. 한 번만 일어난다.
        /// 전환 중에는 패턴을 쓰지 않아 플레이어에게 숨 돌릴 틈이 생긴다.
        /// </summary>
        private void TryEnterPhaseTwo(BossContext ctx)
        {
            if (ctx.Phase >= 2) return;
            if (ctx.Health > ctx.Data.PhaseTwoHealth) return;

            ctx.Phase = 2;

            _brain.OnPhaseChanged(2);
            PhaseChanged?.Invoke(2);

            Debug.Log(
                $"[{ctx.Data.displayName}] 2페이즈 진입 " +
                $"(체력 {ctx.Health}/{ctx.Data.maxHealth})", this);
        }
    }
}
