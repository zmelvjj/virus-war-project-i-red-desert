using UnityEngine;

public class ParticleTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem particle;

    public void PlayParticle()
    {
        particle.Emit(60);
    }
}