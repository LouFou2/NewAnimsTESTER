using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
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
    [SerializeField] private Transform midLinePointObject;
    [SerializeField] private Transform hintObject;

    private float segment1Length; // neccesarry to clamp positions (if limb is not stretchy)
    private float segment2Length;

    // these variables are used to calculate the Hint position
    private Vector3 midLinePoint; // IMPORTANT: note "mid point" is not actually the mid point. It depends on ratio of segment1:segment2
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
        // 7. Orient Root Bone //*** THIS BIT IS STILL COOKED
        
        // == 1. Position the Tip Bone

        PositionTipBone(tipBone, IKtargetObject, rootBone, segment1Length, segment2Length); // positioning the tipbone requires its own method

        // == 2. Position the Mid Line Point (the mid point between the root and tip from where hint position needs to be calculated)

        // theta is the angle between "midline" (connecting root to tip) and "line" of first bone
        // (first bone is hypothenuse, midline is adjacent, thata is angle)
        Vector3 rootToTipDirection = (tipBone.position - rootBone.position).normalized;
        Vector3 rootToJointDirection = (jointBone.position - rootBone.position).normalized;
        float angleBetween = Vector3.Angle(rootToJointDirection, rootToTipDirection);
        float thetaRadians = Mathf.Deg2Rad * angleBetween;
        
        // Using trigonometry to get distance to point on midline (from where hint position needs to be calculated)
        // [ the hypothenuse is the first bone ]
        // [ cosine(theta) * hypothenuse = adjacent length ]
        float distanceToMidPoint = Mathf.Cos(thetaRadians) * segment1Length;

        midLinePoint = rootBone.position + (rootToTipDirection * distanceToMidPoint);
        midLinePointObject.position = midLinePoint;

        // == 3. Orient the Mid Point

        IKTargetRotation = IKtargetObject.rotation;
        OrientYLookat(midLinePointObject, tipBone, IKTargetRotation); // this points the y axis of midPointObject at the tipBone, so now the z negative should be perpendicular

        // == 4. Position the Hint

        midPointToHintDistance = segment1Length; // the hint will be segment1 distance away from midpoint, at perpendicular angle
        hintObject.position = midLinePointObject.position + (midPointToHintDistance * midLinePointObject.forward); // then place the hint in z direction, at distance same as segment1

        // == 5. Position the Joint Bone

        float midLinePointToTipDistance = Vector3.Distance(midLinePoint, tipBone.position);
        float t = Mathf.InverseLerp( segment2Length, 0, midLinePointToTipDistance ); // setting the range t between 0 and 1

        jointBone.position = Vector3.Lerp(midLinePoint, hintObject.position, t);

        // == 6. Orient Joint Bone

        // temporarily orient the bone so z-backward looks at hint (this just stabilises the rotation)
        jointBoneRotation = Quaternion.FromToRotation(-jointBone.forward, (hintObject.position - jointBone.position).normalized) * jointBone.rotation;
        // then make y look at the target it should look at
        OrientYLookat(jointBone, tipBone, jointBoneRotation);

        // == 7. Orient Root Bone

        // temporarily orient the bone so z-backward looks at hint (this just stabilises the rotation)
        rootBoneRotation = Quaternion.FromToRotation(-rootBone.forward, (hintObject.position - rootBone.position).normalized) * rootBone.rotation;
        // then make y look at the target it should look at
        OrientYLookat(rootBone, jointBone, rootBoneRotation);

    }

    void OrientYLookat(Transform thisBone, Transform targetBone, Quaternion tempRotation)
    {
        
        Vector3 boneTargetDirection = targetBone.position - thisBone.position;
        thisBone.rotation = tempRotation; // temporary rotation

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
        Vector3 toTargetDirection = targetObject.position - firstBone.position;

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


