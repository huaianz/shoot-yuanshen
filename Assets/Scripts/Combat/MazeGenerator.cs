using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序化迷宫生成器: DFS 回溯算法(递归回溯)
/// 生成"完美迷宫"(任意两点之间只有一条通路)
/// </summary>
public static class MazeGenerator
{
    private static readonly Vector2Int[] Dir =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    /// <summary>
    /// 生成迷宫
    /// </summary>
    /// <param name="roomCountX">横向房间数</param>
    /// <param name="roomCountY">纵向房间数</param>
    /// <param name="seed">-1=每次随机</param>
    /// <returns>物理格子地图: true=可走(地板), false=墙</returns>
    public static bool[,] Generate(int roomCountX, int roomCountY, int seed = -1)
    {
        if (seed >= 0) Random.InitState(seed);

        // 物理格子 = 房间*2+1 (房间位置在奇数列/奇数行, 之间夹着墙)
        int pw = roomCountX * 2 + 1;
        int ph = roomCountY * 2 + 1;
        bool[,] open = new bool[pw, ph];

        // 先打通所有房间位置
        for (int x = 1; x < pw; x += 2)
            for (int y = 1; y < ph; y += 2)
                open[x, y] = true;

        // DFS 回溯: 从左上角房间出发
        bool[,] visited = new bool[roomCountX, roomCountY];
        visited[0, 0] = true;
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(0, 0));

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Peek();

            // 收集还没访问过的邻居
            List<Vector2Int> unvisited = new List<Vector2Int>();
            foreach (Vector2Int d in Dir)
            {
                Vector2Int next = cur + d;
                if (next.x < 0 || next.x >= roomCountX || next.y < 0 || next.y >= roomCountY) continue;
                if (!visited[next.x, next.y]) unvisited.Add(next);
            }

            if (unvisited.Count == 0)
            {
                stack.Pop();   // 死路, 回溯
                continue;
            }

            // 随机选一个邻居打通墙
            Vector2Int chosen = unvisited[Random.Range(0, unvisited.Count)];
            visited[chosen.x, chosen.y] = true;

            int wallX = cur.x * 2 + 1 + (chosen.x - cur.x);
            int wallY = cur.y * 2 + 1 + (chosen.y - cur.y);
            open[wallX, wallY] = true;

            stack.Push(chosen);
        }

        return open;
    }
}
