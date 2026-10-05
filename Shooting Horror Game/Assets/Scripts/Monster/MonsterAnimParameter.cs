using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MonsterAnimParameter
{
    public static readonly int Idle = Animator.StringToHash("Idle");
    public static readonly int Move = Animator.StringToHash("Move");
    public static readonly int Rage = Animator.StringToHash("Rage");
    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int GetHit = Animator.StringToHash("GetHit");
    public static readonly int Stun = Animator.StringToHash("Stun");
    public static readonly int Execution = Animator.StringToHash("Execution");
}