using UnityEngine;

public class MoverControls : MonoBehaviour
{
    [System.Serializable]
    public class ObjectParameters
    {
        public string objectName;
        public GameObject gameObject;

        public bool movePosition = false;
        public bool moveRotation = false;

        public bool x_IsSineMovement = false;
        public bool x_IsCosineMovement = false;
        public bool x_IsLerpCurveMovement = false;
        public bool y_IsSineMovement = false;
        public bool y_IsCosineMovement = false;
        public bool y_IsLerpCurveMovement = false;
        public bool z_IsSineMovement = false;
        public bool z_IsCosineMovement = false;
        public bool z_IsLerpCurveMovement = false;

        public float X_frequency; // X here refers to local x position of object
        public float X_amplitude;
        public float X_phaseOffset;
        public float X_returnValueOffset;
        public float X_ClampMin = -1;
        public float X_ClampMax = 1;
        public AnimationCurve X_Curve;

        public float Y_frequency; // Y here refers to local x position of object
        public float Y_amplitude;
        public float Y_phaseOffset;
        public float Y_returnValueOffset;
        public float Y_ClampMin = -1;
        public float Y_ClampMax = 1;
        public AnimationCurve Y_Curve;

        public float Z_frequency; // Z here refers to local x position of object
        public float Z_amplitude;
        public float Z_phaseOffset;
        public float Z_returnValueOffset;
        public float Z_ClampMin = -1;
        public float Z_ClampMax = 1;
        public AnimationCurve Z_Curve;

        [HideInInspector] public float xPosition;
        [HideInInspector] public float yPosition;
        [HideInInspector] public float zPosition;

        [HideInInspector] public float xRotation;
        [HideInInspector] public float yRotation;
        [HideInInspector] public float zRotation;
    }

    [SerializeField] private float moveSpeed;
    public ObjectParameters[] moverObjectsParameters;
    private float moverTime = 0f; // this represents the x value of the sine graph

    private void Start()
    {
        foreach (ObjectParameters objParams in moverObjectsParameters)
        {
            GameObject currentObject = objParams.gameObject;
            objParams.xPosition = currentObject.transform.localPosition.x;
            objParams.yPosition = currentObject.transform.localPosition.y;
            objParams.zPosition = currentObject.transform.localPosition.z;

            objParams.xRotation = currentObject.transform.localRotation.x;
            objParams.yRotation = currentObject.transform.localRotation.y;
            objParams.zRotation = currentObject.transform.localRotation.z;
        }
    }

    void Update()
    {
        moverTime += Time.deltaTime * moveSpeed;
        if (moverTime >= Mathf.PI * 2) moverTime = 0f;
        float lerpTimer = Mathf.InverseLerp(0, Mathf.PI * 2, moverTime);
        
        foreach (ObjectParameters objParams in moverObjectsParameters)
        {
            GameObject currentObject = objParams.gameObject;

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
                localX = objParams.xPosition;
                localY = objParams.yPosition;
                localZ = objParams.zPosition;
            }
            if (moveRotation)
            {
                localX = (objParams.xRotation) / 180;
                localY = (objParams.yRotation) / 180;
                localZ = (objParams.zRotation) / 180;
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

