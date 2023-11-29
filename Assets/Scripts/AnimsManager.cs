using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimsManager : MonoBehaviour
{
    [SerializeField]
    private MoveData[] anims;
    public MoveData currentAnim;
    public MoveData previousAnim;
    private int animsIndex;
    
    private bool animsIndexChanged = false;
    [HideInInspector]
    public bool transitionTrue = false;
    public float transitionSpeed = 5f;

    void Awake()
    {
        animsIndex = 0;
        currentAnim = anims[animsIndex];
        previousAnim = currentAnim;
        animsIndexChanged = false;
    }
    void Update()
    {
        transitionTrue = false;
        bool minusAnim = Input.GetKeyDown(KeyCode.Comma);
        bool plusAnim = Input.GetKeyDown(KeyCode.Period);

        animsIndexChanged = (minusAnim || plusAnim) ? true : false;
        if (animsIndexChanged)
        {
            transitionTrue = true;
            previousAnim = currentAnim;
            animsIndexChanged = false;
        }

        animsIndex += (minusAnim) ? -1 : 0;
        animsIndex += (plusAnim) ? 1 : 0;

        if (animsIndex >= anims.Length)
            animsIndex = 0; // cycle back to start
        if (animsIndex < 0 )
            animsIndex = anims.Length - 1; // or reverse cycle to end

        currentAnim = anims[animsIndex];
    }
}
