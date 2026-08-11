using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : SingleMonoBase<QuestManager>
{
    [Header("委托数据")]
    public List<QuestData_SO> allQuests;
    //委托字典
    private Dictionary<int, QuestData_SO> _questDict = new Dictionary<int, QuestData_SO>();
    //当前委托
    private QuestData_SO _activeQuest;
    //当前委托进度
    private int _currentProgress;
    private bool _readyToSubmit;

    // 给外部读取用的只读属性
    public QuestData_SO ActiveQuest => _activeQuest;
    public int CurrentProgress => _currentProgress;
    public int TargetCount => _activeQuest != null ? _activeQuest.targetCount : 0;
    public bool IsQuestActive => _activeQuest != null;

    protected override void Awake()
    {
        base.Awake();
        BuildDictionary();

        EventHandler.EnemyKilledEvent += OnEnemyKilled;
        EventHandler.ItemCollectedEvent += OnItemCollected;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        EventHandler.EnemyKilledEvent -= OnEnemyKilled;
        EventHandler.ItemCollectedEvent -= OnItemCollected;
    }

    /// <summary>
    /// 把列表转成字典
    /// </summary>
    private void BuildDictionary()
    {
        _questDict.Clear();
        foreach (var quest in allQuests)
        {
            if (quest == null) continue;
            if (!_questDict.ContainsKey(quest.questID))
            {
                _questDict.Add(quest.questID, quest);
            }
        }
    }

    /// <summary>
    /// 接受委托
    /// </summary>
    /// <param name="questID"></param>
    public void AcquireQuest(int questID)
    {
        if (!_questDict.TryGetValue(questID, out var quest))
        {
            ToastUI.ShowMessage("委托不存在", Color.gray);
            return;
        }

        //已有进行中的委托，不能再接新的（一次只做一个）
        if (_activeQuest != null)
        {
            ToastUI.ShowMessage("已有进行中的委托", Color.gray);
            return;
        }

        //记录当前委托，重置状态
        _activeQuest = quest;
        _readyToSubmit = false;

        //收集类委托：如果背包里已经有目标物品，进度直接从现有数量开始算
        _currentProgress = (quest.questType == QuestType.Collect) ? GetCollectCount() : 0;

        //提示玩家接到了委托
        ToastUI.ShowMessage($"接受委托: {quest.questName}", Color.white);
        UpdateProgressUI();
        CallQuestUpdatedEvent();
    }

    /// <summary>
    /// 敌人死亡事件回调（消灭类委托用）
    /// </summary>
    /// <param name="enemyType"></param>
    private void OnEnemyKilled(string enemyType)
    {
        // 没委托 / 已经可提交了 -> 不处理
        if (_activeQuest == null || _readyToSubmit) return;
        // 不是消灭类委托 -> 不处理
        if (_activeQuest.questType != QuestType.Kill) return;
        // 敌人类型对不上 -> 不处理（比如委托要杀丘丘人，死的是史莱姆）
        if (_activeQuest.targetEnemyType != enemyType) return;

        // 进度+1，但不能超过目标数（防止溢出显示 6/5）
        _currentProgress = Mathf.Min(_currentProgress + 1, _activeQuest.targetCount);
        UpdateProgressUI();
    }

    /// <summary>
    /// 拾取物品事件回调（收集类委托用）
    /// </summary>
    /// <param name="itemID">拾取的物品ID</param>
    /// <param name="amount">拾取数量</param>
    private void OnItemCollected(int itemID, int amount)
    {
        // 没委托 / 已经可提交了 -> 不处理
        if (_activeQuest == null || _readyToSubmit) return;
        // 不是收集类委托 -> 不处理
        if (_activeQuest.questType != QuestType.Collect) return;
        // 物品ID对不上 -> 不处理
        if (_activeQuest.targetItemID != itemID) return;

        // 收集类进度 = 背包里该物品的实时数量（拾取那一刻重新算一次）
        _currentProgress = Mathf.Min(GetCollectCount(), _activeQuest.targetCount);
        UpdateProgressUI();
    }

    /// <summary>
    /// 计算背包里目标物品的数量（收集类委托的进度依据）
    /// </summary>
    private int GetCollectCount()
    {
        if (_activeQuest == null) return 0;

        // 如果目标ID是素材，直接用素材的数量（素材可以堆叠，比如3个凝液算3）
        if (InventoryManager.INSTANCE.materialData?.GetMaterialByID(_activeQuest.targetItemID) != null)
        {
            return InventoryManager.INSTANCE.GetMaterialCount(_activeQuest.targetItemID);
        }

        // 如果是武器/食物：遍历所有物品
        // 武器一格算1；食物按数量算（和背包逻辑一致）
        int count = 0;
        foreach (var item in InventoryManager.INSTANCE.GetAllItems())
        {
            if (item.itemID == _activeQuest.targetItemID)
            {
                count += (item is FoodItem food) ? food.count : 1;
            }
        }
        return count;
    }

    /// <summary>
    /// 刷新进度提示：没到目标弹进度，到目标弹"可以提交了"
    /// </summary>
    private void UpdateProgressUI()
    {
        if (_activeQuest == null) return;

        if (_currentProgress >= _activeQuest.targetCount)
        {
            // 目标达成：标记可提交，并提示玩家回去找NPC
            _readyToSubmit = true;
            ToastUI.ShowMessage($"委托目标已完成! 回去找NPC提交吧", new Color(1f, 0.9f, 0.4f));
        }
        else
        {
            // 还没完成：弹当前进度
            ToastUI.ShowMessage($"{_activeQuest.questName}: {_currentProgress}/{_activeQuest.targetCount}", Color.white);
        }
        CallQuestUpdatedEvent();
    }
    /// <summary>
    /// 委托进度变化事件
    /// </summary>
    public static event System.Action QuestUpdatedEvent;
    public static void CallQuestUpdatedEvent()
    {
        QuestUpdatedEvent?.Invoke();
    }

    /// <summary>
    /// 提交委托
    /// </summary>
    public bool SubmitQuest()
    {
        if (_activeQuest == null)
        {
            ToastUI.ShowMessage("当前没有进行中的委托", Color.gray);
            return false;
        }
        //目标还没达成，不让提交
        if (!_readyToSubmit)
        {
            ToastUI.ShowMessage("委托还没完成", Color.gray);
            return false;
        }

        //收集类委托,从背包里扣除目标素材
        if (_activeQuest.questType == QuestType.Collect)
        {
            int need = _activeQuest.targetCount; // 还需要扣的数量
            foreach (var item in InventoryManager.INSTANCE.GetAllItems())
            {
                // 扣够了就停
                if (need <= 0) break;
                if (item is MaterialItem material && material.itemID == _activeQuest.targetItemID)
                {
                    int take = Mathf.Min(material.count, need);
                    InventoryManager.INSTANCE.ConsumeMaterial(item.instanceID, take);
                    need -= take;
                }
            }
        }

        //发奖励：金币
        if (_activeQuest.rewardCoin > 0)
        {
            ShopManager.INSTANCE.AddCurrency("Coin", _activeQuest.rewardCoin);
        }
        //发奖励：物品
        if (_activeQuest.rewardItemID > 0)
        {
            GiveRewardItem(_activeQuest.rewardItemID, _activeQuest.rewardAmount);
        }

        //拼一句奖励提示
        string rewardText = "委托完成! ";
        if (_activeQuest.rewardCoin > 0) rewardText += $"金币×{_activeQuest.rewardCoin} ";
        if (_activeQuest.rewardItemID > 0) rewardText += $"{InventoryManager.INSTANCE.GetItemName(_activeQuest.rewardItemID)}×{_activeQuest.rewardAmount}";
        ToastUI.ShowMessage(rewardText, new Color(1f, 0.9f, 0.4f));

        //清空当前委托，回到"没有进行中的委托"状态
        _activeQuest = null;
        _currentProgress = 0;
        _readyToSubmit = false;
        CallQuestUpdatedEvent();
        return true;
    }

    /// <summary>
    /// 发放物品奖励
    /// </summary>
    private void GiveRewardItem(int itemID, int amount)
    {
        // 先看是不是武器
        var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(itemID);
        if (weapon != null)
        {
            // 武器是一格一把，加几把就循环几次
            for (int i = 0; i < amount; i++)
            {
                InventoryManager.INSTANCE.AddWeapon(itemID);
            }
            return;
        }
        // 再看是不是食物
        var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(itemID);
        if (food != null)
        {
            InventoryManager.INSTANCE.AddFood(itemID, amount);
            return;
        }
        // 最后看是不是素材
        var material = InventoryManager.INSTANCE.materialData?.GetMaterialByID(itemID);
        if (material != null)
        {
            InventoryManager.INSTANCE.AddMaterial(itemID, amount);
        }
    }



}
