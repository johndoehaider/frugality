using UnityEngine;

public class Spin : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float directionChangeSpeed = 2f;
    [SerializeField] private float minDuration = 3f;
    [SerializeField] private float maxDuration = 5f;

    private Vector3 currentDirection;
    private Vector3 targetDirection;

    private float changeTimer;
    private float changeTime;

    private void Start()
    {
        targetDirection = GetRandomDirection();
        currentDirection = targetDirection;

        SetRandomChangeTime();
    }

    private void Update()
    {
        changeTimer += Time.deltaTime;

        if (changeTimer >= changeTime)
        {
            targetDirection = GetRandomDirection();

            changeTimer = 0f;
            SetRandomChangeTime();
        }

        currentDirection = Vector3.Lerp(
            currentDirection,
            targetDirection,
            directionChangeSpeed * Time.deltaTime
        );

        transform.Rotate(
            currentDirection * spinSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private Vector3 GetRandomDirection()
    {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    private void SetRandomChangeTime()
    {
        changeTime = Random.Range(minDuration, maxDuration);
    }
}