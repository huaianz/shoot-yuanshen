using UnityEditor;
using UnityEngine;

/// <summary>
/// 把 MMD4Mecanim 的材质批量转成 URP/Lit, 保留主贴图与颜色。
/// 用法: Project 窗口选中 .mat 文件 -> Tools -> Convert Selected MMD Materials To URP
/// </summary>
public static class MMDToURPConverter
{
    [MenuItem("Tools/Convert Selected MMD Materials To URP")]
    private static void ConvertSelected()
    {
        var mats = Selection.GetFiltered<Material>(
            SelectionMode.Assets | SelectionMode.DeepAssets);

        if (mats == null || mats.Length == 0)
        {
            EditorUtility.DisplayDialog("MMD to URP", "请先在 Project 窗口选中要转换的材质(.mat)", "OK");
            return;
        }

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("MMD to URP", "找不到 URP/Lit, 请确认项目使用 URP", "OK");
            return;
        }

        int converted = 0;

        foreach (var mat in mats)
        {
            if (mat == null) continue;

            Texture tex = null;
            if (mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
            else if (mat.HasProperty("_Texture")) tex = mat.GetTexture("_Texture");
            else if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");

            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

            bool alphaClip = mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f;
            float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;

            mat.shader = urpLit;

            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", color);

            if (alphaClip)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", cutoff);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }

            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MMDToURP] 已转换 {converted} 个材质");
    }
}
