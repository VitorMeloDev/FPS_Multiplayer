using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

    public GameObject menuButtons,loadingScreen,creatRoom,roomPanel,errorPanel,roomBrowser,nicknamePanel;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();
    private List<TMP_Text> allPlayersNames = new List<TMP_Text>();

    public TMP_Text loadingText,roomNameText,errorText, playerNameLabel;
    public TMP_InputField roomNameImput,nicknamePlayerInput;
    public static bool hasSetNick;

    public GameObject startButton,testButton;
    public string level;
    public string[] allLevels;
    public bool changeMapBetweenRounds;
    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network";
        PhotonNetwork.ConnectUsingSettings();

        PhotonNetwork.NickName = "Player " + Random.Range(0,10000);

        #if UNITY_EDITOR
            testButton.SetActive(true);
        #endif


        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CloseMenus()
    {
        menuButtons.SetActive(false);
        loadingScreen.SetActive(false);
        creatRoom.SetActive(false);
        roomPanel.SetActive(false);
        errorPanel.SetActive(false);
        roomBrowser.SetActive(false);
        nicknamePanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        loadingText.text = "Connecting to Lobby";
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        if(!hasSetNick)
        {
            CloseMenus();
            nicknamePanel.SetActive(true);
        }
        
    }

    public void OpenCreatRoomPanel()
    {
        CloseMenus();
        creatRoom.SetActive(true);
    }

    public void CreatRoomButton()
    {
        if(!string.IsNullOrEmpty(roomNameImput.text))
        {
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameImput.text, options);
            CloseMenus();
            loadingScreen.SetActive(true);
            loadingText.text = "Creating room...";
        }
        
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomPanel.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();

        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public void ListAllPlayers()
    {
        foreach (TMP_Text player in allPlayersNames)
        {
            Destroy(player.gameObject);
        }
        allPlayersNames.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            newPlayerLabel.text = players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);

            allPlayersNames.Add(newPlayerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);

        allPlayersNames.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorPanel.SetActive(true);
        errorText.text = "Failed To Creat Room:" + message;
    }

    public void CloseErrorPanel()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Leaving Room...";
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowser.SetActive(true);
    }

    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomButton rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();
        theRoomButton.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++)
        {
            if(roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);

                allRoomButtons.Add(newButton);
            }
        }
    }

    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);

        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Joining room " + inputInfo.Name;
    }

    public void TestButton()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("TEST", options);
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Creating room...";
    }

    public void SetNicknameButton()
    {
        if(!string.IsNullOrEmpty(nicknamePlayerInput.text))
        {
            PhotonNetwork.NickName = nicknamePlayerInput.text;

            CloseMenus();
            menuButtons.SetActive(true);
            hasSetNick = true;
        }
    }

    public void QuitGameButton()
    {
        Application.Quit();
    }

    public void StartMatch()
    {
        PhotonNetwork.LoadLevel(allLevels[Random.Range(0, allLevels.Length)]);
    }
}
