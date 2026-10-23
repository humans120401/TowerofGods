namespace RT.Player.Systems
{
    /// <summary>무적을 켠 주체. 하나씩 따로 켜고 끈다.</summary>
    public enum InvincibilitySource
    {
        Dash = 0,       // 대시 앞 0.15초 (설계서 2.3)
        Hit = 1,        // 피격 후 0.35초 (설계서 4.2)
        Skill = 2,      // 시전 중 무적인 스킬
        Ultimate = 3,   // 궁극기 시전 0.5초 (설계서 4.4)
        Dead = 4,       // 시체를 계속 때리지 않게
    }

    /// <summary>
    /// 무적을 **주체별로** 관리한다.
    ///
    /// 왜 bool 하나로 안 되는가:
    ///   대시 중 회피 불가 공격에 맞으면 두 무적이 동시에 걸린다 — 대시 무적과 피격 무적.
    ///   bool 하나면 대시가 끝날 때 `IsInvincible = false` 로 **피격 무적까지 지워 버린다.**
    ///   그러면 설계서 4.2 의 "대응 창 0.2초"가 사라지고,
    ///   다단히트 패턴(아라쿠스 12타, 아디쿠스 다단)에서 뒷 타를 전부 맞는다.
    ///
    /// 그래서 각자 자기 것만 끈다. 하나라도 켜져 있으면 무적이다.
    ///
    /// 두 종류가 있다:
    ///   For(초)  — 시간이 지나면 저절로 꺼진다. 상태가 바뀌어도 살아남는다 (피격·궁극기)
    ///   Hold()   — 끌 때까지 켜져 있다. 켠 쪽이 Release 로 끈다 (시전 중 무적·사망)
    ///
    /// **UnityEngine을 쓰지 않는다.** 시간은 바깥에서 넣어 준다 → 테스트가 된다.
    /// </summary>
    public class Invincibility
    {
        private const int SourceCount = 5;

        private readonly float[] _timers = new float[SourceCount];
        private readonly bool[] _held = new bool[SourceCount];

        /// <summary>하나라도 켜져 있는가</summary>
        public bool IsActive { get; private set; }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < SourceCount; i++)
            {
                if (_timers[i] <= 0f) continue;

                _timers[i] -= deltaTime;
                if (_timers[i] < 0f) _timers[i] = 0f;
            }

            Recompute();
        }

        /// <summary>
        /// 이만큼 동안 무적. 이미 더 긴 게 걸려 있으면 그대로 둔다.
        /// 상태가 바뀌어도 꺼지지 않는다 — 피격 무적이 경직보다 길어야 하기 때문이다.
        /// </summary>
        public void For(InvincibilitySource source, float duration)
        {
            if (duration <= 0f) return;

            int i = (int)source;
            if (duration > _timers[i]) _timers[i] = duration;

            Recompute();
        }

        /// <summary>끌 때까지 무적. 켠 쪽이 반드시 Release 를 불러야 한다.</summary>
        public void Hold(InvincibilitySource source)
        {
            _held[(int)source] = true;
            Recompute();
        }

        /// <summary>**자기 것만** 끈다. 다른 주체의 무적은 건드리지 않는다.</summary>
        public void Release(InvincibilitySource source)
        {
            int i = (int)source;
            _held[i] = false;
            _timers[i] = 0f;

            Recompute();
        }

        public bool IsActiveFrom(InvincibilitySource source)
        {
            int i = (int)source;
            return _held[i] || _timers[i] > 0f;
        }

        public float Remaining(InvincibilitySource source) => _timers[(int)source];

        /// <summary>층 진입·부활 때 전부 끈다.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < SourceCount; i++)
            {
                _timers[i] = 0f;
                _held[i] = false;
            }

            IsActive = false;
        }

        /// <summary>디버그 표시용. 누가 켰는지 보여 준다.</summary>
        public string Describe()
        {
            if (!IsActive) return "-";

            string result = "";

            for (int i = 0; i < SourceCount; i++)
            {
                if (!_held[i] && _timers[i] <= 0f) continue;

                if (result.Length > 0) result += "+";
                result += Label((InvincibilitySource)i);

                if (_timers[i] > 0f) result += $" {_timers[i]:0.00}";
            }

            return result;
        }

        private static string Label(InvincibilitySource source) => source switch
        {
            InvincibilitySource.Dash => "대시",
            InvincibilitySource.Hit => "피격",
            InvincibilitySource.Skill => "시전",
            InvincibilitySource.Ultimate => "궁극기",
            InvincibilitySource.Dead => "사망",
            _ => "?",
        };

        private void Recompute()
        {
            for (int i = 0; i < SourceCount; i++)
            {
                if (_held[i] || _timers[i] > 0f)
                {
                    IsActive = true;
                    return;
                }
            }

            IsActive = false;
        }
    }
}
