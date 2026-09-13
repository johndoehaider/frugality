using System.Collections;
using UnityEngine;

public class PlaySoundCharacterBehaviour : StateMachineBehaviour
{
    private enum SoundType
    {
        Holster,
        Unholster,
        Reload,
        ReloadEmpty,
        Fire,
        FireEmpty
    }

    [Header("Setup")]
    [SerializeField] private float delay;
    [SerializeField] private SoundType soundType;

    private Character playerCharacter;
    private Inventory playerInventory;
    private AudioSource weaponAudioSource;  
    private AudioSource reloadAudioSource;

    // OnStateEnter is called automatically when the Animator enters the state this behaviour is attached to.
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // GetComponentInParent finds our Character and AudioSource from the Animator's Player hierarchy.
        if (playerCharacter == null)
            playerCharacter = animator.GetComponentInParent<Character>();

        if (playerCharacter == null)
            return;

        if (playerInventory == null)
            playerInventory = playerCharacter.GetInventory();
            
        if (weaponAudioSource == null)
        {
            weaponAudioSource = playerCharacter.GetWeaponAudioSource();
        }

        if (reloadAudioSource == null)
        {
            reloadAudioSource = playerCharacter.GetReloadAudioSource();
        }

        if (playerInventory == null || weaponAudioSource == null)
            return;

        Weapon weapon = playerInventory.GetEquipped();

        if (weapon == null)
            return;

        AudioClip clip = soundType switch
        {
            SoundType.Holster => weapon.GetAudioClipHolster(),
            SoundType.Unholster => weapon.GetAudioClipUnholster(),
            SoundType.Reload => weapon.GetAudioClipReload(),
            SoundType.ReloadEmpty => weapon.GetAudioClipReloadEmpty(),
            SoundType.Fire => weapon.GetAudioClipFire(),
            SoundType.FireEmpty => weapon.GetAudioClipFireEmpty(),
            _ => null
        };

        if (clip == null)
        {
            return;
        }

        if (soundType == SoundType.Reload || soundType == SoundType.ReloadEmpty)
        {
            reloadAudioSource.pitch = playerCharacter.GetReloadSpeed();

            if (delay > 0f)
            {
                playerCharacter.StartCoroutine(PlayDelayedReload(clip));
            }
            else
            {
                reloadAudioSource.PlayOneShot(clip);
            }
        }
        else
        {
            if (delay > 0f)
            {
                playerCharacter.StartCoroutine(PlayDelayed(clip));
            }
            else
            {
                weaponAudioSource.PlayOneShot(clip);
            }
        }
    }

    // Waits before playing a sound when this Animator state has a non-zero delay.
    private IEnumerator PlayDelayed(AudioClip clip)
    {
        if (soundType == SoundType.Reload || soundType == SoundType.ReloadEmpty)
        {
            yield return new WaitForSeconds(delay / playerCharacter.GetReloadSpeed());
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        if (weaponAudioSource != null)
        {
            weaponAudioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator PlayDelayedReload(AudioClip clip)
    {
        yield return new WaitForSeconds(delay / playerCharacter.GetReloadSpeed());

        if (reloadAudioSource != null)
        {
            reloadAudioSource.PlayOneShot(clip);
        }
    }
}
