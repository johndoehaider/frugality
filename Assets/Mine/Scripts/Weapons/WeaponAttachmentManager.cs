using UnityEngine;

public class WeaponAttachmentManager : MonoBehaviour
{
    [Header("Scope")]
    [SerializeField] private bool scopeDefaultShow = true;
    [SerializeField] private Scope scopeDefault;

    [Header("Muzzle")]
    [SerializeField] private int muzzleIndex;
    [SerializeField] private Muzzle[] muzzleArray;

    [Header("Magazine")]
    [SerializeField] private int magazineIndex;
    [SerializeField] private Magazine[] magazineArray;

    private Scope equippedScope;
    private Muzzle equippedMuzzle;
    private Magazine equippedMagazine;

    // Selects the starting scope, muzzle, and magazine when the weapon is created.
    private void Awake()
    {
        equippedScope = scopeDefault;

        if (equippedScope != null)
        {
            equippedScope.gameObject.SetActive(scopeDefaultShow);
        }

        equippedMuzzle = SelectMuzzle(muzzleIndex);
        equippedMagazine = SelectMagazine(magazineIndex);
    }

    // Enables only the selected muzzle and returns it.
    private Muzzle SelectMuzzle(int index)
    {
        Muzzle selectedMuzzle = null;

        if (muzzleArray != null && muzzleArray.Length > 0)
        {
            if (index < 0 || index >= muzzleArray.Length)
            {
                index = 0;
            }

            for (int i = 0; i < muzzleArray.Length; i++)
            {
                if (muzzleArray[i] != null)
                {
                    if (i == index)
                    {
                        muzzleArray[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        muzzleArray[i].gameObject.SetActive(false);
                    }
                }
            }

            selectedMuzzle = muzzleArray[index];
        }

        return selectedMuzzle;
    }

    // Enables only the selected magazine and returns it.
    private Magazine SelectMagazine(int index)
    {
        Magazine selectedMagazine = null;

        if (magazineArray != null && magazineArray.Length > 0)
        {
            if (index < 0 || index >= magazineArray.Length)
            {
                index = 0;
            }

            for (int i = 0; i < magazineArray.Length; i++)
            {
                if (magazineArray[i] != null)
                {
                    if (i == index)
                    {
                        magazineArray[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        magazineArray[i].gameObject.SetActive(false);
                    }
                }
            }

            selectedMagazine = magazineArray[index];
        }

        return selectedMagazine;
    }

    public Scope GetEquippedScope()
    {
        return equippedScope;
    }

    public Scope GetEquippedScopeDefault()
    {
        return scopeDefault;
    }

    public Magazine GetEquippedMagazine()
    {
        return equippedMagazine;
    }

    public Muzzle GetEquippedMuzzle()
    {
        return equippedMuzzle;
    }
}
