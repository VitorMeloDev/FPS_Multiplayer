using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    public List<Leaderboard> lboardPlayers = new List<Leaderboard>();
    public int index;
    public int killsForWin = 3;
    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitingAfterEnding = 5f;
    public bool perpetual;
    public float timeMatch;
    private float currentTimeMatch;
    private float sendTimer;

    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpadateStat,
        NextMatch,
        TimerSync
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }



    void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            state = GameState.Playing;
            SetupTime();
        }

        if(!PhotonNetwork.IsMasterClient)
        UI_Controller.instance.timerTxt.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
        {
            if(UI_Controller.instance.leaderboard.activeInHierarchy)
            {
                UI_Controller.instance.leaderboard.gameObject.SetActive(false);
            }
            else
            {
                ShowLeaderBoard();
            }
        }

        if(PhotonNetwork.IsMasterClient)
        {
            
            if(currentTimeMatch > 0f && state == GameState.Playing)
            {
                currentTimeMatch -= Time.deltaTime;

                if(currentTimeMatch <= 0f)
                {
                    currentTimeMatch = 0f;
                    state = GameState.Ending;

                    if(PhotonNetwork.IsMasterClient)
                    {
                        ListPlayersSend();
                        StateCheck();
                    }
                }
                UpdateTimerDisplay();

                sendTimer -= Time.deltaTime;
                if(sendTimer <= 0)
                {
                    sendTimer += 1f;
                    TimerSend();
                }
            }
        }

       
    }

    public void OnEvent(EventData photonEvent)
    {
        if(photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;
            
            // Debug.Log("Evento " + theEvent + " chamado");
            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                break;

                case EventCodes.ListPlayers:
                    ListPlayersReceive(data);
                break;

                case EventCodes.UpadateStat:
                    UpadateStatReceive(data);
                break;
                 
                case EventCodes.NextMatch:
                    NextMatchReceive();
                break;
                
                case EventCodes.TimerSync:
                    TimerReceive(data);
                break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.MasterClient},
            new SendOptions {Reliability = true}
        );

    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);

        allPlayers.Add(player);
        ListPlayersSend();
    }

     public void ListPlayersSend()
    {
        object[] package = new object[allPlayers.Count + 1];

        package[0] = state;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4];

            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }

    public void ListPlayersReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        state = (GameState)dataReceived[0];

        for(int i = 1; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
            );

            allPlayers.Add(player);

            if(PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

        StateCheck();
    }

    public void UpadateStatSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[]{actorSending,statToUpdate,amountToChange};

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpadateStat,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }  

    public void UpadateStatReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int stat = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for(int i = 0; i < allPlayers.Count; i++)
        {
            if(allPlayers[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: //kills
                        allPlayers[i].kills += amount;
                        Debug.Log(allPlayers[i].name + " : kills " +allPlayers[i].kills);
                    break;
                    
                    case 1: //death
                        allPlayers[i].deaths += amount;
                        Debug.Log(allPlayers[i].name + " : deaths " +allPlayers[i].deaths);
                    break;
                }
                if(i == index)
                {
                    UpdateUI();
                }

                if(UI_Controller.instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderBoard();
                }
                break;
            }
        }

        ScoreCheck();
    }

    public void TimerSend()
    {
        object[] package = new object[] {(int) currentTimeMatch, state};

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }
    
    public void TimerReceive(object[] dataReceived)
    {
        currentTimeMatch = (int)dataReceived[0];
        state = (GameState)dataReceived[1];

        UI_Controller.instance.timerTxt.gameObject.SetActive(true);


        UpdateTimerDisplay();
    }

    public void UpdateUI()
    {
        if(allPlayers.Count > index)
        {
            UI_Controller.instance.killsText.text = "KILLS: " + allPlayers[index].kills;
            UI_Controller.instance.deathsText.text = "DEATHS: " + allPlayers[index].deaths;
        }else
        {
            UI_Controller.instance.killsText.text = "KILLS: 0"; 
            UI_Controller.instance.deathsText.text = "DEATHS: 0";
        }
    }


    void ShowLeaderBoard()
    {
        UI_Controller.instance.leaderboard.SetActive(true);

        foreach (Leaderboard lb in lboardPlayers)
        {
            Destroy(lb.gameObject);
        }
        lboardPlayers.Clear();

        UI_Controller.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayer(allPlayers);

        foreach (PlayerInfo player in sorted)
        {
            Leaderboard newPlayerDisplay = Instantiate(UI_Controller.instance.leaderboardPlayerDisplay, UI_Controller.instance.leaderboardPlayerDisplay.transform.parent);
        
            newPlayerDisplay.SetDatails(player.name, player.kills, player.deaths);

            newPlayerDisplay.gameObject.SetActive(true);

            lboardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayer(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while(sorted.Count < allPlayers.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo player in players)
            {
                if(!sorted.Contains(player))
                {
                    if(player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }
            sorted.Add(selectedPlayer);
        }

        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in allPlayers)
        {
            if(player.kills >= killsForWin && killsForWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if(winnerFound)
        {
            if(PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayersSend();
            }
        }
    }
    void StateCheck()
    {
        if(state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        state = GameState.Ending;

        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UI_Controller.instance.endScreen.SetActive(true);
        ShowLeaderBoard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.transform.position;
        Camera.main.transform.rotation = mapCamPoint.transform.rotation;

        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitingAfterEnding);

        if(!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if(PhotonNetwork.IsMasterClient)
            {
                if(!Launcher.instance.changeMapBetweenRounds)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, Launcher.instance.allLevels.Length);

                    if(Launcher.instance.allLevels[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.allLevels[newLevel]);
                    }
                }
            }
        }
        
    }

    public void NextMatchSend()
    {
       PhotonNetwork.RaiseEvent(
        (byte)EventCodes.NextMatch,
        null,
        new RaiseEventOptions{ Receivers = ReceiverGroup.All},
        new SendOptions {Reliability = true}
        );
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;

        UI_Controller.instance.endScreen.SetActive(false);
        UI_Controller.instance.leaderboard.SetActive(false);

        foreach (PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UpdateUI();

        PlayerSpawner.instance.SpawPlayer();
        SetupTime();
    }

    public void SetupTime()
    {
        if(timeMatch > 0)
        {
            currentTimeMatch = timeMatch;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentTimeMatch);

        UI_Controller.instance.timerTxt.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string _name, int _actor,int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}
