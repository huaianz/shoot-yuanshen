using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 伤害结算上下文: 责任链里各环节共享的数据
/// </summary>
public class DamageContext
{
    public float baseDamage;   // 进入链时的原始伤害
    public float finalDamage;  // 链结算完的最终伤害
    public float critRate;     // 暴击率(0~1)
    public float critDamage;   // 暴击额外伤害(0.5 = 暴击1.5倍)
    public float armor;        // 护甲(直接减伤)
    public float resistance;   // 抗性(百分比减伤 0~1)
    public bool isCrit;        // 本次是否暴击
}

/// <summary>伤害修饰环节(责任链的一环)</summary>
public interface IDamageModifier
{
    void Process(DamageContext ctx);
}

/// <summary>
/// 暴击环节: 按暴击率判定, 命中则按暴击伤害放大
/// </summary>
public class CritDamageModifier : IDamageModifier
{
    public void Process(DamageContext ctx)
    {
        if (ctx.critRate <= 0f) return;
        if (Random.value < ctx.critRate)
        {
            ctx.isCrit = true;
            ctx.finalDamage *= 1f + ctx.critDamage;
        }
    }
}

/// <summary>
/// 护甲环节: 直接扣减固定数值, 最低保留1点伤害
/// </summary>
public class ArmorDamageModifier : IDamageModifier
{
    public void Process(DamageContext ctx)
    {
        if (ctx.armor <= 0f) return;
        ctx.finalDamage = Mathf.Max(1f, ctx.finalDamage - ctx.armor);
    }
}

/// <summary>
/// 抗性环节: 按百分比减伤, 最低保留1点伤害
/// </summary>
public class ResistanceDamageModifier : IDamageModifier
{
    public void Process(DamageContext ctx)
    {
        if (ctx.resistance <= 0f) return;
        ctx.finalDamage = Mathf.Max(1f, ctx.finalDamage * Mathf.Clamp01(1f - ctx.resistance));
    }
}

/// <summary>
/// 伤害责任链: 依次执行每个环节
/// 以后加新机制(护盾/元素克制/吸血)只需实现 IDamageModifier 并 Add 进来
/// </summary>
public class DamagePipeline
{
    private readonly List<IDamageModifier> _modifiers = new List<IDamageModifier>();

    public DamagePipeline Add(IDamageModifier modifier)
    {
        _modifiers.Add(modifier);
        return this;
    }

    /// <summary>
    /// 结算: 返回取整后的最终伤害
    /// </summary>
    public int Resolve(DamageContext ctx)
    {
        ctx.finalDamage = ctx.baseDamage;
        ctx.isCrit = false;
        foreach (IDamageModifier m in _modifiers)
        {
            m.Process(ctx);
        }
        return Mathf.Max(1, Mathf.RoundToInt(ctx.finalDamage));
    }

    /// <summary>
    /// 默认链: 暴击 -> 护甲 -> 抗性
    /// </summary>
    public static DamagePipeline BuildDefault()
    {
        return new DamagePipeline()
            .Add(new CritDamageModifier())
            .Add(new ArmorDamageModifier())
            .Add(new ResistanceDamageModifier());
    }
}
