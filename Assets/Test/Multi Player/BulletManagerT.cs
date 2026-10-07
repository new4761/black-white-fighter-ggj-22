using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class BulletManagerT : MonoBehaviourPun
{
    public GameObject env;
    public Color myColor;

    const float LifeTime = 3;

    bool _despawned;

    void Start()
    {
        // Every client has a copy of this bullet, but only the client that fired it decides what it hits
        // and when it goes away. The other copies just fly until the owner's PhotonNetwork.Destroy arrives.
        if (photonView.IsMine)
        {
            Invoke(nameof(Despawn), LifeTime);
        }
    }

    void FixedUpdate()
    {
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine || _despawned)
        {
            return;
        }

        var collider = collision.collider;
        switch (collision.collider.gameObject.tag)
        {
            case "Scene":
                Debug.Log($"OnCollisionEnter Scene {collider.gameObject.name}");

                Environment
                    .Instance
                    .ChangeColor(myColor,
                    collider.gameObject);
                Despawn();
                break;
            case "Player":
            Debug.Log("OnCollisionEnter +" + collision);
                collider
                    .gameObject
                    .GetComponent<PlayerStatusManger>()
                    .TakeDamage(1);
                Despawn();
                break;
        }
    }

    void Despawn()
    {
        if (_despawned)
        {
            return;
        }
        _despawned = true;

        // PhotonNetwork.Destroy removes the bullet on every client and deletes its buffered instantiate
        // event. A plain Destroy() leaves that event in the room, so everyone who joins later would get
        // every bullet ever fired spawned on top of them.
        PhotonNetwork.Destroy(gameObject);
    }
}
