using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("佩戴武器UI")]
    public TextMeshProUGUI weaponName;
    public TextMeshProUGUI weaponType;
    public TextMeshProUGUI weaponAtk;
    public TextMeshProUGUI fireRate;
    public TextMeshProUGUI BulletNum;
    public TextMeshProUGUI weaponDesc;

    [Header("中间武器大图")]
    public Image weaponDisplayImage;
    [Header("入口按钮")]
    public Button weaponEntryBtn;
    public TextMeshProUGUI entryLabel;

    [Header("选武器弹层")]
    public GameObject selectOverlay;
    public Button closeBtn;
    public RectTransform listContent;
    public GameObject weaponRowPrefab;
    public TextMeshProUGUI emptyHint;

    // 行复用池
    private readonly List<GameObject> _rows = new List<GameObject>();
    private int _currentRole = -1;

    private void Awake()
    {
        if (entryLabel == null && weaponEntryBtn != null)
        {
            Transform t = weaponEntryBtn.transform.Find("Text (TMP)");
            if (t != null) entryLabel = t.GetComponent<TextMeshProUGUI>();
        }
        if (weaponEntryBtn != null) weaponEntryBtn.onClick.AddListener(OpenSelect);
        if (closeBtn != null) closeBtn.onClick.AddListener(() => selectOverlay.SetActive(false));
    }

    private void OnDisable()
    {
        if (selectOverlay != null) selectOverlay.SetActive(false);
    }


    /// <summary>
    /// 刷新佩戴武器UI和武器列表
    /// </summary>
    /// <param name="roleID"></param>
    public void RefreshUI(int roleID = -1)
    {
        if (roleID < 0)
        {
            roleID = GameManager.INSTANCE.GetActiveRoleID();
        }
        if (roleID < 0)
        {
            ClearWeaponDisplay();
            return;
        }

        _currentRole = roleID; // 记录当前角色, 列表绑定点击要用
        RefreshList(roleID);

        //获取该角色装备的武器
        var weaponData = InventoryManager.INSTANCE.GetRoleWeaponData(roleID);
        //有武器显示图标, 没武器隐藏
        if (weaponDisplayImage != null)
        {
            bool has = weaponData != null;
            weaponDisplayImage.gameObject.SetActive(has);
            if (has)
            {
                weaponDisplayImage.sprite = InventoryManager.INSTANCE.GetIcon(weaponData.itemID);
            }
        }

        if (entryLabel != null)
        {
            entryLabel.text = weaponData != null ? "替换" : "装备武器";
        }
        //信息区
        if (weaponData == null)
        {
            ClearWeaponDisplay();
            return;
        }

        //从模版获取武器详细信息
        var item = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weaponData.itemID);
        if (item == null)
        {
            ClearWeaponDisplay();
            return;
        }
        if (weaponName != null)
        {
            weaponName.text = item.weaponName;
        }
        if (weaponType != null)
        {
            weaponType.text = item.weaponType;
        }
        if (weaponAtk != null)
        {
            weaponAtk.text = item.weaponATK.ToString();
        }
        if (fireRate != null)
        {
            fireRate.text = item.fireRate.ToString();
        }
        if (BulletNum != null)
        {
            BulletNum.text = item.BulletNum.ToString();
        }
        if (weaponDesc != null)
        {
            weaponDesc.text = item.weaponDescription;
        }
    }

    /// <summary>
    /// 清空武器显示
    /// </summary>
    private void ClearWeaponDisplay()
    {
        if (weaponName != null)
        {
            weaponName.text = "无武器";
        }
        if (weaponType != null)
        {
            weaponType.text = "";
        }
        if (weaponAtk != null)
        {
            weaponAtk.text = "0";
        }
        if (fireRate != null)
        {
            fireRate.text = "0";
        }
        if (BulletNum != null)
        {
            BulletNum.text = "0";
        }
        if (weaponDesc != null)
        {
            weaponDesc.text = "请装备武器";
        }
    }

    /// <summary>
    /// 入口按钮
    /// </summary>
    private void OpenSelect()
    {
        RefreshList(_currentRole);
        if (selectOverlay != null) selectOverlay.SetActive(true);
    }

    /// <summary>
    /// 刷新弹层列表
    /// </summary>
    /// <param name="roleID"></param>
    private void RefreshList(int roleID)
    {
        List<WeaponItem> weapons = InventoryManager.INSTANCE.GetAllWeapons();
        string equippedId = InventoryManager.INSTANCE.GetRoleWeaponId(roleID);
        bool empty = weapons.Count == 0;
        if (emptyHint != null) emptyHint.gameObject.SetActive(empty);
        if (listContent != null) listContent.gameObject.SetActive(!empty);

        for (int i = 0; i < weapons.Count; i++)
        {
            if (i >= _rows.Count)
            {
                if (weaponRowPrefab == null || listContent == null) break;
                GameObject row = Instantiate(weaponRowPrefab, listContent);
                // 去掉背包格子的点击逻辑(PackageCell), 换成我们自己的按钮
                PackageCell cell = row.GetComponent<PackageCell>();
                if (cell != null) DestroyImmediate(cell); // 立即移除, 不留延迟窗口
                row.AddComponent<Button>();
                _rows.Add(row);
            }
            _rows[i].SetActive(true);
            SetupRow(_rows[i], weapons[i], equippedId == weapons[i].instanceID);
        }
        for (int i = weapons.Count; i < _rows.Count; i++)
        {
            _rows[i].SetActive(false);
        }
    }

    /// <summary>
    /// 填充一行: 图标/名字/Top-New显示持有者头像 + 绑定点击
    /// </summary>
    private void SetupRow(GameObject row, WeaponItem weapon, bool isEquipped)
    {
        var item = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weapon.itemID);
        string wName = item != null ? item.weaponName : "未知武器";

        // 武器图标
        Transform iconT = row.transform.Find("Top/icon");
        if (iconT != null)
        {
            iconT.GetComponent<Image>().sprite = InventoryManager.INSTANCE.GetIcon(weapon.itemID);
        }

        // 武器名
        Transform nameT = row.transform.Find("Bottom/nameText");
        if (nameT != null)
        {
            TextMeshProUGUI name = nameT.GetComponent<TextMeshProUGUI>();
            name.text = isEquipped ? wName + "  [已装备]" : wName;
            name.color = isEquipped ? new Color(1f, 0.85f, 0.45f) : Color.white;
        }

        // Top/New: 显示装备了这把武器的角色头像(没有持有者就隐藏)
        Transform newT = row.transform.Find("Top/New");
        if (newT != null)
        {
            Image newImg = newT.GetComponent<Image>();
            Sprite avatar = weapon.ownerID >= 0 ? GameManager.INSTANCE.GetAvatar(weapon.ownerID) : null;
            newImg.sprite = avatar;
            newImg.gameObject.SetActive(avatar != null);
        }

        // 点击装备
        Button btn = row.GetComponent<Button>();
        btn.targetGraphic = row.GetComponent<Image>(); // 按钮点击反馈
        btn.onClick.RemoveAllListeners();
        string uid = weapon.instanceID;
        Debug.Log($"[WeaponUI] 已绑定行: {wName}, uid={uid}");
        // 点击时取当前上阵角色, 避免绑定时的角色过期(-1)
        btn.onClick.AddListener(() => OnSelectWeapon(uid, GameManager.INSTANCE.GetActiveRoleID()));
    }

    /// <summary>
    /// 点击武器 -> 装备并关闭弹层
    /// </summary>
    private void OnSelectWeapon(string instanceID, int roleID)
    {
        Debug.Log($"[WeaponUI] 点击装备: {instanceID}, role={roleID}");
        InventoryManager.INSTANCE.EquipWeapon(instanceID, roleID);
        GameManager.INSTANCE.MarkRoleStatsDirty(roleID);
        GameManager.INSTANCE.RefreshRoleStats(roleID);

        // 装备后立刻刷新当前角色的武器(弹药/伤害马上生效)
        if (PlayerController.INSTANCE != null && PlayerController.INSTANCE.currentPlayerModel != null)
        {
            PlayerWeapon weapon = PlayerController.INSTANCE.currentPlayerModel.weapon;
            if (weapon != null) weapon.RefreshWeaponData();
        }

        if (selectOverlay != null) selectOverlay.SetActive(false);
        RefreshUI(roleID);
    }
}
