using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System.Collections;
using static UnityEngine.Rendering.DebugUI;
using System.Collections.Generic;
using static System.TimeZoneInfo;

public class MoverControls : MonoBehaviour
{
    [SerializeField] private AnimsManager animsManager;
    private MoveData currentMoveData;
    private MoveData previousMoveData;
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

    private float currentMoveSpeed;
    private float previousMoveSpeed;
    private float currentStepDistance;
    private float previousStepDistance;

    private float moverTime = 0f; // controlling time values of curves (dont mess with this)
    private float transitionTime = 1f; //transition time is at 1 (lerp previous - current) to set values to "current"
    private float transitionDuration;
    private bool transitionTrigger = false;

    private void Awake()
    {
        if (animsManager == null)
        {
            Debug.LogError("animsManager reference is not set!");
            return;
        }
        currentMoveData = animsManager.currentAnim;
        previousMoveData = animsManager.previousAnim;
        if (currentMoveData == null)
        {
            Debug.LogError("currentMoveData is null in Awake!");
        }

        if (previousMoveData == null)
        {
            Debug.LogError("previousMoveData is null in Awake!");
        }
    }
    private void Start()
    {
        // Ensure that the arrays have the same length
        if (currentMoveData.moverObjectsParameters.Length == moverObjects.Length)
        {
            for (int i = 0; i < currentMoveData.moverObjectsParameters.Length; i++)
            {
                MoveData.ObjectParameters objParams = currentMoveData.moverObjectsParameters[i];
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

            currentMoveSpeed = currentMoveData.moveSpeed;
            previousMoveSpeed = previousMoveData.moveSpeed;
            currentStepDistance = currentMoveData.stepDistance;
            previousStepDistance = previousMoveData.stepDistance;

            transitionDuration = currentMoveData.transitionDuration;

            movingRootPosition = currentMoveData.movingRootPosition;

            currentRootPosition = rootObject.transform.position;
            previousRootPosition = currentRootPosition;
        }
        else
        {
            Debug.LogError("Mismatched array lengths between moveData.moverObjectsParameters and moverObjects.");
        }

        transitionTime = 1f;
        transitionTrigger = false;
    }
    private void OnEnable()
    {
        if (currentMoveData != null && currentMoveData.updated != null)
        {
            currentMoveData.updated.AddListener(MoveDataUpdates);
        }
    }
    private void OnDisable()
    {
        if (currentMoveData != null && currentMoveData.updated != null)
        {
            currentMoveData.updated.RemoveListener(MoveDataUpdates);
        }
    }
    void MoveDataUpdates()
    {
        currentMoveSpeed = currentMoveData.moveSpeed;
        currentStepDistance = currentMoveData.stepDistance;
        movingRootPosition = currentMoveData.movingRootPosition;

        for (int i = 0; i < currentMoveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters objParams = currentMoveData.moverObjectsParameters[i];
        }
    }

    void Update()
    {
        currentMoveData = animsManager.currentAnim;
        previousMoveData = animsManager.previousAnim;
        transitionTrigger = animsManager.transitionTrue;
        transitionDuration = currentMoveData.transitionDuration;
        currentStepDistance = currentMoveData.stepDistance;
        previousStepDistance = previousMoveData.stepDistance;
        movingRootPosition = currentMoveData.movingRootPosition;

        currentRootPosition = rootObject.transform.position;
        float moveAmount = Vector3.Distance(previousRootPosition, currentRootPosition); // might have to recalculate this to use vector2 (z,x)

        // == Time Stuff == //

        if(!movingRootPosition)
            moverTime += Time.deltaTime * currentMoveSpeed; // use this if movement is NOT related to character movement in world space
        if(movingRootPosition)
            moverTime += moveAmount; // use this if movement IS related to character movement in world space

        if (moverTime >= Mathf.PI) moverTime = 0f;
        float lerpTimer = Mathf.InverseLerp(0, (Mathf.PI), moverTime);

        if (transitionTrigger)
        {
            StartCoroutine(TransitionTimer());
        }
        bool isTransitioning = (transitionTime != 1) ? true : false;

        for (int i = 0; i < currentMoveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters currentObjParams = currentMoveData.moverObjectsParameters[i];
            MoveData.ObjectParameters previousObjParams = previousMoveData.moverObjectsParameters[i];

            GameObject currentObject = moverObjects[i];

            // == Setting up for Position and Rotation Changes == //

            bool movePosition = currentObjParams.movePosition; // the idea is that these should stay the same
            bool moveRotation = currentObjParams.moveRotation; // the parameter setup should remain the same for different anims

            float localX = 0;
            float localY = 0;
            float localZ = 0;

            float initialLocalRotationX = 0;
            float initialLocalRotationY = 0;
            float initialLocalRotationZ = 0;

            if (movePosition)
            {
                localX = currentObjParams.xLocalPosition; // we have to add the returned values to initial position of the object
                localY = currentObjParams.yLocalPosition; // 
                localZ = currentObjParams.zLocalPosition; // see below ...*
            }
            if (moveRotation)
            {
                initialLocalRotationX = currentObjParams.xLocalAngle;
                initialLocalRotationY = currentObjParams.yLocalAngle;
                initialLocalRotationZ = currentObjParams.zLocalAngle;
            }

            // == Variables used in calculations == //

            bool currentX_IsSine = currentObjParams.x_Sine;             
            bool currentX_IsAnimCurve = currentObjParams.x_AnimCurve;
            bool currentY_IsSine = currentObjParams.y_Sine;
            bool currentY_IsAnimCurve = currentObjParams.y_AnimCurve;
            bool currentZ_IsSine = currentObjParams.z_Sine;
            bool currentZ_IsAnimCurve = currentObjParams.z_AnimCurve;

            float currentX_frequency = currentObjParams.X_frequency;
            float currentX_phaseOffset = Mathf.PI * currentObjParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
            float currentX_amplitude = currentObjParams.X_amplitude;
            float currentX_return_Offset = currentObjParams.X_returnValueOffset;
            float currentX_ClampMin = currentObjParams.X_ClampMin;
            float currentX_ClampMax = currentObjParams.X_ClampMax;
            AnimationCurve currentX_Curve = currentObjParams.X_Curve;

            float currentY_frequency = currentObjParams.Y_frequency;
            float currentY_phaseOffset = Mathf.PI * currentObjParams.Y_phaseOffset;
            float currentY_amplitude = currentObjParams.Y_amplitude;
            float currentY_return_Offset = currentObjParams.Y_returnValueOffset;
            float currentY_ClampMin = currentObjParams.Y_ClampMin;
            float currentY_ClampMax = currentObjParams.Y_ClampMax;
            AnimationCurve currentY_Curve = currentObjParams.Y_Curve;

            float currentZ_frequency = currentObjParams.Z_frequency;
            float currentZ_phaseOffset = Mathf.PI * currentObjParams.Z_phaseOffset;
            float currentZ_amplitude = currentObjParams.Z_amplitude;
            float currentZ_return_Offset = currentObjParams.Z_returnValueOffset;
            float currentZ_ClampMin = currentObjParams.Z_ClampMin;
            float currentZ_ClampMax = currentObjParams.Z_ClampMax;
            AnimationCurve currentZ_Curve = currentObjParams.Z_Curve;

            float currentSineValueX = SineValue(moverTime, currentX_frequency, currentX_amplitude, currentX_phaseOffset, currentX_return_Offset);
            float currentSineValueY = SineValue(moverTime, currentY_frequency, currentY_amplitude, currentY_phaseOffset, currentY_return_Offset);
            float currentSineValueZ = SineValue(moverTime, currentZ_frequency, currentZ_amplitude, currentZ_phaseOffset, currentZ_return_Offset);

            if (currentSineValueX < currentX_ClampMin) currentSineValueX = currentX_ClampMin;
            if (currentSineValueX > currentX_ClampMax) currentSineValueX = currentX_ClampMax;

            if (currentSineValueY < currentY_ClampMin) currentSineValueY = currentY_ClampMin;
            if (currentSineValueY > currentY_ClampMax) currentSineValueY = currentY_ClampMax;

            if (currentSineValueZ < currentZ_ClampMin) currentSineValueZ = currentZ_ClampMin;
            if (currentSineValueZ > currentZ_ClampMax) currentSineValueZ = currentZ_ClampMax;

            if(!isTransitioning)
            {
                float currentX = (currentX_IsSine) ? currentSineValueX : (currentX_IsAnimCurve) ? currentX_Curve.Evaluate(lerpTimer) : 0f;
                float currentY = (currentY_IsSine) ? currentSineValueY : (currentY_IsAnimCurve) ? currentY_Curve.Evaluate(lerpTimer) : 0f;
                float currentZ = (currentZ_IsSine) ? currentSineValueZ : (currentZ_IsAnimCurve) ? currentZ_Curve.Evaluate(lerpTimer) : 0f;

                localX += currentX;
                localY += currentY;
                localZ += currentZ;

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
                    float xRotationNormalised = Mathf.InverseLerp(-currentX_amplitude + currentX_return_Offset, currentX_amplitude + currentX_return_Offset, localX);
                    float yRotationNormalised = Mathf.InverseLerp(-currentY_amplitude + currentY_return_Offset, currentY_amplitude + currentY_return_Offset, localY);
                    float zRotationNormalised = Mathf.InverseLerp(-currentZ_amplitude + currentZ_return_Offset, currentZ_amplitude + currentZ_return_Offset, localZ);

                    float xAngle = 0f, yAngle = 0f, zAngle = 0f;
                    // Lerping between min-max angles, using above normalised return values
                    if (currentX_IsSine)
                        xAngle = Mathf.Lerp(minXAngle, maxXAngle, xRotationNormalised);
                    if (currentY_IsSine)
                        yAngle = Mathf.Lerp(minYAngle, maxYAngle, yRotationNormalised);
                    if (currentZ_IsSine)
                        zAngle = Mathf.Lerp(minZAngle, maxZAngle, zRotationNormalised);

                    if (currentX_IsAnimCurve)
                        xAngle = initialLocalRotationX + localX;
                    if (currentY_IsAnimCurve)
                        yAngle = initialLocalRotationY + localY;
                    if (currentZ_IsAnimCurve)
                        zAngle = initialLocalRotationX + localZ;

                    // Applying angles to local rotation of object
                    currentObject.transform.localRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
                }
            }
            else 
            {
                bool previousX_IsSine = previousObjParams.x_Sine;
                bool previousX_IsAnimCurve = previousObjParams.x_AnimCurve;
                bool previousY_IsSine = previousObjParams.y_Sine;
                bool previousY_IsAnimCurve = previousObjParams.y_AnimCurve;
                bool previousZ_IsSine = previousObjParams.z_Sine;
                bool previousZ_IsAnimCurve = previousObjParams.z_AnimCurve;

                float previousX_frequency = previousObjParams.X_frequency;
                float previousX_phaseOffset = Mathf.PI * previousObjParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
                float previousX_amplitude = previousObjParams.X_amplitude;
                float previousX_return_Offset = previousObjParams.X_returnValueOffset;
                float previousX_ClampMin = previousObjParams.X_ClampMin;
                float previousX_ClampMax = previousObjParams.X_ClampMax;
                AnimationCurve previousX_Curve = previousObjParams.X_Curve;

                float previousY_frequency = previousObjParams.Y_frequency;
                float previousY_phaseOffset = Mathf.PI * previousObjParams.Y_phaseOffset;
                float previousY_amplitude = previousObjParams.Y_amplitude;
                float previousY_return_Offset = previousObjParams.Y_returnValueOffset;
                float previousY_ClampMin = previousObjParams.Y_ClampMin;
                float previousY_ClampMax = previousObjParams.Y_ClampMax;
                AnimationCurve previousY_Curve = previousObjParams.Y_Curve;

                float previousZ_frequency = previousObjParams.Z_frequency;
                float previousZ_phaseOffset = Mathf.PI * previousObjParams.Z_phaseOffset;
                float previousZ_amplitude = previousObjParams.Z_amplitude;
                float previousZ_return_Offset = previousObjParams.Z_returnValueOffset;
                float previousZ_ClampMin = previousObjParams.Z_ClampMin;
                float previousZ_ClampMax = previousObjParams.Z_ClampMax;
                AnimationCurve previousZ_Curve = previousObjParams.Z_Curve;

                float previousSineValueX = SineValue(moverTime, previousX_frequency, previousX_amplitude, previousX_phaseOffset, previousX_return_Offset);
                float previousSineValueY = SineValue(moverTime, previousY_frequency, previousY_amplitude, previousY_phaseOffset, previousY_return_Offset);
                float previousSineValueZ = SineValue(moverTime, previousZ_frequency, previousZ_amplitude, previousZ_phaseOffset, previousZ_return_Offset);

                if (previousSineValueX < previousX_ClampMin) previousSineValueX = previousX_ClampMin;
                if (previousSineValueX > previousX_ClampMax) previousSineValueX = previousX_ClampMax;

                if (previousSineValueY < previousY_ClampMin) previousSineValueY = previousY_ClampMin;
                if (previousSineValueY > previousY_ClampMax) previousSineValueY = previousY_ClampMax;

                if (previousSineValueZ < previousZ_ClampMin) previousSineValueZ = previousZ_ClampMin;
                if (previousSineValueZ > previousZ_ClampMax) previousSineValueZ = previousZ_ClampMax;

                float prevTransitionX = 0f, prevTransitionY = 0f, prevTransitionZ = 0f;
                float currTransitionX = 0f, currTransitionY = 0f, currTransitionZ = 0f;

                prevTransitionX = (previousX_IsSine) ? previousSineValueX : (previousX_IsAnimCurve) ? previousX_Curve.Evaluate(lerpTimer) : 0f;
                prevTransitionY = (previousY_IsSine) ? previousSineValueY : (previousY_IsAnimCurve) ? previousY_Curve.Evaluate(lerpTimer) : 0f;
                prevTransitionZ = (previousZ_IsSine) ? previousSineValueZ : (previousZ_IsAnimCurve) ? previousZ_Curve.Evaluate(lerpTimer) : 0f;

                currTransitionX = (currentX_IsSine) ? currentSineValueX : (currentX_IsAnimCurve) ? currentX_Curve.Evaluate(lerpTimer) : 0f;
                currTransitionY = (currentY_IsSine) ? currentSineValueY : (currentY_IsAnimCurve) ? currentY_Curve.Evaluate(lerpTimer) : 0f;
                currTransitionZ = (currentZ_IsSine) ? currentSineValueZ : (currentZ_IsAnimCurve) ? currentZ_Curve.Evaluate(lerpTimer) : 0f;

                float transitionX = Mathf.Lerp(prevTransitionX, currTransitionX, transitionTime);
                float transitionY = Mathf.Lerp(prevTransitionY, currTransitionY, transitionTime);
                float transitionZ = Mathf.Lerp(prevTransitionZ, currTransitionZ, transitionTime);

                localX += transitionX;
                localY += transitionY;
                localZ += transitionZ;

                if (movePosition)
                    currentObject.transform.localPosition = new Vector3(localX, localY, localZ);

                if (moveRotation)
                {
                    float transitionX_amplitude = Mathf.Lerp(previousX_amplitude, currentX_amplitude, transitionTime);
                    float transitionY_amplitude = Mathf.Lerp(previousY_amplitude, currentY_amplitude, transitionTime);
                    float transitionZ_amplitude = Mathf.Lerp(previousZ_amplitude, currentZ_amplitude, transitionTime);

                    float transitionX_returnOffset = Mathf.Lerp(previousX_return_Offset, currentX_return_Offset, transitionTime);
                    float transitionY_returnOffset = Mathf.Lerp(previousY_return_Offset, currentY_return_Offset, transitionTime);
                    float transitionZ_returnOffset = Mathf.Lerp(previousZ_return_Offset, currentZ_return_Offset, transitionTime);

                    // Actual min-max angles, considering initial orientations of objects as well as the return offset values
                    float minXAngle = initialLocalRotationX - transitionX_amplitude + transitionX_returnOffset;
                    float maxXAngle = initialLocalRotationX + transitionX_amplitude + transitionX_returnOffset;
                    float minYAngle = initialLocalRotationY - transitionY_amplitude + transitionY_returnOffset;
                    float maxYAngle = initialLocalRotationY + transitionY_amplitude + transitionY_returnOffset;
                    float minZAngle = initialLocalRotationZ - transitionZ_amplitude + transitionZ_returnOffset;
                    float maxZAngle = initialLocalRotationZ + transitionZ_amplitude + transitionZ_returnOffset;

                    // Using values being returned from curve calculations and normalising it (to use in Lerp)
                    float xRotationNormalised = Mathf.InverseLerp(-transitionX_amplitude + transitionX_returnOffset, transitionX_amplitude + transitionX_returnOffset, localX);
                    float yRotationNormalised = Mathf.InverseLerp(-transitionY_amplitude + transitionY_returnOffset, transitionY_amplitude + transitionY_returnOffset, localY);
                    float zRotationNormalised = Mathf.InverseLerp(-transitionZ_amplitude + transitionZ_returnOffset, transitionZ_amplitude + transitionZ_returnOffset, localZ);

                    float xAngle = 0f, yAngle = 0f, zAngle = 0f;
                    // Lerping between min-max angles, using above normalised return values
                    if (currentX_IsSine)
                        xAngle = Mathf.Lerp(minXAngle, maxXAngle, xRotationNormalised);
                    if (currentY_IsSine)
                        yAngle = Mathf.Lerp(minYAngle, maxYAngle, yRotationNormalised);
                    if (currentZ_IsSine)
                        zAngle = Mathf.Lerp(minZAngle, maxZAngle, zRotationNormalised);

                    if (currentX_IsAnimCurve)
                        xAngle = initialLocalRotationX + localX;
                    if (currentY_IsAnimCurve)
                        yAngle = initialLocalRotationY + localY;
                    if (currentZ_IsAnimCurve)
                        zAngle = initialLocalRotationX + localZ;

                    // Applying angles to local rotation of object
                    currentObject.transform.localRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
                }
            }
        }

        float transitionStepDistance = (!isTransitioning) ? currentStepDistance : Mathf.Lerp(previousStepDistance, currentStepDistance, transitionTime);

        // == Multiply the step movement by step distance == //
        stepControllerL.transform.localPosition += positionScale(stepControllerL, transitionStepDistance, 0, 0, 1); // use 1 for axis to scale
        stepControllerR.transform.localPosition += positionScale(stepControllerR, transitionStepDistance, 0, 0, 1);

        // == Multiply the arm swing movement by step distance == //
        armControllerL.transform.localPosition += positionScale(armControllerL, transitionStepDistance, 0, 0, 1);
        armControllerR.transform.localPosition += positionScale(armControllerR, transitionStepDistance, 0, 0, 1);

        previousRootPosition = currentRootPosition;
    }

    float SineValue(float time, float frequency, float amplitude, float phaseOffset, float returnOffset)
    {
        return Mathf.Sin((time + phaseOffset) * frequency) * amplitude + returnOffset;
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
    private IEnumerator TransitionTimer() 
    {
        transitionTime = 0f;

        while (transitionTime < 1f)
        {
            transitionTime += Time.deltaTime / transitionDuration;
            yield return null;
        }
        
        // Ensure the timer stays at 1 at the end
        transitionTime = 1f;
    }
}


