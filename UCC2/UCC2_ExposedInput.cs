using UnityEngine;

public class UCC2_ExposedInput : UCC2_Input
{
    [Header("Exposed Fields")]
    public Vector2 move;
    public bool jump;
    public bool run;
    public bool crouch;
    public Quaternion orientation;

    public override float HorizontalInput()    => move.x;
    public override float VerticalInput()      => move.y;
    public override bool  CrouchInput()        => crouch;
    public override bool  RunInput()           => run;
    public override bool  JumpInput()          => jump;
    public override Quaternion GetOrientation() => orientation;
}
