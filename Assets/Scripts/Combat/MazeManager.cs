using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序化迷宫试炼: 在安全区生成一个随机迷宫, 玩家走到出口拿奖励
/// 场景内单例(不跨场景), 由 GameManager.Start 调用 SetupPortal 创建入口
/// </summary>
public class MazeManager : MonoBehaviour
{
    private static MazeManager _instance;
    public static MazeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MazeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MazeManager");
                    _instance = go.AddComponent<MazeManager>();
                }
            }
            return _instance;
        }
    }

    [Tooltip("迷宫横向房间数")]
    public int roomCountX = 7;
    [Tooltip("迷宫纵向房间数")]
    public int roomCountY = 7;
    [Tooltip("每个格子边长(米)")]
    public float cellSize = 3f;
    [Tooltip("墙高(米)")]
    public float wallHeight = 3.2f;

    private bool _portalSet;
    private bool _mazeBuilt;
    private readonly List<GameObject> _mazeObjects = new List<GameObject>();
    private Material _floorMat;
    private Material _wallMat;

    /// <summary>在指定位置创建迷宫入口(发光标记 + 触发器), 每个场景只创建一次</summary>
    public static void SetupPortal(Vector3 position)
    {
        Instance.SetupPortalInternal(position);
    }

    private void SetupPortalInternal(Vector3 position)
    {
        if (_portalSet) return;
        _portalSet = true;

        // 入口标记: 一个发光的蓝色圆盘
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "MazePortalMarker";
        marker.transform.position = position + Vector3.up * 0.1f;
        marker.transform.localScale = new Vector3(4f, 0.15f, 4f);
        Renderer markerR = marker.GetComponent<Renderer>();
        markerR.sharedMaterial = CreateColoredMaterial(new Color(0.2f, 0.8f, 1f));

        // 触发器
        GameObject triggerGo = new GameObject("MazePortalTrigger");
        triggerGo.transform.position = position + Vector3.up * 1f;
        BoxCollider col = triggerGo.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(6f, 3f, 6f);
        triggerGo.AddComponent<MazePortalTrigger>();

        ToastUI.ShowMessage("前方出现随机迷宫试炼入口，进去探索吧!", new Color(0.4f, 0.8f, 1f));
    }

    /// <summary>玩家进入入口: 生成迷宫并把玩家传送到起点</summary>
    public void TryEnterMaze(Vector3 portalPos)
    {
        if (_mazeBuilt) return;
        _mazeBuilt = true;

        BuildMaze(portalPos);
        Vector3 startPos = CellToWorld(portalPos, new Vector2Int(1, 1));
        TeleportPlayer(startPos);
        ToastUI.ShowMessage("随机迷宫已生成，找到出口宝箱!", new Color(0.4f, 1f, 0.5f));
    }

    private void BuildMaze(Vector3 center)
    {
        bool[,] grid = MazeGenerator.Generate(roomCountX, roomCountY);
        int pw = grid.GetLength(0);
        int ph = grid.GetLength(1);

        for (int x = 0; x < pw; x++)
        {
            for (int y = 0; y < ph; y++)
            {
                Vector3 pos = CellToWorld(center, new Vector2Int(x, y));
                if (grid[x, y])
                {
                    // 地板
                    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.name = "MazeFloor";
                    floor.transform.position = pos + Vector3.down * 0.05f;
                    floor.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                    SetMaterial(floor, GetFloorMat());
                    _mazeObjects.Add(floor);
                }
                else
                {
                    // 墙
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "MazeWall";
                    wall.transform.position = pos + Vector3.up * (wallHeight * 0.5f);
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
                    SetMaterial(wall, GetWallMat());
                    _mazeObjects.Add(wall);
                }
            }
        }

        // 出口: 右下角房间放一个触发区
        GameObject exitGo = new GameObject("MazeExitTrigger");
        exitGo.transform.position = CellToWorld(center, new Vector2Int(pw - 2, ph - 2)) + Vector3.up * 1f;
        BoxCollider exitCol = exitGo.AddComponent<BoxCollider>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector3(cellSize, 3f, cellSize);
        MazeExitTrigger exit = exitGo.AddComponent<MazeExitTrigger>();
        exit.Init(this);
        _mazeObjects.Add(exitGo);
    }

    /// <summary>玩家到达出口: 发奖励并清理迷宫</summary>
    public void FinishMaze()
    {
        if (!_mazeBuilt) return;
        _mazeBuilt = false;

        // 奖励: 3个鲜果火腿吐司(食物ID 3001)
        InventoryManager.INSTANCE.AddFood(3001, 3);
        ToastUI.ShowMessage("迷宫通关! 获得 鲜果火腿吐司 x3", new Color(1f, 0.9f, 0.4f));
        StartCoroutine(ClearMazeAfterDelay(1.5f));
    }

    private IEnumerator ClearMazeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (GameObject go in _mazeObjects)
        {
            if (go != null) Destroy(go);
        }
        _mazeObjects.Clear();
    }

    /// <summary>逻辑格子坐标 -> 世界坐标(迷宫以入口为中心)</summary>
    private Vector3 CellToWorld(Vector3 center, Vector2Int cell)
    {
        int pw = roomCountX * 2 + 1;
        int ph = roomCountY * 2 + 1;
        Vector3 origin = center - new Vector3(pw * cellSize * 0.5f, 0f, ph * cellSize * 0.5f);
        return origin + new Vector3(cell.x * cellSize + cellSize * 0.5f, 0f, cell.y * cellSize + cellSize * 0.5f);
    }

    private void TeleportPlayer(Vector3 pos)
    {
        PlayerController player = PlayerController.INSTANCE;
        if (player == null) return;

        if (player.currentPlayerModel != null)
        {
            CharacterController cc = player.currentPlayerModel.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.currentPlayerModel.transform.position = pos;
            if (cc != null) cc.enabled = true;
        }
        player.transform.position = pos;
    }

    private void SetMaterial(GameObject go, Material mat)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    private Material GetFloorMat()
    {
        if (_floorMat == null)
        {
            _floorMat = CreateColoredMaterial(new Color(0.35f, 0.35f, 0.38f));
        }
        return _floorMat;
    }

    private Material GetWallMat()
    {
        if (_wallMat == null)
        {
            _wallMat = CreateColoredMaterial(new Color(0.55f, 0.5f, 0.45f));
        }
        return _wallMat;
    }

    /// <summary>
    /// 复制 Unity 默认材质来创建带颜色的材质:
    /// 保证着色器一定有效(直接 Shader.Find 可能找不到而变成紫色)
    /// </summary>
    private Material CreateColoredMaterial(Color color)
    {
        GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material mat = new Material(probe.GetComponent<Renderer>().sharedMaterial);
        Destroy(probe);
        mat.color = color;
        return mat;
    }

    private void OnDestroy()
    {
        if (_floorMat != null) Destroy(_floorMat);
        if (_wallMat != null) Destroy(_wallMat);
    }
}
