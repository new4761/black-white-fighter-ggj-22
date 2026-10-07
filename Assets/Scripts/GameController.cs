using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using TMPro;
using System.Linq;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameController : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI rank, activity, hp;

    public Transform[] spawnLocation;

    public bool MasterMode = false;
    public GameObject MasterCamera;

    // Player property holding PhotonNetwork.ServerTimestamp (ms) from when the player spawned. Every client
    // computes survive time from it locally, so nothing has to be sent while the game runs.
    private const string SpawnTimeKey = "spawnTime";

    private List<string> activitys = new List<string>();

    private PhotonView _pv;
    private static GameController instance;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Locked;
        _pv = GetComponent<PhotonView>();
        instance = this;

        if (MasterMode)
        {
            MasterCamera.SetActive(true);
            hp.gameObject.SetActive(false);
            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            return;
        }

        Random.InitState(System.DateTime.Now.Millisecond);
        if(Random.Range(0, 2) == 1){
   PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerBlack"), spawnLocation[Random.Range(0, spawnLocation.Length)].position, Quaternion.identity);
        }
     else{
  PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerWhite"), spawnLocation[Random.Range(0, spawnLocation.Length)].position, Quaternion.identity);
     }
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { SpawnTimeKey, PhotonNetwork.ServerTimestamp } });
        // Annouce($"{PhotonNetwork.NickName} Joined");
    }

    private void Update()
    {
        // Players leave the room when they die, so everyone still in PlayerList is alive.
        var sortedScore = PhotonNetwork.PlayerList
            .Where(player => player.CustomProperties.ContainsKey(SpawnTimeKey))
            .Select(player => (player, time: GetSurviveTime(player)))
            .OrderByDescending(entry => entry.time)
            .ToList();
        int myIndex = sortedScore.FindIndex(entry => entry.player.IsLocal);

        rank.text ="";
        for (int i = 0; i < Mathf.Min(sortedScore.Count, 5); i++)
        {
            // Keep the local player on the board: if they're not in the top five, they take the last line.
            int idx = (i == 4 && myIndex > 4) ? myIndex : i;
            var entry = sortedScore[idx];
            string playerName = entry.player.IsLocal ? $"<b>{entry.player.NickName}</b>" : entry.player.NickName;
            rank.text += $"{idx + 1}. {playerName} - {GetTimeFormat(entry.time)}\n";
        }

        float GetSurviveTime(Player player) => unchecked(PhotonNetwork.ServerTimestamp - (int)player.CustomProperties[SpawnTimeKey]) / 1000f;

        string GetTimeFormat(float second) => System.TimeSpan.FromSeconds(second).ToString("m\\:ss\\.ff");
    }
    public static GameController Main() => instance;

    public void SetAmmo(int number) => Debug.Log($"Set ammo to {number}");
    public void SetHP(int number) => hp.text = number.ToString();

    // public override void OnLeftRoom() => Annouce($"{PhotonNetwork.NickName} Leaved");
    public void Annouce(string message) => _pv.RPC("RPC_Annoucement", RpcTarget.AllBufferedViaServer, message);

    [PunRPC]
    public void RPC_Annoucement(string message)
    {
        if (activitys.Count >= 5)
        {
            activitys.RemoveAt(0);
        }
        activitys.Add(message);

        activity.text = "";
        foreach (var item in activitys)
        {
            activity.text += item + "\n";
        }
    }
}
