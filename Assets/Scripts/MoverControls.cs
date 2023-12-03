using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System.Collections;
using static UnityEngine.Rendering.DebugUI;
using System.Collections.Generic;
using static System.TimeZoneInfo;

public class MoverControls : MonoBehaviour
{
    [SerializeField] private WalkAnimsManager walkAnimsManager;
    private MoveData walkPreviousMoveData;
    private MoveData walkNextMoveData;
    [SerializeField] private PlayerController playerController;

    [SerializeField] private GameObject rootObject;
    [SerializeField] private bool movingRootPosition = false;

    private Vector3 previousRootPosition;
    private Vector3 currentRootPosition;

    // == Have to add the objects that control the legs == //
    [SerializeField] private GameObject stepControllerL;
    [SerializeField] private GameObject stepControllerR;
    // == Have to add the objects that control the armss == //
    [SerializeField] private GameObject armControllerL;
    [SerializeField] private GameObject armControllerR;

    [Header("Put Mover Objects in same order as in MoveData Scriptable Object")]
    [SerializeField] private GameObject[] moverObjects; // IMPORTANT: this array HAS to have same objects as scriptable object, in SAME ORDER

    private float idleMoveSpeed;
    private float previousMoveSpeed; //*** do I need these?
    private float nextMoveSpeed;

    private float previousStepDistance;
    private float nextStepDistance;

    private float moverTime = 0f; // controlling time values of curves (dont mess with this)
    private float transitionValue = 0f; //transition time is at 0 (lerp previous/current - next) to set values to "current"

    private float startWalkingThreshold;

    private float transitionDuration; // *** eventually I might not need these
    private bool transitionTrigger = false;

    private void Awake()
    {
        if (walkAnimsManager == null)
        {
            Debug.LogError("animsManager reference is not set!");
            return;
        }
        walkPreviousMoveData = walkAnimsManager.previousAnim;
        walkNextMoveData = walkAnimsManager.nextAnim;

        if (walkPreviousMoveData == null)
        {
            Debug.LogError("currentMoveData is null in Awake!");
        }

        if (walkNextMoveData == null)
        {
            Debug.LogError("previousMoveData is null in Awake!");
        }
    }
    private void Start()
    {
        // Ensure that the arrays have the same length
        if (walkPreviousMoveData.moverObjectsParameters.Length == moverObjects.Length)
        {
            for (int i = 0; i < walkPreviousMoveData.moverObjectsParameters.Length; i++)
            {
                MoveData.ObjectParameters objParams = walkPreviousMoveData.moverObjectsParameters[i];
                GameObject currentObject = moverObjects[i];

                Vector3 objectLocalPosition = currentObject.transform.localPosition;
                objParams.xLocalPosition = objectLocalPosition.x; // Have to also pass these values to the Move Data,
                objParams.yLocalPosition = objectLocalPosition.y; // Because the update method needs the initial position
                objParams.zLocalPosition = objectLocalPosition.z;

                // == The Orientation == //
                Quaternion objectLocalOrientation = currentObject.transform.localRotation;
                objParams.xLocalAngle = currentObject.transform.localEulerAngles.x; // Also passing initial angles to Move Data
                objParams.yLocalAngle = currentObject.transform.localEulerAngles.y;
                objParams.zLocalAngle = currentObject.transform.localEulerAngles.z;
            }

            previousMoveSpeed = walkPreviousMoveData.moveSpeed;
            nextMoveSpeed = walkNextMoveData.moveSpeed; // *** might not need these...

            idleMoveSpeed = walkAnimsManager.idleAnim.moveSpeed; // ...*** and instead use this.


            previousStepDistance = walkPreviousMoveData.stepDistance;
            nextStepDistance = walkNextMoveData.stepDistance;

            transitionDuration = walkPreviousMoveData.transitionDuration;
            movingRootPosition = walkPreviousMoveData.movingRootPosition;

            currentRootPosition = rootObject.transform.position;
            previousRootPosition = currentRootPosition;
        }
        else
        {
            Debug.LogError("Mismatched array lengths between moveData.moverObjectsParameters and moverObjects.");
        }

        startWalkingThreshold = walkAnimsManager.walkThreshold;

        transitionValue = 0f;
        transitionTrigger = false; // *** cut this later?

    }
    private void OnEnable()
    {
        if (walkPreviousMoveData != null && walkPreviousMoveData.updated != null)
        {
            walkPreviousMoveData.updated.AddListener(MoveDataUpdates);
        }
    }
    private void OnDisable()
    {
        if (walkPreviousMoveData != null && walkPreviousMoveData.updated != null)
        {
            walkPreviousMoveData.updated.RemoveListener(MoveDataUpdates);
        }
    }
    void MoveDataUpdates()
    {
        previousMoveSpeed = walkPreviousMoveData.moveSpeed; // *** do I even need this?

        previousStepDistance = walkPreviousMoveData.stepDistance;
        movingRootPosition = walkPreviousMoveData.movingRootPosition;

        for (int i = 0; i < walkPreviousMoveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters objParams = walkPreviousMoveData.moverObjectsParameters[i];
        }
    }

