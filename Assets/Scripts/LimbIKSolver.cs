using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LimbIKSolver : MonoBehaviour
{
    [SerializeField] private bool isStretchyLimb; // if this is selected, bone will be "stretchy" (tip will tretch to target and relative distances will stretch)

    //== Components of IK limb ==//

    // Three "bones" (or bone controllers):
    [SerializeField] private Transform rootBone;
    [SerializeField] private Transform jointBone;
    [SerializeField] private Transform tipBone;

    private Quaternion rootBoneRotation; // using these to store intitial rotations
    private float rootForwardCorrector; // a -1 or 1 value to make bone z.forward/backward orient towards hint, depending on bone roll

    private Quaternion jointBoneRotation;
    private float jointForwardCorrector;

    // The IK target:
    [Header("Rotate IKTarget Y to position Hint in z.forward direction")]
    [SerializeField] private Transform IKtargetObject; // VERY IMPORTANT : the y rotation of the target will also locate the hint

    private Quaternion IKTargetRotation; // The target rotation controls orientation of joints.

    // These components are used to interpolate joint movement between a "midpoint" and a "hint" object
    [SerializeField] private Transform midLinePointObject;
    [SerializeField] private Transform hintObject;

    // neccesarry to calculate limb stretching/clamping:
    private float segment1Length; 
    private float segment2Length;
    private float totalLimbLength;
    private float stretchedSegment1;
    private float stretchedSegment2;

    // these variables are used to calculate the Hint position
    private Vector3 midLinePoint; // IMPORTANT: note "mid point" is not actually the mid point. It depends on ratio of segment1:segment2
    private float midPointToHintDistance;

    private void Start()
    {
        segment1Length = Vector3.Distance(rootBone.position, jointBone.position); // set this in the start, it shouldnt update
        segment2Length = Vector3.Distance(jointBone.position, tipBone.position);
        stretchedSegment1 = segment1Length;
        stretchedSegment2 = segment2Length;
        totalLimbLength = segment1Length + segment2Length;

        // Target Object controls and stabilises orientation of bones
        // Also positions the hint (towards the z.forward direction of target)
        IKTargetRotation = IKtargetObject.rotation; 

        rootBoneRotation = rootBone.rotation;
        float rootForward_vs_TargetForward = Vector3.Dot(IKtargetObject.forward, rootBone.forward); // foolproofing for bone roll direction
        rootForwardCorrector = (rootForward_vs_TargetForward > 0) ? 1 : -1; // ***NOT WORKING, MAYBE I DONT UNDERSTAND ROTATION PROPERLY
        
        jointBoneRotation = jointBone.rotation;
        float jointForward_vs_TargetForward = Vector3.Dot(IKtargetObject.forward, jointBone.forward); // foolproofing for bone roll direction
        jointForwardCorrector = (jointForward_vs_TargetForward > 0) ? 1 : -1; // ***NOT WORKING, MAYBE I DONT UNDERSTAND ROTATION PROPERLY

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
        
        // == 1. POSITION THE TIP BONE

        PositionTipBone(tipBone, IKtargetObject, rootBone, segment1Length, segment2Length); // positioning the tipbone requires its own method

        // == 2. POSITION THE MID LINE POINT (the mid point between the root and tip from where hint position needs to be calculated)

        // theta is the angle between "midline" (connecting root to tip) and "line" of first bone
        // (first bone is hypothenuse, midline is adjacent, thata is angle)
        Vector3 rootToTipDirection = (tipBone.position - rootBone.position).normalized;
        Vector3 rootToJointDirection = (jointBone.position - rootBone.position).normalized;
        float angleBetween = Vector3.Angle(rootToJointDirection, rootToTipDirection);
        float thetaRadians = Mathf.Deg2Rad * angleBetween;
        
        // Using trigonometry to get distance to point on midline (from where hint position needs to be calculated)
        // [ the hypothenuse is the first bone ]
        // [ cosine(theta) * hypothenuse = adjacent length ]
        float distanceToMidPoint = Mathf.Cos(thetaRadians) * stretchedSegment1;

        midLinePoint = rootBone.position + (rootToTipDirection * distanceToMidPoint);
        midLinePointObject.position = midLinePoint;

        // == 3. ORIENT THE MID LINE POINT

        IKTargetRotation = IKtargetObject.rotation;
        OrientYLookat(midLinePointObject, tipBone, IKTargetRotation); // this points the y axis of midPointObject at the tipBone, so now the z negative should be perpendicular

        // == 4. POSITION THE HINT

        midPointToHintDistance = stretchedSegment1;
        hintObject.position = midLinePointObject.position + (midPointToHintDistance * midLinePointObject.forward); // then place the hint in z direction, at distance same as segment1

        // == 5. POSITION THE JOINT BONE

        float midLinePointToTipDistance = Vector3.Distance(midLinePoint, tipBone.position);
        float t = Mathf.InverseLerp( stretchedSegment2, 0, midLinePointToTipDistance );

        jointBone.position = Vector3.Lerp(midLinePoint, hintObject.position, t);

        // == 6. ORIENT THE JOINT BONE

        // temporarily orient the bone so z-backward looks at hint (this just stabilises the rotation)
        jointBoneRotation = Quaternion.FromToRotation( jointBone.forward, (hintObject.position - jointBone.position).normalized) * jointBone.rotation;
        // then make y look at the target it should look at
        OrientYLookat(jointBone, tipBone, jointBoneRotation);

        // == 7. ORIENT THE ROOT BONE

        // temporarily orient the bone so z-backward looks at hint (this just stabilises the rotation)
        rootBoneRotation = Quaternion.FromToRotation( rootBone.forward, (hintObject.position - rootBone.position).normalized) * rootBone.rotation;
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
        Vector3 rootPosition = firstBone.position;

        // Calculate the direction from firstBone to targetObject
        Vector3 toTargetDirection = targetPosition - rootPosition;

        float stretchedTotalLimbLength = Vector3.Distance(rootPosition, targetPosition);
        if (stretchedTotalLimbLength > totalLimbLength && isStretchyLimb) 
        {
            stretchedSegment1 = (segment1Length / totalLimbLength) * stretchedTotalLimbLength;
            stretchedSegment2 = (segment2Length / totalLimbLength) * stretchedTotalLimbLength;
        }
        
        // Clamp the distance if necessary
        if (stretchedTotalLimbLength > totalLimbLength && !isStretchyLimb) // will only clamp the tip position is limb is not stretchy
        {
            stretchedSegment1 = segment1Length;
            stretchedSegment2 = segment2Length;

            toTargetDirection = toTargetDirection.normalized * totalLimbLength;
            targetPosition = firstBone.position + toTargetDirection;
        }

        // Set the position of the tipBone
        tipBone.position = targetPosition;
    }
}


