using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractionUI : SingleMonoBase<InteractionUI>
{
    public GameObject interactionHint;
    public TextMeshProUGUI hintText;

    protected override void Awake()
    {
        base.Awake();
        interactionHint.SetActive(false);
    }

    /// <summary>
    /// 显示提示
    /// </summary>
    /// <param name="text"></param>
    public void ShowHint(string text = "按 F 交互")
    {
        if (hintText != null)
        {
            hintText.text = text;
        }
        interactionHint.SetActive(true);
    }

    /// <summary>
    /// 隐藏提示
    /// </summary>
    public void HideHint()
    {
        interactionHint.SetActive(false);
    }

}
