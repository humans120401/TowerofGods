using RT.Core.Constraints;
using RT.Core.Input;
using RT.Data;
using RT.Player.Systems;
using UnityEngine;

namespace RT.Player
{
    /// <summary>
    /// 상태들이 공유하는 것들을 한 곳에 모은 가방.
    ///
    /// 상태 클래스가 MonoBehaviour를 직접 들고 다니면 서로 얽혀서 테스트가 어려워진다.
    /// 필요한 것만 여기 담아 넘긴다.
    /// </summary>
    public class PlayerContext
    {
        // ── 외부에서 주입 ──
        public readonly Rigidbody2D Body;
        public readonly Transform Transform;
        public readonly PlayerStats Stats;
        public readonly IPlayerInput Input;

        /// <summary>Q · W · E 순서로 들어 있다. 비어 있을 수 있다.</summary>
        public readonly SkillData[] Skills;

        /// <summary>다음에 시전할 스킬. 상태가 담아 두면 SkillState 가 읽는다.</summary>
        public SkillData PendingSkill;

        /// <summary>플레이어 공격이 맞출 레이어</summary>
        public LayerMask EnemyLayers;

        // ── 하위 시스템 ──
        public readonly DashStacks Dash;
        public readonly JumpAssist Jump;
        public readonly UltimateGauge Gauge;
        public readonly Cooldowns Cooldowns;

        /// <summary>무적. 주체별로 따로 켜고 끈다 — Invincibility 주석 참고.</summary>
        public readonly Invincibility Invincible = new Invincibility();

        // ── 매 프레임 갱신 ──

        public bool IsGrounded;
        public bool WasGrounded;

        /// <summary>바라보는 방향. -1(왼쪽) 또는 1(오른쪽)</summary>
        public float Facing = 1f;

        /// <summary>
        /// 무적인가. 대시 · 피격 · 시전 · 궁극기 중 하나라도 켜 두면 true.
        /// 켜고 끄는 건 Invincible 로 한다 — 여기에 직접 쓸 수 없다.
        /// </summary>
        public bool IsInvincible => Invincible.IsActive;

        /// <summary>패링 판정이 열려 있는가. ParryState가 켜고 끈다.</summary>
        public bool IsParryActive;

        public float DashDirection = 1f;

        // ── 체력 ──

        public int Health;
        public bool IsDead => Health <= 0;

        // ── 피격 정보 (HitState가 읽는다) ──

        /// <summary>마지막으로 맞은 공격의 넉백 방향</summary>
        public float LastHitDirection = 1f;

        /// <summary>마지막으로 맞은 공격의 넉백 세기</summary>
        public int LastHitKnockback;

        /// <summary>
        /// 보스·디버프가 거는 제약. null이면 제약 없음.
        /// </summary>
        public IPlayerConstraint Constraint;

        public PlayerContext(
            Rigidbody2D body,
            Transform transform,
            PlayerStats stats,
            IPlayerInput input,
            SkillData[] skills)
        {
            Body = body;
            Transform = transform;
            Stats = stats;
            Input = input;
            Skills = skills ?? new SkillData[3];

            Health = stats.maxHealth;

            Dash = new DashStacks(
                stats.dashMaxStacks, stats.dashRechargeTime, stats.dashMinInterval);

            Jump = new JumpAssist(stats.coyoteTime, stats.jumpBufferTime);

            Gauge = new UltimateGauge(
                stats.gaugeMax, stats.gaugeParryGain, stats.gaugeDamagePer100,
                stats.gaugeNaturalRate, stats.gaugeNaturalDelay);

            Cooldowns = new Cooldowns();
        }

        /// <summary>슬롯에 해당하는 스킬. 없으면 null.</summary>
        public SkillData SkillOf(SkillSlot slot)
        {
            int i = (int)slot;
            return (Skills != null && i < Skills.Length) ? Skills[i] : null;
        }

        // ── 제약이 걸린 실효값 ──
        // Constraint가 null일 수 있으므로 여기서 한 번에 처리한다.

        public bool DashBlocked => Constraint != null && Constraint.DashBlocked;
        public bool AttackBlocked => Constraint != null && Constraint.AttackBlocked;
        public bool ParryBlocked => Constraint != null && Constraint.ParryBlocked;

        public float MoveSpeedMultiplier =>
            Constraint == null ? 1f : Constraint.MoveSpeedMultiplier;

        public float DashRechargeMultiplier =>
            Constraint == null ? 1f : Constraint.DashRechargeMultiplier;

        public float DamageDealtMultiplier =>
            Constraint == null ? 1f : Constraint.DamageDealtMultiplier;

        public float CurrentMoveSpeed =>
            (IsGrounded ? Stats.moveSpeed : Stats.airMoveSpeed) * MoveSpeedMultiplier;

        public bool JustLanded => IsGrounded && !WasGrounded;

        /// <summary>계수를 실제 피해로 바꾼다. 제약(공격력 감소 디버프)이 반영된다.</summary>
        public float DamageFrom(float coefficient) =>
            Stats.baseAttackPower * coefficient * DamageDealtMultiplier;

        public void UpdateFacing(float horizontal)
        {
            if (Mathf.Abs(horizontal) < 0.01f) return;

            Facing = Mathf.Sign(horizontal);

            var scale = Transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Facing;
            Transform.localScale = scale;
        }

        public void SetHorizontalVelocity(float vx) =>
            Body.linearVelocity = new Vector2(vx, Body.linearVelocity.y);

        public void SetVerticalVelocity(float vy) =>
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, vy);
    }
}
