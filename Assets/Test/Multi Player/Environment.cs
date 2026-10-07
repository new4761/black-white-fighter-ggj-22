using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Environment : MonoBehaviourPunCallbacks, IChangeColorable
{
    public static Environment Instance;

    public GameObject[] Walls;

    // Each painted wall is stored as a room property "wall<id>" = true (black) / false (white).
    // Room properties live on the server for as long as the room exists, so players who join later
    // get every wall's color, even walls painted by players who already left. (Buffered RPCs are
    // deleted from the room when the player who sent them leaves.)
    const string WallKeyPrefix = "wall";

    // Paintable objects in Hierarchy order. A wall's index in this array is its network id: it is the
    // same on every client, unlike GetInstanceID() which is different on every machine.
    GameObject[] objs;

    Dictionary<GameObject, int> wallIds;

    void Awake()
    {
        Instance = this;
        objs = GameObject.FindGameObjectsWithTag("Scene");

        var paths = new Dictionary<GameObject, int[]>(objs.Length);
        foreach (GameObject obj in objs)
        {
            paths[obj] = GetSiblingPath(obj.transform);
        }
        System.Array.Sort(objs, (a, b) => CompareSiblingPaths(paths[a], paths[b]));

        wallIds = new Dictionary<GameObject, int>(objs.Length);
        for (int i = 0; i < objs.Length; i++)
        {
            wallIds[objs[i]] = i;
        }
    }

    void Start()
    {
        // Catch up on walls that were painted before we joined.
        if (PhotonNetwork.CurrentRoom != null)
        {
            ApplyWallColors(PhotonNetwork.CurrentRoom.CustomProperties);
        }
    }

    public void ChangeColor(Color color, GameObject wall)
    {
        var wallRenderer = wall.GetComponent<Renderer>();
        if (!PhotonNetwork.InRoom || wallRenderer == null || !wallIds.TryGetValue(wall, out int id))
        {
            return;
        }

        // Same rule as before (white turns black, anything else turns white), but we send the resulting
        // color rather than "toggle", so receiving it twice can't flip the wall back.
        bool toBlack = wallRenderer.material.color == Color.white;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { WallKeyPrefix + id, toBlack } });
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        ApplyWallColors(propertiesThatChanged);
    }

    void ApplyWallColors(Hashtable properties)
    {
        foreach (DictionaryEntry entry in properties)
        {
            if (!(entry.Key is string key) || !key.StartsWith(WallKeyPrefix) || !(entry.Value is bool isBlack))
            {
                continue;
            }
            if (!int.TryParse(key.Substring(WallKeyPrefix.Length), out int id) || id < 0 || id >= objs.Length)
            {
                continue;
            }

            var wallRenderer = objs[id].GetComponent<Renderer>();
            if (wallRenderer != null)
            {
                wallRenderer.material.color = isBlack ? Color.black : Color.white;
            }
        }
    }

    // Sibling indices from the scene root down to t, e.g. [3, 0, 12]. Every client loads the same scene
    // file, so this path is identical everywhere.
    static int[] GetSiblingPath(Transform t)
    {
        var path = new List<int>();
        for (; t != null; t = t.parent)
        {
            path.Add(t.GetSiblingIndex());
        }
        path.Reverse();
        return path.ToArray();
    }

    static int CompareSiblingPaths(int[] a, int[] b)
    {
        for (int i = 0; i < Mathf.Min(a.Length, b.Length); i++)
        {
            if (a[i] != b[i])
            {
                return a[i].CompareTo(b[i]);
            }
        }
        return a.Length.CompareTo(b.Length);
    }
}
