using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using static UnityEngine.Rendering.DebugUI;
using System.Collections.Generic;

public class MoverControls2 : MonoBehaviour
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

    private float moveSpeed;
    private float stepDistance;
    private float moverTime = 0f; // this represents the x value of the sine graph

    private void Start()
    {
        // Ensure that the arrays have the same length
        if (moveData.moverObjectsParameters.Length == moverObjects.Length)
        {
            for (int i = 0; i < moveData.moverObjectsParameters.Length; i++)
            {
                MoveData.ObjectParameters objParams = moveData.moverObjectsParameters[i];
                GameObject currentObject = moverObjects[i];

                Vector3 objectLocalPosition = currentObject.transform.localPosition;
                objParams.xLocalPosition = objectLocalPosition.x; // Have to also pass these values to the Move Data,
                objParams.yLocalPosition = objectLocalPosition.y; // Because the update method needs the initial position
                objParams.zLocalPosition = objectLocalPosition.z;

                // == The Orientation == //
                Quaternion objectLocalOrientation = currentObject.transform.localRotation;
                objParams.xLocalAngle = objectLocalOrientation.x; // Also passing initial angles to Move Data
                objParams.yLocalAngle = objectLocalOrientation.y;
                objParams.zLocalAngle = objectLocalOrientation.z;
            }

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
            bool x_IsLerp = objParams.x_LerpCurve;
            bool y_IsSine = objParams.y_Sine;
            bool y_IsLerp = objParams.y_LerpCurve;
            bool z_IsSine = objParams.z_Sine;
            bool z_IsLerp = objParams.z_LerpCurve;

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
}


