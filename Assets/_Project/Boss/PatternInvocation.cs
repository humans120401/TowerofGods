using RT.Core.Combat;
using RT.Data;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>
    /// 패턴을 **한 번 쓴 기록**. 실행에 필요한 전부가 여기 들어 있다.
    ///
    /// 왜 구조체 하나로 모았는가 — 카로스 때문이다.
    /// 카로스의 환영·환각·잔상은 "이미 실행한 패턴을 지연·복제해서 다시 트는 것"이라
    /// **패턴 실행이 재생 가능한 데이터가 아니면 나중에 만들 수 없다** (ARCHITECTURE §3.1).
    ///
    /// 실행기는 상태를 갖지 않고 이 구조체만 받는다.
    /// 그래서 같은 기록을 다른 위치에서 다시 돌리면 그대로 재생된다.
    ///
    /// 보정(TunedPattern)도 기록에 포함한다. 재생할 때 DDA 가 다른 단계로 넘어가 있어도
    /// **원본이 나갔던 그 난이도 그대로** 재생돼야 환영이 원본과 같은 공격이 된다.
    /// </summary>
    public readonly struct PatternInvocation
    {
        public readonly BossPatternData Pattern;

        /// <summary>보정과 안전장치를 통과한 실제 수치</summary>
        public readonly TunedPattern Tuned;

        /// <summary>시전을 시작한 위치</summary>
        public readonly Vector2 Origin;

        /// <summary>시전 시점에 보스가 보던 방향 (-1 또는 1)</summary>
        public readonly float Facing;

        /// <summary>시전 시점의 플레이어 위치. 장판·투사체가 이 지점을 겨냥한다</summary>
        public readonly Vector2 TargetAtStart;

        /// <summary>true 면 환영이 다시 트는 것이다. 연출을 다르게 줄 수 있다</summary>
        public readonly bool IsReplay;

        public PatternInvocation(
            BossPatternData pattern,
            in TunedPattern tuned,
            Vector2 origin,
            float facing,
            Vector2 targetAtStart,
            bool isReplay = false)
        {
            Pattern = pattern;
            Tuned = tuned;
            Origin = origin;
            Facing = facing;
            TargetAtStart = targetAtStart;
            IsReplay = isReplay;
        }

        public bool IsValid => Pattern != null;

        /// <summary>시전 시점 기준으로 플레이어가 있던 방향 (-1 또는 1)</summary>
        public float DirectionToTarget =>
            TargetAtStart.x < Origin.x ? -1f : 1f;

        /// <summary>
        /// 같은 패턴을 다른 자리에서 다시 트는 기록을 만든다. 카로스의 환영용.
        /// 보정과 겨냥 지점은 원본 그대로 가져간다.
        /// </summary>
        public PatternInvocation AsReplayFrom(Vector2 origin, float facing) =>
            new PatternInvocation(Pattern, Tuned, origin, facing, TargetAtStart, true);

        public override string ToString() =>
            $"{(IsReplay ? "[환영] " : "")}{Pattern?.patternId ?? "(없음)"} {Tuned}";
    }
}
