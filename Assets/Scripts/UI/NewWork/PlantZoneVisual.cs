using UnityEngine;

/// <summary>下包范围可视化: 在地面画一个圆环, 进入范围变绿</summary>
public class PlantZoneVisual : MonoBehaviour
{
    public Transform plantSite;
    public float radius = 3f;
    public int segments = 64;
    public float width = 0.09f;

    public Color outColor = new Color(0.3f, 0.9f, 1f, 0.75f);
    public Color insideColor = new Color(0.4f, 1f, 0.5f, 0.95f);

    private LineRenderer _line;
    private bool _subscribed;
    private bool _wasVisible;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) _line.material = new Material(shader);

        _line.useWorldSpace = true;
        _line.loop = true;
        _line.positionCount = Mathf.Max(12, segments);
        _line.startWidth = _line.endWidth = width;
        _line.numCapVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
    }

    private void OnEnable() { TrySubscribe(); }
    private void Start() { TrySubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;
        BattleClient.Instance.OnRoundState += _ => enabled = true;
        _subscribed = true;
    }

    private void Update()
    {
        TrySubscribe();

        if (plantSite == null || BattleClient.Instance == null)
        {
            _line.enabled = false;
            return;
        }

        var round = BattleClient.Instance.LastRound;
        bool inTactical = RoomSession.CurrentMode == "pvp" && RoomSession.InRoom;
        bool activePhase = round != null && (round.phase == 1 || round.phase == 2);

        if (!inTactical || !activePhase)
        {
            if (_wasVisible)
            {
                Debug.Log($"[PlantZone] 隐藏 mode={RoomSession.CurrentMode} phase={(round != null ? round.phase : -1)}");
                _wasVisible = false;
            }

            _line.enabled = false;
            return;
        }

        if (!_wasVisible)
        {
            _wasVisible = true;
            Debug.Log($"[PlantZone] 显示 mode={RoomSession.CurrentMode} phase={round.phase} site={plantSite.position}");
        }

        _line.enabled = true;

        Vector3 center = plantSite.position + Vector3.up * 0.05f;
        int count = _line.positionCount;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            _line.SetPosition(i, p);
        }

        // 进攻方站在圈内时变绿
        bool inside = false;
        var pc = PlayerController.INSTANCE;
        if (pc != null && pc.currentPlayerModel != null)
        {
            Vector3 me = pc.currentPlayerModel.transform.position;
            inside = Vector3.Distance(new Vector3(me.x, 0f, me.z),
                                      new Vector3(center.x, 0f, center.z)) <= radius;
        }

        Color c = (BattleClient.Instance.MyTeam == 0 && inside) ? insideColor : outColor;
        _line.startColor = c;
        _line.endColor = c;
    }
}
