using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行为树冒烟测试
/// </summary>
public class BTSmokeTest : MonoBehaviour
{
    private BTNode _tree;
    private int _frameCount;

    private void Awake()
    {
        _tree = new Selector(
            new Sequence(
                new TestCondition(3),
                new LogAction("攻击!")
            ),
        new LogAction("追击...")
        );
    }

    private void Update()
    {
        _frameCount++;
        NodeState result = _tree.Evaluate();
        Debug.Log($"第 {_frameCount} 帧 → 结果 {result}");
    }
}

/// <summary>测试条件:前 successFrames 帧成功,之后失败</summary>
public class TestCondition : BTNode
{
    private readonly int _successFrames;
    private int _count;

    public TestCondition(int successFrames)
    {
        _successFrames = successFrames;
        NodeName = "测试条件";
    }

    public override NodeState Evaluate()
    {
        _count++;
        return _count <= _successFrames ? NodeState.Success : NodeState.Failure;
    }
}

/// <summary>测试动作:打印一句话,返回成功</summary>
public class LogAction : BTNode
{
    private readonly string _message;

    public LogAction(string message)
    {
        _message = message;
        NodeName = "打印";
    }

    public override NodeState Evaluate()
    {
        Debug.Log(_message);
        return NodeState.Success;
    }
}