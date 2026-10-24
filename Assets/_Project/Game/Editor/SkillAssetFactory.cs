using System.IO;
using RT.Data;
using UnityEditor;
using UnityEngine;

namespace RT.Game.EditorTools
{
    /// <summary>
    /// Q · W · E 스킬 에셋을 전투방식설계서 3.2 수치로 만든다.
    ///
    /// 손으로 세 개를 만들고 열다섯 칸씩 채우게 하는 대신 코드로 둔 이유:
    ///   설계서 수치가 코드 한 곳에 남아, 나중에 "왜 이 값이지"를 설계서와 대조할 수 있다.
    ///   이미 있는 에셋은 덮어쓰지 않고 값만 다시 맞춘다.
    ///
    /// 만들어진 에셋은 Assets/_Project/Data 에 남으므로 인스펙터에서 자유롭게 고칠 수 있다.
    /// 다시 메뉴를 돌리면 설계서 값으로 되돌아간다.
    /// </summary>
    public static class SkillAssetFactory
    {
        public const string DataDir = "Assets/_Project/Data";

        private const string QPath = DataDir + "/Skill_Q_돌진베기.asset";
        private const string WPath = DataDir + "/Skill_W_회전베기.asset";
        private const string EPath = DataDir + "/Skill_E_처형베기.asset";

        /// <summary>Q · W · E 에셋 경로. 씬에 꽂기 직전에 이 경로로 다시 읽어야 한다.</summary>
        public static readonly string[] SkillPaths = { QPath, WPath, EPath };

        /// <summary>
        /// 세 에셋을 만들고 설계서 값으로 맞춘다. **참조를 돌려주지 않는다.**
        ///
        /// 여기서 돌려준 참조를 들고 있다가 씬을 새로 만들면,
        /// NewScene 이 "아무도 안 쓰는 에셋"으로 보고 메모리에서 내려 버린다.
        /// 그 뒤에 꽂으면 씬에는 빈 참조(fileID: 0)가 저장된다 — 실제로 당했다.
        ///
        /// 그래서 만들기와 읽기를 분리했다. 읽기는 LoadAll 로, 씬을 만든 **뒤에** 한다.
        /// </summary>
        public static void CreateAll()
        {
            ConfigureQ(LoadOrCreate<SkillData>(QPath));
            ConfigureW(LoadOrCreate<SkillData>(WPath));
            ConfigureE(LoadOrCreate<SkillData>(EPath));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 디스크에서 지금 막 읽어 온 Q · W · E.
        /// **씬에 꽂기 바로 직전에** 부른다.
        /// </summary>
        public static SkillData[] LoadAll()
        {
            var result = new SkillData[SkillPaths.Length];

            for (int i = 0; i < SkillPaths.Length; i++)
            {
                result[i] = LoadPersisted<SkillData>(SkillPaths[i]);
            }

            return result;
        }

        /// <summary>
        /// 경로에서 읽되, **디스크에 실제로 있는 에셋인지 확인한다.**
        ///
        /// AssetDatabase.Contains 가 false 면 메모리에만 있는 유령이다.
        /// 그걸 씬에 꽂으면 저장할 때 조용히 버려지므로, 여기서 잡고 null 을 돌려준다.
        /// </summary>
        public static T LoadPersisted<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                Debug.LogError($"[에셋] {path} 를 읽지 못했습니다.");
                return null;
            }

            if (!AssetDatabase.Contains(asset))
            {
                Debug.LogError(
                    $"[에셋] {path} 가 디스크 에셋이 아닙니다. " +
                    "씬에 꽂아도 저장되지 않습니다.");
                return null;
            }

            return asset;
        }

        /// <summary>Q 돌진 베기 — 거리를 좁히는 기술. 쿨 6s · 180%</summary>
        private static void ConfigureQ(SkillData s)
        {
            s.slot = SkillSlot.Q;
            s.displayName = "돌진 베기";
            s.cooldown = 6f;
            s.damageCoefficient = 1.8f;
            s.windup = 0.2f;
            s.totalDuration = 0.7f;
            s.shape = HitboxShape.Box;
            s.boxSize = new Vector2(1.8f, 1.8f);
            s.forwardOffset = 1.2f;
            s.dashDistance = 3f;               // 전방 3m 돌진
            s.knockbackLevel = 0;              // 넉백 없음 — 밀어내면 후속타가 빗나간다
            s.canMoveWhileCasting = false;
            s.invincibleWhileCasting = false;  // "돌진 중 무적 없음"이 설계 의도다
            s.cancelledOnHit = false;
            s.cancelCooldownRatio = 1f;

            EditorUtility.SetDirty(s);
        }

        /// <summary>W 회전 베기 — 포위를 푸는 기술. 쿨 10s · 120% · 반경 2.5m</summary>
        private static void ConfigureW(SkillData s)
        {
            s.slot = SkillSlot.W;
            s.displayName = "회전 베기";
            s.cooldown = 10f;
            s.damageCoefficient = 1.2f;
            s.windup = 0.35f;
            s.totalDuration = 0.9f;
            s.shape = HitboxShape.Circle;
            s.radius = 2.5f;
            s.forwardOffset = 0f;              // 자기 중심이라 앞으로 밀지 않는다
            s.boxSize = new Vector2(2.5f, 2.5f);
            s.dashDistance = 0f;
            s.knockbackLevel = 1;              // 약 (0.5m) — 붙은 적을 떼어낸다
            s.canMoveWhileCasting = false;
            s.invincibleWhileCasting = false;
            s.cancelledOnHit = false;
            s.cancelCooldownRatio = 1f;

            EditorUtility.SetDirty(s);
        }

        /// <summary>E 처형 베기 — 그로기 구간 전용. 쿨 15s · 300% · 차지 0.7s</summary>
        private static void ConfigureE(SkillData s)
        {
            s.slot = SkillSlot.E;
            s.displayName = "처형 베기";
            s.cooldown = 15f;
            s.damageCoefficient = 3.0f;
            s.windup = 0.7f;                   // 차지 0.7s — 쓸 타이밍이 곧 실력이다
            s.totalDuration = 1.3f;
            s.shape = HitboxShape.Box;
            s.boxSize = new Vector2(2.5f, 2.2f);
            s.forwardOffset = 1.5f;
            s.radius = 2.5f;
            s.dashDistance = 0f;
            s.knockbackLevel = 2;              // 중 (1m)
            s.canMoveWhileCasting = false;     // 차지 중 이동 불가
            s.invincibleWhileCasting = false;
            s.cancelledOnHit = true;           // 차지 중 피격당하면 취소된다 (설계서 3.3)
            s.cancelCooldownRatio = 0.5f;      // 쿨타임 절반만 소모 — 실수 비용 완화

            EditorUtility.SetDirty(s);
        }

        /// <summary>
        /// 에셋이 없으면 만들고, 있으면 그걸 쓴다.
        /// 어느 쪽이든 **디스크에서 읽은** 인스턴스를 돌려준다.
        /// </summary>
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<T>(path) ?? created;
        }
    }
}
