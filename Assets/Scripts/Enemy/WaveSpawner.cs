using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 波次刷怪器
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("波次配置")]
    [Tooltip("按顺序配置每一波")]
    public List<WaveConfig> waves = new List<WaveConfig>();

    [Header("刷怪点")]
    [Tooltip("可以放多个点；如果某个点下面有子物体，会自动在子物体里随机挑")]
    public Transform[] spawnPoints;

    [Header("进图后多久开始刷第一波")]
    public float startDelay = 10f;

    // 当前是第几波(从0开始, 显示时+1)
    private int _currentWaveIndex = -1;
    // 本波已经生成、还没被销毁的敌人
    private List<GameObject> _aliveEnemies = new List<GameObject>();
    // 是否已经全部通关
    private bool _allFinished;

    public int CurrentWaveNumber => _currentWaveIndex + 1;
    public bool AllFinished => _allFinished;

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    /// <summary>
    /// 波次主循环
    /// </summary>
    private IEnumerator RunWaves()
    {
        // 进场一会儿才刷怪
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < waves.Count; i++)
        {
            if (waves[i] == null) continue; // 跳过空的波次配置

            _currentWaveIndex = i;

            ToastUI.ShowMessage($"第 {i + 1} 波: {waves[i].waveName}", new Color(1f, 0.6f, 0.2f));

            //刷出这一波的全部敌人
            SpawnWave(waves[i]);

            //挂起协程，等这一波敌人全部死亡(被销毁)才继续
            yield return WaitUntilWaveCleared();

            ToastUI.ShowMessage($"第 {i + 1} 波完成!", new Color(0.4f, 1f, 0.5f));

            //如果是最后一波，通关
            if (i == waves.Count - 1)
            {
                _allFinished = true;
                ToastUI.ShowMessage("全部波次通关! 回安全区休整吧", new Color(1f, 0.9f, 0.4f));
                yield break;
            }

            //中间的波次：等几秒再刷下一波
            yield return new WaitForSeconds(waves[i].nextWaveDelay);
        }
    }

    /// <summary>
    /// 刷出指定波次的全部敌人
    /// </summary>
    private void SpawnWave(WaveConfig wave)
    {
        _aliveEnemies.Clear();

        foreach (var group in wave.enemies)
        {
            if (group == null || group.enemyPrefab == null) continue; // 空配置跳过

            for (int i = 0; i < group.count; i++)
            {
                Transform point = PickSpawnPoint();
                if (point == null) continue;

                //随机偏移一点，避免多个敌人完全重叠卡死
                Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

                //只在地面上的有效导航点生成
                if (NavMesh.SamplePosition(point.position + offset, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    GameObject enemy = Instantiate(group.enemyPrefab, hit.position, point.rotation);
                    _aliveEnemies.Add(enemy); // 记入存活列表
                }
            }
        }
    }

    /// <summary>
    /// 挑一个刷怪点：如果点的下面有子物体，就在子物体里随机挑。
    /// </summary>
    private Transform PickSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        Transform p = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (p == null) return null;

        if (p.childCount > 0)
        {
            return p.GetChild(Random.Range(0, p.childCount));
        }
        return p;
    }

    /// <summary>
    /// 等待本波敌人全部被销毁。
    /// 敌人死亡后 DeathAction 会等死亡动画播完再 Destroy，所以这里用节流轮询：
    /// 每 0.5 秒清理一次已销毁的引用，不每帧计算，省性能。
    /// </summary>
    private IEnumerator WaitUntilWaveCleared()
    {
        while (_aliveEnemies.Count > 0)
        {
            // 把已经被销毁的敌人从列表里移除；移完就空了 -> 本波结束
            _aliveEnemies.RemoveAll(e => e == null);
            yield return new WaitForSeconds(0.5f);
        }
    }
}