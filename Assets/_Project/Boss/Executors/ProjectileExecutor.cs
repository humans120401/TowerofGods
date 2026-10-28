using RT.Core.Combat;
using RT.Data;
using UnityEngine;

namespace RT.Boss.Executors
{
    /// <summary>
    /// 투사체. 방패 투척 · 파네스의 탄막(직선 · 부채꼴 · 유도).
    ///
    /// 겨냥 지점은 **시전 시작 시점의 플레이어 위치**(inv.TargetAtStart)다.
    /// 발사 순간의 위치를 쓰면 선딜 동안 움직여도 따라붙어 회피가 불가능해진다.
    /// 유도탄은 예외인데, 그건 BossProjectile 쪽에서 각속도 제한을 걸어 처리한다.
    ///
    /// 타수(repeatCount)가 2 이상이면 연타 간격마다 한 발씩 나간다.
    /// 부채꼴은 한 번의 OnStrike 에서 여러 발을 동시에 쏜다 — spreadCount 로 구분한다.
    /// </summary>
    public class ProjectileExecutor : PatternExecutor
    {
        public override PatternKind Kind => PatternKind.Projectile;

        /// <summary>한 번에 몇 발 퍼뜨릴까. 1이면 직선 한 발</summary>
        private readonly int _spreadCount;

        /// <summary>부채꼴 전체 각도(도)</summary>
        private readonly float _spreadAngle;

        /// <summary>투사체 속도(m/s)</summary>
        private readonly float _speed;

        public ProjectileExecutor(int spreadCount = 1, float spreadAngle = 0f, float speed = 9f)
        {
            _spreadCount = spreadCount < 1 ? 1 : spreadCount;
            _spreadAngle = spreadAngle;
            _speed = speed <= 0f ? 9f : speed;
        }

        public override void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex)
        {
            if (ctx.ProjectilePrefab == null)
            {
                Debug.LogWarning(
                    $"[{inv.Pattern.patternId}] 투사체 프리팹이 없어 발사하지 못했습니다. " +
                    "Boss Brain ▸ Projectile Prefab 을 채워 주세요.", ctx.Transform);
                return;
            }

            // 보스 가슴 높이에서 나간다. 바닥에서 나가면 점프로 넘을 수 없다.
            float muzzleY = inv.Pattern.hitLow
                            + (inv.Pattern.hitHigh - inv.Pattern.hitLow) * 0.5f
                            - (ctx.Data != null ? ctx.Data.height * 0.5f : 1.2f);

            Vector2 muzzle = (Vector2)ctx.Transform.position
                             + new Vector2(inv.Facing * 0.6f, muzzleY);

            Vector2 aim = (inv.TargetAtStart - muzzle).normalized;
            if (aim.sqrMagnitude < 0.0001f) aim = new Vector2(inv.Facing, 0f);

            var info = new HitInfo(
                inv.Pattern.patternId, hitIndex, inv.Tuned.Damage, inv.Facing,
                (KnockbackLevel)Mathf.Clamp(inv.Pattern.knockbackLevel, 0, 3),
                inv.Pattern.parryable, inv.Pattern.ignoresDodge, inv.Pattern.parryStaggerTime);

            for (int i = 0; i < _spreadCount; i++)
            {
                Vector2 dir = _spreadCount == 1 ? aim : Fan(aim, i);

                var go = Object.Instantiate(
                    ctx.ProjectilePrefab, muzzle, Quaternion.identity);

                var projectile = go.GetComponent<BossProjectile>();

                if (projectile == null)
                {
                    Debug.LogWarning(
                        "투사체 프리팹에 BossProjectile 컴포넌트가 없습니다.", go);
                    continue;
                }

                projectile.Launch(in info, dir * _speed, ctx.PlayerLayers, ctx.Target, ctx.Parried);
            }
        }

        /// <summary>부채꼴로 퍼뜨린다. 가운데를 기준으로 좌우 대칭.</summary>
        private Vector2 Fan(Vector2 aim, int index)
        {
            float step = _spreadAngle / (_spreadCount - 1);
            float offset = -_spreadAngle * 0.5f + step * index;

            return Quaternion.Euler(0f, 0f, offset) * aim;
        }
    }
}
