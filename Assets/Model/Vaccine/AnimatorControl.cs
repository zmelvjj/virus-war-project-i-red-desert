using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
        if(GetComponent<MonsterNavMeshMovement>() != null)
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

            Animator anime = item.GetComponent<Animator>();
            
            anime.enabled = true;
            AnimatorStateInfo stateInfo = anime.GetCurrentAnimatorStateInfo(0);

            anime.Play(stateInfo.fullPathHash, 0, 0f);
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

    public void GetSink()
    {
        foreach (GameObject item in rigidObjects)
        {
            if(item == null) continue;
            
            Rigidbody rigid = item.GetComponent<Rigidbody>();

            if(rigid != null)
            {
                Destroy(rigid);
            }
        }

        foreach (GameObject item in rigidObjects)
        {
            if(item == null) continue;
            
            Collider collider = item.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }
        }

        StartCoroutine(Sink());
    }

    IEnumerator Sink()
    {
        float clock = 1.2f;
        Vector3 pos = transform.position;

        while(clock > 0f)
        {
            pos.y = Mathf.Lerp(-0.9f, 1.235f, clock / 1.2f);
            transform.position = pos;
            clock -= Time.deltaTime;

            yield return null;
        }

        Dest();
    }
}
