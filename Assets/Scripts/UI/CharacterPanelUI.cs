using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanelUI : BaseUIPanel
{
    [Header("角色面板")]
    public GameObject characterPanel;
    public GameObject AttibutePanel;
    public GameObject WeaponPanel;
    public GameObject DataPanel;

    [Header("角色面板按钮")]
    public Button ClosePanelBtn;
    public Button AttibuteBtn;
    public Button WeaponBtn;
    public Button DataBtn;
    [Header("伴随按钮点击的UI显示")]
    #region 伴随按钮点击的UI显示
    public GameObject AttibuteImage;
    public GameObject WeaponImage;
    public GameObject DataImage;
    #endregion

    [Header("角色头像列表")]
    //头像格子预制体
    public GameObject characterHeadPrefab;
    public Transform characterHeadParent;//父物体
    [Header("当前角色信息展示")]
    public TextMeshProUGUI currentRoleNameText;

    [Header("角色背景视频")]
    public CharacterVideoUI videoUI;
    //子面板引用
    private AttibuteUI _attibuteUI;
    private WeaponUI _weaponUI;
    private DataUI _dataUI;
    //角色头像列表
    private List<CharacterHeadCell> _headCellPool = new List<CharacterHeadCell>();
    private List<CharacterHeadCell> _activeHeadCells = new List<CharacterHeadCell>();

    //当前选中的角色ID
    private int _selectedRoleID = -1;
    //当前上阵的角色ID
    private int _currentActiveRoleID = -1;

    protected override void Awake()
    {
        base.Awake();
        //初始化角色头像列表
        RefreshCharacterList();
        //获取子面板组件
        _attibuteUI = AttibutePanel.GetComponent<AttibuteUI>();
        _weaponUI = WeaponPanel.GetComponent<WeaponUI>();
        _dataUI = DataPanel.GetComponent<DataUI>();

        //订阅角色切换事件
        GameManager.INSTANCE.OnActiveRoleChanged += OnActiveRoleChanged;
    }

    private void OnDestory()
    {
        if (GameManager.INSTANCE.OnActiveRoleChanged != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged -= OnActiveRoleChanged;
        }
    }
    private void Start()
    {
        #region 默认状态
        AttibutePanel.SetActive(true);
        WeaponPanel.SetActive(false);
        DataPanel.SetActive(false);
        AttibuteImage.SetActive(true);
        WeaponImage.SetActive(false);
        DataImage.SetActive(false);
        #endregion

        #region 按钮监听
        ClosePanelBtn.onClick.AddListener(() =>
        {
            characterPanel.SetActive(false);
            UIManager.ExitUIBlock();
        });

        //角色属性面板
        AttibuteBtn.onClick.AddListener(() =>
        {
            UIManager.INSTANCE.SwitchTabWithPanel(AttibutePanel,
                (AttibutePanel, AttibuteImage),
                (WeaponPanel, WeaponImage),
                (DataPanel, DataImage)
            );
            _attibuteUI?.RefreshUI();
        });

        WeaponBtn.onClick.AddListener(() =>
        {
            UIManager.INSTANCE.SwitchTabWithPanel(WeaponPanel,
                (AttibutePanel, AttibuteImage),
                (WeaponPanel, WeaponImage),
                (DataPanel, DataImage)
            );
            _weaponUI?.RefreshUI();
        });

        DataBtn.onClick.AddListener(() =>
        {
            UIManager.INSTANCE.SwitchTabWithPanel(DataPanel,
                (AttibutePanel, AttibuteImage),
                (WeaponPanel, WeaponImage),
                (DataPanel, DataImage)
            );
            _dataUI?.RefreshUI();
        });
        #endregion

        //初始化角色头像列表
        RefreshCharacterList();

        //默认选中当前上阵角色
        _currentActiveRoleID = GameManager.INSTANCE.GetActiveRoleID();

        if (_currentActiveRoleID >= 0)
        {
            _selectedRoleID = _currentActiveRoleID;
            HighlightSelectedRole();
            RefreshAllPanels();
        }

        //默认播放当前角色的视频
        int activeID = GameManager.INSTANCE.GetActiveRoleID();
        if (activeID >= 0 && videoUI != null)
        {
            // 先激活视频背景,否则协程无法启动
            if (!videoUI.gameObject.activeSelf)
            {
                videoUI.gameObject.SetActive(true);
            }
            videoUI.PlayVideo(activeID);
        }
    }

    /// <summary>
    /// 当角色被外部切换时
    /// </summary>
    /// <param name="newRoleID"></param>
    private void OnActiveRoleChanged(int newRoleID)
    {
        _currentActiveRoleID = newRoleID;
        _selectedRoleID = newRoleID;
        HighlightSelectedRole();
        RefreshAllPanels();
    }


    /// <summary>
    /// 刷新角色头像列表
    /// 用了对象池
    /// </summary>
    private void RefreshCharacterList()
    {
        var allRoles = GameManager.INSTANCE.GetAllRoles();
        if (characterHeadParent == null || characterHeadPrefab == null)
        {
            return;
        }
        int needCount = allRoles.Count;
        //确保池子里有足够的格子
        for (int i = _headCellPool.Count; i < needCount; i++)
        {
            GameObject go = Instantiate(characterHeadPrefab, characterHeadParent);
            var cell = go.GetComponent<CharacterHeadCell>();
            _headCellPool.Add(cell);
        }

        //激活或者隐藏格子
        _activeHeadCells.Clear();
        for (int i = 0; i < _headCellPool.Count; i++)
        {
            bool active = i < needCount;
            _headCellPool[i].gameObject.SetActive(active);
            if (active)
            {
                var data = allRoles[i];
                _headCellPool[i].Refresh(data, this);
                _activeHeadCells.Add(_headCellPool[i]);
            }
        }

        //高亮当前选中的角色
        HighlightSelectedRole();
    }

    /// <summary>
    /// 高亮当前选中的角色头像
    /// </summary>
    private void HighlightSelectedRole()
    {
        foreach (var cell in _activeHeadCells)
        {
            cell.SetSelected(cell.RoleID == _selectedRoleID);
        }
    }

    /// <summary>
    /// 点击角色头像时调用
    /// </summary>
    /// <param name="roleID"></param>
    public void OnCharacterSelected(int roleID)
    {
        if (_selectedRoleID == roleID)
        {
            return;
        }

        //直接切换角色
        GameManager.INSTANCE.SetActiveRole(roleID);

        _selectedRoleID = roleID;
        HighlightSelectedRole();
        //刷新所有面板显示的该角色信息
        RefreshAllPanels();

        //切换背景视频
        if (videoUI != null)
        {
            videoUI.PlayVideo(roleID);
        }
    }


    /// <summary>
    /// 刷新所有子面板
    /// </summary>
    private void RefreshAllPanels()
    {
        if (_selectedRoleID < 0)
        {
            return;
        }
        //刷新该角色的缓存属性
        GameManager.INSTANCE.RefreshRoleStats(_selectedRoleID);

        //刷新各个面板
        _attibuteUI?.RefreshUI(_selectedRoleID);
        _weaponUI?.RefreshUI(_selectedRoleID);
        _dataUI?.RefreshUI(_selectedRoleID);

        //更新当前角色名称
        if (currentRoleNameText != null)
        {
            var data = GameManager.INSTANCE.GetRoleData(_selectedRoleID);
            if (data != null)
            {
                currentRoleNameText.text = data.baseData.characterName;
            }
        }

    }


    public void OpenPanel()
    {
        characterPanel.SetActive(true);

        UIManager.EnterUIBlock();
        //刷新数据
        _currentActiveRoleID = GameManager.INSTANCE.GetActiveRoleID();
        _selectedRoleID = _currentActiveRoleID;
        RefreshAllPanels();
        HighlightSelectedRole();

        // 每次打开都播放当前角色的视频
        if (videoUI != null)
        {
            if (!videoUI.gameObject.activeSelf)
            {
                videoUI.gameObject.SetActive(true);
            }
            videoUI.PlayVideo(GameManager.INSTANCE.GetActiveRoleID());
        }
    }

    private void ClosedPanel()
    {

        characterPanel.SetActive(false);
        if (videoUI != null)
        {
            videoUI.StopVideo();
        }
        UIManager.ExitUIBlock();

    }


}
