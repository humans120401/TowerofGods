# 역전의 탑 (Tower of Gods)

2D 횡스크롤 보스러시 액션. **LinUCB 기반 동적 난이도 조절(DDA)이 보스 패턴 단위로 작동한다.**

> 팀 프로젝트(`kimdal0803/Team_RedBull_Project`)와 **코드를 공유하지 않는** 1인 재구현이다.
> 기획 자료만 가져오고 구조는 처음부터 다시 잡았다. 이유는 `docs/ARCHITECTURE.md` §0.

---

## 지금 상태

**0단계 — 프로젝트 뼈대** ✅

시스템 간 이음매를 인터페이스로 못 박고, 어셈블리로 의존 방향을 컴파일 단계에서 강제했다.
아직 플레이할 수 있는 것은 없다.

| 단계 | 내용 | 상태 |
|---|---|---|
| 0 | 뼈대 · 어셈블리 · 입력 인터페이스 | ✅ |
| 1 | 플레이어 기동 (이동·점프·대시·코요테·버퍼) | ⬜ |
| 2 | 전투 (콤보·Q/W/E·패링·히트스톱) | ⬜ |
| 3 | 보스 프레임워크 (PatternExecutor 6종) | ⬜ |
| 4 | 아라쿠스 | ⬜ |
| 5 | 텔레메트리 + Supabase | ⬜ |
| 6 | **DDA** — 여기까지면 논문 실험이 가능하다 | ⬜ |
| 7 | 로비 · 성장 · 궁극기 계승 | ⬜ |
| 8~11 | 아디쿠스 · 파네스 · 에텔로스 · 카로스 | ⬜ |

---

## 열어 보기

1. **Unity Hub ▸ Add ▸ Add project from disk** 로 이 폴더를 연다
2. 버전은 **6000.6.5f1**
3. 처음 열면 `Library/` 를 만드느라 몇 분 걸린다
4. **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**

스모크 테스트 5개가 통과하면 0단계가 제대로 선 것이다.

---

## 구조

```
Assets/_Project/
├─ Core/        이벤트 · 인터페이스 · 공용 타입   (아무것도 참조하지 않음)
├─ Data/        ScriptableObject 정의
├─ Player/      플레이어 상태 기계 · 입력 구현
├─ Boss/        보스 FSM · 패턴 실행기
├─ Telemetry/   전투 기록
├─ Backend/     Supabase 연동
├─ DDA/
│  ├─ Model/    LinUCB 학습 코어 — **UnityEngine을 참조하지 않는 순수 C#**
│  └─ Runtime/  유니티 쪽 매니저
├─ UI/
├─ Game/        부트스트랩 · 조립 (유일하게 전부를 참조한다)
└─ Tests/
```

### 의존 방향

```
Core ← Data ← {Player, Boss, Telemetry, UI}
                    ↑
            Backend, DDA ← Telemetry
                    ↑
                  Game  (조립만)
```

**Assembly Definition으로 컴파일 단계에서 강제한다.** 규칙을 어기면 빌드가 깨진다.

---

## 지키는 규칙

1. 한 파일 **300줄**을 넘기지 않는다 — 넘으면 "쪼갤까?"를 생각하는 신호등
2. 매직 넘버 금지 — 수치는 ScriptableObject
3. **`UnityEngine.Input`을 쓰는 곳은 `KeyboardInput.cs` 하나뿐이다**
   나머지는 전부 `IPlayerInput`을 본다 → 에이전트·리플레이로 갈아끼울 수 있다
4. `RT.DDA`는 `RT.Boss`·`RT.Player`를 참조하지 않는다 → **DDA를 빼도 게임이 돈다 = 대조군 빌드가 공짜**
5. `RT.Backend`가 꺼져도 게임이 끝까지 돌아간다 → 로컬 CSV로 폴백
6. DDA 학습 코어는 유니티 없이 테스트된다

---

## 문서

| 파일 | 내용 |
|---|---|
| `docs/SPEC.md` | 기획 사양서. 기획 문서 9종을 합친 단일 진실 + 문서 간 충돌 13건 |
| `docs/ARCHITECTURE.md` | 왜 이렇게 설계했는가. 결정과 그 근거 |

수치가 서로 다르면 **`docs/SPEC.md`를 따른다.**

---

## 비밀값

Supabase URL·키는 **커밋하지 않는다.** `.gitignore`가 막고 있다.
연동은 5단계에서 한다.