    void Update()
    {
        walkPreviousMoveData = walkAnimsManager.previousAnim;   // transitions always lerp from previous - next
        walkNextMoveData = walkAnimsManager.nextAnim;           // "previous" is also "current" (think of it like 0-1: previous 0, next 1)

        previousStepDistance = walkPreviousMoveData.stepDistance;
        nextStepDistance = walkNextMoveData.stepDistance;

        movingRootPosition = walkPreviousMoveData.movingRootPosition;
        currentRootPosition = rootObject.transform.position;
        float moveAmount = Vector3.Distance(previousRootPosition, currentRootPosition); // might have to recalculate this to use vector2 (z,x)

        if (!movingRootPosition) 
        {
            HandleIdle(); // use this if movement is NOT related to character movement in world space
        }

        if (movingRootPosition)
        {
            moverTime += moveAmount; // use this if movement IS related to character movement in world space
            HandleWalkTransitions();
        }

        if (moverTime >= Mathf.PI) moverTime = 0f;
        float lerpTimer = Mathf.InverseLerp(0, (Mathf.PI), moverTime);

        bool isTransitioning = (transitionValue > startWalkingThreshold) ? true : false;

        for (int i = 0; i < walkPreviousMoveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters previousObjParams = walkPreviousMoveData.moverObjectsParameters[i];
            MoveData.ObjectParameters nextObjParams = walkNextMoveData.moverObjectsParameters[i];

            GameObject currentObject = moverObjects[i];

            if (!isTransitioning)
                HandleNonTransition(previousObjParams, currentObject, lerpTimer);

            if (isTransitioning)
                HandleAnimTransition(previousObjParams, nextObjParams, currentObject, lerpTimer);
        }

        AdjustStepDistance(isTransitioning);
        
        previousRootPosition = currentRootPosition;
    }
    void HandleNonTransition(MoveData.ObjectParameters previousObjParams, GameObject currentObject, float lerpTimer) 
    {
        bool movePosition = previousObjParams.movePosition; // these should stay the same ( bool movePosition = prev.. = next...)
        bool moveRotation = previousObjParams.moveRotation; // (the parameter setup should remain the same for different anims)

        float localX = 0;
        float localY = 0;
        float localZ = 0;

        float initialLocalRotationX = 0;
        float initialLocalRotationY = 0;
        float initialLocalRotationZ = 0;

        if (movePosition)
        {
            localX = previousObjParams.xLocalPosition; // we have to add the returned values to initial position of the object
            localY = previousObjParams.yLocalPosition; // 
            localZ = previousObjParams.zLocalPosition; // see below ...*
        }
        if (moveRotation)
        {
            initialLocalRotationX = previousObjParams.xLocalAngle;
            initialLocalRotationY = previousObjParams.yLocalAngle;
            initialLocalRotationZ = previousObjParams.zLocalAngle;
        }

        // == Variables used in calculations == //

        bool currentX_IsSine = previousObjParams.x_Sine;
        bool currentX_IsAnimCurve = previousObjParams.x_AnimCurve;
        bool currentY_IsSine = previousObjParams.y_Sine;
        bool currentY_IsAnimCurve = previousObjParams.y_AnimCurve;
        bool currentZ_IsSine = previousObjParams.z_Sine;
        bool currentZ_IsAnimCurve = previousObjParams.z_AnimCurve;

        float currentX_frequency = previousObjParams.X_frequency;
        float currentX_phaseOffset = Mathf.PI * previousObjParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
        float currentX_amplitude = previousObjParams.X_amplitude;
        float currentX_return_Offset = previousObjParams.X_returnValueOffset;
        float currentX_ClampMin = previousObjParams.X_ClampMin;
        float currentX_ClampMax = previousObjParams.X_ClampMax;
        AnimationCurve currentX_Curve = previousObjParams.X_Curve;

        float currentY_frequency = previousObjParams.Y_frequency;
        float currentY_phaseOffset = Mathf.PI * previousObjParams.Y_phaseOffset;
        float currentY_amplitude = previousObjParams.Y_amplitude;
        float currentY_return_Offset = previousObjParams.Y_returnValueOffset;
        float currentY_ClampMin = previousObjParams.Y_ClampMin;
        float currentY_ClampMax = previousObjParams.Y_ClampMax;
        AnimationCurve currentY_Curve = previousObjParams.Y_Curve;

        float currentZ_frequency = previousObjParams.Z_frequency;
        float currentZ_phaseOffset = Mathf.PI * previousObjParams.Z_phaseOffset;
        float currentZ_amplitude = previousObjParams.Z_amplitude;
        float currentZ_return_Offset = previousObjParams.Z_returnValueOffset;
        float currentZ_ClampMin = previousObjParams.Z_ClampMin;
        float currentZ_ClampMax = previousObjParams.Z_ClampMax;
        AnimationCurve currentZ_Curve = previousObjParams.Z_Curve;

        float currentSineValueX = SineValue(moverTime, currentX_frequency, currentX_amplitude, currentX_phaseOffset, currentX_return_Offset);
        float currentSineValueY = SineValue(moverTime, currentY_frequency, currentY_amplitude, currentY_phaseOffset, currentY_return_Offset);
        float currentSineValueZ = SineValue(moverTime, currentZ_frequency, currentZ_amplitude, currentZ_phaseOffset, currentZ_return_Offset);

        if (currentSineValueX < currentX_ClampMin) currentSineValueX = currentX_ClampMin;
        if (currentSineValueX > currentX_ClampMax) currentSineValueX = currentX_ClampMax;

        if (currentSineValueY < currentY_ClampMin) currentSineValueY = currentY_ClampMin;
        if (currentSineValueY > currentY_ClampMax) currentSineValueY = currentY_ClampMax;

        if (currentSineValueZ < currentZ_ClampMin) currentSineValueZ = currentZ_ClampMin;
        if (currentSineValueZ > currentZ_ClampMax) currentSineValueZ = currentZ_ClampMax;

        float currentX = (currentX_IsSine) ? currentSineValueX : (currentX_IsAnimCurve) ? currentX_Curve.Evaluate(lerpTimer) : 0f;
        float currentY = (currentY_IsSine) ? currentSineValueY : (currentY_IsAnimCurve) ? currentY_Curve.Evaluate(lerpTimer) : 0f;
        float currentZ = (currentZ_IsSine) ? currentSineValueZ : (currentZ_IsAnimCurve) ? currentZ_Curve.Evaluate(lerpTimer) : 0f;

        localX += currentX; // the localX/Y/Z values were set to position values
        localY += currentY; // they should not be used for rotation calculations?
        localZ += currentZ; // (see below...*)

        if (movePosition)
            currentObject.transform.localPosition = new Vector3(localX, localY, localZ);

        if (moveRotation)
        {
            // Actual min-max angles, considering initial orientations of objects as well as the return offset values
            float minXAngle = initialLocalRotationX - currentX_amplitude + currentX_return_Offset;
            float maxXAngle = initialLocalRotationX + currentX_amplitude + currentX_return_Offset;
            float minYAngle = initialLocalRotationY - currentY_amplitude + currentY_return_Offset;
            float maxYAngle = initialLocalRotationY + currentY_amplitude + currentY_return_Offset;
            float minZAngle = initialLocalRotationZ - currentZ_amplitude + currentZ_return_Offset;
            float maxZAngle = initialLocalRotationZ + currentZ_amplitude + currentZ_return_Offset;

            // Using values being returned from curve calculations and normalising it (to use in Lerp)
            float xRotationNormalised = Mathf.InverseLerp(-currentX_amplitude + currentX_return_Offset, currentX_amplitude + currentX_return_Offset, currentX); // *... here?
            float yRotationNormalised = Mathf.InverseLerp(-currentY_amplitude + currentY_return_Offset, currentY_amplitude + currentY_return_Offset, currentY); // localX/Y/Z ?
            float zRotationNormalised = Mathf.InverseLerp(-currentZ_amplitude + currentZ_return_Offset, currentZ_amplitude + currentZ_return_Offset, currentZ); // not correct...

            float xAngle = 0f, yAngle = 0f, zAngle = 0f;
            // Lerping between min-max angles, using above normalised return values
            if (currentX_IsSine)
                xAngle = Mathf.Lerp(minXAngle, maxXAngle, xRotationNormalised);
            if (currentY_IsSine)
                yAngle = Mathf.Lerp(minYAngle, maxYAngle, yRotationNormalised);
            if (currentZ_IsSine)
                zAngle = Mathf.Lerp(minZAngle, maxZAngle, zRotationNormalised);

            if (currentX_IsAnimCurve)
                xAngle = initialLocalRotationX + currentX;
            if (currentY_IsAnimCurve)
                yAngle = initialLocalRotationY + currentY;
            if (currentZ_IsAnimCurve)
                zAngle = initialLocalRotationX + currentZ;

            // Applying angles to local rotation of object
            currentObject.transform.localRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
        }
    }

