using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using static UnityEngine.Rendering.DebugUI;
using System.Collections.Generic;

public class MoverControlsWithTransition : MonoBehaviour
{
    [SerializeField] private MoveData moveData;

    [SerializeField] private GameObject rootObject;
    [SerializeField] private bool freezeRootPosition = false;

    // == Have to add the objects that control the legs == //
    [SerializeField] private GameObject stepControllerL;
    [SerializeField] private GameObject stepControllerR;
    // == Have to add the objects that control the armss == //
    [SerializeField] private GameObject armControllerL;
    [SerializeField] private GameObject armControllerR;

    [Header("Put Mover Objects in same order as in MoveData Scriptable Object")]
    [SerializeField] private GameObject[] moverObjects; // IMPORTANT: this array HAS to have same objects as scriptable object, in SAME ORDER
    private Vector3[] currentPosition;
    private Vector3[] targetPosition;
    private Quaternion[] currentOrientation;
    private Quaternion[] targetOrientation;

    private float moveSpeed;
    private float stepDistance;
    private float moverTime = 0f; // this represents the x value of the sine graph

    private bool isTransitioning = false;
    private Dictionary<GameObject, bool> hasTargetPositionSet = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> hasTargetOrientationSet = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, Vector3> storedTargetPosition = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> storedTargetOrientation = new Dictionary<GameObject, Quaternion>();


