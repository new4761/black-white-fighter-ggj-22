using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerMovementManagerT : MonoBehaviour, IPunObservable
{
    private const float _runningForce = 1f;

    private const float _walkingForce = 0.6f;

    private const float _jumpingForce = 10000;

    private const float _maximumSpeed = 1200;

    private const float _airFriction = 0.6f;

    bool _isGrounded;

    public Animator _anim;

    PhotonView _pv;

    Rigidbody rb;

    // Counts jumps so other clients can replay the one-shot "doJump" trigger. A trigger that is set and
    // consumed between two network updates would never be seen if we only sent its current value.
    int _jumpCount;

    int _receivedJumpCount = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _isGrounded = CheckIsGrounded();
        //_anim = GetComponentInChildren<Animator>();
        _pv = this.GetComponent<PhotonView>();

        // Other players' avatars are moved by PhotonTransformView. If physics can also push them, the two
        // fight each other and the avatar jitters and rubber-bands.
        if (!_pv.IsMine) rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        // Remote avatars get their animator state from OnPhotonSerializeView instead.
        if (!_pv.IsMine) return;

        //handle left/right/up/down/walk/run/jump

        _anim.SetBool("isWalk", false);
        _anim.SetBool("isRun", false);
        handleMoveMent();

        //check grounded
        _isGrounded = CheckIsGrounded();
        if (_isGrounded)
            _anim.SetBool("isJump", false);
    }

    void handleMoveMent()
    {
        // Rigidbody rb = GetComponent<Rigidbody>();
        // Debug.Log(_pv.ViewID);
        if (!_pv.IsMine) return;

        if (_isGrounded)
        {
            if (Input.GetAxis("Jump") > 0)
            {
                //Jump
                rb.AddForce(Vector3.up * _jumpingForce);
                _isGrounded = false;
                _anim.SetBool("isJump", true);
                _anim.SetTrigger("doJump");
                _jumpCount++;
            }
        }

        Vector3 targetVelocity =
            new Vector3(Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical"));
        targetVelocity = transform.TransformDirection(targetVelocity);
        targetVelocity =
            targetVelocity.normalized * (_maximumSpeed - rb.velocity.magnitude);

        if (Input.GetAxis("Walking") > 0)
        {
            _anim.SetBool("isWalk", true);

            //walking
            if (!_isGrounded)
                rb.AddForce(targetVelocity * _walkingForce * _airFriction);
            else
                rb.AddForce(targetVelocity * _walkingForce);
        }
        else if (targetVelocity != Vector3.zero)
        {
            _anim.SetBool("isRun", true);

            //running
            if (!_isGrounded)
                rb.AddForce(targetVelocity * _runningForce * _airFriction);
            else
                rb.AddForce(targetVelocity * _runningForce);
        }
        rb.AddForce(Vector3.down * 981f);
    }

    bool CheckIsGrounded()
    {
        return Physics
            .Raycast(transform.position,
            Vector3.down,
            transform.localScale.y / 2 + 0.1f);
    }

    // Listed in the PhotonView's Observed Components, next to PhotonTransformView.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_anim.GetBool("isWalk"));
            stream.SendNext(_anim.GetBool("isRun"));
            stream.SendNext(_anim.GetBool("isJump"));
            stream.SendNext(_jumpCount);
        }
        else
        {
            _anim.SetBool("isWalk", (bool)stream.ReceiveNext());
            _anim.SetBool("isRun", (bool)stream.ReceiveNext());
            _anim.SetBool("isJump", (bool)stream.ReceiveNext());

            int jumpCount = (int)stream.ReceiveNext();
            if (_receivedJumpCount >= 0 && jumpCount != _receivedJumpCount) _anim.SetTrigger("doJump");
            _receivedJumpCount = jumpCount;
        }
    }
}
