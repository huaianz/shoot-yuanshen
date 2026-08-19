using UnityEngine;

/// <summary>
/// 地面掉落物：自动旋转+上下浮动，玩家靠近后自动拾取并提示。
/// PickupType 定义在 Enum.cs，LootDrop 定义在 GameData.cs。
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Header("掉落内容")]
    public PickupType pickupType;
    public int itemID;
    public int amount = 1;

    [Header("拾取参数")]
    [Tooltip("拾取半径")]
    public float pickupRadius = 2f;
    [Tooltip("检测间隔(性能: 不用每帧检测)")]
    public float checkInterval = 0.15f;
    [Tooltip("生成后多少秒内不可拾取")]
    public float spawnCooldown = 0.6f;

    [Header("表现")]
    [Tooltip("自转速度")]
    public float rotateSpeed = 120f;
    [Tooltip("上下浮动速度")]
    public float bobSpeed = 1.5f;
    [Tooltip("上下浮动高度")]
    public float bobHeight = 0.15f;

    private float _spawnTimer;
    private float _checkTimer;
    private Vector3 _basePos;
    private float _bobPhase;
    private bool _picked;

    private void Start()
    {
        _basePos = transform.position;
        _spawnTimer = spawnCooldown;
        _bobPhase = Random.value * 360f;
    }

    private void Update()
    {
        if (_picked) return;

        if (_spawnTimer > 0f)
        {
            _spawnTimer -= Time.deltaTime;
        }

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        _bobPhase += bobSpeed * Time.deltaTime;
        transform.position = _basePos + Vector3.up * (Mathf.Sin(_bobPhase) * bobHeight);

        if (_spawnTimer > 0f) return;
        _checkTimer -= Time.deltaTime;
        if (_checkTimer > 0f) return;
        _checkTimer = checkInterval;

        PlayerModel player = FindNearestPlayer();
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= pickupRadius)
        {
            PickUp();
        }
    }

    private PlayerModel FindNearestPlayer()
    {
        PlayerModel[] players = GameManager.INSTANCE != null ? GameManager.INSTANCE.playerModels : null;
        if (players == null || players.Length == 0) return null;

        PlayerModel nearest = null;
        float minDist = float.MaxValue;
        for (int i = 0; i < players.Length; i++)
        {
            PlayerModel p = players[i];
            if (p == null) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p;
            }
        }
        return nearest;
    }

    private void PickUp()
    {
        _picked = true;
        string message = "";
        Color color = Color.white;

        switch (pickupType)
        {
            case PickupType.Coin:
                if (ShopManager.INSTANCE != null)
                {
                    ShopManager.INSTANCE.AddCurrency("Coin", amount);
                }
                message = $"获得 金币 ×{amount}";
                color = new Color(1f, 0.84f, 0f);
                break;

            case PickupType.Weapon:
                if (InventoryManager.INSTANCE != null)
                {
                    InventoryManager.INSTANCE.AddWeapon(itemID);
                }
                message = $"获得 {InventoryManager.INSTANCE.GetItemName(itemID)}";
                color = new Color(1f, 0.65f, 0.2f);
                break;

            case PickupType.Food:
                if (InventoryManager.INSTANCE != null)
                {
                    InventoryManager.INSTANCE.AddFood(itemID, amount);
                }
                message = $"获得 {InventoryManager.INSTANCE.GetItemName(itemID)} ×{amount}";
                color = new Color(0.4f, 1f, 0.5f);
                break;

            case PickupType.Material:
                if (InventoryManager.INSTANCE != null)
                {
                    InventoryManager.INSTANCE.AddMaterial(itemID, amount);
                }
                message = $"获得 {InventoryManager.INSTANCE.GetItemName(itemID)} ×{amount}";
                color = new Color(0.7f, 0.5f, 1f);
                break;
        }

        EventHandler.CallItemCollectedEvent(itemID, amount);

        ToastUI.ShowMessage(message, color);
        AudioManager.INSTANCE.PlaySFX("Audio/SFX/Pickup", 0.9f);
        Destroy(gameObject);
    }

    public static void Spawn(Vector3 position, LootDrop drop)
    {
        if (drop == null || drop.dropPrefab == null) return;

        if (drop.chance < 1f && Random.value > drop.chance) return;

        Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
        GameObject go = Instantiate(drop.dropPrefab, position + Vector3.up * 0.3f + offset, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        PickupItem item = go.GetComponent<PickupItem>();
        if (item == null)
        {
            item = go.AddComponent<PickupItem>();
        }
        item.pickupType = drop.type;
        item.itemID = drop.itemID;
        item.amount = drop.amount;
    }
}