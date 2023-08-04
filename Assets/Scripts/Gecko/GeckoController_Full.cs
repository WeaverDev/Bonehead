using System.Collections;
using UnityEngine;

public class GeckoController_Full : MonoBehaviour
{
    [SerializeField] Transform target;

    [SerializeField] bool rootMotionEnabled;
    [SerializeField] bool idleBobbingEnabled;
    [SerializeField] bool headTrackingEnabled;
    [SerializeField] bool eyeTrackingEnabled;
    [SerializeField] bool tailSwayEnabled;
    [SerializeField] bool legSteppingEnabled;
    bool legIKEnabled;

    void Awake()
    {
        StartCoroutine(LegUpdateCoroutine());
        TailInitialize();
        RootMotionInitialize();
    }

    void Update()
    {
        RootMotionUpdate();
    }

    void LateUpdate()
    {
        // Update order is important! 
        // We update things in order of dependency, so we update the body first via IdleBobbingUpdate,
        // since the head is moved by the body, then we update the head, since the eyes are moved by the head,
        // and finally the eyes.

        IdleBobbingUpdate();
        HeadTrackingUpdate();
        EyeTrackingUpdate();
        TailUpdate();
    }

    #region Root Motion

    [Header("Root Motion")]
    [SerializeField] float turnSpeed = 100f;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float turnAcceleration = 5f;
    [SerializeField] float moveAcceleration = 5f;
    [SerializeField] float minDistToTarget = 4.5f;
    [SerializeField] float maxDistToTarget = 6f;

    SmoothDamp.Vector3 currentVelocity;
    SmoothDamp.Float currentAngularVelocity;

    void RootMotionUpdate()
    {
        if (!rootMotionEnabled) return;
        Vector3 towardTarget = target.position - transform.position;
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, transform.up);

        var angToTarget = Vector3.SignedAngle(transform.forward, towardTargetProjected, transform.up);

        float targetAngularVelocity = Mathf.Sign(angToTarget) * Mathf.InverseLerp(20f, 45f, Mathf.Abs(angToTarget)) * turnSpeed;
        currentAngularVelocity.Step(targetAngularVelocity, turnAcceleration);


        Vector3 targetVelocity = Vector3.zero;

        // Don't translate if we're facing away, rotate in place
        if (Mathf.Abs(angToTarget) < 90)
        {
            var distToTarget = towardTargetProjected.magnitude;

            // If we're too far away, move toward target
            if (distToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }
            // If we're too close, move in reverse
            else if (distToTarget < minDistToTarget)
            {
                // Speed also reduced since the stubby front legs can't keep up with full speed
                targetVelocity = moveSpeed * -towardTargetProjected.normalized * 0.66f;
            }

            // Limit velocity progressively as we approach max angular velocity,
            // so that above 20% of max angvel we start slowing down translation
            targetVelocity *= Mathf.InverseLerp(turnSpeed, turnSpeed * 0.2f, Mathf.Abs(currentAngularVelocity));
        }

        currentVelocity.Step(targetVelocity, moveAcceleration);

