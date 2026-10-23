namespace RT.Core.Constraints
{
    /// <summary>
    /// 보스나 디버프가 플레이어 능력을 제한하는 창구.
    ///
    /// 기획에 이미 이런 설계가 들어 있다:
    ///   카로스  — "보스가 회피 키를 봉인하고, 특정 횟수 패링을 하면 풀린다. 맞으면 다시 잠김"
    ///   에텔로스 — "빛 무기 피격 시 공격력 감소 / 전기 무기 피격 시 (대시 충전 지연)"
    ///   공통    — 침묵 · 행동 제한 · 이동속도 감소 디버프
    ///
    /// 보스가 플레이어를 직접 참조하지 않게 하려고 인터페이스로 끊는다.
    /// RT.Boss 는 "제약을 건다"는 사실만 알고, 적용은 RT.Player 가 한다.
    ///
    /// 여러 제약이 동시에 걸릴 수 있으므로, 실제 구현은 이것들을 합산(aggregate)한다.
    /// </summary>
    public interface IPlayerConstraint
    {
        /// <summary>대시(회피)가 막혀 있다 — 카로스 회피 봉인</summary>
        bool DashBlocked { get; }

        /// <summary>기본 공격·스킬이 막혀 있다 — 침묵</summary>
        bool AttackBlocked { get; }

        /// <summary>패링이 막혀 있다</summary>
        bool ParryBlocked { get; }

        /// <summary>주는 피해 배율. 1이 기본, 0.8이면 20% 감소 — 에텔로스 빛 피격</summary>
        float DamageDealtMultiplier { get; }

        /// <summary>이동속도 배율. 1이 기본</summary>
        float MoveSpeedMultiplier { get; }

        /// <summary>대시 충전 속도 배율. 1이 기본, 0.5면 충전이 2배 느려짐 — 에텔로스 전기 피격</summary>
        float DashRechargeMultiplier { get; }
    }
}
