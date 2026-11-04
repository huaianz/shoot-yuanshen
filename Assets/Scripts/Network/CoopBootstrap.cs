using UnityEngine;

public class CoopBootstrap : MonoBehaviour
{
    [Tooltip("勾上则跨场景常驻(传送时连接不断)")]
    public bool persistAcrossScenes = true;

    private void Awake()
    {
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        // 弹道表现组件: 场景里漏挂时自动补上, 否则看不到任何玩家/敌人的弹道
        if (GetComponent<EnemyTracerFX>() == null)
            gameObject.AddComponent<EnemyTracerFX>();

        // 其他玩家的真实子弹表现(纯视觉, 不结算伤害)
        if (GetComponent<RemoteBulletFX>() == null)
            gameObject.AddComponent<RemoteBulletFX>();
    }
}
