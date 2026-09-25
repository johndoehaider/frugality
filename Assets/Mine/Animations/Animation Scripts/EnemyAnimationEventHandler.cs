using UnityEngine;

public class EnemyAnimationEventHandler : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    private void OnAttackHit()
    {
        if (enemy != null)
        {
            enemy.AttackHit();
        }
    }

    private void OnAnimationEndedAttack()
    {
        if (enemy != null)
        {
            enemy.AnimationEndedAttack();
        }
    }
}