    void HandleAnimTransition(MoveData.ObjectParameters previousObjParams, MoveData.ObjectParameters nextObjParams, GameObject currentObject, float lerpTimer) 
    {
        bool movePosition = previousObjParams.movePosition; // these should stay the same ( bool movePosition = prev.. = next...)
        bool moveRotation = previousObjParams.moveRotation; // (the parameter setup should remain the same for different anims)

        float localX = 0;
        float localY = 0;
        float localZ = 0;

        float initialLocalRotationX = 0;
        float initialLocalRotationY = 0;
        float initialLocalRotationZ = 0;

        if (movePosition)
        {
            localX = previousObjParams.xLocalPosition; // we have to add the returned values to initial position of the object
            localY = previousObjParams.yLocalPosition; // 
            localZ = previousObjParams.zLocalPosition; // see below ...*
        }
        if (moveRotation)
        {
            initialLocalRotationX = previousObjParams.xLocalAngle;
            initialLocalRotationY = previousObjParams.yLocalAngle;
            initialLocalRotationZ = previousObjParams.zLocalAngle;
        }

        // == Variables used in calculations == //

        bool currentX_IsSine = previousObjParams.x_Sine;
        bool currentX_IsAnimCurve = previousObjParams.x_AnimCurve;
        bool currentY_IsSine = previousObjParams.y_Sine;
        bool currentY_IsAnimCurve = previousObjParams.y_AnimCurve;
        bool currentZ_IsSine = previousObjParams.z_Sine;
        bool currentZ_IsAnimCurve = previousObjParams.z_AnimCurve;

        float currentX_frequency = previousObjParams.X_frequency;
        float currentX_phaseOffset = Mathf.PI * previousObjParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
        float currentX_amplitude = previousObjParams.X_amplitude;
        float currentX_return_Offset = previousObjParams.X_returnValueOffset;
        float currentX_ClampMin = previousObjParams.X_ClampMin;
        float currentX_ClampMax = previousObjParams.X_ClampMax;
        AnimationCurve currentX_Curve = previousObjParams.X_Curve;

        float currentY_frequency = previousObjParams.Y_frequency;
        float currentY_phaseOffset = Mathf.PI * previousObjParams.Y_phaseOffset;
        float currentY_amplitude = previousObjParams.Y_amplitude;
        float currentY_return_Offset = previousObjParams.Y_returnValueOffset;
        float currentY_ClampMin = previousObjParams.Y_ClampMin;
        float currentY_ClampMax = previousObjParams.Y_ClampMax;
        AnimationCurve currentY_Curve = previousObjParams.Y_Curve;

        float currentZ_frequency = previousObjParams.Z_frequency;
        float currentZ_phaseOffset = Mathf.PI * previousObjParams.Z_phaseOffset;
        float currentZ_amplitude = previousObjParams.Z_amplitude;
        float currentZ_return_Offset = previousObjParams.Z_returnValueOffset;
        float currentZ_ClampMin = previousObjParams.Z_ClampMin;
        float currentZ_ClampMax = previousObjParams.Z_ClampMax;
        AnimationCurve currentZ_Curve = previousObjParams.Z_Curve;

        float currentSineValueX = SineValue(moverTime, currentX_frequency, currentX_amplitude, currentX_phaseOffset, currentX_return_Offset);
        float currentSineValueY = SineValue(moverTime, currentY_frequency, currentY_amplitude, currentY_phaseOffset, currentY_return_Offset);
        float currentSineValueZ = SineValue(moverTime, currentZ_frequency, currentZ_amplitude, currentZ_phaseOffset, currentZ_return_Offset);

        if (currentSineValueX < currentX_ClampMin) currentSineValueX = currentX_ClampMin;
        if (currentSineValueX > currentX_ClampMax) currentSineValueX = currentX_ClampMax;

        if (currentSineValueY < currentY_ClampMin) currentSineValueY = currentY_ClampMin;
        if (currentSineValueY > currentY_ClampMax) currentSineValueY = currentY_ClampMax;

        if (currentSineValueZ < currentZ_ClampMin) currentSineValueZ = currentZ_ClampMin;
        if (currentSineValueZ > currentZ_ClampMax) currentSineValueZ = currentZ_ClampMax;

        bool nextX_IsSine = nextObjParams.x_Sine;
        bool nextX_IsAnimCurve = nextObjParams.x_AnimCurve;
        bool nextY_IsSine = nextObjParams.y_Sine;
        bool nextY_IsAnimCurve = nextObjParams.y_AnimCurve;
        bool nextZ_IsSine = nextObjParams.z_Sine;
        bool nextZ_IsAnimCurve = nextObjParams.z_AnimCurve;

        float nextX_frequency = nextObjParams.X_frequency;
        float nextX_phaseOffset = Mathf.PI * nextObjParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
        float nextX_amplitude = nextObjParams.X_amplitude;
        float nextX_return_Offset = nextObjParams.X_returnValueOffset;
        float nextX_ClampMin = nextObjParams.X_ClampMin;
        float nextX_ClampMax = nextObjParams.X_ClampMax;
        AnimationCurve nextX_Curve = nextObjParams.X_Curve;

        float nextY_frequency = nextObjParams.Y_frequency;
        float nextY_phaseOffset = Mathf.PI * nextObjParams.Y_phaseOffset;
        float nextY_amplitude = nextObjParams.Y_amplitude;
        float nextY_return_Offset = nextObjParams.Y_returnValueOffset;
        float nextY_ClampMin = nextObjParams.Y_ClampMin;
        float nextY_ClampMax = nextObjParams.Y_ClampMax;
        AnimationCurve nextY_Curve = nextObjParams.Y_Curve;

        float nextZ_frequency = nextObjParams.Z_frequency;
        float nextZ_phaseOffset = Mathf.PI * nextObjParams.Z_phaseOffset;
        float nextZ_amplitude = nextObjParams.Z_amplitude;
        float nextZ_return_Offset = nextObjParams.Z_returnValueOffset;
        float nextZ_ClampMin = nextObjParams.Z_ClampMin;
        float nextZ_ClampMax = nextObjParams.Z_ClampMax;
        AnimationCurve nextZ_Curve = nextObjParams.Z_Curve;

        float nextSineValueX = SineValue(moverTime, nextX_frequency, nextX_amplitude, nextX_phaseOffset, nextX_return_Offset);
        float nextSineValueY = SineValue(moverTime, nextY_frequency, nextY_amplitude, nextY_phaseOffset, nextY_return_Offset);
        float nextSineValueZ = SineValue(moverTime, nextZ_frequency, nextZ_amplitude, nextZ_phaseOffset, nextZ_return_Offset);

        if (nextSineValueX < nextX_ClampMin) nextSineValueX = nextX_ClampMin;
        if (nextSineValueX > nextX_ClampMax) nextSineValueX = nextX_ClampMax;

        if (nextSineValueY < nextY_ClampMin) nextSineValueY = nextY_ClampMin;
        if (nextSineValueY > nextY_ClampMax) nextSineValueY = nextY_ClampMax;

        if (nextSineValueZ < nextZ_ClampMin) nextSineValueZ = nextZ_ClampMin;
        if (nextSineValueZ > nextZ_ClampMax) nextSineValueZ = nextZ_ClampMax;

        float nextTransitionX = 0f, nextTransitionY = 0f, nextTransitionZ = 0f;
        float currTransitionX = 0f, currTransitionY = 0f, currTransitionZ = 0f;

        nextTransitionX = (nextX_IsSine) ? nextSineValueX : (nextX_IsAnimCurve) ? nextX_Curve.Evaluate(lerpTimer) : 0f;
        nextTransitionY = (nextY_IsSine) ? nextSineValueY : (nextY_IsAnimCurve) ? nextY_Curve.Evaluate(lerpTimer) : 0f;
        nextTransitionZ = (nextZ_IsSine) ? nextSineValueZ : (nextZ_IsAnimCurve) ? nextZ_Curve.Evaluate(lerpTimer) : 0f;

        currTransitionX = (currentX_IsSine) ? currentSineValueX : (currentX_IsAnimCurve) ? currentX_Curve.Evaluate(lerpTimer) : 0f;
        currTransitionY = (currentY_IsSine) ? currentSineValueY : (currentY_IsAnimCurve) ? currentY_Curve.Evaluate(lerpTimer) : 0f;
        currTransitionZ = (currentZ_IsSine) ? currentSineValueZ : (currentZ_IsAnimCurve) ? currentZ_Curve.Evaluate(lerpTimer) : 0f;

        float transitionX = Mathf.Lerp(currTransitionX, nextTransitionX, transitionValue);
        float transitionY = Mathf.Lerp(currTransitionY, nextTransitionY, transitionValue);
        float transitionZ = Mathf.Lerp(currTransitionZ, nextTransitionZ, transitionValue);

        localX += transitionX;
        localY += transitionY;
        localZ += transitionZ;

        if (movePosition)
            currentObject.transform.localPosition = new Vector3(localX, localY, localZ);

        if (moveRotation)
        {
            float transitionX_amplitude = Mathf.Lerp(currentX_amplitude, nextX_amplitude, transitionValue);
            float transitionY_amplitude = Mathf.Lerp(currentY_amplitude, nextY_amplitude, transitionValue);
            float transitionZ_amplitude = Mathf.Lerp(currentZ_amplitude, nextZ_amplitude, transitionValue);

            float transitionX_returnOffset = Mathf.Lerp(currentX_return_Offset, nextX_return_Offset, transitionValue);
            float transitionY_returnOffset = Mathf.Lerp(currentY_return_Offset, nextY_return_Offset, transitionValue);
            float transitionZ_returnOffset = Mathf.Lerp(currentZ_return_Offset, nextZ_return_Offset, transitionValue);

            // Actual min-max angles, considering initial orientations of objects as well as the return offset values
            float minXAngle = initialLocalRotationX - transitionX_amplitude + transitionX_returnOffset;
            float maxXAngle = initialLocalRotationX + transitionX_amplitude + transitionX_returnOffset;
            float minYAngle = initialLocalRotationY - transitionY_amplitude + transitionY_returnOffset;
            float maxYAngle = initialLocalRotationY + transitionY_amplitude + transitionY_returnOffset;
            float minZAngle = initialLocalRotationZ - transitionZ_amplitude + transitionZ_returnOffset;
            float maxZAngle = initialLocalRotationZ + transitionZ_amplitude + transitionZ_returnOffset;

            // Using values being returned from curve calculations and normalising it (to use in Lerp)
            float xRotationNormalised = Mathf.InverseLerp(-transitionX_amplitude + transitionX_returnOffset, transitionX_amplitude + transitionX_returnOffset, transitionX); // ***again
            float yRotationNormalised = Mathf.InverseLerp(-transitionY_amplitude + transitionY_returnOffset, transitionY_amplitude + transitionY_returnOffset, transitionY); // localX/Y/Z
            float zRotationNormalised = Mathf.InverseLerp(-transitionZ_amplitude + transitionZ_returnOffset, transitionZ_amplitude + transitionZ_returnOffset, transitionZ); // relates to position values

            float xAngle = 0f, yAngle = 0f, zAngle = 0f;
            // Lerping between min-max angles, using above normalised return values
            if (currentX_IsSine)
                xAngle = Mathf.Lerp(minXAngle, maxXAngle, xRotationNormalised);
            if (currentY_IsSine)
                yAngle = Mathf.Lerp(minYAngle, maxYAngle, yRotationNormalised);
            if (currentZ_IsSine)
                zAngle = Mathf.Lerp(minZAngle, maxZAngle, zRotationNormalised);

            if (currentX_IsAnimCurve)
                xAngle = initialLocalRotationX + transitionX;
            if (currentY_IsAnimCurve)
                yAngle = initialLocalRotationY + transitionY;
            if (currentZ_IsAnimCurve)
                zAngle = initialLocalRotationX + transitionZ;

            // Applying angles to local rotation of object
            currentObject.transform.localRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
        }

    }

