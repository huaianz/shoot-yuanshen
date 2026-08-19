using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameHUDUI : MonoBehaviour
{
    //游戏HUD内容，打开面板时隐藏它
    private GameObject _hudContent;

    private PackagePanel _packagePanel;
    private CharacterPanelUI _characterPanel;
    private Button _packageBtn;
    private Button _characterBtn;
    private Button _settingsBtn;
    private SettingsPanelUI _settingsPanel;

    private void Awake()
    {
        Transform content = transform.Find("GameplayHUD");
        //如果没有找到这个子物体，就隐藏整个HUD
        _hudContent = content.gameObject;

        _packagePanel = transform.root.GetComponentInChildren<PackagePanel>(true);
        _characterPanel = transform.root.GetComponentInChildren<CharacterPanelUI>(true);
        _settingsPanel = transform.root.GetComponentInChildren<SettingsPanelUI>(true);
        Transform uiIcon = transform.Find("GameplayHUD/UIicon");
        if (uiIcon != null)
        {
            _packageBtn = uiIcon.Find("PackageBtn")?.GetComponent<Button>();
            _characterBtn = uiIcon.Find("CharacterBtn")?.GetComponent<Button>();
            _settingsBtn = uiIcon.Find("SettingsBtn")?.GetComponent<Button>();
        }
    }

    private void Start()
    {
        // 在代码里绑定点击事件
        if (_packageBtn != null)
        {
            _packageBtn.onClick.RemoveAllListeners();
            _packageBtn.onClick.AddListener(OnClickPackage);
        }

        if (_characterBtn != null)
        {
            _characterBtn.onClick.RemoveAllListeners();
            _characterBtn.onClick.AddListener(OnClickCharacter);
        }
        if (_settingsBtn != null)
        {
            _settingsBtn.onClick.RemoveAllListeners();
            _settingsBtn.onClick.AddListener(OnClickSettings);
        }
    }

    private void OnEnable()
    {
        EventHandler.UIStateChangedEvent += OnUIStateChanged;
        ApplyUIState(UIManager.IsAnyUIOpen);
    }

    private void OnDisable()
    {
        EventHandler.UIStateChangedEvent -= OnUIStateChanged;
    }

    private void OnUIStateChanged(bool isUIOpen)
    {
        ApplyUIState(isUIOpen);
    }

    /// <summary>
    /// 有界面打开时隐藏 HUD;全部关闭时显示 HUD
    /// </summary>
    private void ApplyUIState(bool isUIOpen)
    {
        if (_hudContent != null)
        {
            _hudContent.SetActive(!isUIOpen);
        }
    }
    private void OnClickPackage()
    {
        if (_packagePanel != null)
        {
            _packagePanel.OpenPanel();
        }
    }

    private void OnClickCharacter()
    {
        if (_characterPanel != null)
        {
            _characterPanel.OpenPanel();
        }
    }
    private void OnClickSettings()
    {
        if (_settingsPanel != null) _settingsPanel.OpenPanel();
    }
}
