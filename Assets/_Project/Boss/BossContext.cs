using RT.Core.Combat;
using RT.Data;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>
    /// 실행기와 FSM 이 공유하는 가방. 플레이어 쪽 PlayerContext 와 같은 역할이다.
    ///
    /// 실행기가 MonoBehaviour 를 직접 들고 다니면 서로 얽혀서
    /// 보스 다섯을 올릴 때 손댈 곳이 폭발한다. 필요한 것만 여기 담아 넘긴다.
    /// </summary>
    public class BossContext
    {
        public readonly Transform Transform;
        public readonly Rigidbody2D Body;
        public readonly BossData Data;

        /// <summary>때릴 대상(플레이어). 없을 수 있다</summary>
        public Transform Target;

        /// <summary>플레이어가 있는 레이어. 판정이 이 레이어만 본다</summary>
        public LayerMask PlayerLayers;

        /// <summary>이번 전투에서 쓴 패턴 기록. 카로스의 환영이 여기서 꺼낸다</summary>
        public readonly PatternHistory History = new PatternHistory();

        /// <summary>이 보스가 쓰는 실행기 묶음. 환영이 원본 실행기를 찾을 때도 쓴다</summary>
        public PatternExecutorSet Executors;

        // ── 매 프레임 갱신 ──

        public int Health;
        public int Phase = 1;

        /// <summary>바라보는 방향. -1(왼쪽) 또는 1(오른쪽)</summary>
        public float Facing = -1f;

        /// <summary>패링당해 굳어 있는 동안 true. 이 동안은 패턴을 쓰지 않는다</summary>
        public bool Staggered;

        /// <summary>
        /// 플레이어가 패링에 성공했다. 인자는 굳는 시간(초).
        ///
        /// 판정을 쏜 곳(BossAttack · 투사체)이 결과를 바로 알지만, 거기서 보스를
        /// 직접 멈출 수는 없다 — 실행기는 상태를 모른다. 그래서 콜백으로 넘긴다.
        /// </summary>
        public System.Action<float> Parried;

        // ── 실행기가 쓰는 프리팹 ──

        /// <summary>투사체. ProjectileExecutor 가 복제한다</summary>
        public GameObject ProjectilePrefab;

        /// <summary>소환물. SummonExecutor 가 복제한다</summary>
        public GameObject SummonPrefab;

        /// <summary>장판·기둥. AreaExecutor 가 복제한다</summary>
        public GameObject AreaPrefab;

        public BossContext(Transform transform, Rigidbody2D body, BossData data)
        {
            Transform = transform;
            Body = body;
            Data = data;
            Health = data != null ? data.maxHealth : 1;
        }

        public bool IsDead => Health <= 0;

        public Vector2 Position => Transform.position;

        public bool HasTarget => Target != null;

        /// <summary>플레이어까지의 가로 거리(m). 대상이 없으면 아주 큰 값</summary>
        public float DistanceToTarget =>
            Target == null
                ? float.MaxValue
                : Mathf.Abs(Target.position.x - Transform.position.x);

        public Vector2 TargetPosition =>
            Target == null ? Position : (Vector2)Target.position;

        /// <summary>플레이어가 있는 쪽 (-1 또는 1)</summary>
        public float DirectionToTarget
        {
            get
            {
                if (Target == null) return Facing;

                float dx = Target.position.x - Transform.position.x;
                return Mathf.Abs(dx) < 0.01f ? Facing : Mathf.Sign(dx);
            }
        }

        public void FaceTarget()
        {
            if (Target == null) return;

            Facing = DirectionToTarget;
        }

        /// <summary>
        /// 패턴 하나의 기록을 만든다. 지금 위치·방향·플레이어 위치를 함께 굳힌다.
        /// 선딜 중에 플레이어가 움직여도 겨냥 지점은 바뀌지 않는다 — 그래야 피할 수 있다.
        /// </summary>
        public PatternInvocation Invoke(BossPatternData pattern, in PatternTuning tuning,
                                        float playerMaxHealth)
        {
            var tuned = TunedPattern.Resolve(pattern.Baseline, tuning, playerMaxHealth);

            return new PatternInvocation(
                pattern, tuned, Position, Facing, TargetPosition);
        }
    }
}
