using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 顺序器：从左到右全部执行，全部成功才算成功
/// 任何一个失败则失败，遇到Running就挂起
/// </summary>
public class Sequence : BTNode
{
    private readonly BTNode[] _children;

    public Sequence(params BTNode[] children)
    {
        _children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (BTNode child in _children)
        {
            NodeState state = child.Evaluate();
            if (state == NodeState.Failure)
            {
                return NodeState.Failure;
            }
            if (state == NodeState.Running)
            {
                return NodeState.Running;
            }
        }
        return NodeState.Success;
    }
}
