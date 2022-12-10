using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawManager : MonoBehaviour
{
    public static SpawManager instance;
    public Transform[] spawPoints;
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        foreach (Transform spaw in spawPoints)
        {
            spaw.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Transform GetSpawPoint()
    {
        return spawPoints[Random.Range(0, spawPoints.Length)];
    }
}
