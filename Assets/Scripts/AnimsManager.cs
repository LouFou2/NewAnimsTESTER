using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimsManager : MonoBehaviour
{
    [SerializeField]
    private MoveData[] anims;
    public MoveData currentAnim;
    private int animsIndex;

    void Awake()
    {
        animsIndex = 0;
        currentAnim = anims[animsIndex];
    }
    void Update()
    {
        bool previousAnim = Input.GetKeyDown(KeyCode.Comma);
        bool nextAnim = Input.GetKeyDown(KeyCode.Period);

        animsIndex += (previousAnim) ? -1 : 0;
        animsIndex += (nextAnim) ? 1 : 0;

        if (animsIndex >= anims.Length)
            animsIndex = 0; // cycle back to start
        if (animsIndex < 0 )
            animsIndex = anims.Length - 1; // or reverse cycle to end

        currentAnim = anims[animsIndex];

    }
}
