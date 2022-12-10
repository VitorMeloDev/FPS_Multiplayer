using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;
    public GameObject playerPrefab, deathEffect;
    private GameObject player;

    public float timeRespaw = 5f;
    void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        if(PhotonNetwork.IsConnected)
        {
            SpawPlayer();
        }
    }

    public void SpawPlayer()
    {
        Transform spawPosition = SpawManager.instance.GetSpawPoint();

        player = PhotonNetwork.Instantiate(playerPrefab.name, spawPosition.position, spawPosition.rotation);
    }

    public void Die(string damager)
    {
        UI_Controller.instance.deathMessage.text = "You were killed by " + damager;
        MatchManager.instance.UpadateStatSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
        UI_Controller.instance.leaderboard.gameObject.SetActive(false);

        if(player != null)
        {
            StartCoroutine(DieCo());
        }
        
    }

    public IEnumerator DieCo()
    {
        UI_Controller.instance.deathScreen.SetActive(true);
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        player = null;
        yield return new WaitForSeconds(timeRespaw);

        UI_Controller.instance.deathScreen.SetActive(false);
        
        if(MatchManager.instance.state == MatchManager.GameState.Playing && player == null)
        {
            SpawPlayer();
        }

    }
}
