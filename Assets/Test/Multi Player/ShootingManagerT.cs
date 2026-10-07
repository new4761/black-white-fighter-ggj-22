using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEngine;

public class ShootingManagerT : MonoBehaviour, IPunObservable
{
    public GameObject _bulletEmitter;

    private GameObject _bullet;
    public GameObject _bulletBlack;
    public GameObject _bulletWhite;

    public GameObject _camera;

    public float _bulletForwardForce;

    private float _fireRate = 3;

    private float _currentTime;

    PhotonView _pv;
    public Animator _anim;

    bool _isTesting = false;

    private int bulletCount = 5;

    // Counts shots so other clients can replay the one-shot "doShot" trigger (see PlayerMovementManagerT).
    private int _shotCount;

    private int _receivedShotCount = -1;

    void Start()
    {
        _currentTime = 0;
        _pv = this.gameObject.GetComponent<PhotonView>();

         if(Random.value < 0.5f)
            _bullet = _bulletBlack;
        else
            _bullet = _bulletWhite;
    }

    void Update()
    {
        if (!_isTesting && !_pv.IsMine) return;

       if (Input.GetAxis("Fire1") > 0 && _currentTime >= 1 / _fireRate)
        // if (Input.GetAxis("Fire1") > 0 && _currentTime >= 1 / _fireRate && bulletCount > 0)
        {
            bulletCount--;
            GameController.Main().SetAmmo(bulletCount);
            _anim.SetTrigger("doShot");
            _anim.SetBool("isShot", true);
            _shotCount++;
            GameObject tempBullet;

            if(_isTesting)  tempBullet = Instantiate (_bullet, _bulletEmitter.transform.position, _bulletEmitter.transform.rotation) as GameObject;

            else tempBullet =
                PhotonNetwork
                    .Instantiate(Path.Combine("PhotonPrefabs", "BulletWhite"),
                    _bulletEmitter.transform.position,
                    _bulletEmitter.transform.rotation) as
                GameObject;

            tempBullet.transform.Rotate(Vector3.left * 90);

            Rigidbody tempRigidBody = tempBullet.GetComponent<Rigidbody>();

            Vector3 shootingDir =
                _camera.transform.forward + new Vector3(0, 0.2f, 0);

            tempRigidBody.AddForce(shootingDir * _bulletForwardForce);

            _currentTime = 0;
        }
        else if (Input.GetAxis("Fire1") == 0)
        {
            _anim.SetBool("isShot", false);
        }

        _currentTime += Time.deltaTime;
    }
    

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "ItemBullet")
        {
            bulletCount++;
            GameController.Main().SetAmmo(bulletCount);
            Destroy(other.gameObject);
            PhotonNetwork.Destroy(other.gameObject);
        }
    }

    // Listed in the PhotonView's Observed Components, next to PhotonTransformView.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_anim.GetBool("isShot"));
            stream.SendNext(_shotCount);
        }
        else
        {
            _anim.SetBool("isShot", (bool)stream.ReceiveNext());

            int shotCount = (int)stream.ReceiveNext();
            if (_receivedShotCount >= 0 && shotCount != _receivedShotCount) _anim.SetTrigger("doShot");
            _receivedShotCount = shotCount;
        }
    }
}
