using UnityEngine;
using UnityEngine.Rendering;

public class MoverControls2 : MonoBehaviour
{
    [SerializeField] private MoveData moveData;

    [SerializeField] private GameObject rootObject;
    [SerializeField] private bool freezeRootPosition = false;

    // == Have to add the objects that control the legs == //
    [SerializeField] private GameObject stepControllerL;
    [SerializeField] private GameObject stepControllerR;

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

                objParams.xLocalPosition = currentObject.transform.localPosition.x;
                objParams.yLocalPosition = currentObject.transform.localPosition.y;
                objParams.zLocalPosition = currentObject.transform.localPosition.z;

                objParams.xLocalRotation = currentObject.transform.localRotation.x;
                objParams.yLocalRotation = currentObject.transform.localRotation.y;
                objParams.zLocalRotation = currentObject.transform.localRotation.z;
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

            bool x_IsSine = objParams.x_IsSineMovement;
            bool x_IsCosine = objParams.x_IsCosineMovement;
            bool x_IsLerp = objParams.x_IsLerpCurveMovement;
            bool y_IsSine = objParams.y_IsSineMovement;
            bool y_IsCosine = objParams.y_IsCosineMovement;
            bool y_IsLerp = objParams.y_IsLerpCurveMovement;
            bool z_IsSine = objParams.z_IsSineMovement;
            bool z_IsCosine = objParams.z_IsCosineMovement;
            bool z_IsLerp = objParams.z_IsLerpCurveMovement;

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

            float cosineValueX = CosineValue(moverTime, X_frequency, X_amplitude, X_phaseOffset, X_return_Offset);
            float cosineValueY = CosineValue(moverTime, Y_frequency, Y_amplitude, Y_phaseOffset, Y_return_Offset);
            float cosineValueZ = CosineValue(moverTime, Z_frequency, Z_amplitude, Z_phaseOffset, Z_return_Offset);

            if (cosineValueX < X_ClampMin) cosineValueX = X_ClampMin;
            if (cosineValueX > X_ClampMax) cosineValueX = X_ClampMax;

            if (cosineValueY < Y_ClampMin) cosineValueY = Y_ClampMin;
            if (cosineValueY > Y_ClampMax) cosineValueY = Y_ClampMax;

            if (cosineValueZ < Z_ClampMin) cosineValueZ = Z_ClampMin;
            if (cosineValueZ > Z_ClampMax) cosineValueZ = Z_ClampMax;

            float localX = 0;
            float localY = 0;
            float localZ = 0;

            if (movePosition)
            {
                localX = objParams.xLocalPosition;
                localY = objParams.yLocalPosition;
                localZ = objParams.zLocalPosition;
            }
            if (moveRotation)
            {
                localX = (objParams.xLocalRotation) / 180;
                localY = (objParams.yLocalRotation) / 180;
                localZ = (objParams.zLocalRotation) / 180;
            }

            if (x_IsSine)
                localX += sineValueX;
            if (x_IsCosine)
                localX += cosineValueX;
            if (x_IsLerp)
                localX += Mathf.Lerp(X_ClampMin, X_ClampMax, X_Curve.Evaluate(lerpTimer));

            if (y_IsSine)
                localY += sineValueY;
            if (y_IsCosine)
                localY += cosineValueY;
            if (y_IsLerp)
                localY += Mathf.Lerp(Y_ClampMin, Y_ClampMax, Y_Curve.Evaluate(lerpTimer));

            if (z_IsSine)
                localZ += sineValueZ;
            if (z_IsCosine)
                localZ += cosineValueZ;
            if (z_IsLerp)
                localZ += Mathf.Lerp(Z_ClampMin, Z_ClampMax, Z_Curve.Evaluate(lerpTimer));

            if (movePosition)
                currentObject.transform.localPosition = new Vector3(localX, localY, localZ);

            if (moveRotation)
            {
                localX *= 180;
                localY *= 180;
                localZ *= 180;
                currentObject.transform.localRotation = Quaternion.Euler(localX, localY, localZ);
            }
        }

        // == Multiply the step movement by step distance == //
        Vector3 stepperL_LocalPosition = stepControllerL.transform.localPosition;
        Vector3 stepperR_LocalPosition = stepControllerR.transform.localPosition;
        float stepperL_LocalZ_Position = stepperL_LocalPosition.z;
        float stepperR_LocalZ_Position = stepperR_LocalPosition.z;
        stepperL_LocalZ_Position *= stepDistance;
        stepperR_LocalZ_Position *= stepDistance;
        stepperL_LocalZ_Position += stepDistance / 2; // have to shift position forward otherwise root is moving along with front foot
        stepperR_LocalZ_Position += stepDistance / 2;
        stepControllerL.transform.localPosition = new Vector3(stepperL_LocalPosition.x, stepperL_LocalPosition.y, stepperL_LocalZ_Position);
        stepControllerR.transform.localPosition = new Vector3(stepperR_LocalPosition.x, stepperR_LocalPosition.y, stepperR_LocalZ_Position);
    }

    float SineValue(float X, float frequency, float amplitude, float phaseOffset, float Y_Offset) // X and Y represents values on sine graph, not to be confused with object space
    {
        return Mathf.Sin((X + phaseOffset) * frequency) * amplitude + Y_Offset;
    }
    float CosineValue(float X, float frequency, float amplitude, float phaseOffset, float Y_Offset) // X and Y represents values on sine graph, not to be confused with object space
    {
        return Mathf.Cos((X + phaseOffset) * frequency) * amplitude + Y_Offset;
    }
}


