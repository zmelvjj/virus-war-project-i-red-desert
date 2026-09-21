using UnityEngine;

public class MonsterFactory : MonoBehaviour
{
    public static MonsterFactory Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public MonsterBuilder Create(MonsterDefinition definition)
    {
        return new MonsterBuilder(definition);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
