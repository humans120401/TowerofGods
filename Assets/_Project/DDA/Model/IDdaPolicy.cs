namespace RT.DDA.Model
{
    /// <summary>
    /// DDA가 한 패턴에 대해 결정한 내용.
    /// 로그에 그대로 남아야 분석이 되므로, 고른 값뿐 아니라 '왜'와 '예측'도 함께 담는다.
    /// (DDA 설계서 10장 — PatternStats 추가 필드)
    /// </summary>
    public readonly struct DdaDecision
    {
        /// <summary>보정 단계 −2 ~ +2</summary>
        public readonly int Step;

        /// <summary>왜 이 단계가 됐는가: first / noData / learned / protected / excluded / fallback / off</summary>
        public readonly string Reason;

        /// <summary>이 단계를 걸면 대응 성공률이 얼마가 될지 예측한 값</summary>
        public readonly float Predicted;

        /// <summary>그 예측의 불확실성</summary>
        public readonly float Uncertainty;

        public DdaDecision(int step, string reason, float predicted = -1f, float uncertainty = -1f)
        {
            Step = step;
            Reason = reason;
            Predicted = predicted;
            Uncertainty = uncertainty;
        }

        /// <summary>DDA를 끈 그룹(대조군)이 쓰는 결정 — 항상 0단계</summary>
        public static DdaDecision Off => new DdaDecision(0, "off");
    }

    /// <summary>
    /// 다음 전투에 걸 보정 단계를 고르는 정책.
    ///
    /// 구현체:
    ///   LinUcbPolicy      채택안 (DDA 설계서 8장)
    ///   RuleBasedPolicy   폴백 겸 비교군 — "지난 전투가 구간 아래면 한 칸 쉽게, 위면 한 칸 어렵게"
    ///   RandomPolicy      플레이테스트용 — 웜 스타트 초기값은 이 기록으로만 만든다
    ///   OffPolicy         대조군 — 항상 0단계
    ///
    /// **이 어셈블리(RT.DDA.Model)는 UnityEngine을 참조하지 않는다.**
    ///   → EditMode 테스트로 18×18 행렬 연산을 검증할 수 있다
    ///   → 파이썬 ddasim과 같은 입력에 같은 출력이 나오는지 대조할 수 있다
    /// </summary>
    public interface IDdaPolicy
    {
        /// <summary>로그에 남길 정책 이름: linucb / rule / random / off</summary>
        string PolicyName { get; }

        /// <summary>
        /// 문맥을 보고 다음 단계를 고른다.
        /// </summary>
        /// <param name="context">DDA 설계서 6장의 x0~x5 (길이 6)</param>
        /// <param name="currentStep">지금 이 패턴에 걸려 있는 단계</param>
        DdaDecision Choose(float[] context, int currentStep);

        /// <summary>
        /// 전투가 끝난 뒤 결과로 배운다.
        /// </summary>
        /// <param name="context">단계를 고를 때 썼던 문맥</param>
        /// <param name="step">실제로 걸렸던 단계</param>
        /// <param name="responseRate">실제 대응 성공률 y = (패링 + 회피) ÷ 유효 타</param>
        /// <param name="effectiveStrikes">유효 타 수 n. 관측 가중치로 쓴다 (n/6)</param>
        void Observe(float[] context, int step, float responseRate, int effectiveStrikes);
    }
}
