using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] MonsterDefinition monster;
    [SerializeField] float range = 10f;
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] int maxMonsters = 10;

    IEnumerator Start()
    {
        var wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            yield return wait;

            if (MonsterManager.Instance.ActiveCount >= maxMonsters)
                continue;

            MonsterFactory.Instance
                .Create(monster)
                .At(RandomPointInArea())
                .Build();
        }
    }

    Vector3 RandomPointInArea()
    {
        var randomPoint = Random.insideUnitCircle * range;
        return transform.position + new Vector3(randomPoint.x, 0f, randomPoint.y);
    }
}
