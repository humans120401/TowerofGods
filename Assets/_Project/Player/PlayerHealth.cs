using System;
using RT.Core.Combat;
using UnityEngine;

namespace RT.Player
{
    /// <summary>
    /// 플레이어가 맞는 쪽을 담당한다. 보스는 IAttackReceiver 만 알고 이 클래스를 모른다.
    ///
    /// **판정 순서가 설계의 핵심이다.**
    ///
    ///   1) 죽었으면            → Ignored
    ///   2) 회피 불가 공격이 아니고 무적이면 → Ignored
    ///   3) 패링 창이 열려 있고 **패링 가능 패턴**이면 → Parried
    ///   4) 그 외                → Hit
    ///
    /// 3번의 "패링 가능 패턴이면" 이 빠지면 패링 한 번으로 모든 공격이 막힌다.
    /// 그러면 "보스 패턴의 35~40%만 패링 가능" 이라는 설계 전체가 무의미해진다.
    /// 팀 프로젝트가 정확히 그 상태였다.
    /// </summary>
    [RequireComponent(typeof(PlayerBrain))]
    [AddComponentMenu("Tower of Gods/플레이어/플레이어 체력")]
    public class PlayerHealth : MonoBehaviour, IAttackReceiver
    {
        [Header("디버그")]
        [SerializeField] private bool logHits = true;

        private PlayerBrain _brain;

        /// <summary>(현재 체력, 최대 체력)</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>패링에 성공했다. 보스 경직 시간을 함께 넘긴다.</summary>
        public event Action<float> Parried;

        public event Action Died;

        private void Awake()
        {
            _brain = GetComponent<PlayerBrain>();
        }

        public HitResult ReceiveHit(in HitInfo info)
        {
            var ctx = _brain != null ? _brain.Context : null;

            if (ctx == null) return HitResult.Ignored;
            if (ctx.IsDead) return HitResult.Ignored;

            // ── 무적 ──
            // 회피 불가 공격은 무적을 뚫는다.
            if (ctx.IsInvincible && !info.IgnoresDodge)
            {
                Log(info, HitResult.Ignored, "무적");
                return HitResult.Ignored;
            }

            // ── 패링 ──
            // 패링 창이 열려 있어도 '패링 가능' 패턴이 아니면 막히지 않는다.
            if (ctx.IsParryActive && info.Parryable)
            {
                ctx.Gauge.OnParrySuccess();
                _brain.NotifyParrySuccess();

                HitStop.Play(ctx.Stats.hitStopParry);
                Parried?.Invoke(info.ParryStaggerTime);

                Log(info, HitResult.Parried, $"게이지 +{ctx.Stats.gaugeParryGain}");
                return HitResult.Parried;
            }

            // ── 피격 ──
            int damage = Mathf.Max(1, Mathf.RoundToInt(info.Damage));
            ctx.Health = Mathf.Max(0, ctx.Health - damage);

            ctx.LastHitDirection = info.Direction;
            ctx.LastHitKnockback = (int)info.Knockback;

            HealthChanged?.Invoke(ctx.Health, ctx.Stats.maxHealth);

            _brain.NotifyHit();

            if (ctx.IsDead)
            {
                Died?.Invoke();
                Log(info, HitResult.Hit, "사망");
            }
            else
            {
                Log(info, HitResult.Hit, $"남은 체력 {ctx.Health}");
            }

            return HitResult.Hit;
        }

        /// <summary>회복. 포션·로비 복귀에서 쓴다.</summary>
        public void Heal(int amount)
        {
            var ctx = _brain != null ? _brain.Context : null;
            if (ctx == null || ctx.IsDead) return;

            ctx.Health = Mathf.Min(ctx.Stats.maxHealth, ctx.Health + amount);
            HealthChanged?.Invoke(ctx.Health, ctx.Stats.maxHealth);
        }

        private void Log(in HitInfo info, HitResult result, string note)
        {
            if (!logHits) return;

            string parryTag = info.Parryable ? "패링가능" : "패링불가";
            string dodgeTag = info.IgnoresDodge ? " 회피불가" : "";

            Debug.Log(
                $"[피격] {info.PatternId} {info.HitIndex + 1}타 → <b>{result}</b> " +
                $"({parryTag}{dodgeTag}, 피해 {info.Damage:0.#}) {note}", this);
        }
    }
}
