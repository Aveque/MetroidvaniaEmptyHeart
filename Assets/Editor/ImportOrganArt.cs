#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ImportOrganArt
{
    [InitializeOnLoadMethod]static void Schedule()=>EditorApplication.delayCall+=Import;
    static void Import()
    {
        if(EditorApplication.isPlaying)return;
        const string dir="Assets/Resources/Organs/";
        foreach(string n in new[]{"siphon","reservoir","tendon"})
        {
            string path=dir+n+".png";if(!File.Exists(path))continue;
            var i=AssetImporter.GetAtPath(path) as TextureImporter;if(!i || (i.textureType==TextureImporterType.Sprite && i.spriteImportMode==SpriteImportMode.Single))continue;
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=true;i.filterMode=FilterMode.Trilinear;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=1024;i.SaveAndReimport();
        }
        if(!AssetDatabase.LoadAssetAtPath<Sprite>(dir+"impulse.asset"))
        {
            var glow=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/StationRefinement/MachineEmission.asset");if(glow){var copy=Object.Instantiate(glow);copy.name="Residual neural impulse";AssetDatabase.CreateAsset(copy,dir+"impulse.asset");AssetDatabase.SaveAssets();}
        }
    }
}
#endif

