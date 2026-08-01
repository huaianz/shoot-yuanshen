using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : SingleMonoBase<DialogueManager>
{
    [Header("对话数据")]
    public List<DialogueData_SO> allDialogues;

    private Dictionary<int, DialogueData_SO> _dialogueDict = new Dictionary<int, DialogueData_SO>();
    private DialogueData_SO _currentDialogue;
    private bool _isInDialogue = false;
    public bool IsInDialogue => _isInDialogue;

    protected override void Awake()
    {
        BuildDictionary();
    }

    /// <summary>
    /// 构建字典索引
    /// </summary>
    private void BuildDictionary()
    {
        _dialogueDict.Clear();
        foreach (var dialogue in allDialogues)
        {
            if (dialogue == null)
            {
                return;
            }
            if (!_dialogueDict.ContainsKey(dialogue.dialogueID))
            {
                _dialogueDict.Add(dialogue.dialogueID, dialogue);
            }
            else
            {
                Debug.LogWarning($"对话ID重复：{dialogue.dialogueID}");
            }
        }
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    /// <param name="dialogueID"></param>
    public void StartDialogue(int dialogueID)
    {
        if (_isInDialogue)
        {
            return;
        }
        if (!_dialogueDict.TryGetValue(dialogueID, out var dialogue))
        {
            return;
        }

        _currentDialogue = dialogue;
        _isInDialogue = true;

        //发送对话开始事件
        EventHandler.CallDialogueStartEvent(dialogueID);
        //显示对话UI
        DialogueUI.INSTANCE?.ShowDialogue(dialogue);
    }

    /// <summary>
    /// 选择选项
    /// </summary>
    /// <param name="option"></param>
    public void SelectOption(DialogueOption option)
    {
        if (option == null)
        {
            return;
        }
        //执行动作
        ExecuteAction(option.action);

        //跳转
        if (option.nextDialogueID > 0)
        {
            if (_dialogueDict.TryGetValue(option.nextDialogueID, out var next))
            {
                _currentDialogue = next;
                //发送说话事件
                EventHandler.CallDialogueTalkEvent();
                DialogueUI.INSTANCE?.ShowDialogue(next);
            }
            else
            {
                EndDialogue();
            }
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 执行动作
    /// </summary>
    /// <param name="action"></param>
    private void ExecuteAction(DialogueAction action)
    {
        if (action == null)
        {
            return;
        }
        switch (action.type)
        {
            case ActionType.GiveItem:
                GivePlayerItem(action.itemID, action.amount);
                break;
            case ActionType.RemoveItem:
                RemovePlayerItem(action.itemID, action.amount);
                break;
            case ActionType.OpenShop:
                EventHandler.CallOpenShopEvent(1);//默认商店ID=1
                break;
            case ActionType.TriggerEvent:
                break;
            case ActionType.CompleteQuest:
                break;
        }
    }

    /// <summary>
    /// 给玩家物品
    /// </summary>
    /// <param name="itemID"></param>
    /// <param name="amount"></param>
    private void GivePlayerItem(int itemID, int amount)
    {
        //尝试作为武器添加
        var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(itemID);
        if (weapon != null)
        {
            for (int i = 0; i < amount; i++)
            {
                InventoryManager.INSTANCE.AddWeapon(itemID);
            }
            return;
        }

        //尝试作为食物添加
        var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(itemID);
        if (food != null)
        {
            InventoryManager.INSTANCE.AddFood(itemID, amount);
            return;
        }
    }

    /// <summary>
    /// 移除玩家物品
    /// </summary>
    /// <param name="itemID"></param>
    /// <param name="amount"></param>
    private void RemovePlayerItem(int itemID, int amount)
    {
        var allItems = InventoryManager.INSTANCE.GetAllItems();
        int removed = 0;
        foreach (var item in allItems)
        {
            if (removed >= amount)
            {
                break;
            }
            if (item.itemID == itemID)
            {
                InventoryManager.INSTANCE.RemoveItem(item.instanceID);
                removed++;
            }
        }
    }

    public void EndDialogue()
    {
        _isInDialogue = false;
        _currentDialogue = null;
        //隐藏对话UI
        DialogueUI.INSTANCE?.HideDialogue();
        //发送对话结束事件（npc切换到待机动画）
        EventHandler.CallDialogueEndEvent();
    }
}