    private void Start()
    {
        // Ensure that the arrays have the same length
        if (moveData.moverObjectsParameters.Length == moverObjects.Length)
        {
            // Initialize arrays
            currentPosition = new Vector3[moverObjects.Length];
            targetPosition = new Vector3[moverObjects.Length];
            currentOrientation = new Quaternion[moverObjects.Length];
            targetOrientation = new Quaternion[moverObjects.Length];

            for (int i = 0; i < moveData.moverObjectsParameters.Length; i++)
            {
                MoveData.ObjectParameters objParams = moveData.moverObjectsParameters[i];
                GameObject currentObject = moverObjects[i];

                bool movePosition = objParams.movePosition;
                bool moveRotation = objParams.moveRotation;
                bool x_IsSine = objParams.x_Sine;
                bool x_IsLerp = objParams.x_AnimCurve;
                bool y_IsSine = objParams.y_Sine;
                bool y_IsLerp = objParams.y_AnimCurve;
                bool z_IsSine = objParams.z_Sine;
                bool z_IsLerp = objParams.z_AnimCurve;

                float X_frequency = objParams.X_frequency;
                float X_phaseOffset = Mathf.PI * objParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
                float X_amplitude = objParams.X_amplitude;
                float X_return_Offset = objParams.X_returnValueOffset;
                float X_ClampMin = objParams.X_ClampMin;
                float X_ClampMax = objParams.X_ClampMax;
                AnimationCurve X_Curve = objParams.X_Curve;

                float Y_frequency = objParams.Y_frequency;
                float Y_phaseOffset = Mathf.PI * objParams.Y_phaseOffset;
                float Y_amplitude = objParams.Y_amplitude;
                float Y_return_Offset = objParams.Y_returnValueOffset;
                float Y_ClampMin = objParams.Y_ClampMin;
                float Y_ClampMax = objParams.Y_ClampMax;
                AnimationCurve Y_Curve = objParams.Y_Curve;

                float Z_frequency = objParams.Z_frequency;
                float Z_phaseOffset = Mathf.PI * objParams.Z_phaseOffset;
                float Z_amplitude = objParams.Z_amplitude;
                float Z_return_Offset = objParams.Z_returnValueOffset;
                float Z_ClampMin = objParams.Z_ClampMin;
                float Z_ClampMax = objParams.Z_ClampMax;
                AnimationCurve Z_Curve = objParams.Z_Curve;

                // == The Position == // 
                Vector3 objectLocalPosition = currentObject.transform.localPosition;
                objParams.xLocalPosition = objectLocalPosition.x; // Have to also pass these values to the Move Data,
                objParams.yLocalPosition = objectLocalPosition.y; // Because the update method needs the initial position
                objParams.zLocalPosition = objectLocalPosition.z;
                Debug.Log($"= {i} = {currentObject} ..storing INITIAL POSITION at {objectLocalPosition}");

                // Calculate transition target position: the position it should be at the first frame
                Vector3 objectTargetPosition = Vector3.zero;
                if (!hasTargetPositionSet.ContainsKey(currentObject))
                {
                    objectTargetPosition = objectLocalPosition; // this wil only change if we want to change position

                    if (movePosition)
                    {
                        float xValue = objectLocalPosition.x;
                        float yValue = objectLocalPosition.y;
                        float zValue = objectLocalPosition.z;

                        if (x_IsSine)
                            xValue += SineValue(0, X_frequency, X_amplitude, X_phaseOffset, X_return_Offset); // Set all time values to zero, so it calculates first frame
                        if (x_IsLerp)
                            xValue += X_Curve.Evaluate(0);
                        if (y_IsSine)
                            yValue += SineValue(0, Y_frequency, Y_amplitude, Y_phaseOffset, Y_return_Offset);
                        if (y_IsLerp)
                            yValue += Y_Curve.Evaluate(0);
                        if (z_IsSine)
                            zValue += SineValue(0, Z_frequency, Z_amplitude, Z_phaseOffset, Z_return_Offset);
                        if (z_IsLerp)
                            zValue += Z_Curve.Evaluate(0);

                        if (xValue < X_ClampMin) xValue = X_ClampMin;
                        if (xValue > X_ClampMax) xValue = X_ClampMax;

                        if (yValue < Y_ClampMin) yValue = Y_ClampMin;
                        if (yValue > Y_ClampMax) yValue = Y_ClampMax;

                        if (zValue < Z_ClampMin) zValue = Z_ClampMin;
                        if (zValue > Z_ClampMax) zValue = Z_ClampMax;

                        objectTargetPosition = new Vector3(xValue, yValue, zValue);

                        // Set the target position for the currentObject in the dictionaries
                        hasTargetPositionSet[currentObject] = true;
                        storedTargetPosition[currentObject] = objectTargetPosition;
                    }
                }
                else if (hasTargetPositionSet.ContainsKey(currentObject))
                { // Target position has already been set, retrieve the stored target position
                    if (storedTargetPosition.ContainsKey(currentObject))
                    {
                        objectTargetPosition = storedTargetPosition[currentObject];
                    }
                    else
                    {
                        Debug.LogError($"Target position not found for {currentObject}");
                        // Handle the error or set a default target position
                        objectTargetPosition = Vector3.zero;
                    }
                }
                Debug.Log($"= {i} = {currentObject} ..storing TARGET POSITION at {objectTargetPosition}");


                // == The Orientation == //
                Quaternion objectLocalOrientation = currentObject.transform.localRotation;
                objParams.xLocalAngle = objectLocalOrientation.x; // Also passing initial angles to Move Data
                objParams.yLocalAngle = objectLocalOrientation.y;
                objParams.zLocalAngle = objectLocalOrientation.z;
                Debug.Log($"= {i} = {currentObject} ..storing INITIAL ORIENTATION at {objectLocalOrientation.eulerAngles}");

                Quaternion objectTargetOrientation = Quaternion.identity;
                if (!hasTargetOrientationSet.ContainsKey(currentObject))
                {
                    objectTargetOrientation = objectLocalOrientation; // this wil only change if we want to change orientation

                    if (moveRotation)
                    {
                        float xAngle = currentObject.transform.localEulerAngles.x;
                        float yAngle = currentObject.transform.localEulerAngles.y;
                        float zAngle = currentObject.transform.localEulerAngles.z;

                        if (x_IsSine)
                            xAngle += SineValue(0, X_frequency, X_amplitude, X_phaseOffset, X_return_Offset); // Set all time values to zero, so it calculates first frame
                        if (x_IsLerp)
                            xAngle += X_Curve.Evaluate(0);
                        if (y_IsSine)
                            yAngle += SineValue(0, X_frequency, X_amplitude, X_phaseOffset, X_return_Offset); // Set all time values to zero, so it calculates first frame
                        if (y_IsLerp)
                            yAngle += X_Curve.Evaluate(0);
                        if (z_IsSine)
                            zAngle += SineValue(0, X_frequency, X_amplitude, X_phaseOffset, X_return_Offset); // Set all time values to zero, so it calculates first frame
                        if (z_IsLerp)
                            zAngle += X_Curve.Evaluate(0);

                        if (xAngle < X_ClampMin) xAngle = X_ClampMin;
                        if (xAngle > X_ClampMax) xAngle = X_ClampMax;

                        if (yAngle < Y_ClampMin) yAngle = Y_ClampMin;
                        if (yAngle > Y_ClampMax) yAngle = Y_ClampMax;

                        if (zAngle < Z_ClampMin) zAngle = Z_ClampMin;
                        if (zAngle > Z_ClampMax) zAngle = Z_ClampMax;

                        objectTargetOrientation = Quaternion.Euler(xAngle, yAngle, zAngle);

                        // Set the target orientation for the currentObject in the dictionaries
                        hasTargetOrientationSet[currentObject] = true;
                        storedTargetOrientation[currentObject] = objectTargetOrientation;
                    }
                }
                else if (hasTargetOrientationSet.ContainsKey(currentObject))
                { // Target position has already been set, retrieve the stored target position
                    if (storedTargetOrientation.ContainsKey(currentObject))
                    {
                        objectTargetOrientation = storedTargetOrientation[currentObject];
                    }
                    else
                    {
                        Debug.LogError($"Target position not found for {currentObject}");
                        // Handle the error or set a default target position
                        objectTargetOrientation = Quaternion.identity;
                    }
                }
                Debug.Log($"= {i} = {currentObject} ..storing TARGET ORIENTATION at {objectTargetOrientation.eulerAngles}");

                currentPosition[i] = objectLocalPosition;
                targetPosition[i] = objectTargetPosition;
                currentOrientation[i] = objectLocalOrientation;
                targetOrientation[i] = objectTargetOrientation;
            }

            StartCoroutine(Transition(currentPosition, targetPosition, currentOrientation, targetOrientation));

            moveSpeed = moveData.moveSpeed;
            stepDistance = moveData.stepDistance;
            freezeRootPosition = moveData.freezePosition;
        }
        else
        {
            Debug.LogError("Mismatched array lengths between moveData.moverObjectsParameters and moverObjects.");
        }
    }
    private void OnEnable()
    {
        moveData.updated.AddListener(MoveDataUpdates);
    }
    private void OnDisable()
    {
        moveData.updated.RemoveListener(MoveDataUpdates);
    }
    void MoveDataUpdates()
    {
        moveSpeed = moveData.moveSpeed;
        stepDistance = moveData.stepDistance;
        freezeRootPosition = moveData.freezePosition;

        for (int i = 0; i < moveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters objParams = moveData.moverObjectsParameters[i];
        }
    }

