using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 手动挂到 PreviewImage 上，接收 UI 拖拽事件并旋转已经绑定好的模型根节点。
/// </summary>
public class CharacterPreviewDragRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform target;
    public float sensitivity = 0.35f;
    public float inertiaDamping = 10f;

    private float _yaw;
    private float _velocity;
    private bool _dragging;

    public void SetTarget(Transform newTarget, float initialYaw)
    {
        target = newTarget;
        _yaw = initialYaw;
        _velocity = 0f;

        if (target != null)
            target.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = true;
        _velocity = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null) return;

        float delta = -eventData.delta.x * sensitivity;
        _yaw += delta;
        _velocity = delta / Mathf.Max(0.001f, Time.unscaledDeltaTime);
        ApplyRotation();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
    }

    private void LateUpdate()
    {
        if (_dragging || target == null || Mathf.Abs(_velocity) < 0.01f)
            return;

        _yaw += _velocity * Time.unscaledDeltaTime;
        _velocity = Mathf.Lerp(_velocity, 0f,
            1f - Mathf.Exp(-inertiaDamping * Time.unscaledDeltaTime));
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        target.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }
}
