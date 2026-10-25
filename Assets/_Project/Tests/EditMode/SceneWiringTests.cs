using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace RT.Tests
{
    /// <summary>
    /// 저장된 씬 파일을 **텍스트로 읽어서** 참조가 비어 있지 않은지 본다.
    ///
    /// 왜 이런 테스트가 필요한가:
    ///   에디터에서 참조를 꽂고 바로 읽으면 멀쩡해 보이는데,
    ///   저장할 때 조용히 버려지는 경우가 있다.
    ///   에셋을 NewScene 전에 읽어 두면 NewScene 이 그걸 메모리에서 내리고,
    ///   남은 건 빈 껍데기라 씬에는 `{fileID: 0}` 이 저장된다.
    ///
    ///   Play 를 눌러야만 알 수 있었고, 두 번 당했다.
    ///   .unity 는 YAML 텍스트라 그냥 읽어서 확인할 수 있다.
    ///
    /// 씬이 아직 없으면(클론 직후 등) 그냥 넘어간다. 실패로 치지 않는다.
    /// </summary>
    public class SceneWiringTests
    {
        private const string Stage1 = "Scenes/Stage1_Movement.unity";
        private const string Stage2 = "Scenes/Stage2_Combat.unity";

        /// <summary>PlayerBrain 블록에서 비어 있으면 안 되는 칸들.</summary>
        private static readonly string[] MustNotBeEmpty = { "stats" };

        [Test]
        public void 일단계_씬의_Stats_가_연결되어_있다()
        {
            AssertWired(Stage1, expectSkills: false);
        }

        [Test]
        public void 이단계_씬의_Stats_와_스킬이_연결되어_있다()
        {
            AssertWired(Stage2, expectSkills: true);
        }

        private static void AssertWired(string relativePath, bool expectSkills)
        {
            string full = Path.Combine(Application.dataPath, relativePath);

            if (!File.Exists(full))
            {
                Assert.Ignore(
                    $"{relativePath} 가 아직 없습니다. " +
                    "메뉴 Tower of Gods 에서 씬을 만든 뒤 다시 돌리세요.");
                return;
            }

            var lines = File.ReadAllLines(full);
            var brainBlock = FindPlayerBrainBlock(lines);

            Assert.IsNotNull(
                brainBlock, $"{relativePath} 안에서 PlayerBrain 을 찾지 못했습니다.");

            foreach (var field in MustNotBeEmpty)
            {
                string line = Find(brainBlock, $"{field}:");

                Assert.IsNotNull(line, $"'{field}' 칸이 씬에 없습니다.");

                Assert.IsFalse(
                    line.Contains("fileID: 0}"),
                    $"'{field}' 가 비어 있습니다 ({line.Trim()}).\n\n" +
                    $"먼저 할 것: 메뉴 Tower of Gods 에서 {MenuFor(relativePath)} 를 " +
                    "한 번 더 실행하고 이 테스트를 다시 돌려 보세요.\n" +
                    "씬 파일이 고치기 전에 저장된 것이면 이것만으로 해결됩니다.\n\n" +
                    "다시 만들었는데도 실패하면 코드 문제입니다 — " +
                    "에셋을 NewScene 전에 읽어 두면 저장할 때 참조가 버려집니다.");
            }

            if (!expectSkills) return;

            int at = IndexOf(brainBlock, "skills:");
            Assert.Greater(at, -1, "'skills' 배열이 씬에 없습니다.");

            for (int i = at + 1; i < brainBlock.Count; i++)
            {
                string line = brainBlock[i];
                if (!line.TrimStart().StartsWith("- {")) break;

                Assert.IsFalse(
                    line.Contains("fileID: 0}"),
                    $"스킬 슬롯 {i - at - 1} 이 비어 있습니다.\n\n" +
                    $"먼저 할 것: 메뉴 Tower of Gods 에서 {MenuFor(relativePath)} 를 " +
                    "한 번 더 실행하고 이 테스트를 다시 돌려 보세요.");
            }
        }

        /// <summary>어느 메뉴로 이 씬을 다시 만드는지 알려 준다.</summary>
        private static string MenuFor(string relativePath) =>
            relativePath == Stage1
                ? "'1단계 테스트 씬 만들기'"
                : "'2단계 전투 테스트 씬 만들기'";

        /// <summary>PlayerBrain MonoBehaviour 블록만 잘라 온다.</summary>
        private static List<string> FindPlayerBrainBlock(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("RT.Player.PlayerBrain")) continue;

                var block = new List<string>();

                for (int j = i; j < lines.Length; j++)
                {
                    // 다음 오브젝트 블록이 시작되면 끝.
                    if (j > i && lines[j].StartsWith("---")) break;
                    block.Add(lines[j]);
                }

                return block;
            }

            return null;
        }

        private static string Find(List<string> block, string key)
        {
            int at = IndexOf(block, key);
            return at < 0 ? null : block[at];
        }

        private static int IndexOf(List<string> block, string key)
        {
            for (int i = 0; i < block.Count; i++)
            {
                if (block[i].TrimStart().StartsWith(key)) return i;
            }

            return -1;
        }
    }
}
