using UnityEditor;

public sealed class FarmArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath != "Assets/Resources/FarmAtlas.png" && assetPath != "Assets/Resources/FarmHouse.png") return;
        var importer = (TextureImporter)assetImporter;
        if(assetPath.EndsWith("FarmHouse.png"))
        {
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=204.8f; importer.filterMode=UnityEngine.FilterMode.Point;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
            return;
        }
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
