using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色选择界面的 3D 预览控制器。
/// Camera、Light、RenderTexture、预览根节点和拖拽组件全部在场景中手动配置。
/// </summary>
public class CharacterPreview : MonoBehaviour
{
    private const string PlayerControllerPath = "Animation/Player/Player";

    [Header("手动绑定")]
    public RawImage previewImage;
    public Camera previewCamera;
    public Transform modelRoot;
    public GameObject previewStageRoot;
    public CharacterPreviewDragRotator dragRotator;

    [Header("角色 Prefab")]
    public GameObject[] characterPrefabs;

    [Header("动画")]
    public RuntimeAnimatorController idleController;
    public string idleStateName = "Idle";

    [Header("相机取景")]
    public bool autoFrameCamera = true;
    public float framingPadding = 1.18f;
    public float initialYaw = 180f;
    public int previewLayer = 31;

    private GameObject _current;
    private int _currentIndex = -1;
    private Coroutine _frameRoutine;

    private void Awake()
    {
        if (previewStageRoot != null)
            previewStageRoot.SetActive(false);
    }

    public void Show(int index)
    {
        if (!HasValidConfiguration())
        {
            Debug.LogWarning("[CharacterPreview] 请先在 Inspector 中绑定预览组件和角色 Prefab");
            return;
        }

        if (index < 0 || index >= characterPrefabs.Length || characterPrefabs[index] == null)
        {
            Clear();
            return;
        }

        previewStageRoot.SetActive(true);
        previewImage.enabled = true;

        if (index == _currentIndex && _current != null)
            return;

        DestroyCurrentModel();
        _currentIndex = index;
        _current = Instantiate(characterPrefabs[index], modelRoot, false);
        _current.name = $"CharacterPreview_{index}";
        _current.transform.localPosition = Vector3.zero;
        _current.transform.localRotation = Quaternion.identity;
        _current.transform.localScale = Vector3.one;

        SetLayerRecursively(_current, previewLayer);
        ConfigureAnimator(_current);

        modelRoot.localRotation = Quaternion.Euler(0f, initialYaw, 0f);
        if (dragRotator != null)
            dragRotator.SetTarget(modelRoot, initialYaw);

        if (autoFrameCamera)
        {
            if (_frameRoutine != null)
                StopCoroutine(_frameRoutine);
            _frameRoutine = StartCoroutine(FrameCameraAfterAnimation());
        }
    }

    public void Clear()
    {
        if (_frameRoutine != null)
        {
            StopCoroutine(_frameRoutine);
            _frameRoutine = null;
        }

        DestroyCurrentModel();
        if (previewStageRoot != null)
            previewStageRoot.SetActive(false);
    }

    private bool HasValidConfiguration()
    {
        return previewImage != null
               && previewCamera != null
               && modelRoot != null
               && previewStageRoot != null
               && characterPrefabs != null
               && characterPrefabs.Length > 0;
    }

    private void ConfigureAnimator(GameObject model)
    {
        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        if (animators.Length == 0)
        {
            Debug.LogWarning($"[CharacterPreview] {model.name} 没有 Animator，将显示静态模型");
            return;
        }

        Animator target = animators[0];
        foreach (Animator animator in animators)
        {
            if (animator.avatar != null && animator.avatar.isValid)
            {
                target = animator;
                break;
            }
        }

        target.applyRootMotion = false;
        target.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        RuntimeAnimatorController playerController =
            Resources.Load<RuntimeAnimatorController>(PlayerControllerPath);
        target.runtimeAnimatorController = playerController != null ? playerController : idleController;
        target.Rebind();

        if (target.runtimeAnimatorController != null && target.avatar != null && target.avatar.isValid)
            target.Play(Animator.StringToHash(idleStateName), 0, 0f);
        else
            Debug.LogWarning($"[CharacterPreview] {model.name} 的 Avatar 或 Idle Controller 无效");
    }

    private IEnumerator FrameCameraAfterAnimation()
    {
        yield return null;
        yield return null;

        if (_current == null || previewCamera == null)
            yield break;

        Bounds bounds = CalculateBounds(_current);
        if (bounds.size.y < 0.01f)
            yield break;

        float aspect = Mathf.Max(0.1f, previewCamera.aspect);
        float halfFov = previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distanceByHeight = bounds.extents.y / Mathf.Tan(halfFov);
        float distanceByWidth = bounds.extents.x / (Mathf.Tan(halfFov) * aspect);
        float distance = Mathf.Max(distanceByHeight, distanceByWidth) * framingPadding;

        Vector3 target = new Vector3(
            bounds.center.x,
            bounds.min.y + bounds.size.y * 0.52f,
            bounds.center.z);

        previewCamera.transform.position = target - Vector3.forward * distance;
        previewCamera.transform.rotation = Quaternion.identity;
        _frameRoutine = null;
    }

    private static Bounds CalculateBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds result = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                result = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        return result;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void DestroyCurrentModel()
    {
        if (_current != null)
        {
            _current.SetActive(false);
            Destroy(_current);
        }
        _current = null;
        _currentIndex = -1;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
}
