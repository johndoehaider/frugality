using UnityEngine;

public class EnemyEntry : MonoBehaviour
{
    #region Entry State

    private enum EntryState
    {
        ApproachingEntrance,
        WaitingForSlot,
        ApproachingSlot,
        Aligning,
        AttackingBarrier,
        Traversing
    }

    #endregion

    #region Dependencies

    private Enemy owner;
    private EnemyNavigation navigation;
    private EnemyBehaviorData behaviorData;
    private Animator animator;

    #endregion

    #region Runtime State

    private Barrier barrier;
    private EntryState state;
    private int slotIndex = -1;
    private float nextSlotAttemptTime;
    private Vector3 entranceTarget;
    private Vector3 traversalTarget;

    #endregion

    #region Initialization

    public void Initialize(
        Enemy enemy,
        EnemyNavigation enemyNavigation,
        EnemyBehaviorData data,
        Animator enemyAnimator)
    {
        owner = enemy;
        navigation = enemyNavigation;
        behaviorData = data;
        animator = enemyAnimator;
    }

    public bool Begin(Barrier assignedBarrier)
    {
        Cancel();

        if (owner == null ||
            navigation == null ||
            behaviorData == null ||
            animator == null ||
            assignedBarrier == null)
        {
            return false;
        }

        if (!assignedBarrier.TryGetEntryApproachCenter(out entranceTarget))
        {
            return false;
        }

        barrier = assignedBarrier;
        state = EntryState.ApproachingEntrance;

        navigation.UseEntryAvoidance();
        navigation.SetStoppingDistance(behaviorData.GetEntryStagingDistance());
        navigation.ResumeMovement();

        return true;
    }

    public void Cancel()
    {
        ReleaseSlot();
        navigation.CancelManualTraversal();
        navigation.UseDefaultAvoidance();
        navigation.UseDefaultStoppingDistance();
        barrier = null;
    }

    #endregion

    #region Entry Tick

    public void Tick()
    {
        if (barrier == null)
        {
            return;
        }

        switch (state)
        {
            case EntryState.ApproachingEntrance:
                UpdateApproachingEntrance();
                break;

            case EntryState.WaitingForSlot:
                UpdateWaitingForSlot();
                break;

            case EntryState.ApproachingSlot:
                UpdateApproachingSlot();
                break;

            case EntryState.Aligning:
                UpdateAlignment();
                break;

            case EntryState.AttackingBarrier:
                break;

            case EntryState.Traversing:
                UpdateTraversal();
                break;
        }
    }

    #endregion

    #region Approach And Slot Reservation

    private void UpdateApproachingEntrance()
    {
        if (navigation.IsWithinHorizontalDistance(
            entranceTarget,
            behaviorData.GetEntryStagingDistance()))
        {
            navigation.ClearPath();
            state = EntryState.WaitingForSlot;
            nextSlotAttemptTime = Time.time;
            return;
        }

        navigation.MoveTowards(
            entranceTarget,
            behaviorData.GetEntryTurnSpeed()
        );
    }

    private void UpdateWaitingForSlot()
    {
        if (Time.time < nextSlotAttemptTime)
        {
            return;
        }

        if (barrier.TryReserveClosestEntrySlot(owner, out slotIndex))
        {
            navigation.SetStoppingDistance(
                behaviorData.GetEntryApproachDistance()
            );

            navigation.ResumeMovement();
            state = EntryState.ApproachingSlot;
            return;
        }

        ScheduleNextSlotAttempt();
    }

    private void UpdateApproachingSlot()
    {
        if (!barrier.TryGetEntrySlot(
            slotIndex,
            out Vector3 approachPosition,
            out _))
        {
            ReturnToWaiting();
            return;
        }

        if (navigation.IsWithinHorizontalDistance(
            approachPosition,
            behaviorData.GetEntryApproachDistance()))
        {
            navigation.PauseMovement();
            state = EntryState.Aligning;
            return;
        }

        navigation.MoveTowards(
            approachPosition,
            behaviorData.GetEntryTurnSpeed()
        );
    }

    private void UpdateAlignment()
    {
        if (!barrier.TryGetEntrySlot(
            slotIndex,
            out _,
            out Vector3 exitPosition))
        {
            ReturnToWaiting();
            return;
        }

        if (!navigation.AlignTowardsPosition(
            exitPosition,
            behaviorData.GetEntryAlignmentAngle(),
            behaviorData.GetEntryTurnSpeed()))
        {
            return;
        }

        if (barrier.IsBroken())
        {
            BeginTraversal(exitPosition);
            return;
        }

        BeginBarrierAttack();
    }

    private void ReturnToWaiting()
    {
        ReleaseSlot();
        navigation.ClearPath();
        navigation.SetStoppingDistance(
            behaviorData.GetEntryStagingDistance()
        );

        state = EntryState.WaitingForSlot;
        ScheduleNextSlotAttempt();
    }

    private void ScheduleNextSlotAttempt()
    {
        nextSlotAttemptTime = Time.time + Random.Range(
            behaviorData.GetMinEntrySlotRetryTime(),
            behaviorData.GetMaxEntrySlotRetryTime()
        );
    }

    #endregion

    #region Barrier Attacking

    private void BeginBarrierAttack()
    {
        state = EntryState.AttackingBarrier;
        animator.SetTrigger("Attack");
    }

    public void AttackHit()
    {
        if (state != EntryState.AttackingBarrier ||
            barrier == null ||
            barrier.IsBroken())
        {
            return;
        }

        barrier.ZombieDamage();
    }

    public void AnimationEndedAttack()
    {
        if (state != EntryState.AttackingBarrier ||
            barrier == null)
        {
            return;
        }

        if (!barrier.TryGetEntrySlot(
            slotIndex,
            out _,
            out Vector3 exitPosition))
        {
            ReturnToWaiting();
            return;
        }

        if (barrier.IsBroken())
        {
            BeginTraversal(exitPosition);
            return;
        }

        animator.SetTrigger("Attack");
    }

    #endregion

    #region Traversal

    private void BeginTraversal(Vector3 exitPosition)
    {
        traversalTarget = exitPosition;
        state = EntryState.Traversing;
        navigation.BeginManualTraversal();
    }

    private void UpdateTraversal()
    {
        if (!navigation.MoveManuallyTowards(
            traversalTarget,
            owner.GetEnemySpeed(),
            behaviorData.GetEntryTurnSpeed()))
        {
            return;
        }

        if (!navigation.CompleteManualTraversal())
        {
            Debug.LogError(
                $"Enemy '{owner.name}' reached barrier exit '{barrier.name}' but could not reconnect to the NavMesh.",
                owner
            );

            return;
        }

        ReleaseSlot();
        barrier = null;

        owner.CompleteEntry();
    }

    #endregion

    #region Slot Cleanup

    private void ReleaseSlot()
    {
        if (barrier != null && slotIndex >= 0)
        {
            barrier.ReleaseEntrySlot(slotIndex, owner);
        }

        slotIndex = -1;
    }

    #endregion
}
