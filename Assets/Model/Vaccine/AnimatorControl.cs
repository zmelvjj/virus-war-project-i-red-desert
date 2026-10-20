using UnityEngine;
using System.Collections.Generic;

public class AnimatorControl : MonoBehaviour
{
    [SerializeField] private Animator anime;
    [SerializeField] private List<Transform> rotateList = new List<Transform>();
    private Quaternion[] rotateListBaseRotations;

    void Awake()
    {
        anime = GetComponent<Animator>();
        anime.applyRootMotion = true;

        rotateListBaseRotations = new Quaternion[rotateList.Count];
        for (var i = 0; i < rotateList.Count; i++)
            rotateListBaseRotations[i] = rotateList[i].localRotation;
    }


    public void GetReady()
    {
        anime.applyRootMotion = false;
        anime?.SetInteger("Random", Random.Range(0, 3));
        GetComponent<MonsterNavMeshMovement>().enabled = false;
        gameObject.transform.rotation = Quaternion.Euler(0, gameObject.transform.eulerAngles.y, 0);

        for (var i = 0; i < rotateList.Count; i++)
            rotateList[i].localRotation = rotateListBaseRotations[i];
    }

    public void Dest()
    {
        Destroy(gameObject);
    }
}
