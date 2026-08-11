using TMPro;
using UnityEngine;

/// <summary>
/// UI 文字工具：负责给所有自动创建的 UI 提供中文字体。
/// 之后所有 UI（提示/委托/地区）都复用同一个缓存，不再重复读盘。
/// </summary>
public static class UITextHelper
{
    private static TMP_FontAsset _font;

    /// <summary>
    /// 获取中文字体
    /// </summary>
    public static TMP_FontAsset GetFont()
    {
        if (_font != null) return _font;

        string[] candidates =
        {
            "font/MSYH SDF",
            "font/汉仪文黑-85W SDF",
            "font/genshin-impact-font-regular SDF"
        };
        foreach (string path in candidates)
        {
            TMP_FontAsset f = Resources.Load<TMP_FontAsset>(path);
            if (f != null)
            {
                _font = f;
                return _font;
            }
        }
        return null;
    }
}