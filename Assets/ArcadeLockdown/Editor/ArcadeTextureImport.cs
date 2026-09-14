#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArcadeLockdown.Editor
{
    public sealed class ArcadeTextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("ArcadeLockdown/Resources/PBR/PolyHaven/")) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            bool normal = assetPath.Contains("_nor_gl");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = assetPath.Contains("_diff");
            importer.maxTextureSize = 1024;
            importer.anisoLevel = 8;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
        }
    }
}
#endif
