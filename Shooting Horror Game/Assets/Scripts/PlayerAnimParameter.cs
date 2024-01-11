using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerAnimParameter
{
    public static readonly int Move = Animator.StringToHash("Move");
    public static readonly int Sprint = Animator.StringToHash("Sprint");
    public static readonly int Aim = Animator.StringToHash("Aim");
    public static readonly int Shot = Animator.StringToHash("Shot");
}