    float SineValue(float time, float frequency, float amplitude, float phaseOffset, float returnOffset)
    {
        return Mathf.Sin((time + phaseOffset) * frequency) * amplitude + returnOffset;
    }

    void AdjustStepDistance(bool isTransitioning) {
        float transitionStepDistance = (!isTransitioning) ? previousStepDistance : Mathf.Lerp(previousStepDistance, nextStepDistance, transitionValue);

        // == Multiply the step movement by step distance == //
        stepControllerL.transform.localPosition += positionScale(stepControllerL, transitionStepDistance, 0, 0, 1); // use 1 for axis to scale
        stepControllerR.transform.localPosition += positionScale(stepControllerR, transitionStepDistance, 0, 0, 1);

        // == Multiply the arm swing movement by step distance == //
        armControllerL.transform.localPosition += positionScale(armControllerL, transitionStepDistance, 0, 0, 1);
        armControllerR.transform.localPosition += positionScale(armControllerR, transitionStepDistance, 0, 0, 1);
    }
    Vector3 positionScale(GameObject objectToScale, float transitionStepLength, float xScaleFactor, float yScaleFactor, float zScaleFactor)
    {
        Vector3 objectLocalPosition = objectToScale.transform.localPosition;

        xScaleFactor *= transitionStepLength;
        yScaleFactor *= transitionStepLength;
        zScaleFactor *= transitionStepLength;

        float scaledXPosition = objectLocalPosition.x * xScaleFactor;
        float scaledYPosition = objectLocalPosition.y * yScaleFactor;
        float scaledZPosition = objectLocalPosition.z * zScaleFactor;

        Vector3 scaledPosition = new Vector3(scaledXPosition, scaledYPosition, scaledZPosition);
        return scaledPosition;
    }
   
    void HandleIdle()
    {
        transitionValue = 0f;
        float idleTime = moverTime;
        idleTime += Time.deltaTime * walkAnimsManager.idleAnim.moveSpeed;
        moverTime = idleTime;
    }
    
    void HandleWalkTransitions() 
    {
        float walkToJog = playerController.movementInput.magnitude;
        
        if (walkToJog >= startWalkingThreshold)
        {
            transitionValue = Mathf.InverseLerp(startWalkingThreshold, 1, walkToJog);
        }
    }
}


