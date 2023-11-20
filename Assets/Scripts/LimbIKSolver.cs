using Unity.Burst.CompilerServices;
using UnityEngine;

public class LimbIKSolver : MonoBehaviour
{
    [SerializeField] private bool isStretchyLimb; // if this is selected, bone will be "stretchy" (tip will tretch to target and relative distances will stretch)

    //== Components of IK limb ==//

    // Three "bones" (or bone controllers):
    [SerializeField] private Transform rootBone;
    [SerializeField] private Transform jointBone;
    [SerializeField] private Transform tipBone;

    private Quaternion rootBoneRotation; // using these to store intitial rotations
    private Quaternion jointBoneRotation;

    // The IK target:
    [Header("Rotate IKTarget Y to position Hint in z.forward direction")]
    [SerializeField] private Transform IKtargetObject; // VERY IMPORTANT : the y rotation of the target will also locate the hint

    private Quaternion IKTargetRotation; // using these to store intitial rotations

    // These components are used to clamp joint movement between a "midpoint"and a "hint" object
    [SerializeField] private Transform midPointObject;
    [SerializeField] private Transform hintObject;

    private float segment1Length; // neccesarry to clamp positions (if limb is not stretchy)
    private float segment2Length;

    // these variables are used to calculate the Hint position
    private Vector3 midPoint; // IMPORTANT: note "mid point" is not actually the mid point. It depends on ratio of segment1:segment2
    private float midPointToHintDistance;

    private void Start()
    {
        segment1Length = Vector3.Distance(rootBone.position, jointBone.position); // set this in the start, it shouldnt update
        segment2Length = Vector3.Distance(jointBone.position, tipBone.position);

        rootBoneRotation = rootBone.rotation;
        jointBoneRotation = jointBone.rotation;
        IKTargetRotation = IKtargetObject.rotation;
    }
    void Update()
    {
        // == IK Solving == //
        // Happens in inverse order of bone order, i.e. from tip to root:
        // 1. Position Tip Bone
        // 2. Position Mid Point
        // 3. Orient Mid Point
        // 4. Position Hint
        // 5. Position Joint Bone
        // 6. Orient Joint Bone
        // 7. Orient Root Bone
        
        // 1. Position the Tip Bone
        PositionTipBone(tipBone, IKtargetObject, rootBone, segment1Length, segment2Length); // positioning the tipbone requires its own method

        // 2. Position the Mid Point (the mid point between the root and tip)
        midPoint = (rootBone.position + tipBone.position) / 2; // this only works if segments are same length. Needs better calculation:
        midPointObject.position = midPoint;

        // 3. Orient the Mid Point
        midPointObject.rotation = IKtargetObject.rotation; // temporarily aligning orientation with Target Object
        OrientYLookat(midPointObject, tipBone, IKTargetRotation); // this points the y axis of midPointObject at the tipBone, so now the z negative should be perpendicular

        // 4. Position the Hint
        midPointToHintDistance = segment1Length; // the hint will be segment1 distance away from midpoint, at perpendicular angle
        hintObject.position = midPointObject.position + (midPointToHintDistance * midPointObject.forward); // then place the hint in z direction, at distance same as segment1

        // 5. Position the Joint Bone
        float distanceFromFirstBoneToMidpoint = Vector3.Distance(rootBone.position, midPoint);
        float normalisedFactor = distanceFromFirstBoneToMidpoint / (segment1Length); // **THINK ABOUT THIS< IS IT CORRECT? (it works though)
        jointBone.position = Vector3.Lerp(hintObject.position, midPoint, normalisedFactor);

        // 6. Orient Joint Bone
        float jointYRotation = jointBoneRotation.eulerAngles.y;
        OrientYLookat(jointBone, tipBone, jointBoneRotation);

        // 7. Orient Root Bone
        OrientYLookat(rootBone, jointBone, rootBoneRotation);

    }

    void OrientYLookat(Transform thisBone, Transform targetBone, Quaternion initialRotation)
    {
        
        Vector3 boneTargetDirection = targetBone.position - thisBone.position;
        thisBone.localRotation = initialRotation;
        Quaternion boneTargetRotation = Quaternion.FromToRotation(thisBone.up, boneTargetDirection.normalized) * thisBone.rotation;

        // Rotate bone
        thisBone.rotation = boneTargetRotation;
    }

    void PositionTipBone(Transform tipBone, Transform targetObject, Transform firstBone, float segment1Length, float segment2Length)
    {
        // Optionally, this can be a stretchy limb, which means the tipbone will simply follow the IK target object
        
        // Calculate the target position based on the targetObject's position
        Vector3 targetPosition = targetObject.position;

        // Calculate the direction from firstBone to targetObject
        Vector3 toTargetDirection = targetPosition - firstBone.position;

        float totalLimbLength = segment1Length + segment2Length;

        // Clamp the distance if necessary
        if (toTargetDirection.magnitude > totalLimbLength && !isStretchyLimb) // will only clamp the tip position is limb is not stretchy
        {
            toTargetDirection = toTargetDirection.normalized * totalLimbLength;
            targetPosition = firstBone.position + toTargetDirection;
        }

        // Set the position of the tipBone
        tipBone.position = targetPosition;
    }
}


