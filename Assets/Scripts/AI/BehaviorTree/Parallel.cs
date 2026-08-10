using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 并行：同一帧把所有孩子都执行一遍
/// allMustSucceed=true  → 全部成功才成功
/// allMustSucceed=false → 一个成功就成功
/// 史莱姆一边跳一遍朝玩家方向调整，两个动作并行跑
/// </summary>
public class Parallel : BTNode
{
    private readonly BTNode[] _children;
    private readonly bool _allMustSucceed;
    private BTNode _lastRunningChild;

    public Parallel(bool allMustSucceed, params BTNode[] children)
    {
        _allMustSucceed = allMustSucceed;
        _children = children;
    }

    public override NodeState Evaluate()
    {
        bool anyRunning = false;
        foreach (BTNode child in _children)
        {
            NodeState state = child.Evaluate();
            if (state == NodeState.Running)
            {
                anyRunning = true;
                if (_lastRunningChild == null) _lastRunningChild = child;   // 记第一个
            }
            else if (state == NodeState.Success && !_allMustSucceed)
            {
                //一个成功就结束
                return NodeState.Success;
            }
            else if (state == NodeState.Failure && _allMustSucceed)
            {
                //一个失败就结束
                return NodeState.Failure;
            }
        }
        if (anyRunning)
        {
            return NodeState.Running;
        }
        return _allMustSucceed ? NodeState.Success : NodeState.Failure;
    }

    public override BTNode GetActiveNode()
    {
        return _lastRunningChild != null ? _lastRunningChild.GetActiveNode() : this;
    }
}
