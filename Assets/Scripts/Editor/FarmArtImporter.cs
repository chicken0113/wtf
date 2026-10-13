using UnityEditor;

public sealed class FarmArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath != "Assets/Resources/FarmAtlas.png") return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.maxTextureSize = 2048;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    }
}
