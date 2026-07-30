using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterHeadCell : MonoBehaviour
{
    [Header("UI组件")]
    public Image avatarImage;
    public Transform selectParent;//高亮父物体
    [Header("高亮框")]
    public GameObject highlightPrefab;

    private GameObject _highlightInstance;
    public int RoleID
    {
        get;
        private set;
    }
    private CharacterPanelUI _parentPanel;

    private void Awake()
    {
        //在高亮父物体下动态创建高亮框，并默认隐藏
        if (selectParent != null && highlightPrefab != null)
        {
            _highlightInstance = Instantiate(highlightPrefab, selectParent);
            _highlightInstance.SetActive(false);
        }
    }

    public void Refresh(RoleRuntimeData data, CharacterPanelUI parent)
    {
        RoleID = data.roleID;
        _parentPanel = parent;

        //设置头像
        if (avatarImage != null)
        {
            Sprite avatar = GameManager.INSTANCE.GetAvatar(RoleID);
            if (avatar != null)
                avatarImage.sprite = avatar;
        }
        //绑定点击事件
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => _parentPanel.OnCharacterSelected(RoleID));
        }
    }

    /// <summary>
    /// 设置选中高亮状态
    /// </summary>
    /// <param name="selected"></param>
    public void SetSelected(bool selected)
    {
        if (_highlightInstance != null)
        {
            _highlightInstance.SetActive(selected);
        }
    }
}
