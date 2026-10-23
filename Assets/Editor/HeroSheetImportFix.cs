#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the hero sprite sheet imported as a plain, full-size, uncompressed texture.
/// The game cuts the frames itself at runtime, so the sheet must NOT be sliced in the Sprite Editor.
/// Runs automatically after scripts compile, whenever the sheet is imported, and from Build Everything.
/// </summary>
public class HeroSheetImportFix : AssetPostprocessor
{
    const string SheetPath = "Assets/Resources/GlitchVerse/hero_sheet.png";

    void OnPreprocessTexture()
    {
        if (assetPath != SheetPath) return;
        Apply((TextureImporter)assetImporter);
    }

    static void Apply(TextureImporter ti)
    {
        ti.textureType = TextureImporterType.Default;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.isReadable = false;
        ti.npotScale = TextureImporterNPOTScale.None;
    }

    static bool NeedsFix(TextureImporter ti)
    {
        return ti.textureType != TextureImporterType.Default || ti.maxTextureSize < 4096 ||
               ti.textureCompression != TextureImporterCompression.Uncompressed || ti.mipmapEnabled;
    }

    public static void Fix()
    {
        TextureImporter ti = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (ti == null) return;
        if (!NeedsFix(ti)) return;
        Apply(ti);
        ti.SaveAndReimport();
        Debug.Log("GlitchVerse: hero sheet import settings fixed (plain texture, 4096, uncompressed).");
    }

    [InitializeOnLoadMethod]
    static void AutoFix()
    {
        EditorApplication.delayCall += Fix;
    }
}
#endif
