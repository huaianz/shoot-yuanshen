using UnityEngine;

/// <summary>
/// 行为树运行时测试器: 挂到场景空物体上, 从 Resources 加载树并每帧执行
/// </summary>
public class BehaviorTreeRunner : MonoBehaviour
{
    [Tooltip("Resources 下行为树 JSON 的路径(不带扩展名)")]
    public string treePath = "BehaviorTrees/MyTree";

    [Tooltip("是否打印行为日志")]
    public bool logActions = true;

    private RuntimeBehaviorTree _tree;

    private void Start()
    {
        TextAsset asset = Resources.Load<TextAsset>(treePath);
        if (asset == null)
        {
            Debug.LogError($"找不到行为树文件: Resources/{treePath}");
            return;
        }

        var data = JsonUtility.FromJson<BTGraphData>(asset.text);
        _tree = new RuntimeBehaviorTree(data, ExecuteAction, _ => BTNodeRuntime.Result.Success);   // 条件暂时都算成功, 方便测试

        if (_tree.IsValid)
        {
            Debug.Log("行为树加载成功");
        }
        else
        {
            Debug.LogError("行为树构建失败: 检查是否有多根节点/成环");
        }
    }

    private void Update()
    {
        _tree?.Evaluate();   // 每帧执行
    }

    /// <summary>Action 节点的实际执行(教学版打印, 以后替换成敌人行为)</summary>
    private BTNodeRuntime.Result ExecuteAction(string actionName)
    {
        if (logActions)
        {
            Debug.Log($"[行为树] 执行行为: {actionName}");
        }
        return BTNodeRuntime.Result.Success;   // 教学版: 执行完就算成功
    }
}