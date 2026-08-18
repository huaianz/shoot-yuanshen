using System.Collections;
using UnityEngine;

/// <summary>
/// 房间门: 在关闭位置和打开位置之间移动
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("门的位置(本地坐标)")]
    public Vector3 closedLocalPos = Vector3.zero;             // 关闭时的位置
    public Vector3 openedLocalPos = new Vector3(0f, 3f, 0f);  // 打开时的位置(默认向上移)
    [Tooltip("移动速度")]
    public float speed = 2f;

    public bool IsOpen { get; private set; }

    private Coroutine _move;

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        StartMove(openedLocalPos);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        StartMove(closedLocalPos);
    }

    private void StartMove(Vector3 target)
    {
        if (_move != null) StopCoroutine(_move);
        _move = StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        while (Vector3.Distance(transform.localPosition, target) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.localPosition = target;
        _move = null;
    }
}