using System.Collections.Generic;

/// <summary>
/// 行为树调试总线: 运行时把"每个节点本轮执行结果"记到这里
/// 编辑器窗口定时读取, 实现运行中节点高亮
/// </summary>
public static class BehaviorTreeDebugBus
{
    private static readonly Dictionary<string, BTNodeRuntime.Result> LastResults = new();

    /// <summary>记录一个节点的本轮结果</summary>
    public static void Record(string guid, BTNodeRuntime.Result result)
    {
        if (string.IsNullOrEmpty(guid)) return;
        LastResults[guid] = result;
    }

    /// <summary>读取指定节点的结果</summary>
    public static bool TryGet(string guid, out BTNodeRuntime.Result result)
    {
        return LastResults.TryGetValue(guid, out result);
    }

    /// <summary>每帧开头清空: 只保留本轮真正执行过的节点</summary>
    public static void BeginFrame()
    {
        LastResults.Clear();
    }
}