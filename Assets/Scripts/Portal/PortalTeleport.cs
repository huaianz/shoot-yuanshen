using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
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
        if (other.GetComponentInParent<PlayerController>() == null && other.GetComponentInParent<PlayerModel>() == null) return;
        if (Time.time - _lastTeleportTime < cooldown) return;
        _lastTeleportTime = Time.time;
        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        //加载目标场景
        if (!string.IsNullOrEmpty(targetScene))
        {
            Scene target = SceneManager.GetSceneByName(targetScene);
            if (!target.isLoaded)
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
                while (!op.isDone) yield return null;
            }
        }

        //找到玩家根物体和出口
        PlayerController player = FindObjectOfType<PlayerController>();
        GameObject exit = GameObject.Find(exitObjectName);
        if (player == null || exit == null) yield break;

        Vector3 targetPos = exit.transform.position;
        Quaternion targetRot = exit.transform.rotation;

        player.transform.position = targetPos;
        player.transform.rotation = targetRot;

        //再传所有角色模型(包含当前模型), 保证旧场景不留任何"玩家"残影
        if (player.currentPlayerModel != null)
        {
            TeleportModel(player.currentPlayerModel, targetPos, targetRot, true);
        }
        if (GameManager.INSTANCE != null && GameManager.INSTANCE.playerModels != null)
        {
            foreach (PlayerModel m in GameManager.INSTANCE.playerModels)
            {
                if (m != null && m != player.currentPlayerModel)
                {
                    TeleportModel(m, targetPos, targetRot, false);
                }
            }
        }

        //站稳后卸载旧场景
        if (!string.IsNullOrEmpty(unloadScene))
        {
            yield return null;
            Scene oldScene = SceneManager.GetSceneByName(unloadScene);
            if (oldScene.isLoaded) SceneManager.UnloadSceneAsync(oldScene);
        }
    }

    /// <summary>
    /// 传送单个角色模型: 当前控制模型关寻路, 备用模型开寻路继续跟随
    /// </summary>
    private void TeleportModel(PlayerModel model, Vector3 pos, Quaternion rot, bool isControlled)
    {
        CharacterController cc = model.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        model.transform.position = pos;
        model.transform.rotation = rot;

        NavMeshAgent agent = model.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (isControlled)
            {
                agent.enabled = false;
            }
            else
            {
                if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                {
                    model.transform.position = navHit.position;
                }
                agent.enabled = true;
                agent.Warp(model.transform.position);
                // 只有确实在 NavMesh 上才恢复移动, 避免 Resume 报错
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }
        }

        if (cc != null) cc.enabled = true;
    }
}