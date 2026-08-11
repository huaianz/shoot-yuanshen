using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 委托数据
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "Game/QuestData")]
public class QuestData_SO : ScriptableObject
{
    public int questID;
    public string questName;
    [TextArea(2, 4)]
    public string description;
    [Header("委托类型")]
    public QuestType questType;

    [Header("消灭类: 目标敌人类型(和敌人Inspector的enemyType一致)")]
    public string targetEnemyType = "Hilichurl";

    [Header("收集类: 目标物品ID(素材/食物/武器)")]
    public int targetItemID;

    [Header("目标数量")]
    public int targetCount = 5;

    [Header("奖励")]
    public int rewardCoin = 100;
    public int rewardItemID;
    public int rewardAmount = 1;
}
