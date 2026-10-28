using System.Collections.Generic;

namespace RT.Boss
{
    /// <summary>
    /// 이번 전투에서 쓴 패턴 기록. 최근 것부터 보관한다.
    ///
    /// 쓰는 곳 둘:
    ///   1) 카로스의 환영 — 최근에 쓴 패턴을 그대로 다시 튼다
    ///   2) 텔레메트리 — 어떤 패턴이 언제 나갔는지
    ///
    /// 고정 길이 링버퍼다. 전투가 길어도 메모리가 늘지 않고,
    /// 환영은 어차피 '최근'만 복제하므로 오래된 건 필요 없다.
    /// </summary>
    public class PatternHistory
    {
        private readonly List<PatternInvocation> _items;
        private readonly int _capacity;

        public PatternHistory(int capacity = 16)
        {
            _capacity = capacity < 1 ? 1 : capacity;
            _items = new List<PatternInvocation>(_capacity);
        }

        public int Count => _items.Count;

        public void Record(in PatternInvocation invocation)
        {
            if (!invocation.IsValid) return;

            _items.Insert(0, invocation);

            if (_items.Count > _capacity) _items.RemoveAt(_items.Count - 1);
        }

        /// <summary>최근 것부터 index 번째. 0 이 가장 최근이다.</summary>
        public bool TryGet(int index, out PatternInvocation invocation)
        {
            if (index < 0 || index >= _items.Count)
            {
                invocation = default;
                return false;
            }

            invocation = _items[index];
            return true;
        }

        /// <summary>
        /// 다시 틀 수 있는 가장 최근 패턴.
        ///
        /// 환영 자신(Replay)은 건너뛴다 — 안 그러면 환영이 환영을 복제해 무한히 늘어난다.
        /// 피해가 없는 기동·버프도 건너뛴다. 복제해도 플레이어가 대응할 게 없다.
        /// </summary>
        public bool TryGetReplayable(out PatternInvocation invocation)
        {
            foreach (var item in _items)
            {
                if (item.IsReplay) continue;
                if (item.Pattern == null) continue;
                if (item.Pattern.kind == Data.PatternKind.Replay) continue;
                if (!item.Pattern.IsAttack) continue;

                invocation = item;
                return true;
            }

            invocation = default;
            return false;
        }

        /// <summary>층을 다시 시작할 때 비운다.</summary>
        public void Clear() => _items.Clear();
    }
}
