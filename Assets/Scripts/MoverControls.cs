using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using static UnityEngine.Rendering.DebugUI;
using System.Collections.Generic;

public class MoverControls : MonoBehaviour
{
    [SerializeField] private AnimsManager animsManager;
    private MoveData moveData;
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

    private float moveSpeed;
    private float stepDistance;
    private float moverTime = 0f; // this represents the x value of the sine graph

    private void Awake()
    {
        moveData = animsManager.currentAnim;
    }
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
                objParams.xLocalAngle = currentObject.transform.localEulerAngles.x; // Also passing initial angles to Move Data
                objParams.yLocalAngle = currentObject.transform.localEulerAngles.y;
                objParams.zLocalAngle = currentObject.transform.localEulerAngles.z;
            }

            moveSpeed = moveData.moveSpeed;
            stepDistance = moveData.stepDistance;
            movingRootPosition = moveData.movingRootPosition;
            currentRootPosition = rootObject.transform.position;
            previousRootPosition = currentRootPosition;
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
        movingRootPosition = moveData.movingRootPosition;

        for (int i = 0; i < moveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters objParams = moveData.moverObjectsParameters[i];
        }
    }

    void Update()
    {
        moveData = animsManager.currentAnim;
        
        currentRootPosition = rootObject.transform.position;
        float moveAmount = Vector3.Distance(previousRootPosition, currentRootPosition); // might have to recalculate this to use vector2 (z,x)

        float stepDistanceFactor = playerController.movementInput.magnitude;
        //stepDistance = moveData.stepDistance * stepDistanceFactor;

        if(!movingRootPosition)
            moverTime += Time.deltaTime * moveSpeed; // use this if movement is NOT related to character movement in world space
        if(movingRootPosition)
            moverTime += moveAmount; // use this if movement IS related to character movement in world space

        if (moverTime >= Mathf.PI * 2) moverTime = 0f;
        float lerpTimer = Mathf.InverseLerp(0, Mathf.PI * 2, moverTime);

        for (int i = 0; i < moveData.moverObjectsParameters.Length; i++)
        {
            MoveData.ObjectParameters objParams = moveData.moverObjectsParameters[i];

            GameObject currentObject = moverObjects[i];

            bool movePosition = objParams.movePosition;
            bool moveRotation = objParams.moveRotation;
            bool x_IsSine = objParams.x_Sine;
            bool x_IsAnimCurve = objParams.x_AnimCurve;
            bool y_IsSine = objParams.y_Sine;
            bool y_IsAnimCurve = objParams.y_AnimCurve;
            bool z_IsSine = objParams.z_Sine;
            bool z_IsAnimCurve = objParams.z_AnimCurve;

            float X_frequency = objParams.X_frequency;
            float X_phaseOffset = Mathf.PI * objParams.X_phaseOffset; // using PI, so if offset is 1, the movement is exactly inverse
            float X_amplitude = objParams.X_amplitude;// * stepDistanceFactor;
            float X_return_Offset = objParams.X_returnValueOffset;
            float X_ClampMin = objParams.X_ClampMin;
            float X_ClampMax = objParams.X_ClampMax;
            AnimationCurve X_Curve = objParams.X_Curve;

            float Y_frequency = objParams.Y_frequency;
            float Y_phaseOffset = Mathf.PI * objParams.Y_phaseOffset;
            float Y_amplitude = objParams.Y_amplitude;// * stepDistanceFactor;
            float Y_return_Offset = objParams.Y_returnValueOffset;
            float Y_ClampMin = objParams.Y_ClampMin;
            float Y_ClampMax = objParams.Y_ClampMax;
            AnimationCurve Y_Curve = objParams.Y_Curve;

            float Z_frequency = objParams.Z_frequency;
            float Z_phaseOffset = Mathf.PI * objParams.Z_phaseOffset;
            float Z_amplitude = objParams.Z_amplitude;// * stepDistanceFactor;
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
            if (x_IsAnimCurve)
                localX += X_Curve.Evaluate(lerpTimer);

            if (y_IsSine)
                localY += sineValueY;
            if (y_IsAnimCurve)
                localY += Y_Curve.Evaluate(lerpTimer);

            if (z_IsSine)
                localZ += sineValueZ;
            if (z_IsAnimCurve)
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

                float xAngle = 0f, yAngle = 0f, zAngle = 0f;
                // Lerping between min-max angles, using above normalised return values
                if (x_IsSine)
                    xAngle = Mathf.Lerp(minXAngle, maxXAngle, xRotationNormalised);
                if (y_IsSine)
                    yAngle = Mathf.Lerp(minYAngle, maxYAngle, yRotationNormalised);
                if(z_IsSine)
                    zAngle = Mathf.Lerp(minZAngle, maxZAngle, zRotationNormalised);

                if (x_IsAnimCurve)
                    xAngle = localX;
                if (y_IsAnimCurve)
                    yAngle = localY;
                if (z_IsAnimCurve)
                    zAngle = localZ;

                // Applying angles to local rotation of object
                currentObject.transform.localRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
            }
        }

        // == Multiply the step movement by step distance == //
        stepControllerL.transform.localPosition += positionScale(stepControllerL, 0, 0, 1); // use 1 for axis to scale
        stepControllerR.transform.localPosition += positionScale(stepControllerR, 0, 0, stepDistanceFactor);

        // == Multiply the arm swing movement by step distance == //
        armControllerL.transform.localPosition += positionScale(armControllerL, 0, 0, 1);
        armControllerR.transform.localPosition += positionScale(armControllerR, 0, 0, 1);

        previousRootPosition = currentRootPosition;
    }

    float SineValue(float time, float frequency, float amplitude, float phaseOffset, float returnOffset)
    {
        return Mathf.Sin((time + phaseOffset) * frequency) * amplitude + returnOffset;
    }

    Vector3 positionScale(GameObject objectToScale, float xScaleFactor, float yScaleFactor, float zScaleFactor)
    {
        Vector3 objectLocalPosition = objectToScale.transform.localPosition;
        
        xScaleFactor *= stepDistance;
        yScaleFactor *= stepDistance;
        zScaleFactor *= stepDistance;

        float scaledXPosition = objectLocalPosition.x * xScaleFactor;
        float scaledYPosition = objectLocalPosition.y * yScaleFactor;
        float scaledZPosition = objectLocalPosition.z * zScaleFactor;

        Vector3 scaledPosition = new Vector3(scaledXPosition, scaledYPosition, scaledZPosition);
        return scaledPosition;
    }
}


