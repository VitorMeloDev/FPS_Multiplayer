using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
public class UI_Controller : MonoBehaviour
{
    public static UI_Controller instance;
    public TMP_Text overheatedMessage, deathMessage;
    public Slider weaponTemp;
    public GameObject deathScreen;
    public Slider healthSlider;
    public TMP_Text killsText, deathsText, timerTxt;
    public GameObject leaderboard;
    public Leaderboard leaderboardPlayerDisplay;
    public GameObject endScreen;
    public GameObject pauseScreen;
    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            PauseMenuOnOff();
        }

        if(pauseScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void PauseMenuOnOff()
    {
        if(!pauseScreen.activeInHierarchy)
        {
            pauseScreen.SetActive(true);
        }
        else
        {
            pauseScreen.SetActive(false);
        }
    }

    public void MenuBack()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
