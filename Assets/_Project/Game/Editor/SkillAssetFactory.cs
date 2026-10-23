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

        /// <summary>Q · W · E 순서로 돌려준다. 디스크에서 읽은 것이라 씬에 바로 연결해도 된다.</summary>
        public static SkillData[] CreateAll()
        {
            ConfigureQ(LoadOrCreate<SkillData>(QPath));
            ConfigureW(LoadOrCreate<SkillData>(WPath));
            ConfigureE(LoadOrCreate<SkillData>(EPath));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 메모리 인스턴스를 씬에 연결하면 저장할 때 참조가 조용히 버려진다.
            // 반드시 디스크에서 다시 읽어서 넘긴다.
            return new[]
            {
                AssetDatabase.LoadAssetAtPath<SkillData>(QPath),
                AssetDatabase.LoadAssetAtPath<SkillData>(WPath),
                AssetDatabase.LoadAssetAtPath<SkillData>(EPath),
            };
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
