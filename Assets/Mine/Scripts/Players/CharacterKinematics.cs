using System.Collections.Generic;
using UnityEngine;

public class CharacterKinematics : MonoBehaviour
{
    [Header("Left Arm")]
    [SerializeField] private Transform armLeftTarget;
    [Range(0f, 1f)] [SerializeField] private float armLeftWeightPosition = 1f;
    [Range(0f, 1f)] [SerializeField] private float armLeftWeightRotation = 1f;
    [SerializeField] private Transform[] armLeftHierarchy;

    [Header("Right Arm")]
    [SerializeField] private Transform armRightTarget;
    [Range(0f, 1f)] [SerializeField] private float armRightWeightPosition = 1f;
    [Range(0f, 1f)] [SerializeField] private float armRightWeightRotation = 1f;
    [SerializeField] private Transform[] armRightHierarchy;

    [Header("IK Hint")]
    [SerializeField] private Transform hint;
    [Range(0f, 1f)] [SerializeField] private float weightHint;

    private bool maintainTargetPositionOffset;
    private bool maintainTargetRotationOffset;

    private const float SqrEpsilon = 1e-8f;

    // Runs the IK solver for both arms and lets each arm use its own strength.
    public void Compute(float weightLeft = 1f, float weightRight = 1f)
    {
        ComputeArm(
            armLeftHierarchy,
            armLeftTarget,
            armLeftWeightPosition * weightLeft,
            armLeftWeightRotation * weightLeft
        );

        ComputeArm(
            armRightHierarchy,
            armRightTarget,
            armRightWeightPosition * weightRight,
            armRightWeightRotation * weightRight
        );
    }

    // IK (Inverse Kinematics) works backward from a desired hand position to rotate the upper arm and forearm into place.
    // This method solves one three-bone arm chain: upper arm, forearm, hand.
    private void ComputeArm(
        IReadOnlyList<Transform> hierarchy,
        Transform target,
        float positionWeight = 1f,
        float rotationWeight = 1f)
    {
        Vector3 targetPositionOffset = Vector3.zero;
        Quaternion targetRotationOffset = Quaternion.identity;

        if (maintainTargetPositionOffset)
            targetPositionOffset = hierarchy[2].position - target.position;

        if (maintainTargetRotationOffset)
            targetRotationOffset = Quaternion.Inverse(target.rotation) * hierarchy[2].rotation;

        Vector3 upperArmPosition = hierarchy[0].position;
        Vector3 forearmPosition = hierarchy[1].position;
        Vector3 handPosition = hierarchy[2].position;

        Vector3 targetPosition = Vector3.Lerp(
            handPosition,
            target.position + targetPositionOffset,
            positionWeight
        );

        Quaternion targetRotation = Quaternion.Lerp(
            hierarchy[2].rotation,
            target.rotation * targetRotationOffset,
            rotationWeight
        );

        bool hasHint = hint != null && weightHint > 0f;

        Vector3 upperToForearm = forearmPosition - upperArmPosition;
        Vector3 forearmToHand = handPosition - forearmPosition;
        Vector3 upperToHand = handPosition - upperArmPosition;
        Vector3 upperToTarget = targetPosition - upperArmPosition;

        float upperLength = upperToForearm.magnitude;
        float forearmLength = forearmToHand.magnitude;
        float currentReach = upperToHand.magnitude;
        float targetReach = upperToTarget.magnitude;

        float oldElbowAngle = TriangleAngle(currentReach, upperLength, forearmLength);
        float newElbowAngle = TriangleAngle(targetReach, upperLength, forearmLength);

        // Cross products give us the axis the elbow should bend around.
        Vector3 bendAxis = Vector3.Cross(upperToForearm, forearmToHand);

        if (bendAxis.sqrMagnitude < SqrEpsilon)
        {
            bendAxis = hasHint
                ? Vector3.Cross(hint.position - upperArmPosition, forearmToHand)
                : Vector3.zero;

            if (bendAxis.sqrMagnitude < SqrEpsilon)
                bendAxis = Vector3.Cross(upperToTarget, forearmToHand);

            if (bendAxis.sqrMagnitude < SqrEpsilon)
                bendAxis = Vector3.up;
        }

        bendAxis.Normalize();

        float halfAngleDifference = 0.5f * (oldElbowAngle - newElbowAngle);
        float sin = Mathf.Sin(halfAngleDifference);
        float cos = Mathf.Cos(halfAngleDifference);

        Quaternion elbowRotation = new Quaternion(
            bendAxis.x * sin,
            bendAxis.y * sin,
            bendAxis.z * sin,
            cos
        );

        hierarchy[1].rotation = elbowRotation * hierarchy[1].rotation;

        handPosition = hierarchy[2].position;
        upperToHand = handPosition - upperArmPosition;

        hierarchy[0].rotation =
            Quaternion.FromToRotation(upperToHand, upperToTarget) * hierarchy[0].rotation;

        if (hasHint)
            ApplyHint(hierarchy, upperArmPosition, upperLength, forearmLength);

        hierarchy[2].rotation = targetRotation;
    }

    // Uses the optional IK hint to control which direction the elbow points.
    private void ApplyHint(
        IReadOnlyList<Transform> hierarchy,
        Vector3 upperArmPosition,
        float upperLength,
        float forearmLength)
    {
        Vector3 forearmPosition = hierarchy[1].position;
        Vector3 handPosition = hierarchy[2].position;

        Vector3 upperToForearm = forearmPosition - upperArmPosition;
        Vector3 upperToHand = handPosition - upperArmPosition;

        float reachSqrMagnitude = upperToHand.sqrMagnitude;

        if (reachSqrMagnitude <= 0f)
            return;

        Vector3 armDirection = upperToHand / Mathf.Sqrt(reachSqrMagnitude);
        Vector3 hintDirection = hint.position - upperArmPosition;

        Vector3 forearmProjection =
            upperToForearm - armDirection * Vector3.Dot(upperToForearm, armDirection);

        Vector3 hintProjection =
            hintDirection - armDirection * Vector3.Dot(hintDirection, armDirection);

        float maxReach = upperLength + forearmLength;

        if (forearmProjection.sqrMagnitude <= maxReach * maxReach * 0.001f ||
            hintProjection.sqrMagnitude <= 0f)
            return;

        Quaternion hintRotation =
            Quaternion.FromToRotation(forearmProjection, hintProjection);

        hintRotation.x *= weightHint;
        hintRotation.y *= weightHint;
        hintRotation.z *= weightHint;
        hintRotation = Quaternion.Normalize(hintRotation);

        hierarchy[0].rotation = hintRotation * hierarchy[0].rotation;
    }

    // Uses the law of cosines to find an angle from the three side lengths of a triangle.
    private static float TriangleAngle(float oppositeSide, float sideA, float sideB)
    {
        float cosine = Mathf.Clamp(
            (sideA * sideA + sideB * sideB - oppositeSide * oppositeSide) /
            (sideA * sideB * 2f),
            -1f,
            1f
        );

        return Mathf.Acos(cosine);
    }
}
