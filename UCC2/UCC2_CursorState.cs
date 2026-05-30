using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UCC2_CursorState : MonoBehaviour
{
    public bool isLocked = true;

    void Update()
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }
}
