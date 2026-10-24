using System.IO;
using RT.Core.Combat;
using RT.Data;
using RT.Player;
using RT.Player.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RT.Game.EditorTools
{
    /// <summary>
    /// 2단계 전투 테스트 씬을 메뉴 한 번으로 만든다.
    ///
    /// 1단계 씬에 더해:
    ///   - 스킬 에셋 Q · W · E 를 설계서 3.2 수치로 만든다
    ///   - 때릴 허수아비 두 개 (가만히 있는 것 / 공격해 오는 것)
    ///   - 히트스톱 관리자
    ///   - 레이어 설정 (Player / Enemy)
    ///
    /// 메뉴: Tower of Gods ▸ 2단계 전투 테스트 씬 만들기
    /// </summary>
    public static class Stage2SceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Stage2_Combat.unity";
        private const string StatsPath =
            SkillAssetFactory.DataDir + "/PlayerStats.asset";

        [MenuItem("Tower of Gods/2단계 전투 테스트 씬 만들기", false, 2)]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog(
                    "2단계 전투 테스트 씬 만들기",
                    "콤보 · Q/W/E · 패링 · 피격을 시험할 씬을 새로 만듭니다.\n\n" +
                    "저장하지 않은 현재 씬은 먼저 저장하라고 물어봅니다.",
                    "만들기", "취소"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var stats = SkillAssetFactory.LoadOrCreate<PlayerStats>(StatsPath);
            var skills = SkillAssetFactory.CreateAll();

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildCamera();
            BuildGround();

            var player = BuildPlayer(stats, skills);

            MakeDummy("Dummy_가만히", new Vector2(5f, 0.8f), attacks: false);
            MakeDummy("Dummy_공격함", new Vector2(10f, 0.8f), attacks: true);

            new GameObject("HitStop").AddComponent<HitStop>();
            BuildOverlay(player);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorUtility.DisplayDialog(
                "완성",
                "Stage2_Combat 씬을 만들었습니다.\n\n" +
                "Z 공격 · F 패링 · Q/W/E 스킬\n\n" +
                "오른쪽 허수아비가 색으로 예고합니다 (네 종류를 돌아가며 씁니다):\n" +
                "  노랑 — 패링도 대시도 통함\n" +
                "  빨강 — 패링 불가. F를 눌러도 맞아야 정상\n" +
                "  보라 — 둘 다 불가. 범위를 벗어나는 수밖에 없음\n" +
                "  주황 — 패링만 통함. 대시로 피하려 하면 맞아야 정상\n\n" +
                "F2 를 누르면 체력·쿨·게이지가 초기화됩니다.",
                "확인");
        }

        // ── 씬 구성 ──

        private static void BuildCamera()
        {
            var cam = Camera.main;

            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(5f, 3f, -10f);
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void BuildGround()
        {
            var root = new GameObject("Ground");

            MakeBlock(root.transform, "Floor",
                new Vector2(5f, -1f), new Vector2(50f, 2f),
                new Color(0.25f, 0.27f, 0.32f));

            MakeBlock(root.transform, "Wall_2m_넘지못함",
                new Vector2(-6f, 1f), new Vector2(0.6f, 2f),
                new Color(0.5f, 0.25f, 0.25f));

            MakeBlock(root.transform, "Wall_1.1m_넘을수있음",
                new Vector2(-9f, 0.55f), new Vector2(0.6f, 1.1f),
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
            sr.sprite = EditorPrimitives.WhiteSquare();
            sr.color = color;

            go.AddComponent<BoxCollider2D>();
        }

        private static PlayerBrain BuildPlayer(PlayerStats stats, SkillData[] skills)
        {
            var go = new GameObject("Player") { layer = 0 };
            go.transform.position = new Vector3(0f, 1f, 0f);
            go.transform.localScale = new Vector3(0.35f, 1.6f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EditorPrimitives.WhiteSquare();
            sr.color = new Color(0.4f, 0.85f, 0.9f);
            sr.sortingOrder = 10;

            var body = go.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = stats.GravityScale;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            go.AddComponent<BoxCollider2D>();
            go.AddComponent<KeyboardInput>();

            var check = new GameObject("GroundCheck");
            check.transform.SetParent(go.transform);
            check.transform.localPosition = new Vector3(0f, -0.52f, 0f);

            var brain = go.AddComponent<PlayerBrain>();
            go.AddComponent<PlayerHealth>();

            var so = new SerializedObject(brain);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("groundCheck").objectReferenceValue = check.transform;
            so.FindProperty("groundLayer").intValue = ~0;
            so.FindProperty("enemyLayers").intValue = ~0;
            so.FindProperty("groundCheckSize").vector2Value = new Vector2(0.9f, 0.1f);
            so.FindProperty("logStateChanges").boolValue = false;

            var arr = so.FindProperty("skills");
            arr.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // 연결이 조용히 실패하면 게임에서는 '스킬 (없음)' 으로만 보인다.
            // 원인을 찾기 어려우므로 여기서 바로 잡아 준다.
            var verify = new SerializedObject(brain);

            if (verify.FindProperty("stats").objectReferenceValue == null)
            {
                Debug.LogError(
                    "[2단계 세팅] Stats 연결 실패. Player ▸ Player Brain ▸ Stats 칸에 " +
                    "Assets/_Project/Data/PlayerStats 를 직접 넣어 주세요.");
            }

            var verifySkills = verify.FindProperty("skills");

            for (int i = 0; i < verifySkills.arraySize; i++)
            {
                if (verifySkills.GetArrayElementAtIndex(i).objectReferenceValue != null) continue;

                Debug.LogError(
                    $"[2단계 세팅] {(SkillSlot)i} 스킬 연결 실패. " +
                    "Assets/_Project/Data 의 Skill_ 에셋을 " +
                    "Player ▸ Player Brain ▸ Skills 에 직접 넣어 주세요.");
            }

            return brain;
        }

        private static void MakeDummy(string name, Vector2 pos, bool attacks)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 2.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EditorPrimitives.WhiteSquare();
            sr.color = attacks
                ? new Color(0.8f, 0.45f, 0.45f)
                : new Color(0.55f, 0.55f, 0.6f);
            sr.sortingOrder = 5;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;   // 플레이어를 밀지 않게

            var dummy = go.AddComponent<TrainingDummy>();

            var so = new SerializedObject(dummy);
            so.FindProperty("attacks").boolValue = attacks;
            so.FindProperty("playerLayers").intValue = ~0;

            // 플레이어 공격 사거리는 약 1.7m (오프셋 0.9 + 박스 1.6의 절반).
            // 허수아비 범위를 그보다 좁게 두면 때려도 반격을 안 맞아
            // 패링을 연습할 상황 자체가 만들어지지 않는다.
            so.FindProperty("hitboxSize").vector2Value = new Vector2(4f, 3f);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildOverlay(PlayerBrain player)
        {
            var go = new GameObject("DebugOverlay");
            var overlay = go.AddComponent<PlayerDebugOverlay>();

            var so = new SerializedObject(overlay);
            so.FindProperty("player").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