        // Apply translation and rotation
        transform.position += currentVelocity.currentValue * Time.deltaTime;
        transform.rotation *= Quaternion.AngleAxis(Time.deltaTime * currentAngularVelocity, transform.up);
    }

    #endregion

    #region Head Tracking

    [Header("Head Tracking")]
    [SerializeField, Range(1, 3)] int headTrackingBoneCount = 1;
    [Space]
    [SerializeField] Transform headBone;
    [SerializeField] Transform spine2Bone;
    [SerializeField] Transform spine1Bone;
    [SerializeField] Transform hipBone;

    [SerializeField] float headMaxTurnAngle = 70;
    [SerializeField] float headTrackingSpeed = 8f;
    [SerializeField] float hipVerticalTiltInfluence = 0.5f;

    Vector3 lastLocalHeadRotationEulers;
    SmoothDamp.EulerAngles currentLocalHeadEulerAngles;

    void HeadTrackingUpdate()
    {
        if (!headTrackingEnabled)
        {
            headBone.localRotation = Quaternion.identity;
            spine1Bone.localRotation = Quaternion.identity;
            spine2Bone.localRotation = Quaternion.identity;
            hipBone.localRotation = Quaternion.identity;
            return;
        }


        // Freelook dir
        Vector3 targetLookDir = target.position - headBone.position;

        // Since we have multiple bones in the chain, we clamp the angle here from the root forward vector 
        // rather than from the bone's default orientation
        targetLookDir = Vector3.RotateTowards(transform.forward, targetLookDir, Mathf.Deg2Rad * headMaxTurnAngle, 0);

        // Look dir on the gecko's XZ plane
        Vector3 planarLookDir = Vector3.ProjectOnPlane(targetLookDir, transform.up).normalized;

        // If target is behind the gecko, we approach the planar look dir to prevent wacky up/down head rotations
        var dotProduct = Vector3.Dot(transform.forward, targetLookDir);
        if (dotProduct < 0)
        {
            targetLookDir = Vector3.Lerp(targetLookDir, Vector3.ProjectOnPlane(planarLookDir, transform.up), -dotProduct);
        }

        // Up dir is partially biased toward world up for a more interesting head rotation when upside down
        Quaternion targetWorldRotation = Quaternion.LookRotation(targetLookDir, Vector3.Slerp(transform.up, Vector3.up, 0.5f));

        // Get head world rotation when its local rotations are zero
        Quaternion defaultHeadRotation = headBone.rotation * Quaternion.Inverse(headBone.localRotation);

        // Move the look rotation to local space by "subtracting" the world rotation
        Quaternion targetLocalRotation = Quaternion.Inverse(defaultHeadRotation) * targetWorldRotation;

        // Since we apply this to each bone, the speed is multiplied by the bone count,
        // so we divide here to keep it constant
        float headTrackingSpeed = this.headTrackingSpeed / headTrackingBoneCount;

        currentLocalHeadEulerAngles.Step(targetLocalRotation.eulerAngles, headTrackingSpeed);

        headBone.localEulerAngles = currentLocalHeadEulerAngles;

        // Because the target rotation is derived from the last bone in the chain,
        // the angles will balance themselves out automatically, even if we don't apply
        // it to all three bones!

        if (headTrackingBoneCount > 1)
            spine1Bone.localEulerAngles = currentLocalHeadEulerAngles;
        else
            spine1Bone.localRotation = Quaternion.identity;

        if (headTrackingBoneCount > 2)
            spine2Bone.localEulerAngles = currentLocalHeadEulerAngles;
        else
            spine2Bone.localRotation = Quaternion.identity;

        // Apply a forward tilt to hips to prevent front legs coming off the floor
        hipBone.localEulerAngles = new Vector3(Mathf.DeltaAngle(headBone.localEulerAngles.x * headTrackingBoneCount, 0) * hipVerticalTiltInfluence, 0, 0);
    }

    #endregion

    #region Eye Tracking

    [Header("Eye Tracking")]
    [SerializeField] Transform leftEyeBone;
    [SerializeField] Transform rightEyeBone;

    [SerializeField] float eyeTrackingSpeed = 30f;
    [SerializeField] float leftEyeMaxYRotation = 10f;
    [SerializeField] float leftEyeMinYRotation = -180f;
    [SerializeField] float rightEyeMaxYRotation = 180f;
    [SerializeField] float rightEyeMinYRotation = -10f;

    void EyeTrackingUpdate()
    {
        if (!eyeTrackingEnabled)
        {
            leftEyeBone.localRotation = Quaternion.identity;
            rightEyeBone.localRotation = Quaternion.identity;
            return;
        }

        // We use head position here just because the gecko doesn't look so great when cross eyed.
        // Other models may want to split this and use directions relative to the eye origin itself
        Quaternion targetEyeRotation = Quaternion.LookRotation(
            target.position - headBone.position,
            transform.up
        );

        leftEyeBone.rotation = Quaternion.Slerp(
            leftEyeBone.rotation,
            targetEyeRotation,
            1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        rightEyeBone.rotation = Quaternion.Slerp(
            rightEyeBone.rotation,
            targetEyeRotation,
            1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        float leftEyeCurrentYRotation = leftEyeBone.localEulerAngles.y;
        float rightEyeCurrentYRotation = rightEyeBone.localEulerAngles.y;

        // Ensure the Y rotation is in the range -180 ~ 180
        if (leftEyeCurrentYRotation > 180)
        {
            leftEyeCurrentYRotation -= 360;
        }
        if (rightEyeCurrentYRotation > 180)
        {
            rightEyeCurrentYRotation -= 360;
        }

        // Clamp the Y axis rotation
        float leftEyeClampedYRotation =
            Mathf.Clamp(
                leftEyeCurrentYRotation,
                leftEyeMinYRotation,
                leftEyeMaxYRotation
            );
        float rightEyeClampedYRotation =
            Mathf.Clamp(
                rightEyeCurrentYRotation,
                rightEyeMinYRotation,
                rightEyeMaxYRotation
            );

        leftEyeBone.localEulerAngles = new Vector3(
            leftEyeBone.localEulerAngles.x,
            leftEyeClampedYRotation,
            leftEyeBone.localEulerAngles.z
        );
        rightEyeBone.localEulerAngles = new Vector3(
            rightEyeBone.localEulerAngles.x,
            rightEyeClampedYRotation,
            rightEyeBone.localEulerAngles.z
        );
    }

    #endregion

    #region Tail

    [Header("Tail")]
    [SerializeField] Transform[] tailBones;
    [SerializeField] float tailTurnMultiplier;
    [SerializeField] float tailTurnSpeed;

    Quaternion[] tailHomeLocalRotation;

    SmoothDamp.Float tailRotation;

    void TailInitialize()
    {
        // Store the default rotation of the tail bones
        tailHomeLocalRotation = new Quaternion[tailBones.Length];
        for (int i = 0; i < tailHomeLocalRotation.Length; i++)
        {
            tailHomeLocalRotation[i] = tailBones[i].localRotation;
        }
    }

    void TailUpdate()
    {
        if (tailSwayEnabled)
        {
            // Rotate the tail opposite to the current angular velocity to give us a counteracting tail curl
            tailRotation.Step(-currentAngularVelocity / turnSpeed * tailTurnMultiplier, tailTurnSpeed);

            for (int i = 0; i < tailBones.Length; i++)
            {
                Quaternion rotation = Quaternion.Euler(0, tailRotation, 0);
                tailBones[i].localRotation = rotation * tailHomeLocalRotation[i];
            }
        }
        else
        {
            for (int i = 0; i < tailBones.Length; i++)
            {
                tailBones[i].localRotation = tailHomeLocalRotation[i];
            }
        }
    }

    #endregion

    #region Idle Bobbing

    [Header("Idle Bobbing")]
    [SerializeField] Transform rootBone;

    [SerializeField] Vector3 idleRotationAmplitude;
    [SerializeField] Vector3 idleRotationSpeed;
    [SerializeField] Vector3 idleRotationCycleOffset;
    [SerializeField] Vector3 idleMotionAmplitude;
    [SerializeField] Vector3 idleMotionSpeed;
    [SerializeField] float idleSpeedMultiplier = 1;

    [SerializeField] float bodyIdleWeightChangeVelocity = 1;

    Vector3 rootHomePos;
    SmoothDamp.Float bodyIdleAnimWeight;

    void RootMotionInitialize()
    {
        // Store this so we can reset it when disabled at runtime
        rootHomePos = rootBone.localPosition;
    }

    void IdleBobbingUpdate()
    {
        if (!idleBobbingEnabled)
        {
            rootBone.localPosition = rootHomePos;
            rootBone.localRotation = Quaternion.identity;
            return;
        }

        // How much we want the idle bobbing to influence the skeleton
        float turnSpeedFrac = currentAngularVelocity / turnSpeed;
        float moveSpeedFrac = currentVelocity.currentValue.magnitude / moveSpeed;

        float targetIdleAnimWeight = Mathf.Max(1 - turnSpeedFrac * 4, 1 - moveSpeedFrac * 4);
        targetIdleAnimWeight = Mathf.Clamp01(targetIdleAnimWeight);

        bodyIdleAnimWeight.Step(targetIdleAnimWeight, bodyIdleWeightChangeVelocity);

        // Rotate the root in local space over time
        rootBone.localEulerAngles = new Vector3(
            Mathf.Sin(Time.time * idleRotationSpeed.x * idleSpeedMultiplier + idleRotationCycleOffset.x * Mathf.PI * 2) * idleRotationAmplitude.x,
            Mathf.Sin(Time.time * idleRotationSpeed.y * idleSpeedMultiplier + idleRotationCycleOffset.y * Mathf.PI * 2) * idleRotationAmplitude.y,
            Mathf.Sin(Time.time * idleRotationSpeed.z * idleSpeedMultiplier + idleRotationCycleOffset.z * Mathf.PI * 2) * idleRotationAmplitude.z
        ) * bodyIdleAnimWeight;

        // Move the root in local space
        rootBone.localPosition = rootHomePos + new Vector3(
            Mathf.Sin(Time.time * idleMotionSpeed.x * idleSpeedMultiplier) * idleMotionAmplitude.x,
            Mathf.Sin(Time.time * idleMotionSpeed.y * idleSpeedMultiplier) * idleMotionAmplitude.y,
            Mathf.Sin(Time.time * idleMotionSpeed.z * idleSpeedMultiplier) * idleMotionAmplitude.z
        ) * bodyIdleAnimWeight;
    }

    #endregion

    #region Legs

    [Header("Legs")]
    [SerializeField] LegStepper_Full frontLeftLegStepper;
    [SerializeField] LegStepper_Full frontRightLegStepper;
    [SerializeField] LegStepper_Full backLeftLegStepper;
    [SerializeField] LegStepper_Full backRightLegStepper;

    // Only allow diagonal leg pairs to step together
    IEnumerator LegUpdateCoroutine()
    {
        while (true)
        {
            while (!legSteppingEnabled) yield return null;

            do
            {
                frontLeftLegStepper.TryMove();
                backRightLegStepper.TryMove();
                yield return null;
            } while (backRightLegStepper.Moving || frontLeftLegStepper.Moving);

            do
            {
                frontRightLegStepper.TryMove();
                backLeftLegStepper.TryMove();
                yield return null;
            } while (backLeftLegStepper.Moving || frontRightLegStepper.Moving);
        }
    }

    #endregion

    [Button]
    void ToggleLegIK()
    {
        legIKEnabled = !legIKEnabled;
        foreach (var ik in GetComponentsInChildren<InverseKinematics>())
        {
            ik.enabled = legIKEnabled;
        }
    }
}
