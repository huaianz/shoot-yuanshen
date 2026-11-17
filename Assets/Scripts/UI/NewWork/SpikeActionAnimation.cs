using UnityEngine;

/// <summary>
/// 用 CC0 Humanoid 下包动作临时覆盖 Idle。
/// 动作只负责表现，是否成功下包仍由服务器决定。
/// </summary>
[DisallowMultipleComponent]
public sealed class SpikeActionAnimation : MonoBehaviour
{
    private const string PlantClipPath = "Animation/Player/SpikePlant";

    private PlayerController _controller;
    private PlayerWeapon _weapon;
    private Animator _animator;
    private RuntimeAnimatorController _originalController;
    private AnimatorOverrideController _actionController;
    private AnimationClip _plantClip;
    private bool _playing;

    public bool Begin(PlayerController controller)
    {
        if (_playing || controller == null || controller.currentPlayerModel == null)
            return false;

        PlayerModel model = controller.currentPlayerModel;
        Animator animator = model.animtor;
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        EnsurePlantClip();
        if (_plantClip == null)
        {
            Debug.LogWarning("[SpikeActionAnimation] 未找到 Resources/Animation/Player/SpikePlant");
            return false;
        }

        _controller = controller;
        _weapon = model.weapon;
        _animator = animator;
        _originalController = animator.runtimeAnimatorController;

        RuntimeAnimatorController baseController = _originalController;
        if (baseController is AnimatorOverrideController overrideController)
            baseController = overrideController.runtimeAnimatorController;

        _actionController = new AnimatorOverrideController(baseController);
        bool replaced = false;
        AnimationClipPair[] pairs = _actionController.clips;
        for (int i = 0; i < pairs.Length; i++)
        {
            AnimationClip source = pairs[i].originalClip;
            if (source == null)
                continue;

            if (source.name.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            pairs[i].overrideClip = _plantClip;
            replaced = true;
        }

        // 某些角色的 Idle 位于混合树中，clip 名称可能不是 "Idle"。
        // 这种情况下覆盖第一个可替换的状态，再显式播放 Idle 状态。
        if (!replaced && pairs.Length > 0 && pairs[0].originalClip != null)
        {
            pairs[0].overrideClip = _plantClip;
        }

        _actionController.clips = pairs;
        _controller.BeginInteraction();
        _weapon?.SetWeaponVisible(false);

        animator.runtimeAnimatorController = _actionController;
        animator.Play("Idle", 0, 0f);
        animator.Update(0f);
        _playing = true;
        return true;
    }

    public void End()
    {
        if (!_playing)
            return;

        _playing = false;
        if (_animator != null && _originalController != null)
        {
            _animator.runtimeAnimatorController = _originalController;
            _animator.CrossFadeInFixedTime("Idle", 0.15f);
        }

        _weapon?.SetWeaponVisible(true);
        _controller?.EndInteraction();

        _controller = null;
        _weapon = null;
        _animator = null;
        _originalController = null;
        _actionController = null;
    }

    private void EnsurePlantClip()
    {
        if (_plantClip != null)
            return;

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(PlantClipPath);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null
                && clips[i].name.IndexOf("SpikePlant", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _plantClip = clips[i];
                return;
            }
        }

        if (clips.Length > 0)
            _plantClip = clips[0];
    }

    private void OnDisable()
    {
        End();
    }
}
