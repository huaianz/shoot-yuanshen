using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从编辑器保存的 BTGraphData 构建一棵可运行的树
/// </summary>
public class RuntimeBehaviorTree
{
    private readonly BTNodeRuntime _root;

    public RuntimeBehaviorTree(BTGraphData data,
        System.Func<string, BTNodeRuntime.Result> actionExecutor,
        System.Func<string, BTNodeRuntime.Result> conditionExecutor)
    {
        if (data == null || data.nodes.Count == 0) return;

        // 1. 创建所有运行时节点(GUID -> 节点)
        var nodeMap = new Dictionary<string, BTNodeRuntime>();
        var childrenMap = new Dictionary<string, List<string>>();  // 父GUID -> 子GUID列表
        var hasParent = new HashSet<string>();                     // 谁有父节点

        foreach (var nodeData in data.nodes)
        {
            childrenMap[nodeData.guid] = new List<string>();
            switch (nodeData.type)
            {
                case "Selector": nodeMap[nodeData.guid] = new SelectorNode(); break;
                case "Sequence": nodeMap[nodeData.guid] = new SequenceNode(); break;
                case "Condition": nodeMap[nodeData.guid] = new ConditionNode(nodeData.conditionType, conditionExecutor); break;
                case "Inverter": nodeMap[nodeData.guid] = new InverterNode(); break;
                case "Wait":     nodeMap[nodeData.guid] = new WaitNode(nodeData.waitTime); break;
                default: nodeMap[nodeData.guid] = new ActionNode(nodeData.actionName, actionExecutor); break;
            }
            nodeMap[nodeData.guid].NodeGUID = nodeData.guid;   // 记住画布ID(调试用)
        }

        // 2. 按连线整理父子关系
        foreach (var edgeData in data.edges)
        {
            hasParent.Add(edgeData.toGUID);
            if (childrenMap.TryGetValue(edgeData.fromGUID, out var list))
            {
                list.Add(edgeData.toGUID);
            }
        }

        // 3. 给组合节点填子节点(按连线顺序)
        foreach (var pair in childrenMap)
        {
            if (nodeMap[pair.Key] is not CompositeNode composite) continue;

            var childNodes = new List<BTNodeRuntime>();
            foreach (var childGuid in pair.Value)
            {
                if (nodeMap.TryGetValue(childGuid, out var child))
                {
                    childNodes.Add(child);
                }
            }
            composite.SetChildren(childNodes.ToArray());
        }

        // 4. 找根节点: 没有被任何连线指向的节点(整棵树只有一个根)
        string rootGuid = null;
        foreach (var nodeData in data.nodes)
        {
            if (!hasParent.Contains(nodeData.guid))
            {
                rootGuid = nodeData.guid;
                break;
            }
        }
        _root = rootGuid != null && nodeMap.TryGetValue(rootGuid, out var root) ? root : null;
    }

    public bool IsValid => _root != null;

    /// <summary>每帧执行整棵树</summary>
    public void Evaluate()
    {
        BehaviorTreeDebugBus.BeginFrame();   // 清空上一帧的调试记录
        _root?.Evaluate();
    }
}