using RT.Player;
using UnityEngine;

namespace RT.Game
{
    /// <summary>
    /// 화면 좌상단에 플레이어 상태를 띄운다.
    ///
    /// 1단계에서 "손맛"을 판정하려면 숫자가 보여야 한다.
    /// 코요테가 실제로 먹었는지, 대시 스택이 언제 차는지는 느낌만으로 알 수 없다.
    ///
    /// UI 에셋을 안 쓰고 OnGUI로 그린다. 씬 세팅이 필요 없기 때문이다.
    /// 정식 HUD는 나중에 RT.UI에서 만든다.
    /// </summary>
    [AddComponentMenu("Tower of Gods/디버그/플레이어 상태 표시")]
    public class PlayerDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerBrain player;
        [SerializeField] private bool show = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private GUIStyle _style;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<PlayerBrain>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) show = !show;
        }

        private void OnGUI()
        {
            if (!show || player == null) return;

            var ctx = player.Context;
            if (ctx == null) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = Color.white },
            };

            var v = ctx.Body.linearVelocity;

            string text =
                $"상태      {player.CurrentState}\n" +
                $"속도      x {v.x,6:0.00}   y {v.y,6:0.00}\n" +
                $"접지      {(ctx.IsGrounded ? "O" : "X")}\n" +
                $"바라보기  {(ctx.Facing > 0 ? "오른쪽" : "왼쪽")}\n" +
                $"무적      {(ctx.IsInvincible ? "O" : "X")}\n" +
                $"\n" +
                $"대시 스택 {ctx.Dash.Current} / {ctx.Dash.Max}   충전 {ctx.Dash.RechargeProgress * 100f,3:0}%\n" +
                $"코요테    {(ctx.Jump.HasCoyoteGrace ? "유예 중" : "-")}\n" +
                $"점프 버퍼 {(ctx.Jump.HasBufferedJump ? "대기 중" : "-")}\n" +
                $"\n" +
                $"A/D 이동  Space 점프  Shift 대시\n" +
                $"F1 이 창 켜고 끄기";

            // 읽기 좋게 뒤에 어두운 판을 깐다.
            GUI.Box(new Rect(8, 8, 290, 210), GUIContent.none);
            GUI.Label(new Rect(18, 14, 280, 200), text, _style);
        }
    }
}
