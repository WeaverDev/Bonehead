using System.Collections;
using UnityEngine;

public class LegStepper_Full : MonoBehaviour
{
    // The position and rotation we want to stay in range of
    [SerializeField] Transform homeTransform;
    // How far above the ground should we be when at rest
    //   Necessary when foot joints aren't exactly at the base of the foot geometry
    [SerializeField] float heightOffset;
    // If we exceed this distance from home, next move try will succeed
    [SerializeField] float wantStepAtDistance = 2f;
    // How far should our new position be past home
    [SerializeField, Range(0, 1)] float stepOvershootFraction = 0.8f;
    // If we exceed this angle from home, next move try will succeed
    [SerializeField] float wantStepAtAngle = 135f;
    // How long a step takes to complete
    [SerializeField] float moveDuration = 1.5f;
    // What layers are considered ground
    [SerializeField] LayerMask groundRaycastMask = ~0;

    public bool Moving { get; private set; }

    Coroutine moveCoroutine;

    void Awake()
    {
        // Exit hierarchy to avoid influence from root
        transform.SetParent(null);

        // Move to a valid position right away
        TryMove();
    }

    // Move leg if move conditions are met
    public void TryMove()
    {
        if (Moving) return;

        float distFromHome = Vector3.Distance(transform.position, homeTransform.position);
        float angleFromHome = Quaternion.Angle(transform.rotation, homeTransform.rotation);

        // If we are too far off in position or rotation
        if (distFromHome > wantStepAtDistance ||
             angleFromHome > wantStepAtAngle)
        {
            // If we can't find a good target position, don't step
            if (GetGroundedEndPosition(out Vector3 endPos, out Vector3 endNormal))
            {
                // Get rotation facing in the home forward direction but aligned with the normal plane
                Quaternion endRot = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(homeTransform.forward, endNormal),
                    endNormal
                );

                // Start a MoveToPointCoroutine and store it
                StartCoroutine(
                    MoveToPointCoroutine(
                        endPos,
                        endRot,
                        moveDuration
                    )
                );
            }
        }
    }

    // Find a grounded point using home position and overshoot fraction
    // Returns true if a point was found
    bool GetGroundedEndPosition(out Vector3 position, out Vector3 normal)
    {
        Vector3 towardHome = (homeTransform.position - transform.position).normalized;

        // Limit overshoot to a fraction of the step distance.
        // This prevents infinite step cycles when a foot end point ends up outside its home position radius bounds.
        float overshootDistance = wantStepAtDistance * stepOvershootFraction;
        Vector3 overshootVector = towardHome * overshootDistance;

        Vector3 raycastOrigin = homeTransform.position + overshootVector + homeTransform.up * 2f;

        if (Physics.Raycast(
            raycastOrigin,
            -homeTransform.up,
            out RaycastHit hit,
            Mathf.Infinity,
            groundRaycastMask
        ))
        {
            position = hit.point;
            normal = hit.normal;
            return true;
        }
        position = Vector3.zero;
        normal = Vector3.zero;
        return false;
    }

    IEnumerator MoveToPointCoroutine(Vector3 endPoint, Quaternion endRot, float moveTime)
    {
        // Indicate we're moving
        Moving = true;

        // Store the initial conditions for interpolation
        Vector3 startPoint = transform.position;
        Quaternion startRot = transform.rotation;

        // Apply the height offset
        endPoint += homeTransform.up * heightOffset;

        // We want to pass through the center point
        Vector3 centerPoint = (startPoint + endPoint) / 2;
        // But also lift off, so we move it up arbitrarily by half the step distance
        centerPoint += homeTransform.up * Vector3.Distance(startPoint, endPoint) / 2f;

        // Time since step started
        float timeElapsed = 0;

        // Here we use a do-while loop so normalized time goes past 1.0 on the last iteration,
        // placing us at the end position before exiting.
        do
        {
            timeElapsed += Time.deltaTime;

            // Get the normalized time
            float normalizedTime = timeElapsed / moveTime;

            // Apply easing
            normalizedTime = Easing.EaseInOutCubic(normalizedTime);

            // Note: Unity's Lerp and Slerp functions are clamped at 0.0 and 1.0, 
            // so even if our normalizedTime goes past 1.0, we won't overshoot the end

            // Quadratic bezier curve
            // See https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Constructing_B.C3.A9zier_curves
            transform.position =
                Vector3.Lerp(
                    Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                    Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                    normalizedTime
                );

            transform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);

            // Wait for one frame
            yield return null;
        }
        while (timeElapsed < moveTime);

        Moving = false;
    }

    void OnDrawGizmosSelected()
    {
        if (Moving)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, homeTransform.position);
        Gizmos.DrawWireCube(homeTransform.position, Vector3.one * 0.1f);
    }
}