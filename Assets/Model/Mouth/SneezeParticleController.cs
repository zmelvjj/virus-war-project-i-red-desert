using UnityEngine;

public class SneezeParticleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem particle_Spit;
    [SerializeField] private ParticleSystem particle_Mist;
    [SerializeField] private Transform range;

    public void Fire()
    {
        SetSpitVelocity();
        SetMistVelocity();

        particle_Spit.Emit(200);
        particle_Mist.Emit(25);
    }

    void SetSpitVelocity()
    {
        if (particle_Spit == null || range == null)
            return;

        // 공격 범위의 Z축 크기
        float rangeX = range.localScale.x;
        float rangeZ = range.localScale.z;
        
        // 파티클 수명
        var main = particle_Spit.main;
        float lifetime = main.startLifetime.constant;

        if (lifetime <= 0f)
            return;

        // 범위 끝까지 도달하기 위한 속도
        float speed = rangeZ / lifetime;

        // Y_Angle을 기준으로 월드 방향 계산
        Vector3 worldDirection = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;

        Vector3 worldVelocity = worldDirection * speed;

        // 월드 좌표계 속도 적용
        var velocity = particle_Spit.velocityOverLifetime;

        Vector3 maxVelocity = new Vector3(rangeX * 0.2f, 1f, rangeZ * 1.6f);
        Vector3 minVelocity = new Vector3(-maxVelocity.x, -0.2f, maxVelocity.z * 0.25f);

        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        minVelocity = rotation * minVelocity;
        maxVelocity = rotation * maxVelocity;

        velocity.x = new ParticleSystem.MinMaxCurve(minVelocity.x, maxVelocity.x);
        velocity.z = new ParticleSystem.MinMaxCurve(minVelocity.z, maxVelocity.z);
    }

    void SetMistVelocity()
    {
        if (particle_Mist == null || range == null)
            return;

        // 공격 범위의 Z축 크기
        float rangeZ = range.localScale.z;

        // 파티클 수명
        var main = particle_Mist.main;
        float lifetime = main.startLifetime.constant;

        if (lifetime <= 0f)
            return;

        // 범위 끝까지 도달하기 위한 속도
        float speed = rangeZ / lifetime;

        // Y_Angle을 기준으로 월드 방향 계산
        Vector3 worldDirection =
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;

        Vector3 worldVelocity = worldDirection * speed;

        // 월드 좌표계 속도 적용
        var velocity = particle_Mist.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        velocity.x = new ParticleSystem.MinMaxCurve(worldVelocity.x * 0.25f, worldVelocity.x);
        velocity.z = new ParticleSystem.MinMaxCurve(worldVelocity.z * 0.1f, worldVelocity.z * 0.6f);
    }
}
