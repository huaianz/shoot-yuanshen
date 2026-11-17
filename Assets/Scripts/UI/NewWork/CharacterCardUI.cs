using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>角色选择里的一张卡</summary>
public class CharacterCardUI : MonoBehaviour
{
    public Image portrait;
    public TMP_Text nameText;
    public Image selectedMark;
    public Button button;

    private int _index;
    private Action<int> _onClick;

    public void Bind(int index, string displayName, Sprite sprite, bool selected, Action<int> onClick)
    {
        _index = index;
        _onClick = onClick;

        if (nameText != null) nameText.text = displayName;

        if (portrait != null)
        {
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
        }

        if (selectedMark != null)
            selectedMark.gameObject.SetActive(selected);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_index));
        }
    }
}
