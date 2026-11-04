using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] Transform spawnArea;
    [SerializeField] List<MonsterDefinition> monsters = new List<MonsterDefinition>();
    [SerializeField] float range = 10f;
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] int maxMonsters = 10;

    IEnumerator Start()
    {
        var wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            yield return wait;

            if (!GameFlowManager.IsMainGame)
                continue;

            if (MonsterManager.Instance.ActiveCount >= maxMonsters)
                continue;

            var spawnPosition = RandomPointInArea();
            MonsterFactory.Instance
                .Create(monsters[Random.Range(0, monsters.Count)])
                .At(spawnPosition)
                .Within(transform.position, range)
                .Build()
                .transform.parent = spawnArea;
        }
    }

    Vector3 RandomPointInArea()
    {
        var randomPoint = Random.insideUnitCircle * range;
        return transform.position + new Vector3(randomPoint.x, 0f, randomPoint.y);
    }
}
