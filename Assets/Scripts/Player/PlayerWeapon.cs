// using UnityEngine;

// public class PlayerWeapon : MonoBehaviour
// {
//     [Tooltip("子弹生成的位置(枪口)")]
//     public Transform bullletSpawnPoint;
//     [Tooltip("对象池")]
//     public BulletPool bulletPool;
//     [Tooltip("子弹发射间隔")]
//     public float bulletInterval = 0.15f;
//     private float lastFireTime;

//     private void Awake()
//     {
//         if (bulletPool == null)
//         {
//             bulletPool = GetComponent<BulletPool>();
//         }
//     }

//     public void Fire(Vector3 targetPos)
//     {
//         if (Time.time - lastFireTime < bulletInterval)
//         {
//             return;
//         }
//         lastFireTime = Time.time;

//         // 从枪口射出,保持手感
//         Vector3 origin = bullletSpawnPoint.position;
//         Vector3 direction = (targetPos - origin).normalized;

//         if (bulletPool != null)
//         {
//             bulletPool.SpawnBullet(origin, direction);
//             bulletPool.SpawnSpark(origin, direction);
//         }
//     }
// }

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerWeapon : MonoBehaviour
{
    [Tooltip("枪口")]
    public Transform bullletSpawnPoint;
    [Tooltip("对象池")]
    public BulletPool bulletPool;
    [Tooltip("子弹发射间隔")]
    public float bulletInterval = 0.15f;
    [Tooltip("换弹耗时(秒)")]
    public float reloadDuration = 1.5f;

    //弹匣数据
    public int currentAmmo;
    //弹匣容量
    public int magazineSize;
    public bool isReloading;
    //枪械模型
    private GameObject _currentGun;   // 当前生成的枪(换枪时销毁旧的)
    public Transform leftHandTarget;  // 左手目标
    //右手挂载武器的父物体位置
    public Transform rightHand;

    private float lastFireTime;
    private int _bulletDamage = 10;
    private int _lastRoleID = -1;
    private int _lastWeaponID = -1;
    //当前武器的专属枪声
    private AudioClip _currentGunshot;
    private Camera _cam;   // 缓存主相机(避免每发子弹查找)
    private float _critRate;   // 当前武器暴击率(词条计算)
    private float _critDamage; // 当前武器暴击伤害加成(词条计算)

    private void Awake()
    {
        _cam = Camera.main;   // 缓存主相机(避免每发子弹查找)
        if (bulletPool == null)
        {
            bulletPool = GetComponent<BulletPool>();
        }
    }

    private void Start()
    {
        RefreshWeaponData();
    }

    /// <summary>
    /// 从当前装备武器刷新弹匣容量和伤害
    /// </summary>
    public void RefreshWeaponData()
    {
        int roleID = GameManager.INSTANCE.GetActiveRoleID();
        var weaponItem = InventoryManager.INSTANCE.GetRoleWeaponData(roleID);
        int weaponID = weaponItem != null ? weaponItem.itemID : -1;
        if (roleID == _lastRoleID && weaponID == _lastWeaponID)
        {
            return;
        }
        _lastRoleID = roleID;
        _lastWeaponID = weaponID;

        if (weaponItem == null)
        {
            magazineSize = 0;
            currentAmmo = 0;
            _bulletDamage = 10;
            _critRate = 0f;
            _critDamage = 0f;
            bulletInterval = 0.15f;
            NotifyAmmoChanged();
            return;
        }

        var item = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weaponItem.itemID);
        if (item == null)
        {
            // 武器模板缺失: 也把弹匣归零, 避免 HUD 显示旧数据
            magazineSize = 0;
            currentAmmo = 0;
            _bulletDamage = 10;
            NotifyAmmoChanged();
            return;
        }
        //换枪，生成对应模型并且设置左手扶枪位置
        UpdateWeaponModel(item);
        _currentGunshot = item.gunshotClip;   // 缓存本武器的枪声
        // 切换对应的子弹/火花预制体(只在武器变化时执行)
        if (bulletPool != null)
        {
            bulletPool.SetupWeapon(item.bulletPrefab, item.sparkPrefab);
        }
        // 装饰者链: 基础属性 + 词条加成(攻击/射速/弹匣/暴击)
        IWeaponStatProvider stats = WeaponStatProviderFactory.Build(weaponItem);
        _bulletDamage = Mathf.Max(1, stats.ATK);
        _critRate = Mathf.Clamp01(stats.CritRate);
        _critDamage = Mathf.Max(0f, stats.CritDamage);
        bulletInterval = Mathf.Clamp(0.15f / Mathf.Max(0.1f, stats.FireRateMultiplier), 0.03f, 1f);
        magazineSize = Mathf.Max(1, item.BulletNum + stats.MagazineBonus);
        currentAmmo = magazineSize;
        NotifyAmmoChanged();
    }

    /// <summary>
    /// 换枪: 在右手骨 DEF-hand.R 下生成对应模型, 并设置左手扶枪位置
    /// </summary>
    private void UpdateWeaponModel(Weapon template)
    {
        // 先检查新枪模型是否配置了, 再删旧枪(避免配置缺失时手里变空)
        if (template == null || template.weaponModel == null)
        {
            Debug.LogWarning($"[PlayerWeapon] 武器 {(template != null ? template.weaponName : "未知")} 没有配置模型, 保留上一把枪");
            return;
        }

        // 删除上一把枪
        if (_currentGun != null)
        {
            Destroy(_currentGun);
            _currentGun = null;
        }

        // 生成新枪, 用武器数据里的 Transform 组件数值
        _currentGun = Instantiate(template.weaponModel, rightHand);
        _currentGun.transform.localPosition = template.weaponPosition;
        _currentGun.transform.localRotation = Quaternion.Euler(template.weaponRotation);
        _currentGun.transform.localScale = template.weaponScale;

        // 左手扶住枪管: 把 Left Hand Target 移到数据里的位置
        if (leftHandTarget != null)
        {
            leftHandTarget.localPosition = template.LeftPosition;
            leftHandTarget.localRotation = Quaternion.Euler(template.LeftRotation);
            leftHandTarget.localScale = template.LeftScale;
        }
    }

    public void Fire(Vector3 targetPos)
    {
        //换弹中不能开火
        if (isReloading)
        {
            return;
        }
        //空仓: 响空枪声, 不自动换弹(按 R 手动换弹)
        if (currentAmmo <= 0)
        {
            // 加节流, 按住开火不会每秒响几十次咔哒
            if (Time.time - lastFireTime >= bulletInterval)
            {
                lastFireTime = Time.time;
                AudioManager.Instance.PlaySFX("Audio/SFX/Empty", 0.7f);
            }
            return;
        }
        if (Time.time - lastFireTime < bulletInterval)
        {
            return;
        }
        lastFireTime = Time.time;
        currentAmmo--; // 弹匣容量减一
        NotifyAmmoChanged();      // 弹药变了, 通知HUD

        // 发射原点: 枪口(手感最好)
        Vector3 origin = bullletSpawnPoint != null
            ? bullletSpawnPoint.position
            : (_cam != null ? _cam.transform.position : Vector3.zero);

        if (_cam != null && Vector3.Distance(origin, _cam.transform.position) > 10f)
        {
            origin = _cam.transform.position;
        }

        Vector3 direction = (targetPos - origin).normalized;

        if (bulletPool != null)
        {
            PlayerWeaponBullet bullet = bulletPool.SpawnBullet(origin, direction);
            if (bullet != null)
            {
                bullet.damage = _bulletDamage; // 伤害跟随武器攻击力(含词条)
                bullet.critRate = _critRate;   // 暴击率给子弹, 受击时走责任链
                bullet.critDamage = _critDamage;
            }
            bulletPool.SpawnSpark(origin, direction);

            // 枪声: 武器有专属枪声就用, 没有则用默认
            if (_currentGunshot != null)
                AudioManager.Instance.PlaySFX(_currentGunshot, 0.5f);
            else
                AudioManager.Instance.PlaySFX("Audio/SFX/Gunshot", 0.5f);
        }
    }

    /// <summary>
    /// 开始换弹
    /// </summary>
    public void TryReload()
    {
        if (isReloading) return;
        if (magazineSize <= 0 || currentAmmo >= magazineSize) return;
        isReloading = true;
        AudioManager.Instance.PlaySFX("Audio/SFX/Reload", 0.8f);  // 换弹音效
        NotifyAmmoChanged(); // 进入换弹状态(显示"装填中...")
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(reloadDuration);
        currentAmmo = magazineSize;
        isReloading = false;
        NotifyAmmoChanged(); // 换弹完成
    }

    /// <summary>
    /// 弹药状态变化统一入口(通过 EventHandler 广播)
    /// </summary>
    private void NotifyAmmoChanged()
    {
        EventHandler.CallAmmoChangedEvent(_lastRoleID, currentAmmo, magazineSize, isReloading);
    }
}