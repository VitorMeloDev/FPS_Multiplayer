using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public bool isAutomatic;
    public float timeBetweenShots = .1f, heatPerShot = 1f;
    public GameObject muzzle;
    public int gunDamage;
    public float adsValue;
    public AudioSource shotSound;

}
