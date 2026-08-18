using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 房间触发器: 玩家进入 -> 关门 + 刷怪; 全部怪物死亡 -> 开门
/// </summary>
public class RoomTrigger : MonoBehaviour
{
    [Tooltip("房间的门")]
    public DoorController door;
    [Tooltip("进入房间的提示")]
    public string prompt = "门已关闭！";

    [Header("刷怪配置")]
    [Tooltip("刷怪点(可以放多个)")]
    public Transform[] spawnPoints;
    [Tooltip("敌人预制体(每次随机挑)")]
    public GameObject[] enemyPrefabs;
    [Tooltip("刷怪数量")]
    public int spawnCount = 3;

    [Header("清怪开门")]
    [Tooltip("清完怪后的提示")]
    public string clearPrompt = "所有怪物已清除，门已打开！";

    private bool _entered;   // 只触发一次
    private bool _cleared;   // 是否已开门
    private readonly List<GameObject> _spawned = new List<GameObject>(); // 本房间刷出的怪物

    private void OnTriggerEnter(Collider other)
    {
        if (_entered) return;
        if (other.GetComponentInParent<PlayerController>() == null
            && other.GetComponentInParent<PlayerModel>() == null) return;

        _entered = true;
        if (door != null) door.Close();
        ToastUI.ShowMessage(prompt, new Color(1f, 0.6f, 0.2f));
        SpawnEnemies();
        StartCoroutine(CheckCleared());
    }

    /// <summary>
    /// 在刷怪点生成怪物, 并记录到列表(用于清怪判定)
    /// </summary>
    private void SpawnEnemies()
    {
        _spawned.Clear();
        if (enemyPrefabs == null || enemyPrefabs.Length == 0
            || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[RoomTrigger] 没配置敌人预制体或刷怪点");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            if (NavMesh.SamplePosition(point.position + offset, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                _spawned.Add(Instantiate(prefab, hit.position, point.rotation));
            }
        }
        ToastUI.ShowMessage("怪物来袭！", new Color(1f, 0.5f, 0.2f));
    }

    /// <summary>
    /// 清怪判定: 每0.5秒清理已销毁的怪物, 全没了就开门(节流轮询, 不每帧算)
    /// </summary>
    private IEnumerator CheckCleared()
    {
        while (!_cleared)
        {
            // 把已销毁(死亡动画播完)的怪物移除; 空了 -> 开门
            _spawned.RemoveAll(e => e == null);
            if (_spawned.Count == 0)
            {
                _cleared = true;
                if (door != null) door.Open();
                ToastUI.ShowMessage(clearPrompt, new Color(0.4f, 1f, 0.5f));
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}