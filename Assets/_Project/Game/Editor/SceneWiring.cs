using RT.Data;
using RT.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RT.Game.EditorTools
{
    /// <summary>
    /// 씬을 저장한 **뒤에** 연결이 살아남았는지 확인하고, 끊겼으면 다시 붙인다.
    ///
    /// 왜 필요한가:
    ///   에디터에서 참조를 꽂고 바로 읽으면 멀쩡해 보인다.
    ///   그런데 저장 과정에서 조용히 버려지는 경우가 있다 —
    ///   참조 대상이 아직 디스크에 완전히 들어가지 않았거나,
    ///   중간에 에셋 재임포트가 끼어든 경우다.
    ///
    ///   그러면 Play 를 눌러야만 "Stats 가 비어 있다"는 경고로 알게 된다.
    ///   두 번 당했다. 그래서 추측으로 한 곳을 고치는 대신,
    ///   **저장한 파일을 다시 열어서 눈으로 확인하는** 단계를 둔다.
    ///
    /// 고쳐지면 조용히 넘어가고, 그래도 안 되면 무엇을 손으로 꽂아야 하는지 알려 준다.
    /// </summary>
    public static class SceneWiring
    {
        /// <summary>
        /// 저장된 씬을 다시 열어 PlayerBrain 의 연결을 확인한다.
        /// 끊긴 게 있으면 다시 붙이고 저장한다.
        /// </summary>
        /// <returns>전부 멀쩡하면 true</returns>
        public static bool VerifyAndRepair(
            string scenePath, PlayerStats stats, SkillData[] skills)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var brain = Object.FindAnyObjectByType<PlayerBrain>();

            if (brain == null)
            {
                Debug.LogError($"[씬 검증] {scenePath} 안에 PlayerBrain 이 없습니다.");
                return false;
            }

            var so = new SerializedObject(brain);
            bool repaired = false;

            repaired |= Reconnect(so.FindProperty("stats"), stats, "Stats");

            if (skills != null)
            {
                var arr = so.FindProperty("skills");
                if (arr.arraySize < skills.Length) arr.arraySize = skills.Length;

                for (int i = 0; i < skills.Length; i++)
                {
                    repaired |= Reconnect(
                        arr.GetArrayElementAtIndex(i), skills[i], $"Skills[{i}]");
                }
            }

            if (!repaired) return true;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(brain);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);

            // 두 번째도 안 붙었으면 손으로 꽂는 수밖에 없다.
            return ReportIfStillBroken(scenePath);
        }

        /// <summary>비어 있으면 다시 꽂는다. 꽂았으면 true.</summary>
        private static bool Reconnect(
            SerializedProperty property, Object value, string label)
        {
            if (property == null)
            {
                Debug.LogError($"[씬 검증] '{label}' 칸을 찾지 못했습니다.");
                return false;
            }

            if (property.objectReferenceValue != null) return false;

            if (value == null)
            {
                Debug.LogError(
                    $"[씬 검증] '{label}' 에 넣을 에셋 자체가 없습니다. " +
                    "Assets/_Project/Data 를 확인하세요.");
                return false;
            }

            property.objectReferenceValue = value;

            Debug.Log($"[씬 검증] '{label}' 연결이 끊겨 있어 다시 붙였습니다.");
            return true;
        }

        /// <summary>다시 저장한 씬을 한 번 더 열어 확인한다.</summary>
        private static bool ReportIfStillBroken(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var brain = Object.FindAnyObjectByType<PlayerBrain>();
            if (brain == null) return false;

            var check = new SerializedObject(brain);

            if (check.FindProperty("stats").objectReferenceValue != null) return true;

            Debug.LogError(
                "[씬 검증] 두 번 시도했는데도 Stats 연결이 저장되지 않습니다.\n" +
                "Hierarchy 의 Player 를 고르고, Project 창의 " +
                "Assets/_Project/Data/PlayerStats 를 " +
                "Player Brain ▸ Stats 칸으로 끌어다 놓은 뒤 Ctrl+S 하세요.");

            return false;
        }
    }
}
