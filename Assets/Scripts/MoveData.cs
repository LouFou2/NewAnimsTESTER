using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "MoveData", menuName = "someMoveData")]
public class MoveData : ScriptableObject
{
    [HideInInspector] public UnityEvent updated;
    [System.Serializable]
    public class ObjectParameters
    {
        public string objectName;

        public bool movePosition = false;
        public bool moveRotation = false;

        public bool x_Sine = false;
        public bool x_AnimCurve = false;
        public bool y_Sine = false;
        public bool y_AnimCurve = false;
        public bool z_Sine = false;
        public bool z_AnimCurve = false;

        public float X_frequency; // X here refers to local x position of object
        public float X_amplitude;
        public float X_phaseOffset;
        public float X_returnValueOffset;
        public float X_ClampMin;
        public float X_ClampMax;
        public AnimationCurve X_Curve;

        public float Y_frequency; // Y here refers to local x position of object
        public float Y_amplitude;
        public float Y_phaseOffset;
        public float Y_returnValueOffset;
        public float Y_ClampMin;
        public float Y_ClampMax;
        public AnimationCurve Y_Curve;

        public float Z_frequency; // Z here refers to local x position of object
        public float Z_amplitude;
        public float Z_phaseOffset;
        public float Z_returnValueOffset;
        public float Z_ClampMin;
        public float Z_ClampMax;
        public AnimationCurve Z_Curve;

        [HideInInspector] public float xLocalPosition;
        [HideInInspector] public float yLocalPosition;
        [HideInInspector] public float zLocalPosition;

        [HideInInspector] public float xLocalAngle;
        [HideInInspector] public float yLocalAngle;
        [HideInInspector] public float zLocalAngle;
    }
    public float transitionDuration = 0.1f;
    public float moveSpeed = 1f;
    public float stepDistance = 1f;

    public bool movingRootPosition = false; //*** could probably remove this later (useful for making anims)

    [Header("LegIK Targets Z position Amplitude should be 1 unit\n 'Amplitude' is set by Step Distance")]
    //[Header("'Amplitude' is set by Step Distance")]
    public ObjectParameters[] moverObjectsParameters;

    private void OnEnable()
    {
        // called when the instance is setup
        if (updated == null)
            updated = new UnityEvent();
    }

    private void OnValidate()
    {
        // called when any value is changed in the inspector
        updated.Invoke();
    }
}
