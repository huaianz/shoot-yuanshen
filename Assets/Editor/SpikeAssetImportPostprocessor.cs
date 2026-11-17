using UnityEditor;
using UnityEngine;

/// <summary>
/// 新导入的下包动作强制使用 Humanoid 重定向；
/// 爆能器模型不导入外部材质，颜色全部由 SpikeBeaconVisual 在运行时生成。
/// </summary>
public sealed class SpikeAssetImportPostprocessor : AssetPostprocessor
{
    public override uint GetVersion()
    {
        return 3;
    }

    private void OnPreprocessModel()
    {
        string normalized = assetPath.Replace('\\', '/');

        if (normalized.EndsWith("/Resources/Animation/Player/SpikePlant.fbx"))
        {
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.resampleCurves = true;
        }
        else if (normalized.EndsWith("/Resources/Prefabs/Network/Spike/SpikeBeacon.fbx"))
        {
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
