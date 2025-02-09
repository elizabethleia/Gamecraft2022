using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerStates;
public class WindDash : BaseSecondaryAbility
{
    public override BaseSecondary SecondaryState() => new WindDashState(player);
    public float dashSpeed;
    public float dashDur;
}
