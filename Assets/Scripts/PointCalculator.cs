using UnityEngine;

public class PointCalculator : MonoBehaviour
{
    // == Script to solve a stretching limb == //
    // calculates points between source and target objects

    [SerializeField] private GameObject _parentObject; // script is attached to Source Object
    [SerializeField] private GameObject _stretchToObject;
    [SerializeField] private GameObject[] _childrenPoints;
    private int _pointsAmount;
    private Vector3 _startPosition;
    private Vector3 _endPosition;


    void Start()
    {
        _pointsAmount = _childrenPoints.Length;
        CalculatePointsPositions();
    }

    private void Update()
    {
        _startPosition = _parentObject.transform.position;
        _endPosition = _stretchToObject.transform.position;
        CalculatePointsPositions();
        OrientChildObjects();
    }

    void CalculatePointsPositions()
    {
        // calculate positions for _childrenPoints, equally spaced between startPosition and endPosition

        float _parentBoneLength = Vector3.Distance(_startPosition, _endPosition);
        Vector3 _normalizedDirection = (_endPosition - _startPosition).normalized;
        float _subdivisionDistance = _parentBoneLength / _pointsAmount;

        for (int i = 0; i < _childrenPoints.Length; i++)
        {
            float t = i * _subdivisionDistance;
            Vector3 pointPosition = _startPosition + ( _normalizedDirection * t );
            _childrenPoints[i].transform.position = pointPosition; // Assign the calculated position to the betweenPoint
        }
    }

    void OrientChildObjects() 
    {
        // == Orient each child object so y-up axis "looks at" next child object == //
        for (int i = _childrenPoints.Length - 1; i >= 0; i--) 
        {
            Vector3 _normalizedDirection = (_endPosition - _startPosition).normalized;
            Quaternion boneTargetRotation = Quaternion.FromToRotation(_childrenPoints[i].transform.up, _normalizedDirection) * _childrenPoints[i].transform.rotation;

            // Rotate bone
            _childrenPoints[i].transform.rotation = boneTargetRotation;
        }
    }
}

