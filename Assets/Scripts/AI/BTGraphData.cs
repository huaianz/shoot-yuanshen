using System;
using System.Collections.Generic;

/// <summary>行为树节点数据(序列化用)</summary>
[Serializable]
public class BTNodeData
{
    public string guid;          // 节点唯一ID
    public string type;          // Selector / Sequence / Action
    public string actionName;    // Action 节点的行为名
    public float posX;           // 节点在画布上的位置
    public float posY;
    public string conditionType;   // Condition节点用: HasTarget/InAttackRange/IsDead/IsHit
    public float waitTime;         // Wait节点用: 等待秒数
}

/// <summary>连线数据(序列化用)</summary>
[Serializable]
public class BTEdgeData
{
    public string fromGUID;      // 父节点(输出端)
    public string toGUID;        // 子节点(输入端)
}

/// <summary>整张行为树图(包装类, JsonUtility 不支持直接序列化 List)</summary>
[Serializable]
public class BTGraphData
{
    public List<BTNodeData> nodes = new List<BTNodeData>();
    public List<BTEdgeData> edges = new List<BTEdgeData>();
}