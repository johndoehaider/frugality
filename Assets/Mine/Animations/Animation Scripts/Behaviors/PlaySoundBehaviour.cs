using UnityEngine;

public class PlaySoundBehaviour : StateMachineBehaviour
{
    [Header("Setup")]
    [SerializeField] private AudioClip clip;

    private AudioSource weaponAudioSource;

    // OnStateEnter is called automatically when the Animator enters the state this behaviour is attached to.
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (clip == null)
            return;

        // Uses the Player's second AudioSource, which is reserved for weapon/action sounds.
        if (weaponAudioSource == null)
        {
            AudioSource[] audioSources = animator.GetComponentsInParent<AudioSource>();

            if (audioSources.Length > 1)
                weaponAudioSource = audioSources[1];
        }

        if (weaponAudioSource == null)
            return;

        weaponAudioSource.PlayOneShot(clip);
    }
}
