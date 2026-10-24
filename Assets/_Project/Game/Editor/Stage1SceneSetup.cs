using System.IO;
using RT.Data;
using RT.Player;
using RT.Player.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RT.Game.EditorTools
{
    /// <summary>
    /// 1단계 테스트 씬을 메뉴 한 번으로 만든다.
    ///
    /// 손으로 만들면 오브젝트 8개에 컴포넌트를 붙이고 값을 맞춰야 하는데,
    /// 한 군데만 틀려도 원인을 찾기 어렵다. 자동으로 만들면 늘 같은 상태에서 시작한다.
    ///
    /// 메뉴: Tower of Gods ▸ 1단계 테스트 씬 만들기
    ///
    /// 다시 눌러도 안전하다 — 씬을 새로 만들어 덮어쓴다.
    /// </summary>
    public static class Stage1SceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Stage1_Movement.unity";
        private const string StatsPath = "Assets/_Project/Data/PlayerStats.asset";

        [MenuItem("Tower of Gods/1단계 테스트 씬 만들기", false, 1)]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog(
                    "1단계 테스트 씬 만들기",
                    "이동·점프·대시를 시험할 씬을 새로 만듭니다.\n\n" +
                    "저장하지 않은 현재 씬은 먼저 저장하라고 물어봅니다.",
                    "만들기", "취소"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 1) 에셋을 먼저 만들어 디스크에 확정시킨다. 참조는 들고 가지 않는다.
            SkillAssetFactory.LoadOrCreate<PlayerStats>(StatsPath);
            EditorPrimitives.WhiteSquare();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            // 2) 씬을 만든 **뒤에** 디스크에서 다시 읽는다.
            //    NewScene 이 안 쓰는 에셋을 메모리에서 내리기 때문에,
            //    앞에서 읽어 둔 참조를 꽂으면 저장할 때 조용히 버려진다.
            var stats = SkillAssetFactory.LoadPersisted<PlayerStats>(StatsPath);

            BuildCamera();
            BuildGround();
            var player = BuildPlayer(stats);
            BuildOverlay(player);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 저장 뒤에 연결이 살아남았는지 확인한다. 1단계는 스킬이 없다.
            bool ok = SceneWiring.VerifyAndRepair(ScenePath, StatsPath, null);

            EditorUtility.DisplayDialog(
                ok ? "완성" : "완성 — 다만 확인할 게 있습니다",
                (ok ? "" : "연결 일부가 저장되지 않았습니다. Console 을 보세요.\n\n") +
                "Stage1_Movement 씬을 만들었습니다.\n\n" +
                "Play 를 누르고 Game 창을 한 번 클릭한 뒤\n" +
                "A / D 이동, Space 점프, Shift 대시를 해 보세요.\n\n" +
                "좌상단 숫자가 안 보이면 F1 을 누르세요.",
                "확인");
        }

        // ── 카메라 ──

        private static void BuildCamera()
        {
            var cam = Camera.main;

            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.orthographic = true;

            // 설계서 기준 보스방 가로 20m. 세로 9m 정도면 16:9에서 가로 16m가 보인다.
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 2f, -10f);
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // ── 지면 ──

        private static void BuildGround()
        {
            var root = new GameObject("Ground");

            // 바닥 — 가로 40m
            MakeBlock(root.transform, "Floor",
                new Vector2(0f, -1f), new Vector2(40f, 2f),
                new Color(0.25f, 0.27f, 0.32f));

            // 발판 — 코요테 타임을 시험하려면 '끝'이 있는 발판이 필요하다
            MakeBlock(root.transform, "Platform_Low",
                new Vector2(7f, 1.2f), new Vector2(4f, 0.5f),
                new Color(0.3f, 0.34f, 0.4f));

            MakeBlock(root.transform, "Platform_Mid",
                new Vector2(13f, 2.6f), new Vector2(4f, 0.5f),
                new Color(0.3f, 0.34f, 0.4f));

            // 점프 높이를 눈으로 재는 벽.
            // 1.8m 점프로 못 넘는 2m 벽과, 넘을 수 있는 1.1m 벽을 나란히 둔다.
            MakeBlock(root.transform, "Wall_2m_넘지못함",
                new Vector2(-6f, 1f), new Vector2(0.6f, 2f),
                new Color(0.5f, 0.25f, 0.25f));

            MakeBlock(root.transform, "Wall_1.1m_넘을수있음",
                new Vector2(-10f, 0.55f), new Vector2(0.6f, 1.1f),
                new Color(0.25f, 0.45f, 0.3f));
        }

        private static void MakeBlock(
            Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSquare();
            sr.color = color;

            go.AddComponent<BoxCollider2D>();
        }

        // ── 플레이어 ──

        private static PlayerBrain BuildPlayer(PlayerStats stats)
        {
            var go = new GameObject("Player");
            go.transform.position = new Vector3(0f, 1f, 0f);

            // 설계서 기준 PC 규격: 폭 0.35m, 키 1.6m
            go.transform.localScale = new Vector3(0.35f, 1.6f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSquare();
            sr.color = new Color(0.4f, 0.85f, 0.9f);
            sr.sortingOrder = 10;

            var body = go.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = stats.GravityScale;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            go.AddComponent<BoxCollider2D>();
            go.AddComponent<KeyboardInput>();

            // 발밑 기준점 — 콜라이더 바닥보다 아주 조금 아래
            var check = new GameObject("GroundCheck");
            check.transform.SetParent(go.transform);
            check.transform.localPosition = new Vector3(0f, -0.52f, 0f);

            var brain = go.AddComponent<PlayerBrain>();

            // private [SerializeField] 는 SerializedObject 로 넣는다.
            var so = new SerializedObject(brain);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("groundCheck").objectReferenceValue = check.transform;
            so.FindProperty("groundLayer").intValue = ~0;      // 전부
            so.FindProperty("logStateChanges").boolValue = false;

            // 발 크기는 스케일이 걸린 로컬 기준이라 작게 잡는다.
            so.FindProperty("groundCheckSize").vector2Value = new Vector2(0.9f, 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // 주입이 실제로 됐는지 확인한다.
            // SerializedObject 는 조용히 실패할 수 있어서, 눈으로 확인할 수단을 둔다.
            var verify = new SerializedObject(brain);

            if (verify.FindProperty("stats").objectReferenceValue == null)
            {
                Debug.LogError(
                    "[1단계 세팅] Stats 연결에 실패했습니다.\n" +
                    "Hierarchy 에서 Player 를 고르고, Inspector 의 Player Brain ▸ Stats 칸에 " +
                    "Assets/_Project/Data/PlayerStats 를 끌어다 놓아 주세요.");
            }

            return brain;
        }

        private static void BuildOverlay(PlayerBrain player)
        {
            var go = new GameObject("DebugOverlay");
            var overlay = go.AddComponent<PlayerDebugOverlay>();

            var so = new SerializedObject(overlay);
            so.FindProperty("player").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 흰 사각형은 EditorPrimitives 가 디스크 에셋으로 만들어 준다.
        // 메모리에서 만든 스프라이트는 씬 저장 때 참조가 버려진다.

        private static Sprite WhiteSquare() => EditorPrimitives.WhiteSquare();
    }
}
