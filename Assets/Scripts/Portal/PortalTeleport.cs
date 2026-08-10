using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PortalTeleport : MonoBehaviour
{
    [Header("要加载的目标场景(留空 = 不加载)")]
    public string targetScene = "";

    [Header("传送完成后要卸载的场景(返回传送门用)")]
    public string unloadScene = "";

    [Header("出口物体的名字(要在目标场景里)")]
    public string exitObjectName = "PortalExit_Map";

    [Header("传送冷却时间(秒)")]
    public float cooldown = 1f;

    private float _lastTeleportTime = -10f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (Time.time - _lastTeleportTime < cooldown) return;
        _lastTeleportTime = Time.time;
        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        // 1. 加载目标场景
        if (!string.IsNullOrEmpty(targetScene))
        {
            Scene target = SceneManager.GetSceneByName(targetScene);
            if (!target.isLoaded)
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
                while (!op.isDone) yield return null;
            }
        }

        // 2. 找到玩家和出口
        PlayerController player = FindObjectOfType<PlayerController>();
        GameObject exit = GameObject.Find(exitObjectName);
        if (player == null || exit == null) yield break;

        // 3. 关键修复:传送当前角色模型,而不是玩家根物体
        //    因为移动/碰撞都在模型身上(它有 CharacterController)
        Transform model = player.currentPlayerModel != null
            ? player.currentPlayerModel.transform
            : player.transform;

        CharacterController cc = model.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;      // 先关控制器才能改位置
        model.position = exit.transform.position;
        model.rotation = exit.transform.rotation;
        if (cc != null) cc.enabled = true;

        // 4. 返回传送门:站稳后卸载旧场景
        if (!string.IsNullOrEmpty(unloadScene))
        {
            yield return null;
            Scene oldScene = SceneManager.GetSceneByName(unloadScene);
            if (oldScene.isLoaded) SceneManager.UnloadSceneAsync(oldScene);
        }
    }
}