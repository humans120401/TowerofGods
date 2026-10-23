using RT.Core.Input;
using UnityEngine;

namespace RT.Player.Input
{
    /// <summary>
    /// 사람이 키보드로 플레이할 때 쓰는 IPlayerInput 구현.
    ///
    /// **UnityEngine.Input을 쓰는 곳은 프로젝트 전체에서 이 파일 하나뿐이다.**
    /// 플레이어 상태 기계는 IPlayerInput만 본다.
    /// 그래서 나중에 에이전트나 리플레이로 갈아끼워도 플레이어 코드는 한 줄도 안 바뀐다.
    ///
    /// 키 배치는 전부 Inspector에서 바꿀 수 있다.
    /// </summary>
    [AddComponentMenu("역전의 탑/입력/키보드 입력")]
    public class KeyboardInput : MonoBehaviour, IPlayerInput
    {
        [Header("이동")]
        [SerializeField] private KeyCode left = KeyCode.A;
        [SerializeField] private KeyCode right = KeyCode.D;
        [SerializeField] private bool alsoUseArrowKeys = true;

        [Header("기동")]
        [SerializeField] private KeyCode jump = KeyCode.Space;
        [SerializeField] private KeyCode dash = KeyCode.LeftShift;

        [Header("전투")]
        [SerializeField] private KeyCode attack = KeyCode.Z;
        [SerializeField] private KeyCode parry = KeyCode.F;
        [SerializeField] private KeyCode skillQ = KeyCode.Q;
        [SerializeField] private KeyCode skillW = KeyCode.W;
        [SerializeField] private KeyCode skillE = KeyCode.E;
        [SerializeField] private KeyCode ultimate = KeyCode.R;
        [SerializeField] private KeyCode potion = KeyCode.G;

        // 프레임 동안 고정되는 스냅샷.
        // 같은 프레임에 여러 번 읽어도 같은 값이 나와야 하므로 Poll에서 한 번만 채운다.
        private float _horizontal;
        private bool _jumpPressed, _jumpHeld, _jumpReleased;
        private bool _dashPressed, _parryPressed, _attackPressed;
        private bool _qPressed, _wPressed, _ePressed;
        private bool _ultimatePressed, _potionPressed;

        public float Horizontal      => _horizontal;
        public bool JumpPressed      => _jumpPressed;
        public bool JumpHeld         => _jumpHeld;
        public bool JumpReleased     => _jumpReleased;
        public bool DashPressed      => _dashPressed;
        public bool ParryPressed     => _parryPressed;
        public bool AttackPressed    => _attackPressed;
        public bool SkillQPressed    => _qPressed;
        public bool SkillWPressed    => _wPressed;
        public bool SkillEPressed    => _ePressed;
        public bool UltimatePressed  => _ultimatePressed;
        public bool PotionPressed    => _potionPressed;

        public void Poll(float deltaTime)
        {
            _horizontal = ReadHorizontal();

            _jumpPressed     = UnityEngine.Input.GetKeyDown(jump);
            _jumpHeld        = UnityEngine.Input.GetKey(jump);
            _jumpReleased    = UnityEngine.Input.GetKeyUp(jump);

            _dashPressed     = UnityEngine.Input.GetKeyDown(dash);
            _parryPressed    = UnityEngine.Input.GetKeyDown(parry)
                               || UnityEngine.Input.GetMouseButtonDown(1);
            _attackPressed   = UnityEngine.Input.GetKeyDown(attack)
                               || UnityEngine.Input.GetMouseButtonDown(0);

            _qPressed        = UnityEngine.Input.GetKeyDown(skillQ);
            _wPressed        = UnityEngine.Input.GetKeyDown(skillW);
            _ePressed        = UnityEngine.Input.GetKeyDown(skillE);
            _ultimatePressed = UnityEngine.Input.GetKeyDown(ultimate);
            _potionPressed   = UnityEngine.Input.GetKeyDown(potion);
        }

        private float ReadHorizontal()
        {
            float v = 0f;

            if (UnityEngine.Input.GetKey(left)) v -= 1f;
            if (UnityEngine.Input.GetKey(right)) v += 1f;

            if (alsoUseArrowKeys)
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) v += 1f;
            }

            // 좌우를 같이 누르면 0이 되도록 클램프한다.
            return Mathf.Clamp(v, -1f, 1f);
        }
    }
}
