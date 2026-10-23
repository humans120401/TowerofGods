using RT.Core.Combat;
using RT.Data;
using RT.Player.Systems;
using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// Q · W · E 를 한 상태가 공통으로 다룬다. 차이는 전부 SkillData 에 있다.
    /// 전투방식설계서 3.2
    ///
    ///   Q 돌진 베기  6s   180%  전방 3m 돌진 후 베기   선딜 0.2  총 0.7
    ///   W 회전 베기  10s  120%  자기 중심 반경 2.5m    선딜 0.35 총 0.9
    ///   E 처형 베기  15s  300%  전방 2.5m, 차지 0.7s   선딜 0.7  총 1.3
    ///
    /// 세 스킬을 한 클래스로 묶은 이유: 흐름이 똑같다.
    /// 선딜 → 판정 → 후딜. 다른 건 숫자와 모양뿐이라 코드를 세 벌 둘 이유가 없다.
    /// </summary>
    public class SkillState : PlayerState
    {
        private SkillData _skill;
        private float _elapsed;
        private bool _hitDone;

        public SkillState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Skill;

        /// <summary>E(처형 베기)는 차지 중 피격당하면 취소된다 — 설계서 3.3</summary>
        public override bool InterruptedByHit => _skill != null && _skill.cancelledOnHit;

        public override void Enter()
        {
            // 어떤 스킬인지는 들어오기 직전에 상태가 Ctx.PendingSkill 에 담아 둔다.
            _skill = Ctx.PendingSkill;
            _elapsed = 0f;
            _hitDone = false;

            if (_skill == null) return;

            Ctx.UpdateFacing(Ctx.Input.Horizontal);
            Ctx.Cooldowns.Start(_skill.slot, _skill.cooldown);

            if (_skill.invincibleWhileCasting)
            {
                Ctx.Invincible.Hold(InvincibilitySource.Skill);
            }
        }

        public override PlayerStateId? Tick(float deltaTime)
        {
            if (_skill == null) return PlayerStateId.Grounded;

            _elapsed += deltaTime;

            if (!_hitDone && _elapsed >= _skill.windup)
            {
                _hitDone = true;
                Fire();
            }

            // 대시 캔슬 — 쿨타임은 이미 돌아갔으므로 그대로 둔다 (설계서 3.3)
            if (_hitDone && WantsDash) return PlayerStateId.Dash;

            if (_elapsed < _skill.totalDuration) return null;

            return Ctx.IsGrounded ? PlayerStateId.Grounded : PlayerStateId.Airborne;
        }

        private void Fire()
        {
            float damage = Ctx.DamageFrom(_skill.damageCoefficient);

            int hits = AttackResolver.FromSkill(Ctx, _skill, damage, Ctx.EnemyLayers);

            if (hits > 0)
            {
                Ctx.Gauge.OnDamageDealt(damage * hits);

                HitStop.Play(_skill.IsHeavy
                    ? Ctx.Stats.hitStopHeavy
                    : Ctx.Stats.hitStopLight);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            if (_skill == null) return;

            // 돌진 — 선딜 동안 앞으로 나아간다 (Q)
            if (_skill.dashDistance > 0f && _elapsed < _skill.windup && _skill.windup > 0f)
            {
                float speed = _skill.dashDistance / _skill.windup;
                Ctx.SetHorizontalVelocity(Ctx.Facing * speed);
                return;
            }

            if (_skill.canMoveWhileCasting)
            {
                float target = Ctx.Input.Horizontal * Ctx.CurrentMoveSpeed * 0.5f;
                Ctx.SetHorizontalVelocity(target);
                return;
            }

            // 차지 중 이동 불가 (E)
            if (Ctx.IsGrounded) Ctx.SetHorizontalVelocity(0f);
        }

        public override void Exit()
        {
            // 자기 것만 끈다 — 시전 중 맞아서 켜진 피격 무적은 남겨 둔다.
            if (_skill != null && _skill.invincibleWhileCasting)
            {
                Ctx.Invincible.Release(InvincibilitySource.Skill);
            }
        }

        /// <summary>
        /// 피격으로 취소됐다. 쿨타임을 일부만 돌려준다.
        /// E는 cancelCooldownRatio 가 0.5 라서 절반만 소모한다 — 실수 비용 완화.
        /// </summary>
        public void CancelByHit()
        {
            if (_skill == null) return;

            Ctx.Cooldowns.StartPartial(
                _skill.slot, _skill.cooldown, _skill.cancelCooldownRatio);
        }
    }
}
