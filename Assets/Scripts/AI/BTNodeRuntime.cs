using System;
using UnityEngine;

/// <summary>
/// 运行时行为树节点基类: 每帧 Evaluate 返回 成功/失败/运行中
/// </summary>
public abstract class BTNodeRuntime
{
    public enum Result { Success, Failure, Running }

    public string NodeGUID = "";   // 编辑器里的节点ID(构建时传入, 调试用)

    /// <summary>统一出口: 记录调试状态后返回</summary>
    protected Result Finish(Result r)
    {
        BehaviorTreeDebugBus.Record(NodeGUID, r);
        return r;
    }

    public abstract Result Evaluate();
}

/// <summary>组合节点基类: 能塞子节点</summary>
public abstract class CompositeNode : BTNodeRuntime
{
    protected BTNodeRuntime[] children = Array.Empty<BTNodeRuntime>();

    public void SetChildren(BTNodeRuntime[] list)
    {
        children = list;
    }
}

/// <summary>选择节点: 从左到右找第一个成功的行为, 遇到成功就停</summary>
public class SelectorNode : CompositeNode
{
    public override Result Evaluate()
    {
        foreach (var child in children)
        {
            var r = child.Evaluate();
            if (r == Result.Success) return Finish(Result.Success);
            if (r == Result.Running) return Finish(Result.Running);   // 有节点在跑, 整体算运行中
        }
        return Finish(Result.Failure);
    }
}

/// <summary>顺序节点: 从左到右全部执行, 全部成功才成功</summary>
public class SequenceNode : CompositeNode
{
    public override Result Evaluate()
    {
        foreach (var child in children)
        {
            var r = child.Evaluate();
            if (r == Result.Failure) return Finish(Result.Failure);
            if (r == Result.Running) return Finish(Result.Running);
        }
        return Finish(Result.Success);
    }
}

/// <summary>行为节点: 执行一个具体行为, 结果由外部注入决定</summary>
public class ActionNode : BTNodeRuntime
{
    private readonly string _actionName;
    private readonly Func<string, Result> _executor;

    public ActionNode(string actionName, Func<string, Result> executor)
    {
        _actionName = actionName;
        _executor = executor;
    }

    public override Result Evaluate()
    {
        return Finish(_executor != null ? _executor(_actionName) : Result.Success);
    }
}

/// <summary>条件节点: 根据条件类型判断 成功/失败(条件的执行由外部注入)</summary>
public class ConditionNode : BTNodeRuntime
{
    private readonly string _conditionType;
    private readonly Func<string, Result> _executor;

    public ConditionNode(string conditionType, Func<string, Result> executor)
    {
        _conditionType = conditionType;
        _executor = executor;
    }

    public override Result Evaluate()
    {
        return Finish(_executor != null ? _executor(_conditionType) : Result.Failure);
    }
}

/// <summary>反转节点: 子节点成功变失败, 失败变成功(Running保持)</summary>
public class InverterNode : CompositeNode
{
    public override Result Evaluate()
    {
        if (children.Length == 0) return Finish(Result.Failure);
        var r = children[0].Evaluate();
        if (r == Result.Running) return Finish(Result.Running);
        return Finish(r == Result.Success ? Result.Failure : Result.Success);
    }
}

/// <summary>等待节点: 进入后等指定秒数, 时间到返回成功</summary>
public class WaitNode : CompositeNode
{
    private readonly float _waitTime;
    private float _startTime = -1f;   // -1 = 还没开始计时

    public WaitNode(float waitTime)
    {
        _waitTime = Mathf.Max(0f, waitTime);
    }

    public override Result Evaluate()
    {
        if (_startTime < 0f) _startTime = Time.time;   // 第一次进入开始计时
        if (Time.time - _startTime >= _waitTime)
        {
            _startTime = -1f;   // 计时完成, 下次重新计
            return Finish(Result.Success);
        }
        return Finish(Result.Running);
    }
}