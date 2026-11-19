using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] Transform spawnArea;
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

            if (!StageManager.HasInstance
                || !StageManager.Instance.TrySelectMonster(out var definition))
            {
                continue;
            }

            var spawnPosition = RandomPointInArea();
            MonsterFactory.Instance
                .Create(definition)
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
