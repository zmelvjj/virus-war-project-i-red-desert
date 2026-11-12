using UnityEngine;
using System.Collections.Generic;

public class AnimatorControl : MonoBehaviour
{
    [SerializeField] private Animator anime;
    [SerializeField] private List<GameObject> rigidObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> animeObjects = new List<GameObject>();

    void Awake()
    {
        anime = GetComponent<Animator>();
        anime.applyRootMotion = true;
    }


    public void GetReady()
    {
        anime.applyRootMotion = false;
        GetComponent<MonsterNavMeshMovement>().enabled = false;

        foreach (GameObject item in rigidObjects)
        {
            if(item == null) continue;
            
            item.AddComponent<Rigidbody>();
        }

        foreach (GameObject item in animeObjects)
        {
            if(item == null || item.GetComponent<Animator>() == null) continue;
            
            item.GetComponent<Animator>().enabled = false;
        }
    }

    public void AnimeOn()
    {
        foreach (GameObject item in animeObjects)
        {
            if(item == null || item.GetComponent<Animator>() == null) continue;
            
            item.GetComponent<Animator>().enabled = true;
        }
    }

    public void AnimeOff()
    {
        foreach (GameObject item in animeObjects)
        {
            if(item == null || item.GetComponent<Animator>() == null) continue;
            
            item.GetComponent<Animator>().enabled = false;
        }
    }

    public void Dest()
    {
        Destroy(gameObject);
    }
}
