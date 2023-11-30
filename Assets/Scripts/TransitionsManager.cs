using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "TransitionsManager", menuName = "transitionsData")]
public class TransitionsManager : ScriptableObject
{
    [HideInInspector] public UnityEvent updated;
    [System.Serializable]
    public class AnimTransitionInfo
    {
        public MoveData thisAnim;
        public MoveData transitionFromAnim;
        public MoveData transitionToAnim;

        [Header("Transition From Parameter:")]
        public bool transitionFromUseBool = false;
        public bool transitionFromUseInt = false;
        public bool transitionFromUseFloat = false;
        [Header("Transition To Parameter:")]
        public bool transitionToUseBool = false;
        public bool transitionToUseInt = false;
        public bool transitionToUseFloat = false;
    }
    
    public AnimTransitionInfo[] animTransitionInfo;

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
