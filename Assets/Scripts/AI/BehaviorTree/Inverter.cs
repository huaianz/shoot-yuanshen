using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 反转器：Success和Failure互换，Running不变
/// </summary>
public class Inverter : BTNode
{
    private readonly BTNode _child;

    public Inverter(BTNode child)
    {
        _child = child;
    }

    public override NodeState Evaluate()
    {
        NodeState state = _child.Evaluate();

        if (state == NodeState.Running)
        {
            return NodeState.Running;
        }
        return state == NodeState.Success ? NodeState.Failure : NodeState.Success;
    }
}
