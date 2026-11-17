using UnityEngine;

/// <summary>
/// 给爆能器 FBX 补充发光核心、地面能量环和脉冲灯。
/// 这样模型不依赖外部贴图，不会出现粉色材质。
/// </summary>
[DisallowMultipleComponent]
public sealed class SpikeBeaconVisual : MonoBehaviour
{
    private Material _bodyMaterial;
    private Material _glowMaterial;
    private Material _ringMaterial;
    private Transform _core;
    private Transform _ring;
    private Light _pulseLight;
    private readonly Color _idleGlowColor = new Color(0.08f, 1.8f, 3.8f, 1f);
    private readonly Color _plantedGlowColor = new Color(4.2f, 0.12f, 0.04f, 1f);
    private Collider _contactCollider;
    private bool _planted;

    public Collider ContactCollider => _contactCollider;

    public void SetPlanted(bool planted)
    {
        _planted = planted;
    }

    private void Awake()
    {
        BuildMaterials();
        ApplyBodyMaterial();
        BuildCore();
        BuildEnergyRing();
        BuildLight();
        BuildContactCollider();
    }

    private void Update()
    {
        Color glowBase = _planted ? _plantedGlowColor : _idleGlowColor;
        float pulse = _planted
            ? Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Time.time * 12f), 3f)
            : 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f);

        if (_core != null)
        {
            float scale = _planted
                ? Mathf.Lerp(0.15f, 0.19f, pulse)
                : Mathf.Lerp(0.145f, 0.17f, pulse);
            _core.localScale = new Vector3(scale, scale, scale);
        }

        if (_glowMaterial != null)
        {
            Color glow = glowBase * Mathf.Lerp(1f, _planted ? 3.2f : 2.2f, pulse);
            glow.a = 1f;
            _glowMaterial.SetColor("_BaseColor", glow);
            _glowMaterial.SetColor("_Color", glow);
            _glowMaterial.SetColor("_EmissionColor", glow);
        }

        if (_ringMaterial != null)
        {
            Color ringColor = glowBase * Mathf.Lerp(
                0.4f, _planted ? 3f : 2f, pulse);
            ringColor.a = 1f;
            _ringMaterial.SetColor("_BaseColor", ringColor);
            _ringMaterial.SetColor("_Color", ringColor);
        }

        if (_ring != null)
            _ring.Rotate(0f, (_planted ? 65f : 28f) * Time.deltaTime, 0f, Space.Self);

        if (_pulseLight != null)
        {
            _pulseLight.color = _planted
                ? new Color(1f, 0.08f, 0.025f)
                : new Color(0.15f, 0.7f, 1f);
            _pulseLight.intensity = _planted
                ? Mathf.Lerp(0.25f, 3.2f, pulse)
                : Mathf.Lerp(0.7f, 1.7f, pulse);
        }
    }

    private void BuildMaterials()
    {
        _bodyMaterial = CreateLitMaterial(
            "Spike Body Runtime",
            new Color(0.025f, 0.045f, 0.07f, 1f),
            new Color(0f, 0f, 0f, 1f),
            0.78f,
            0.28f);
        _glowMaterial = CreateLitMaterial(
            "Spike Glow Runtime",
            new Color(0.02f, 0.18f, 0.32f, 1f),
            _idleGlowColor,
            0.05f,
            0.2f);
        _ringMaterial = CreateUnlitMaterial("Spike Ring Runtime", _idleGlowColor);
    }

    private void ApplyBodyMaterial()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer.gameObject.name.StartsWith("Spike Core")
                || renderer.gameObject.name.StartsWith("Spike Energy Ring"))
                continue;
            renderer.sharedMaterial = _bodyMaterial;
        }
    }

    private void BuildCore()
    {
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Spike Core";
        core.transform.SetParent(transform, false);
        core.transform.localPosition = new Vector3(0f, 0.33f, 0f);
        core.transform.localScale = Vector3.one * 0.16f;
        DisableCollider(core);
        core.GetComponent<Renderer>().sharedMaterial = _glowMaterial;
        _core = core.transform;
    }

    private void BuildEnergyRing()
    {
        GameObject ringObject = new GameObject("Spike Energy Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, 0.025f, 0f);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 49;
        line.widthMultiplier = 0.018f;
        line.startColor = _idleGlowColor;
        line.endColor = _idleGlowColor;
        line.sharedMaterial = _ringMaterial;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        const float radius = 0.34f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)(line.positionCount - 1) * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius));
        }

        _ring = ringObject.transform;
    }

    private void BuildLight()
    {
        GameObject lightObject = new GameObject("Spike Pulse Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.33f, 0f);
        _pulseLight = lightObject.AddComponent<Light>();
        _pulseLight.type = LightType.Point;
        _pulseLight.color = new Color(0.15f, 0.7f, 1f);
        _pulseLight.range = 1.8f;
        _pulseLight.intensity = 1f;
        _pulseLight.shadows = LightShadows.None;
    }

    private void BuildContactCollider()
    {
        SphereCollider contact = gameObject.AddComponent<SphereCollider>();
        contact.center = new Vector3(0f, 0.31f, 0f);
        contact.radius = 0.32f;
        contact.isTrigger = true;
        _contactCollider = contact;
    }

    private static Material CreateLitMaterial(
        string name,
        Color baseColor,
        Color emission,
        float metallic,
        float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.SetColor("_EmissionColor", emission);
        material.EnableKeyword("_EMISSION");
        return material;
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        return material;
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }

    private void OnDestroy()
    {
        if (_bodyMaterial != null) Destroy(_bodyMaterial);
        if (_glowMaterial != null) Destroy(_glowMaterial);
        if (_ringMaterial != null) Destroy(_ringMaterial);
    }
}
