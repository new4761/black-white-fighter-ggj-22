using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStatusManger : MonoBehaviour
{
    public int _maxHp;

    private int currentHealth;

    bool _isDead;

    PhotonView _pv;

    public Animator _anim;

    void Start()
    {
        currentHealth = _maxHp;
        _isDead = false;
        _pv = this.GetComponent<PhotonView>();

        // Start runs for every player's avatar on every client; only our own avatar owns the HP display.
        if (_pv.IsMine) GameController.Main().SetHP(currentHealth);
    }

    void Update()
    {
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("Take damage :" + damage);
        if (!_isDead) _pv.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(int damage)
    {
        if (!_pv.IsMine) return;

        Debug.Log("Take damage" + _pv.ViewID);
        currentHealth -= damage;

        // healthbarImage.fillAmount = currentHealth / maxHealth;
        GameController.Main().SetHP(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (!_pv.IsMine) return;
        _isDead = true;

        //Do Something to die
        _anim.SetTrigger("doDead");

        Destroy(this.gameObject);
        PhotonNetwork.Disconnect();
        Application.Quit();
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentHealth < _maxHp && other.gameObject.tag == "ItemHeart")
        {
            currentHealth++;
            GameController.Main().SetHP(currentHealth);
            Destroy(other.gameObject);
            PhotonNetwork.Destroy(other.gameObject);
        }
    }
}
