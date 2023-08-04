using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform pole;

    [SerializeField] Transform firstBone;
    [SerializeField] Vector3 firstBoneEulerAngleOffset;
    [SerializeField] Transform secondBone;
    [SerializeField] Vector3 secondBoneEulerAngleOffset;
    [SerializeField] Transform thirdBone;
    [SerializeField] Vector3 thirdBoneEulerAngleOffset;
    [SerializeField] bool alignThirdBoneWithTargetRotation = true;

    void OnEnable()
    {
        // Prevent null ref spam in case we didn't link up the bones
        if (
            firstBone == null ||
            secondBone == null ||
            thirdBone == null ||
            pole == null ||
            target == null
        )
        {
            Debug.LogError("IK bones not initialized", this);
            enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        Vector3 towardPole = pole.position - firstBone.position;
        Vector3 towardTarget = target.position - firstBone.position;

        float rootBoneLength = Vector3.Distance(firstBone.position, secondBone.position);
        float secondBoneLength = Vector3.Distance(secondBone.position, thirdBone.position);
        float totalChainLength = rootBoneLength + secondBoneLength;

        // Align root with target
        firstBone.rotation = Quaternion.LookRotation(towardTarget, towardPole);
        firstBone.localRotation *= Quaternion.Euler(firstBoneEulerAngleOffset);

        Vector3 towardSecondBone = secondBone.position - firstBone.position;

        var targetDistance = Vector3.Distance(firstBone.position, target.position);

        // Limit hypotenuse to under the total bone distance to prevent invalid triangles
        targetDistance = Mathf.Min(targetDistance, totalChainLength * 0.9999f);

        // Solve for the angle for the root bone
        // See https://en.wikipedia.org/wiki/Law_of_cosines
        var adjacent =
            (
                (rootBoneLength * rootBoneLength) +
                (targetDistance * targetDistance) -
                (secondBoneLength * secondBoneLength)
            ) / (2 * targetDistance * rootBoneLength);
        var angle = Mathf.Acos(adjacent) * Mathf.Rad2Deg;

        // We rotate around the vector orthogonal to both pole and second bone
        Vector3 cross = Vector3.Cross(towardPole, towardSecondBone);

        if (!float.IsNaN(angle))
        {
            firstBone.RotateAround(firstBone.position, cross, -angle);
        }

        // We've rotated the root bone to the right place, so we just 
        // look at the target from the elbow to get the final rotation
        var secondBoneTargetRotation = Quaternion.LookRotation(target.position - secondBone.position, cross);
        secondBoneTargetRotation *= Quaternion.Euler(secondBoneEulerAngleOffset);
        secondBone.rotation = secondBoneTargetRotation;

        if (alignThirdBoneWithTargetRotation)
        {
            thirdBone.rotation = target.rotation;
            thirdBone.localRotation *= Quaternion.Euler(thirdBoneEulerAngleOffset);
        }
    }
}
