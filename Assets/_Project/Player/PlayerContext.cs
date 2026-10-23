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

        // ── 하위 시스템 ──
        public readonly DashStacks Dash;
        public readonly JumpAssist Jump;

        // ── 매 프레임 갱신되는 상태 ──

        /// <summary>지금 지면에 닿아 있는가. PlayerBrain이 갱신한다.</summary>
        public bool IsGrounded;

        /// <summary>직전 프레임의 접지 상태. 착지 순간을 잡는 데 쓴다.</summary>
        public bool WasGrounded;

        /// <summary>바라보는 방향. -1(왼쪽) 또는 1(오른쪽)</summary>
        public float Facing = 1f;

        /// <summary>무적인가. 대시·피격이 켠다.</summary>
        public bool IsInvincible;

        /// <summary>이번 대시의 진행 방향</summary>
        public float DashDirection = 1f;

        /// <summary>
        /// 보스·디버프가 거는 제약. 없으면 null이고, 그때는 제약이 없는 것으로 본다.
        /// 2단계 이후에 보스가 채운다.
        /// </summary>
        public IPlayerConstraint Constraint;

        public PlayerContext(
            Rigidbody2D body,
            Transform transform,
            PlayerStats stats,
            IPlayerInput input)
        {
            Body = body;
            Transform = transform;
            Stats = stats;
            Input = input;

            Dash = new DashStacks(
                stats.dashMaxStacks,
                stats.dashRechargeTime,
                stats.dashMinInterval);

            Jump = new JumpAssist(
                stats.coyoteTime,
                stats.jumpBufferTime);
        }

        // ── 제약이 걸린 실효값 ──
        // Constraint가 null일 수 있으므로 여기서 한 번에 처리한다.
        // 상태 클래스들이 매번 null 검사를 하지 않게 하려는 것이다.

        public bool DashBlocked => Constraint != null && Constraint.DashBlocked;

        public bool AttackBlocked => Constraint != null && Constraint.AttackBlocked;

        public bool ParryBlocked => Constraint != null && Constraint.ParryBlocked;

        public float MoveSpeedMultiplier =>
            Constraint == null ? 1f : Constraint.MoveSpeedMultiplier;

        public float DashRechargeMultiplier =>
            Constraint == null ? 1f : Constraint.DashRechargeMultiplier;

        /// <summary>지상·공중에 맞는 이동속도 (제약 반영)</summary>
        public float CurrentMoveSpeed =>
            (IsGrounded ? Stats.moveSpeed : Stats.airMoveSpeed) * MoveSpeedMultiplier;

        /// <summary>이번 프레임에 착지했는가</summary>
        public bool JustLanded => IsGrounded && !WasGrounded;

        /// <summary>바라보는 방향을 입력에 맞춰 돌린다. 0 입력이면 그대로 둔다.</summary>
        public void UpdateFacing(float horizontal)
        {
            if (Mathf.Abs(horizontal) < 0.01f) return;

            Facing = Mathf.Sign(horizontal);

            var scale = Transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Facing;
            Transform.localScale = scale;
        }

        /// <summary>수평 속도만 바꾸고 수직은 건드리지 않는다.</summary>
        public void SetHorizontalVelocity(float vx)
        {
            Body.linearVelocity = new Vector2(vx, Body.linearVelocity.y);
        }

        /// <summary>수직 속도만 바꾸고 수평은 건드리지 않는다.</summary>
        public void SetVerticalVelocity(float vy)
        {
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, vy);
        }
    }
}
