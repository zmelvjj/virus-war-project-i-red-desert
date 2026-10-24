using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem particle;

    public void Chipped()
    {
        particle.Emit(5);
    }

    public void Damaged()
    {
        particle.Emit(20);
    }

    public void Break()
    {
        particle.Emit(60);
    }
}