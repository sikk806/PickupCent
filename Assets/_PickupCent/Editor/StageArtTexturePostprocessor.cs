using System;
using UnityEditor;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// Assets/_PickupCent/Art/Stage*/{item,tool,sprite} 밑에 새 이미지가 들어올 때마다
    /// Pixels Per Unit을 자동으로 통일한다. 값은 ArtAssetLinker.TargetSpritePixelsPerUnit 하나로만
    /// 관리해서(하드코딩 중복 없음) 두 도구가 항상 같은 기준을 쓰도록 한다.
    /// texture 폴더(지형 텍스처)는 Sprite가 아니라 PPU 개념이 없어 대상에서 제외한다.
    /// </summary>
    public class StageArtTexturePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!IsStageArtSpriteFolder(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            importer.spritePixelsPerUnit = ArtAssetLinker.TargetSpritePixelsPerUnit;
        }

        private static bool IsStageArtSpriteFolder(string path)
        {
            if (path.IndexOf("/Art/Stage", StringComparison.OrdinalIgnoreCase) < 0) return false;

            return path.IndexOf("/item/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/tool/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/sprite/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
