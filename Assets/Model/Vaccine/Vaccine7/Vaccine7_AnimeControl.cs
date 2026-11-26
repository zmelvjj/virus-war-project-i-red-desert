using UnityEngine;

public class Vaccine7_AnimeControl : MonoBehaviour
{
    [SerializeField] private Animator anime;
    MonsterEntity entity;
    float HP_Ratio = 1f;

    void Start()
    {
        entity = GetComponent<MonsterEntity>();
    }

    void Update()
    {
        if(entity == null) entity = GetComponent<MonsterEntity>();

        if(entity == null) return;
        
        HP_Ratio = entity.CurrentHp / entity.MaxHp;
        anime.SetLayerWeight(1, (1 - HP_Ratio) * 1.25f);
    }
}
