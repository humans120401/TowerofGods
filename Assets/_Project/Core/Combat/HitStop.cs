using System.Collections;
using UnityEngine;

namespace RT.Core.Combat
{
    /// <summary>
    /// 히트스톱 — 타격이 맞는 순간 화면을 아주 잠깐 멈춘다.
    ///
    /// 2D 액션에서 "맞았다"는 느낌의 8할이 여기서 나온다.
    /// 애니메이션 품질보다 이게 손맛을 더 좌우한다.
    ///
    ///   약공격 0.05s · 스킬 0.12s · 패링 성공 0.15s
    ///
    /// 패링을 가장 길게 두는 이유: 패링은 "자원을 버는 기술"이라
    /// 성공했을 때 보상감이 가장 커야 한다 (설계서 4.1).
    ///
    /// 구현 메모:
    ///   Time.timeScale 을 0 으로 내렸다가 되돌린다. 전역이라 단일 코루틴으로만 돌린다.
    ///   겹쳐서 들어오면 더 긴 쪽을 택한다 — 짧은 게 긴 걸 끊으면 밋밋해진다.
    /// </summary>
    [AddComponentMenu("Tower of Gods/전투/히트스톱")]
    public class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }

        [Tooltip("끄면 히트스톱이 걸리지 않는다. 손맛 비교용")]
        [SerializeField] private bool enabledFlag = true;

        private Coroutine _running;
        private float _endsAtRealtime;

        private void Awake()
        {
            // 씬에 둘 이상 있으면 나중 것을 버린다.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // 멈춘 채로 파괴되면 게임이 영영 멈춘다.
                Time.timeScale = 1f;
                Instance = null;
            }
        }

        /// <summary>어디서든 부를 수 있는 입구. 인스턴스가 없으면 조용히 넘어간다.</summary>
        public static void Play(float duration)
        {
            if (Instance == null || duration <= 0f) return;
            Instance.Begin(duration);
        }

        private void Begin(float duration)
        {
            if (!enabledFlag) return;

            float endsAt = Time.realtimeSinceStartup + duration;

            // 이미 더 긴 멈춤이 걸려 있으면 그대로 둔다.
            if (_running != null && endsAt <= _endsAtRealtime) return;

            if (_running != null) StopCoroutine(_running);

            _endsAtRealtime = endsAt;
            _running = StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            Time.timeScale = 0f;

            // timeScale 이 0 이므로 실시간으로 기다려야 한다.
            while (Time.realtimeSinceStartup < _endsAtRealtime)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            _running = null;
        }
    }
}
