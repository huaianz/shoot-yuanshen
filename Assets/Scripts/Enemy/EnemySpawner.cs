using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("敌人预制体")]
    public GameObject[] enemyPrefabs;
    [Header("刷怪点")]
    public Transform[] spawnPoints;
    [Header("每波敌人数量")]
    public int enemiesPerWave = 3;
    [Header("波次间隔(秒)")]
    public float waveInterval = 5f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= waveInterval)
        {
            _timer = 0f;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: 没配置敌人预制体或刷怪点");
            return;
        }

        for (int i = 0; i < enemiesPerWave; i++)
        {
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // 随机偏移,避免几个敌人重叠卡死
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

            // 只在有效导航点生成(不在墙里/半空)
            if (NavMesh.SamplePosition(point.position + offset, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                Instantiate(prefab, hit.position, point.rotation);
            }
        }
        Debug.Log("刷出一波敌人");
    }
}