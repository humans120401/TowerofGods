using RT.Data;

namespace RT.Boss
{
    /// <summary>
    /// 패턴 한 종류를 어떻게 실행하는가. 설계서(ARCHITECTURE §3.1)의 실행기 6종이 이걸 상속한다.
    ///
    /// **실행기는 상태를 갖지 않는다.** 진행 상황(얼마나 지났나, 몇 타째인가)은
    /// PatternRunner 가 들고 있고, 실행기는 PatternInvocation 만 받아 그때그때 판단한다.
    ///
    /// 왜 그렇게 했는가:
    ///   상태가 실행기에 있으면 같은 패턴을 동시에 둘 돌릴 수 없다.
    ///   카로스의 환영은 원본과 복제가 **겹쳐서** 돌아야 하고,
    ///   파네스의 분신도 마찬가지다. 상태를 밖에 두면 인스턴스 하나를 공유해도 된다.
    ///
    /// 호출 순서:
    ///   OnBegin → (선딜) → OnStrike(0) → … OnStrike(n-1) → (후딜) → OnEnd
    ///   OnTick 은 그 사이 매 프레임.
    /// </summary>
    public abstract class PatternExecutor
    {
        public abstract PatternKind Kind { get; }

        /// <summary>시전이 시작됐다. 선딜 연출·자세 고정 등.</summary>
        public virtual void OnBegin(BossContext ctx, in PatternInvocation inv) { }

        /// <summary>
        /// 판정이 나가는 순간. 실행기마다 하는 일이 다르다 —
        /// 근접은 겹침 검사, 투사체는 생성, 소환은 복제.
        /// </summary>
        /// <param name="hitIndex">몇 번째 타인가 (0부터)</param>
        public abstract void OnStrike(BossContext ctx, in PatternInvocation inv, int hitIndex);

        /// <summary>매 프레임. 돌진처럼 시전 중에 움직이는 패턴이 쓴다.</summary>
        /// <param name="elapsed">시전 시작부터 흐른 시간(초)</param>
        public virtual void OnTick(BossContext ctx, in PatternInvocation inv, float elapsed) { }

        /// <summary>끝났다. 정리할 게 있으면 여기서.</summary>
        public virtual void OnEnd(BossContext ctx, in PatternInvocation inv) { }

        /// <summary>
        /// 중간에 끊겼다 (패링 경직 · 페이즈 전환 · 사망).
        /// 기본은 OnEnd 와 같게 처리한다.
        /// </summary>
        public virtual void OnCancel(BossContext ctx, in PatternInvocation inv) => OnEnd(ctx, inv);
    }
}
