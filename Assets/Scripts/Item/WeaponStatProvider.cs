using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// 词条表 + 随机生成工具
/// </summary>
public static class WeaponAffixTable
{
    private static readonly List<WeaponAffixData> All = new List<WeaponAffixData>
    {
        new WeaponAffixData { id = 1, type = WeaponAffixType.ATKPercent,      value = 0.10f, name = "锋利" },
        new WeaponAffixData { id = 2, type = WeaponAffixType.FireRatePercent, value = 0.15f, name = "狂热" },
        new WeaponAffixData { id = 3, type = WeaponAffixType.MagazineBonus,   value = 8f,    name = "扩容" },
        new WeaponAffixData { id = 4, type = WeaponAffixType.CritRateBonus,   value = 0.10f, name = "精准" },
        new WeaponAffixData { id = 5, type = WeaponAffixType.CritDamageBonus, value = 0.50f, name = "致命" },
    };

    public static WeaponAffixData Get(int id)
    {
        return All.Find(a => a.id == id);
    }

    /// <summary>
    /// 随机挑 count 个不重复词条
    /// </summary>
    public static List<int> Roll(int count)
    {
        List<int> ids = new List<int>();
        List<int> pool = new List<int>();
        foreach (var a in All) pool.Add(a.id);

        while (ids.Count < count && pool.Count > 0)
        {
            int idx = Random.Range(0, pool.Count);
            ids.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return ids;
    }
}

/// <summary>
/// 武器属性提供者接口(装饰者模式的基础)
/// </summary>
public interface IWeaponStatProvider
{
    int ATK { get; }
    float FireRateMultiplier { get; }
    int MagazineBonus { get; }
    float CritRate { get; }
    float CritDamage { get; }
}

/// <summary>
/// 基础武器属性(直接读武器模板, 无任何加成)
/// </summary>
public class BaseWeaponStatProvider : IWeaponStatProvider
{
    private readonly Weapon _template;

    public BaseWeaponStatProvider(Weapon template)
    {
        _template = template;
    }

    public int ATK => _template != null ? _template.weaponATK : 0;
    public float FireRateMultiplier => 1f;
    public int MagazineBonus => 0;
    public float CritRate => 0f;
    // 基础暴击伤害加成 50%
    public float CritDamage => 0.5f;
}

/// <summary>
/// 词条装饰器: 包裹上一层提供者, 在它的基础上修正某一项属性
/// </summary>
public class WeaponAffixDecorator : IWeaponStatProvider
{
    private readonly IWeaponStatProvider _inner;
    private readonly WeaponAffixData _affix;

    public WeaponAffixDecorator(IWeaponStatProvider inner, WeaponAffixData affix)
    {
        _inner = inner;
        _affix = affix;
    }

    public int ATK
    {
        get
        {
            int v = _inner.ATK;
            return _affix.type == WeaponAffixType.ATKPercent ? Mathf.RoundToInt(v * (1f + _affix.value)) : v;
        }
    }

    public float FireRateMultiplier
    {
        get
        {
            float v = _inner.FireRateMultiplier;
            return _affix.type == WeaponAffixType.FireRatePercent ? v * (1f + _affix.value) : v;
        }
    }

    public int MagazineBonus
    {
        get
        {
            int v = _inner.MagazineBonus;
            return _affix.type == WeaponAffixType.MagazineBonus ? v + Mathf.RoundToInt(_affix.value) : v;
        }
    }

    public float CritRate
    {
        get
        {
            float v = _inner.CritRate;
            return _affix.type == WeaponAffixType.CritRateBonus ? v + _affix.value : v;
        }
    }

    public float CritDamage
    {
        get
        {
            float v = _inner.CritDamage;
            return _affix.type == WeaponAffixType.CritDamageBonus ? v + _affix.value : v;
        }
    }
}

/// <summary>
/// 工厂: 根据武器的词条列表组装装饰者链
/// </summary>
public static class WeaponStatProviderFactory
{
    public static IWeaponStatProvider Build(WeaponItem item)
    {
        Weapon template = InventoryManager.INSTANCE?.weaponData?.GetWeaponByID(item.itemID);
        IWeaponStatProvider provider = new BaseWeaponStatProvider(template);
        if (item.affixIds != null)
        {
            foreach (int id in item.affixIds)
            {
                WeaponAffixData affix = WeaponAffixTable.Get(id);
                if (affix != null)
                {
                    provider = new WeaponAffixDecorator(provider, affix);
                }
            }
        }
        return provider;
    }

    /// <summary>
    /// 生成词条描述文字, 用于 UI 展示
    /// </summary>
    public static string BuildAffixDescription(WeaponItem item)
    {
        if (item.affixIds == null || item.affixIds.Count == 0) return "";

        List<string> parts = new List<string>();
        foreach (int id in item.affixIds)
        {
            WeaponAffixData a = WeaponAffixTable.Get(id);
            if (a == null) continue;

            string statName;
            switch (a.type)
            {
                case WeaponAffixType.ATKPercent: statName = "攻击力"; break;
                case WeaponAffixType.FireRatePercent: statName = "射速"; break;
                case WeaponAffixType.MagazineBonus: statName = "弹匣"; break;
                case WeaponAffixType.CritRateBonus: statName = "暴击率"; break;
                default: statName = "暴击伤害"; break;
            }

            string v = a.type == WeaponAffixType.MagazineBonus
                ? $"+{Mathf.RoundToInt(a.value)}"
                : $"+{Mathf.RoundToInt(a.value * 100f)}%";
            parts.Add($"{a.name} {v}{statName}");
        }
        return string.Join("  ", parts);
    }
}
