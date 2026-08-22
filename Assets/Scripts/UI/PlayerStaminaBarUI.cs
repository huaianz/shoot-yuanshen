using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家体力条(世界空间, 跟随角色头顶, 自动面向摄像机)
/// 一直显示在当前操控角色头顶
/// </summary>
public class PlayerStaminaBarUI : MonoBehaviour
{
    [Tooltip("体力条填充块")]
    public Image staminaSlider;

    private Transform cameraTransform;
    private PlayerModel owner;//属于哪个角色

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        staminaSlider.fillAmount = 1;
    }

    private void OnEnable()
    {
        EventHandler.PlayerStaminaChangedEvent += OnStaminaChanged;
    }

    private void OnDisable()
    {
        EventHandler.PlayerStaminaChangedEvent -= OnStaminaChanged;
    }

    private void Update()
    {
        //始终面向摄像机(和怪物血条一样)
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        Vector3 dir = cameraTransform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(-dir);
    }

    /// <summary>
    /// 绑定所属角色(实例化时调用)
    /// </summary>
    public void Init(PlayerModel ownerModel)
    {
        owner = ownerModel;
    }

    private void OnStaminaChanged(float current, float max)
    {
        //只显示当前操控角色的体力条(一直显示, 不隐藏)
        bool isMine = owner == null || owner == PlayerController.INSTANCE?.currentPlayerModel;
        // 状态没变就不重复 SetActive
        if (gameObject.activeSelf != isMine)
        {
            gameObject.SetActive(isMine);
        }
        if (!isMine) return;

        if (max > 0f) staminaSlider.fillAmount = current / max;
    }
}
