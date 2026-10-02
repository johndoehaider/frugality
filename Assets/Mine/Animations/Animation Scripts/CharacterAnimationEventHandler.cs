using UnityEngine;

public class CharacterAnimationEventHandler : MonoBehaviour
{
    private Character playerCharacter;

    // Finds the Character component on this object or one of its parents.
    private void Awake()
    {
        playerCharacter = GetComponentInParent<Character>();
    }

    // Animation Events call methods on a component at specific moments inside an animation clip.
    // Called by a firing animation when a casing should be ejected.
    private void OnEjectCasing()
    {
        if (playerCharacter != null)
            playerCharacter.EjectCasing();
    }

    // Called by a reload animation when ammunition should actually be added to the weapon.
    private void OnAmmunitionFill()
    {
        if (playerCharacter != null)
            playerCharacter.FillAmmunition();
    }

    // Called by a reload animation to show or hide the weapon's magazine.
    private void OnSetActiveMagazine(int active)
    {
        if (playerCharacter != null)
            playerCharacter.SetActiveMagazine(active);
    }

    // Called when the reload animation finishes so Character can allow other actions again.
    private void OnAnimationEndedReload()
    {
        if (playerCharacter != null)
            playerCharacter.AnimationEndedReload();
    }

    // Called when the inspect animation finishes so Character can allow other actions again.
    private void OnAnimationEndedInspect()
    {
        if (playerCharacter != null)
            playerCharacter.AnimationEndedInspect();
    }

    // Called when the holster animation finishes so Character can finish a weapon switch.
    private void OnAnimationEndedHolster()
    {
        if (playerCharacter != null)
            playerCharacter.AnimationEndedHolster();
    }

    private void OnSlideBack(int back)
	{
	}

    private void OnKnifeHit()
    {
        if (playerCharacter != null)
            playerCharacter.KnifeHit();
    }

    private void OnAnimationEndedKnife()
    {
        if (playerCharacter != null)
            playerCharacter.AnimationEndedKnife();
    }

    private void OnAnimationEndedLanding()
    {
        if (playerCharacter != null)
            playerCharacter.AnimationEndedLanding();
    }
    
}
