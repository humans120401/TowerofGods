using System.IO;
using UnityEditor;
using UnityEngine;

namespace RT.Game.EditorTools
{
    /// <summary>
    /// 아트가 없는 동안 쓸 임시 도형을 **디스크 에셋으로** 만든다.
    ///
    /// 왜 디스크냐:
    ///   씬은 '파일로 존재하는' 에셋만 참조할 수 있다.
    ///   코드에서 Sprite.Create 로 만든 스프라이트를 SpriteRenderer 에 꽂으면
    ///   에디터에서는 보이지만, 씬을 저장할 때 참조가 조용히 버려진다.
    ///   다시 열면 블록이 전부 안 보인다 (콜라이더는 남아 있어서 더 헷갈린다).
    ///
    /// 그래서 PNG 를 한 번 써 두고, 임포터 설정까지 맞춘 뒤 그걸 읽어서 쓴다.
    /// 3단계에서 실제 아트가 들어오면 이 파일은 지워도 된다.
    /// </summary>
    public static class EditorPrimitives
    {
        private const string Dir = "Assets/_Project/Art/Generated";
        private const string WhitePath = Dir + "/White.png";

        /// <summary>1 unit = 1m 가 되도록 맞춘 흰 사각형.</summary>
        public static Sprite WhiteSquare()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Dir);

            // 8×8 흰 텍스처를 PNG 로 굽는다.
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color32[8 * 8];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);

            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(WhitePath, ImportAssetOptions.ForceSynchronousImport);

            ConfigureImporter(WhitePath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[EditorPrimitives] {WhitePath} 를 스프라이트로 읽지 못했습니다. " +
                    "Project 창에서 해당 파일의 Texture Type 이 Sprite 인지 확인하세요.");
            }

            return sprite;
        }

        /// <summary>
        /// 8픽셀 = 1미터, 점 필터(픽셀이 번지지 않게), 밉맵 없음.
        /// Pixels Per Unit 을 8로 맞추는 게 핵심이다 —
        /// 이걸 안 하면 기본값 100 이라 8픽셀 사각형이 0.08m 로 나온다.
        /// </summary>
        private static void ConfigureImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 8f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.SaveAndReimport();
        }
    }
}
