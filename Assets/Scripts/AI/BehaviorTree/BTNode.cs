using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行为树节点基类
/// </summary>
public abstract class BTNode
{
    //调试面板会用到
    public string NodeName = "Node";

    /// <summary>
    /// 执行一次这个节点，返回结果
    /// </summary>
    /// <returns></returns>
    public abstract NodeState Evaluate();

    /// <summary>
    /// 获取当前正在执行的子节点(调试用):组合节点会一路下钻到叶子
    /// </summary>
    public virtual BTNode GetActiveNode()
    {
        return this;
    }
}
