using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 选择器:从左到右尝试，第一个返回Success的就赢
/// 全部失败才返回Failure,遇到Running就挂起
/// </summary>
public class Selector : BTNode
{
    private readonly BTNode[] _children;
    //当前正在运行的子节点
    private BTNode _lastRunningChild;
    public Selector(params BTNode[] children)
    {
        _children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (BTNode child in _children)
        {
            NodeState state = child.Evaluate();

            if (state == NodeState.Success)
            {
                return NodeState.Success;
            }
            if (state == NodeState.Running)
            {
                //记住是谁在跑
                _lastRunningChild = child;
                return NodeState.Running;
            }
            //如果是Failure则继续尝试下一个
        }
        //全都不行就返回失败
        return NodeState.Failure;
    }

    //下钻到正在运行的子节点
    public override BTNode GetActiveNode()
    {
        return _lastRunningChild != null ? _lastRunningChild.GetActiveNode() : this;
    }
}
