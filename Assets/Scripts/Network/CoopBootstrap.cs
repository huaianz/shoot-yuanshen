using UnityEngine;

public class CoopBootstrap : MonoBehaviour
{
    [Tooltip("勾上则跨场景常驻(传送时连接不断)")]
    public bool persistAcrossScenes = true;

    private void Awake()
    {
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
    }
}