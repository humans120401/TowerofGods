using RT.Core.Combat;
using RT.Data;
using UnityEngine;

namespace RT.Player.Systems
{
    /// <summary>
    /// 공격 판정을 한 번 쏘고, 맞은 것들에게 피해를 전달한다.
    ///
    /// 콜라이더를 켰다 껐다 하지 않고 **판정 프레임에 한 번만 겹침 검사**를 한다.
    ///   - 프리팹에 히트박스 오브젝트를 붙일 필요가 없다
    ///   - "언제 켜고 언제 끄는지" 타이밍 버그가 생기지 않는다
    ///   - 같은 공격이 두 번 맞는 일이 없다
    ///
    /// 판정 모양·크기는 전부 데이터(PlayerStats · SkillData)에서 온다.
    /// </summary>
    public static class AttackResolver
    {
        // 한 번에 최대 16개까지만 본다. 늘려야 하면 여기만 바꾸면 된다.
        private static readonly Collider2D[] Buffer = new Collider2D[16];

        /// <summary>
        /// 박스 판정을 쏜다.
        /// </summary>
        /// <returns>실제로 피해를 준 대상 수</returns>
        public static int Box(
            PlayerContext ctx,
            Vector2 size,
            float forwardOffset,
            float damage,
            KnockbackLevel knockback,
            LayerMask targetLayers,
            string sourceId)
        {
            Vector2 center = CenterOf(ctx, forwardOffset);

            int count = Physics2D.OverlapBoxNonAlloc(
                center, size, 0f, Buffer, targetLayers);

            return Deliver(ctx, count, damage, knockback, sourceId);
        }

        /// <summary>원 판정을 쏜다. W(회전 베기)처럼 자기 중심 범위에 쓴다.</summary>
        public static int Circle(
            PlayerContext ctx,
            float radius,
            float forwardOffset,
            float damage,
            KnockbackLevel knockback,
            LayerMask targetLayers,
            string sourceId)
        {
            Vector2 center = CenterOf(ctx, forwardOffset);

            int count = Physics2D.OverlapCircleNonAlloc(
                center, radius, Buffer, targetLayers);

            return Deliver(ctx, count, damage, knockback, sourceId);
        }

        /// <summary>SkillData 의 모양에 맞춰 알아서 쏜다.</summary>
        public static int FromSkill(
            PlayerContext ctx,
            SkillData skill,
            float damage,
            LayerMask targetLayers)
        {
            var knockback = (KnockbackLevel)Mathf.Clamp(skill.knockbackLevel, 0, 3);

            return skill.shape == HitboxShape.Circle
                ? Circle(ctx, skill.radius, skill.forwardOffset,
                         damage, knockback, targetLayers, skill.displayName)
                : Box(ctx, skill.boxSize, skill.forwardOffset,
                      damage, knockback, targetLayers, skill.displayName);
        }

        private static Vector2 CenterOf(PlayerContext ctx, float forwardOffset)
        {
            // 바라보는 방향으로 밀어낸다. Facing 은 -1 또는 1.
            return (Vector2)ctx.Transform.position
                   + new Vector2(ctx.Facing * forwardOffset, 0f);
        }

        private static int Deliver(
            PlayerContext ctx,
            int count,
            float damage,
            KnockbackLevel knockback,
            string sourceId)
        {
            int hits = 0;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;

                // 자기 자신은 때리지 않는다.
                if (col.transform.root == ctx.Transform.root) continue;

                var target = col.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                var info = new HitInfo(
                    patternId: sourceId,
                    hitIndex: hits,
                    damage: damage,
                    direction: ctx.Facing,
                    knockback: knockback);

                target.TakeDamage(in info);
                hits++;
            }

            return hits;
        }
    }
}
