using RT.Core.Combat;
using RT.Data;
using RT.Player.Systems;
using UnityEngine;

namespace RT.Player.States
{
    /// <summary>
    /// 검 콤보. 전투방식설계서 3.1
    ///
    ///   1타 · 2타 각각 선딜 0.15s / 총 0.35s / 계수 100% / 범위 1.6 × 1.8m
    ///   콤보 유예 0.4s — 1타 후 0.4초 안에 다시 누르면 2타로 이어진다
    ///   **넉백 없음** — 보스를 밀어내면 2타가 빗나가 콤보가 성립하지 않는다
    ///
    /// 설계 의도: 짧고 빠르게 둬서 "보스 패턴 사이 빈 시간에 몇 타를 넣을지" 판단하게 만든다.
    /// </summary>
    public class AttackState : PlayerState
    {
        private float _elapsed;
        private bool _hitDone;
        private bool _buffered;
        private int _step;          // 1 또는 2

        public AttackState(PlayerContext ctx) : base(ctx) { }

        public override PlayerStateId Id => PlayerStateId.Attack;

        public override void Enter()
        {
            _step = 1;
            StartSwing();
        }

        private void StartSwing()
        {
            _elapsed = 0f;
            _hitDone = false;
            _buffered = false;

            // 휘두르기 시작할 때 방향을 고정한다.
            // 모션 중에 돌아가면 판정이 엉뚱한 쪽으로 나간다.
            Ctx.UpdateFacing(Ctx.Input.Horizontal);
        }

        public override PlayerStateId? Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            // 판정 프레임 — 선딜이 끝나는 순간 한 번만 쏜다.
            if (!_hitDone && _elapsed >= Ctx.Stats.attackWindup)
            {
                _hitDone = true;
                Swing();
            }

            // 대시 캔슬 — 설계서 3.3. 단 자기 공격 판정이 이미 나간 뒤에만.
            if (_hitDone && WantsDash) return PlayerStateId.Dash;

            // 패링은 언제든 끼어들 수 있다 (설계서의 [미정] 항목이지만 일단 허용)
            if (WantsParry) return PlayerStateId.Parry;

            // 2타 입력 선입력. 1타 진행 중에 눌러 두면 이어진다.
            if (_step == 1 && Ctx.Input.AttackPressed) _buffered = true;

            if (_elapsed < Ctx.Stats.attackTotalDuration) return null;

            // 1타가 끝났고 2타 입력이 들어와 있으면 이어서 휘두른다.
            if (_step == 1 && _buffered)
            {
                _step = 2;
                StartSwing();
                return null;
            }

            return Ctx.IsGrounded ? PlayerStateId.Grounded : PlayerStateId.Airborne;
        }

        private void Swing()
        {
            float damage = Ctx.DamageFrom(Ctx.Stats.attackCoefficient);

            int hits = AttackResolver.Box(
                Ctx,
                Ctx.Stats.attackHitboxSize,
                Ctx.Stats.attackHitboxOffset,
                damage,
                KnockbackLevel.None,     // 콤보가 끊기지 않도록 넉백을 주지 않는다
                Ctx.EnemyLayers,
                $"Combo{_step}");

            if (hits > 0)
            {
                Ctx.Gauge.OnDamageDealt(damage * hits);
                HitStop.Play(Ctx.Stats.hitStopLight);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // 지상에서 1타 중에는 제자리에 선다. 발이 미끄러지면 타격감이 죽는다.
            if (Ctx.IsGrounded && _step == 1)
            {
                Ctx.SetHorizontalVelocity(0f);
                return;
            }

            // 2타와 공중에서는 이동 입력을 조금 받아 준다.
            float target = Ctx.Input.Horizontal * Ctx.CurrentMoveSpeed * 0.4f;
            Ctx.SetHorizontalVelocity(
                Mathf.MoveTowards(Ctx.Body.linearVelocity.x, target,
                                  Ctx.Stats.moveSpeed * fixedDeltaTime * 4f));
        }
    }
}
