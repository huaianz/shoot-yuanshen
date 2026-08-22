using UnityEngine;

/// <summary>
/// 图驱动敌人AI: 加载行为树JSON, 把 Action 节点映射到真实敌人行为
/// 挂在敌人身上即可, 会自动接管行为树
/// </summary>
public class EnemyGraphAIController : MonoBehaviour
{
    [Tooltip("Resources下的行为树JSON路径(不带扩展名)")]
    public string treePath = "BehaviorTrees/MyTree";

    private EnemyBase _enemy;
    private RuntimeBehaviorTree _tree;

    // 复用项目里现成的行为节点(巡逻/追击/攻击/撤退的全部逻辑都在里面)
    private PatrolAction _patrol;
    private ChaseAction _chase;
    private MeleeAttackAction _attack;
    private RetreatAction _retreat;

    // 现成的条件节点(判断有没有目标/够不够近)
    private HasTargetCondition _hasTarget;
    private InAttackRangeCondition _inAttackRange;
    private IsHitCondition _isHit;

    // 死亡处理(替代内部树的死亡节点)
    private bool _deathStarted;
    private float _deathEndTime;

    private void Start()
    {
        _enemy = GetComponent<EnemyBase>();
        if (_enemy == null)
        {
            Debug.LogError("EnemyGraphAIController 需要挂在 EnemyBase 上");
            enabled = false;
            return;
        }

        // 接管: 内部行为树不再自己跑
        _enemy.useExternalBehaviorTree = true;

        // 复用现有动作节点
        _patrol = new PatrolAction(_enemy);
        _chase = new ChaseAction(_enemy);
        _attack = new MeleeAttackAction(_enemy);
        _retreat = new RetreatAction(_enemy);
        _hasTarget = new HasTargetCondition(_enemy);
        _inAttackRange = new InAttackRangeCondition(_enemy);
        _isHit = new IsHitCondition(_enemy);

        // 加载编辑器里画的树
        TextAsset asset = Resources.Load<TextAsset>(treePath);
        if (asset == null)
        {
            Debug.LogError($"找不到行为树文件: Resources/{treePath}");
            enabled = false;
            return;
        }
        var data = JsonUtility.FromJson<BTGraphData>(asset.text);
        _tree = new RuntimeBehaviorTree(data, ExecuteAction, ExecuteCondition);
    }

    private void Update()
    {
        // 死亡: 播死亡动画, 1.5秒后清理(替代内部树的死亡节点)
        if (_enemy.IsDead)
        {
            if (!_deathStarted)
            {
                _deathStarted = true;
                _enemy.PlayDeathAnimation();
                _deathEndTime = Time.time + 1.5f;
            }
            if (Time.time >= _deathEndTime)
            {
                _enemy.Clear();
            }
            return;
        }

        // 活着: 每帧跑图树
        _tree?.Evaluate();
    }

    /// <summary>
    /// 图里的 Action 节点执行到这里: 根据行为名映射到真实敌人行为
    /// 返回 Success/Failure 让 Selector 决定要不要换下一个行为
    /// </summary>
    private BTNodeRuntime.Result ExecuteAction(string actionName)
    {
        switch (actionName)
        {
            case "Attack":
                // 有目标且够近才攻击, 否则返回失败(Selector会尝试下一个行为)
                if (_hasTarget.Evaluate() == NodeState.Success
                    && _inAttackRange.Evaluate() == NodeState.Success)
                {
                    var attackResult = ToResult(_attack.Evaluate());
                    // 攻击冷却中(返回Running)但目标已跑出攻击范围 -> 不阻塞, 让树去追击
                    if (attackResult == BTNodeRuntime.Result.Running
                        && _inAttackRange.Evaluate() != NodeState.Success)
                    {
                        return BTNodeRuntime.Result.Failure;
                    }
                    return attackResult;
                }
                return BTNodeRuntime.Result.Failure;

            case "Chase":
                if (_hasTarget.Evaluate() == NodeState.Success)
                {
                    return ToResult(_chase.Evaluate());
                }
                return BTNodeRuntime.Result.Failure;

            case "Retreat":
                if (_hasTarget.Evaluate() == NodeState.Success)
                {
                    return ToResult(_retreat.Evaluate());
                }
                return BTNodeRuntime.Result.Failure;

            case "Patrol":
                return ToResult(_patrol.Evaluate());

            default:
                return BTNodeRuntime.Result.Success;   // 不认识的行为名不阻塞树
        }
    }

    /// <summary>
    /// 图里的条件节点执行到这里: 根据条件类型判断真假
    /// </summary>
    private BTNodeRuntime.Result ExecuteCondition(string conditionType)
    {
        switch (conditionType)
        {
            case "HasTarget":     return ToResult(_hasTarget.Evaluate());
            case "InAttackRange": return ToResult(_inAttackRange.Evaluate());
            case "IsDead":        return _enemy.IsDead ? BTNodeRuntime.Result.Success : BTNodeRuntime.Result.Failure;
            case "IsHit":         return ToResult(_isHit.Evaluate());
            default:                return BTNodeRuntime.Result.Failure;
        }
    }

    /// <summary>把现有节点的 NodeState 转成图树的 Result</summary>
    private static BTNodeRuntime.Result ToResult(NodeState state)
    {
        switch (state)
        {
            case NodeState.Success: return BTNodeRuntime.Result.Success;
            case NodeState.Running: return BTNodeRuntime.Result.Running;
            default: return BTNodeRuntime.Result.Failure;
        }
    }
}