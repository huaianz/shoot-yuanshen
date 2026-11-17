using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 按 MMD4Mecanim 导出的 XML 自动给材质绑定颜色贴图。
/// 支持多个 FBX 变体共用材质目录，以及材质名带 .001/.002 后缀的情况。
/// 用法: 在 Project 窗口选中模型文件夹(包含 .xml 和 Materials) -> Tools -> Assign MMD Textures From XML
/// </summary>
public static class MMDTextureAssigner
{
    private static readonly Dictionary<string, string> KnownTextureOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 宵宫 top/topless 变体共用同名的衣装材质，需要按 FBX 导入后的后缀区分。
            { "衣.002", "tex/衣2.png" },
            { "衣2.001", "tex/衣2.png" },
            { "衣.004", "tex/衣2.png" },
            { "衣2.002", "tex/衣2.png" },
            { "衣3", "tex/衣.png" },
            { "衣3.001", "tex/衣.png" }
        };

    private sealed class MaterialTextureMapping
    {
        public string MaterialName;
        public string JapaneseName;
        public string SurfaceName;
        public string TexturePath;
    }

    [MenuItem("Tools/Assign MMD Textures From XML")]
    private static void Assign()
    {
        string folder = GetSelectedFolder();
        if (string.IsNullOrEmpty(folder))
        {
            EditorUtility.DisplayDialog("MMD Textures", "请先在 Project 窗口选中模型文件夹", "OK");
            return;
        }

        var xmlFiles = FindMmdXmls(folder);
        if (xmlFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("MMD Textures", "选中目录里没找到有效的 MMD XML", "OK");
            return;
        }

        var exactMappings = new Dictionary<string, MaterialTextureMapping>(StringComparer.OrdinalIgnoreCase);
        var nameMappings = new Dictionary<string, MaterialTextureMapping>(StringComparer.OrdinalIgnoreCase);
        var surfaceMappings = new Dictionary<string, MaterialTextureMapping>(StringComparer.OrdinalIgnoreCase);

        foreach (string xmlPath in xmlFiles)
            ReadMappings(xmlPath, exactMappings, nameMappings, surfaceMappings);

        var materials = new List<Material>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) materials.Add(material);
        }

        int assigned = 0;
        int skipped = 0;
        int missing = 0;

        foreach (Material material in materials)
        {
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
            {
                skipped++;
                continue;
            }

            string texturePath = null;
            MaterialTextureMapping mapping = null;
            if (!KnownTextureOverrides.TryGetValue(material.name, out texturePath)
                && !TryResolveMapping(material.name, exactMappings, nameMappings, surfaceMappings,
                    out mapping))
            {
                missing++;
                continue;
            }

            if (string.IsNullOrEmpty(texturePath))
                texturePath = mapping.TexturePath;

            Texture2D texture = LoadTexture(folder, texturePath);
            if (texture == null)
            {
                missing++;
                continue;
            }

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);

            EditorUtility.SetDirty(material);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MMD Textures] XML {xmlFiles.Count} 个, 材质 {materials.Count} 个, " +
                  $"已绑定 {assigned} 个, 已有贴图跳过 {skipped} 个, 失败/缺图 {missing}");
    }

    private static bool TryResolveMapping(
        string materialName,
        Dictionary<string, MaterialTextureMapping> exactMappings,
        Dictionary<string, MaterialTextureMapping> nameMappings,
        Dictionary<string, MaterialTextureMapping> surfaceMappings,
        out MaterialTextureMapping mapping)
    {
        if (exactMappings.TryGetValue(materialName, out mapping))
            return true;

        string normalizedName = Regex.Replace(materialName, @"\.\d{3}$", string.Empty);
        if (nameMappings.TryGetValue(normalizedName, out mapping))
            return true;

        return surfaceMappings.TryGetValue(normalizedName, out mapping);
    }

    private static void ReadMappings(
        string xmlPath,
        Dictionary<string, MaterialTextureMapping> exactMappings,
        Dictionary<string, MaterialTextureMapping> nameMappings,
        Dictionary<string, MaterialTextureMapping> surfaceMappings)
    {
        XDocument doc = LoadSanitizedXml(xmlPath);
        var textureFiles = new List<string>();
        XElement textureList = doc.Root?.Element("textureList");
        if (textureList != null)
        {
            foreach (XElement texture in textureList.Elements("Texture"))
                textureFiles.Add(texture.Element("fileName")?.Value ?? string.Empty);
        }

        XElement materialList = doc.Root?.Element("materialList");
        if (materialList == null) return;

        foreach (XElement material in materialList.Elements("Material"))
        {
            int textureId = int.TryParse(material.Element("textureID")?.Value, out int id) ? id : -1;
            if (textureId < 0 || textureId >= textureFiles.Count) continue;

            var mapping = new MaterialTextureMapping
            {
                MaterialName = material.Element("materialName")?.Value?.Trim(),
                JapaneseName = material.Element("nameJp")?.Value?.Trim(),
                SurfaceName = material.Element("surfaceName")?.Value?.Trim(),
                TexturePath = textureFiles[textureId]
            };

            AddMapping(exactMappings, mapping.MaterialName, mapping);
            AddMapping(nameMappings, mapping.JapaneseName, mapping);
            AddMapping(surfaceMappings, mapping.SurfaceName, mapping);
        }
    }

    private static void AddMapping(
        Dictionary<string, MaterialTextureMapping> mappings,
        string key,
        MaterialTextureMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!mappings.ContainsKey(key))
            mappings[key] = mapping;
    }

    private static XDocument LoadSanitizedXml(string path)
    {
        string text = File.ReadAllText(path);
        // MMD 发布的 XML 偶尔在注释文本中夹带 0x08 等非法控制字符。
        text = Regex.Replace(text, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", string.Empty);
        return XDocument.Parse(text, LoadOptions.PreserveWhitespace);
    }

    private static string GetSelectedFolder()
    {
        UnityEngine.Object selected = Selection.activeObject;
        if (selected == null) return null;

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path)) return null;
        return Directory.Exists(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/');
    }

    private static List<string> FindMmdXmls(string folder)
    {
        var result = new List<string>();
        foreach (string file in Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories))
        {
            try
            {
                XDocument doc = LoadSanitizedXml(file);
                if (doc.Root != null && doc.Root.Name.LocalName == "MMDModel")
                    result.Add(file.Replace('\\', '/'));
            }
            catch
            {
                // MMD4Mecanim.xml 等非模型数据文件会被跳过。
            }
        }
        return result;
    }

    private static Texture2D LoadTexture(string folder, string relativePath)
    {
        string normalizedPath = relativePath.Replace('\\', '/');
        string fullPath = folder + "/" + normalizedPath;

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
        if (texture != null) return texture;

        string fileName = Path.GetFileNameWithoutExtension(normalizedPath);
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Texture2D", new[] { folder }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(assetPath) == fileName)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        return null;
    }
}
