using UnityEngine;

namespace RT.Game
{
    /// <summary>
    /// 허수아비가 쏘는 공격 한 종류.
    ///
    /// **(패링 가능 × 회피 불가) 네 조합을 전부 둔다.**
    /// 그게 이 프로젝트가 팀 프로젝트와 갈리는 지점이라서,
    /// 눈으로 네 칸을 다 확인할 수 있어야 한다.
    ///
    ///   패링가능 · 회피가능   노랑   가장 흔한 패턴. 패링도 대시도 통한다
    ///   패링불가 · 회피가능   빨강   대시로만 흘린다. F를 눌러도 맞아야 정상
    ///   패링불가 · 회피불가   보라   둘 다 안 통한다. 범위를 벗어나는 수밖에 없다
    ///   패링가능 · 회피불가   주황   **패링만 통한다** — 설계상 가장 중요한 칸
    ///
    /// 마지막 칸이 핵심이다. 이게 있으면 패링을 배울 이유가 생기고,
    /// 없으면 모든 상황에서 대시가 패링의 상위 호환이 되어 패링 설계가 죽는다.
    /// </summary>
    public readonly struct DummyAttack
    {
        public readonly string Label;
        public readonly bool Parryable;
        public readonly bool IgnoresDodge;
        public readonly Color TelegraphColor;

        public DummyAttack(string label, bool parryable, bool ignoresDodge, Color color)
        {
            Label = label;
            Parryable = parryable;
            IgnoresDodge = ignoresDodge;
            TelegraphColor = color;
        }

        /// <summary>돌아가며 쏠 네 종류. 순서는 고정이다 — 무작위보다 검증하기 쉽다.</summary>
        public static readonly DummyAttack[] All =
        {
            new DummyAttack("패링가능_횡베기", true, false,
                new Color(1f, 0.90f, 0.30f)),    // 노랑

            new DummyAttack("패링불가_돌진", false, false,
                new Color(1f, 0.35f, 0.35f)),    // 빨강

            new DummyAttack("회피불가_내려찍기", false, true,
                new Color(0.70f, 0.40f, 1f)),    // 보라

            new DummyAttack("패링만가능_찌르기", true, true,
                new Color(1f, 0.55f, 0.15f)),    // 주황
        };
    }
}
