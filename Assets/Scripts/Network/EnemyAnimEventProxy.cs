using UnityEngine;

/// <summary>
/// 接收敌人动画剪辑里的 AnimationEvent。
/// 展示版预制体删掉了原 AI 脚本, 没有接收者会刷 "has no receiver" 警告。
/// 这里只做空实现, 不影响网络表现。
/// </summary>
public class EnemyAnimEventProxy : MonoBehaviour
{
    public void OnThrowRock() { }
    public void OnAttack() { }
    public void OnAttackEnd() { }
    public void OnHit() { }
    public void OnDeath() { }
}
