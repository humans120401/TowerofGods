using RT.Data;
using RT.Player;
using UnityEngine;

namespace RT.Game
{
    /// <summary>
    /// 화면 좌상단에 플레이어 상태를 띄운다.
    ///
    /// 손맛을 판정하려면 숫자가 보여야 한다.
    /// 패링 창이 실제로 열렸는지, 무적이 언제 끊기는지는 느낌만으로 알 수 없다.
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

        [Tooltip("층 진입 상태로 되돌린다 — 체력·쿨·게이지 초기화. 죽어도 듣는다")]
        [SerializeField] private KeyCode resetKey = KeyCode.F2;

        private GUIStyle _style;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<PlayerBrain>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) show = !show;

            // 전투를 시험하다 죽으면 씬을 다시 켜야 한다. 그게 제일 성가시다.
            if (Input.GetKeyDown(resetKey) && player != null) player.ResetForFloor();
        }

        private void OnGUI()
        {
            if (!show || player == null) return;

            var ctx = player.Context;
            if (ctx == null) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                richText = true,
            };

            var v = ctx.Body.linearVelocity;

            string parry = ctx.IsParryActive
                ? "<color=#6cf>열림</color>"
                : "-";

            // 누가 무적을 켰는지까지 보여 준다.
            // "대시"와 "피격"이 동시에 켜지는 경우를 눈으로 확인해야 하기 때문이다.
            string invincible = ctx.IsInvincible
                ? $"<color=#fd6>{ctx.Invincible.Describe()}</color>"
                : "-";

            string text =
                $"<b>{player.CurrentState}</b>   속도 x{v.x,6:0.0} y{v.y,6:0.0}\n" +
                $"접지 {(ctx.IsGrounded ? "O" : "X")}   " +
                $"무적 {invincible}\n" +
                $"패링창 {parry}\n" +
                $"\n" +
                $"체력   {ctx.Health,3} / {ctx.Stats.maxHealth}\n" +
                $"게이지 {ctx.Gauge.Current,3} / {ctx.Gauge.Max}" +
                $"{(ctx.Gauge.IsFull ? "  <color=#fd6>궁극기 준비</color>" : "")}\n" +
                $"대시   {ctx.Dash.Current} / {ctx.Dash.Max}" +
                $"   충전 {ctx.Dash.RechargeProgress * 100f,3:0}%\n" +
                $"\n" +
                $"{CooldownLine(ctx, SkillSlot.Q, "Q 돌진")}\n" +
                $"{CooldownLine(ctx, SkillSlot.W, "W 회전")}\n" +
                $"{CooldownLine(ctx, SkillSlot.E, "E 처형")}\n" +
                $"\n" +
                $"코요테 {(ctx.Jump.HasCoyoteGrace ? "유예" : "-")}   " +
                $"버퍼 {(ctx.Jump.HasBufferedJump ? "대기" : "-")}\n" +
                $"\n" +
                $"<color=#999>A/D 이동  Space 점프  Shift 대시\n" +
                $"Z 공격  F 패링  Q/W/E 스킬\n" +
                $"F1 이 창  F2 초기화</color>";

            GUI.Box(new Rect(8, 8, 330, 300), GUIContent.none);
            GUI.Label(new Rect(18, 14, 320, 290), text, _style);
        }

        private static string CooldownLine(PlayerContext ctx, SkillSlot slot, string label)
        {
            var skill = ctx.SkillOf(slot);

            if (skill == null) return $"{label}  <color=#666>(없음)</color>";

            float remaining = ctx.Cooldowns.Remaining(slot);

            return remaining <= 0f
                ? $"{label}  <color=#6f6>준비</color>"
                : $"{label}  {remaining,4:0.0}s";
        }
    }
}
