using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;

public class RoomButton : MonoBehaviour
{
    public TMP_Text roomNameText;

    private RoomInfo info;
   
    public void SetButtonDetails(RoomInfo inputInfo)
    {
        info = inputInfo;
        roomNameText.text = info.Name;
    }

    public void JoinRoomButton()
    {
        Launcher.instance.JoinRoom(info);
    }
}
