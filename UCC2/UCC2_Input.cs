
using UnityEngine;

public class UCC2_Input : MonoBehaviour
{
    public virtual float HorizontalInput()    { return 0f; }
    public virtual float VerticalInput()      { return 0f; }
    public virtual bool  CrouchInput()        { return false; }
    public virtual bool  RunInput()           { return false; }
    public virtual bool  JumpInput()          { return false; }
    public virtual Quaternion GetOrientation() { return Quaternion.identity; }
}
