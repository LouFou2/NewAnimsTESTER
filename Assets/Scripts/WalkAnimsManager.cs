using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkAnimsManager : MonoBehaviour
{
    [Header("Anims go in order: idle, walk, jog, run")]
    [SerializeField]
    private MoveData[] anims;

    [SerializeField]
    private PlayerController playerController;

    private int animsIndex;

    public MoveData idleAnim;
    public MoveData walkAnim;
    public MoveData jogAnim;
    //public MoveData runAnim;

    public float walkThreshold = 0.1f; //change to appropriate value

    public MoveData previousAnim;
    public MoveData nextAnim;

    private bool animsIndexChanged = false;
    [HideInInspector]
    public bool transitionTrue = false;

    void Awake()
    {
        animsIndex = 0;
        idleAnim = anims[0];
        walkAnim = anims[1];
        jogAnim = anims[2];
        //runAnim = anims[3];

        previousAnim = anims[0];
        nextAnim = anims[1];

        animsIndexChanged = false;
    }
    void Update()
    {
        //AnimCycleCheck(); // *** this one is only used for testing, disable before build
        InputTransitionsCheck();
    }
    void AnimCycleCheck() {

        transitionTrue = false;
        bool minusAnim = Input.GetKeyDown(KeyCode.Comma);
        bool plusAnim = Input.GetKeyDown(KeyCode.Period);

        animsIndexChanged = (minusAnim || plusAnim) ? true : false;

        if (animsIndexChanged)
        {
            transitionTrue = true;
            previousAnim = nextAnim;
            animsIndexChanged = false;
        }

        animsIndex += (minusAnim) ? -1 : 0;
        animsIndex += (plusAnim) ? 1 : 0;

        if (animsIndex >= anims.Length)
            animsIndex = 0; // cycle back to start
        if (animsIndex < 0)
            animsIndex = anims.Length - 1; // or reverse cycle to end

        nextAnim = anims[animsIndex];
    }
    void InputTransitionsCheck() 
    {
        float moveSpeed = playerController.movementInput.magnitude;
        
        if (moveSpeed < walkThreshold)
        {
            previousAnim = idleAnim;
            nextAnim = walkAnim;
        }

        if (moveSpeed >= walkThreshold) 
        {
            previousAnim = walkAnim;
            nextAnim = jogAnim;
        }
    }
}
