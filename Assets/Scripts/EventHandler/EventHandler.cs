using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class EventHandler
{
    #region 对话系统事件
    /// <summary>
    /// 对话开始事件
    /// </summary>
    public static event Action<int> DialogueStartEvent;
    public static void CallDialogueStartEvent(int dialogueID)
    {
        DialogueStartEvent?.Invoke(dialogueID);
    }

    /// <summary>
    /// 对话结束事件
    /// </summary>
    public static event Action DialogueEndEvent;
    public static void CallDialogueEndEvent()
    {
        DialogueEndEvent?.Invoke();
    }

    /// <summary>
    /// NPC思考事件
    /// </summary>
    public static event Action DialogueThinkEvent;
    public static void CallDialogueThinkEvent()
    {
        DialogueThinkEvent?.Invoke();
    }

    /// <summary>
    /// NPC说话事件
    /// </summary>
    public static event Action DialogueTalkEvent;
    public static void CallDialogueTalkEvent()
    {
        DialogueTalkEvent?.Invoke();
    }
    #endregion

    #region 商店系统事件
    /// <summary>
    /// 打开商店事件
    /// </summary>
    public static event Action<int> OpenShopEvent;
    public static void CallOpenShopEvent(int shopID)
    {
        OpenShopEvent?.Invoke(shopID);
    }

    /// <summary>
    /// 购买成功事件
    /// </summary>
    public static event Action<int, int> PurchaseSuccessEvent;
    public static void CallPurchaseSuccessEvent(int itemID, int amount)
    {
        PurchaseSuccessEvent?.Invoke(itemID, amount);
    }

    /// <summary>
    /// 货币更新事件
    /// </summary>
    public static event Action<string, int> CurrencyUpdateEvent;
    public static void CallCurrencyUpdateEvent(string currentcyType, int amount)
    {
        CurrencyUpdateEvent?.Invoke(currentcyType, amount);
    }

    /// <summary>
    /// 商店关闭事件
    /// </summary>
    public static event Action ShopClosedEvent;
    public static void CallShopClosedEvent()
    {
        ShopClosedEvent?.Invoke();
    }
    #endregion

    #region 玩家血量事件
    /// <summary>
    /// 玩家血量变化事件
    /// </summary>
    public static event Action<int, float, float> PlayerHealthChangedEvent;
    public static void CallPlayerHealthChangedEvent(int roleID, float currentHealth, float maxHealth)
    {
        PlayerHealthChangedEvent?.Invoke(roleID, currentHealth, maxHealth);
    }
    #endregion


    #region UI状态事件
    /// <summary>
    /// UI打开状态变化事件
    /// </summary>
    public static event Action<bool> UIStateChangedEvent;
    public static void CallUIStateChangedEvent(bool isUIOpen)
    {
        UIStateChangedEvent?.Invoke(isUIOpen);
    }
    #endregion
}
