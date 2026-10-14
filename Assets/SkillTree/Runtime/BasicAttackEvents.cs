using System;
using UnityEngine;

public readonly struct BasicAttackHit
{
    public readonly MonsterEntity Target;
    public readonly Vector3 Position;
    public readonly int Damage;

    public BasicAttackHit(MonsterEntity target, Vector3 position, int damage)
    {
        Target = target;
        Position = position;
        Damage = damage;
    }
}

public static class BasicAttackEvents
{
    public static event Action<BasicAttackHit> Hit;

    public static void Raise(BasicAttackHit hit)
    {
        Hit?.Invoke(hit);
    }
}
