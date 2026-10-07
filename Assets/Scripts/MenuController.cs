using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviourPunCallbacks
{
    public TMP_InputField playerName;
    public TMP_Text status;
    public Button start_button;

    private const int MAX_PLAYER = 16;

    private MenuController instance = null;

    private string[] nameGenerated = { "Aspect","Kraken","Bender", "Lynch", "Big Papa",
        "Mad Dog", "Bowser", "O'Doyle", "Bruise", "Psycho",
        "Cannon", "Ranger", "Clink", "Ratchet", "Cobra",
        "Reaper", "Colt", "Rigs", "Crank", "Ripley",
        "Creep", "Roadkill", "Daemon", "Ronin", "Decay",
        "Rubble", "Diablo", "Sasquatch", "Doom", "Scar" 
    };
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Random.InitState(System.DateTime.Now.Millisecond);
        playerName.text = nameGenerated[Random.Range(0, nameGenerated.Length)];
    }

    private void Update()
    {
        
    }

    private void Start()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        start_button.interactable = true;
    }

    public void GameStart()
    {
        Debug.Log($"Current player in some rooms {PhotonNetwork.CountOfPlayersInRooms}");
        if (PhotonNetwork.CountOfPlayersInRooms >= MAX_PLAYER)
        {
            status.text = "server full";
            return;
        }

        PhotonNetwork.NickName = $"{playerName.text} [{Random.Range(0, 10)}{Random.Range(0, 10)}{Random.Range(0, 10)}{Random.Range(0, 10)}]";
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        var roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = MAX_PLAYER;
        PhotonNetwork.JoinOrCreateRoom("Shampoo", roomOptions, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Created Room");
        PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
        base.OnCreatedRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        Debug.Log("[PUN] joined room " + PhotonNetwork.CurrentRoom);
        Debug.Log("[PUN] All rooms player count :" + PhotonNetwork.CountOfPlayersInRooms);
        Debug.Log("[PUN] CurrentRoom room player count :" + PhotonNetwork.CurrentRoom.PlayerCount);

        // Only the master loads the level. With AutomaticallySyncScene on, PUN already loaded the master's
        // scene for everyone else as soon as they joined (this callback runs after that load finishes).
        // Calling LoadLevel here too reloads the Game scene, which can leave the player spawned by the
        // first load behind as a frozen ghost on everyone else's screen.
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Room Creation Failed: " + message);
    }
}