    void Update()
    {
        // Check if any transition is in progress
        if (isTransitioning)
        {
            // If a transition is ongoing, you might want to skip the rest of the Update logic
            return;
        }

        Debug.Log("== UPDATE IS RUNNING ==");

        moverTime += Time.deltaTime * moveSpeed;
        if (moverTime >= Mathf.PI * 2) moverTime = 0f;
        float lerpTimer = Mathf.InverseLerp(0, Mathf.PI * 2, moverTime);

        // == Moving the Character (moving the root) == //
        float rootZ_MoveDistance = Time.deltaTime * moveSpeed * stepDistance;
        if (freezeRootPosition) rootZ_MoveDistance = 0f;
        rootObject.transform.Translate(rootObject.transform.forward * rootZ_MoveDistance, Space.World);

        for (int i = 0; i < moveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters objParams = moveData.moverObjectsParameters[i];

            GameObject currentObject = moverObjects[i];

            bool movePosition = objParams.movePosition;
            bool moveRotation = objParams.moveRotation;
            bool x_IsSine = objParams.x_Sine;
            bool x_IsLerp = objParams.x_AnimCurve;
            bool y_IsSine = objParams.y_Sine;
            bool y_IsLerp = objParams.y_AnimCurve;
            bool z_IsSine = objParams.z_Sine;
            bool z_IsLerp = objParams.z_AnimCurve;

            float X_frequency = objParams.X_frequency;
            float X_phaseOffset = Mathf.PI * objParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
            float X_amplitude = objParams.X_amplitude;
            float X_return_Offset = objParams.X_returnValueOffset;
            float X_ClampMin = objParams.X_ClampMin;
            float X_ClampMax = objParams.X_ClampMax;
            AnimationCurve X_Curve = objParams.X_Curve;

            float Y_frequency = objParams.Y_frequency;
            float Y_phaseOffset = Mathf.PI * objParams.Y_phaseOffset;
            float Y_amplitude = objParams.Y_amplitude;
            float Y_return_Offset = objParams.Y_returnValueOffset;
            float Y_ClampMin = objParams.Y_ClampMin;
            float Y_ClampMax = objParams.Y_ClampMax;
            AnimationCurve Y_Curve = objParams.Y_Curve;

            float Z_frequency = objParams.Z_frequency;
            float Z_phaseOffset = Mathf.PI * objParams.Z_phaseOffset;
            float Z_amplitude = objParams.Z_amplitude;
            float Z_return_Offset = objParams.Z_returnValueOffset;
            float Z_ClampMin = objParams.Z_ClampMin;
            float Z_ClampMax = objParams.Z_ClampMax;
            AnimationCurve Z_Curve = objParams.Z_Curve;

            float sineValueX = SineValue(moverTime, X_frequency, X_amplitude, X_phaseOffset, X_return_Offset);
            float sineValueY = SineValue(moverTime, Y_frequency, Y_amplitude, Y_phaseOffset, Y_return_Offset);
            float sineValueZ = SineValue(moverTime, Z_frequency, Z_amplitude, Z_phaseOffset, Z_return_Offset);

            if (sineValueX < X_ClampMin) sineValueX = X_ClampMin;
            if (sineValueX > X_ClampMax) sineValueX = X_ClampMax;

            if (sineValueY < Y_ClampMin) sineValueY = Y_ClampMin;
            if (sineValueY > Y_ClampMax) sineValueY = Y_ClampMax;

            if (sineValueZ < Z_ClampMin) sineValueZ = Z_ClampMin;
            if (sineValueZ > Z_ClampMax) sineValueZ = Z_ClampMax;

            float localX = 0;
            float localY = 0;
            float localZ = 0;

            float initialLocalRotationX = 0;
            float initialLocalRotationY = 0;
            float initialLocalRotationZ = 0;

            if (movePosition)
            {
                localX = objParams.xLocalPosition; // if movement, then we add the initial position of the object
                localY = objParams.yLocalPosition; // otherwise it will just use the returned value for the rotations
                localZ = objParams.zLocalPosition; // * see below...
            }
            if (moveRotation)
            {
                initialLocalRotationX = (objParams.xLocalAngle);
                initialLocalRotationY = (objParams.yLocalAngle);
                initialLocalRotationZ = (objParams.zLocalAngle);
            }

            if (x_IsSine)
                localX += sineValueX; // ...* see here, this is the clean returned value (but will have localPosition added if we are moving position)
            if (x_IsLerp)
                localX += X_Curve.Evaluate(lerpTimer);

            if (y_IsSine)
                localY += sineValueY;
            if (y_IsLerp)
                localY += Y_Curve.Evaluate(lerpTimer);

            if (z_IsSine)
                localZ += sineValueZ;
            if (z_IsLerp)
                localZ += Z_Curve.Evaluate(lerpTimer);

            if (movePosition)
                // Applying values to new position
                currentObject.transform.localPosition = new Vector3(localX, localY, localZ);


            if (moveRotation)
            {
                // Actual min-max angles, considering initial orientations of objects as well as the return offset values
                float minXAngle = initialLocalRotationX - X_amplitude + X_return_Offset;
                float maxXAngle = initialLocalRotationX + X_amplitude + X_return_Offset;
                float minYAngle = initialLocalRotationY - Y_amplitude + Y_return_Offset;
                float maxYAngle = initialLocalRotationY + Y_amplitude + Y_return_Offset;
                float minZAngle = initialLocalRotationZ - Z_amplitude + Z_return_Offset;
                float maxZAngle = initialLocalRotationZ + Z_amplitude + Z_return_Offset;

                // Using values being returned from curve calculations and normalising it (to use in Lerp)
                float xRotationNormalised = Mathf.InverseLerp(-X_amplitude + X_return_Offset, X_amplitude + X_return_Offset, localX);
                float yRotationNormalised = Mathf.InverseLerp(-Y_amplitude + Y_return_Offset, Y_amplitude + Y_return_Offset, localY);
                float zRotationNormalised = Mathf.InverseLerp(-Z_amplitude + Z_return_Offset, Z_amplitude + Z_return_Offset, localZ);

                // Lerping between min-max angles, using above normalised return values
                float xAngle = Mathf.Lerp(minXAngle, maxXAngle, xRotationNormalised);
                float yAngle = Mathf.Lerp(minYAngle, maxYAngle, yRotationNormalised);
                float zAngle = Mathf.Lerp(minZAngle, maxZAngle, zRotationNormalised);

                // Applying angles to local rotation of object
                currentObject.transform.localRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
            }
        }

        // == Multiply the step movement by step distance == //
        Vector3 stepperL_LocalPosition = stepControllerL.transform.localPosition;
        Vector3 stepperR_LocalPosition = stepControllerR.transform.localPosition;
        float stepperL_LocalZ_Position = stepperL_LocalPosition.z;
        float stepperR_LocalZ_Position = stepperR_LocalPosition.z;
        stepperL_LocalZ_Position *= stepDistance;
        stepperR_LocalZ_Position *= stepDistance;
        stepControllerL.transform.localPosition = new Vector3(stepperL_LocalPosition.x, stepperL_LocalPosition.y, stepperL_LocalZ_Position);
        stepControllerR.transform.localPosition = new Vector3(stepperR_LocalPosition.x, stepperR_LocalPosition.y, stepperR_LocalZ_Position);
        // == Multiply the arm swing movement by step distance == //
        Vector3 armswingL_LocalPosition = armControllerL.transform.localPosition;
        Vector3 armswingR_LocalPosition = armControllerR.transform.localPosition;
        float armswingL_LocalZ_Position = armswingL_LocalPosition.z;
        float armswingR_LocalZ_Position = armswingR_LocalPosition.z;
        armswingL_LocalZ_Position *= stepDistance;
        armswingR_LocalZ_Position *= stepDistance;
        armControllerL.transform.localPosition = new Vector3(armswingL_LocalPosition.x, armswingL_LocalPosition.y, armswingL_LocalZ_Position);
        armControllerR.transform.localPosition = new Vector3(armswingR_LocalPosition.x, armswingR_LocalPosition.y, armswingR_LocalZ_Position);
    }

    float SineValue(float time, float frequency, float amplitude, float phaseOffset, float returnOffset)
    {
        return Mathf.Sin((time + phaseOffset) * frequency) * amplitude + returnOffset;
    }

    IEnumerator Transition(Vector3[] initialPosition, Vector3[] targetPosition, Quaternion[] initialOrientation, Quaternion[] targetOrientation)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"Coroutine is already running.");
            yield break;
        }
        Debug.Log("=== TRANSITIONING ===");
        isTransitioning = true;

        float transitionTime = 5f;
        float elapsedTime = 0;
        float previousTimeValue = elapsedTime;

        while (elapsedTime < transitionTime)
        {
            for (int i = 0; i < moverObjects.Length; i++)
            {
                Vector3 transitionPosition = Vector3.Lerp(initialPosition[i], targetPosition[i], elapsedTime / transitionTime);
                Quaternion transitionOrientation = Quaternion.Slerp(initialOrientation[i], targetOrientation[i], elapsedTime / transitionTime);

                if (previousTimeValue != elapsedTime)
                { // this is if we just want to check one index changing each time change
                    Debug.Log($"={2}= Object: {moverObjects[2].name} | current position: {initialPosition[2]}, target position: {targetPosition[2]}\n current orientation: {initialOrientation[2]}, target orientation: {targetOrientation[2]}");
                    Debug.Log($"transition position: {transitionPosition}, transition orientation: {transitionOrientation}");
                }
                /*Debug.Log($"={2}= Object: {moverObjects[2].name} | current position: {initialPosition[2]}, target position: {targetPosition[2]}\n current orientation: {initialOrientation[2]}, target orientation: {targetOrientation[2]}");
                Debug.Log($"transition position: {transitionPosition}, transition orientation: {transitionOrientation}");*/

                // Apply the transition to the specified object
                moverObjects[i].transform.localPosition = transitionPosition;
                moverObjects[i].transform.localRotation = transitionOrientation;

                if (previousTimeValue != elapsedTime)
                {
                    Debug.Log($"Elapsed Time: {elapsedTime}");
                    Debug.Log($"Actual Object Position: {moverObjects[2].transform.localPosition}");
                }
                /*Debug.Log($"Elapsed Time: {elapsedTime}");
                Debug.Log($"Actual Object Position: {moverObjects[2].transform.localPosition}");*/

                previousTimeValue = elapsedTime; // just usin this to only check one index iteration if I need to
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Apply the final values to all objects
        for (int i = 0; i < targetPosition.Length; i++)
        {
            moverObjects[i].transform.localPosition = targetPosition[i];
            moverObjects[i].transform.localRotation = targetOrientation[i];
        }

        isTransitioning = false;
    }

}


