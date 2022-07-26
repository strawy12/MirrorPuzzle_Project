using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMove : MonoBehaviour
{
    public float _maxSpeed = 5f;
    public float _acceleration = 50f;
    public float _deAcceleration = 50f;

    protected float _currentVelocity = 3f;
    protected Vector2 _movementDir;

    private Rigidbody2D _rigid;
    private bool _isWarping;
    private bool _findMirror;

    private Animator _visualAnimator;

    private ParticleSystem _walkParticle;
    public UnityEvent OnTriggerInteraction;

    void Start()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _visualAnimator = transform.Find("VisualSprite").GetComponent<Animator>();
        _walkParticle = GetComponentInChildren<ParticleSystem>();
    }

    // 실행되는 동안 반복 => 1 프레임 한번씩 호출
    private void Update()
    {
        if (GameManager.Inst.gameState != EGameState.Game) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnTriggerInteraction?.Invoke();
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Inst.gameState != EGameState.Game) return;

        InputDirection();

        _rigid.velocity = _movementDir * _currentVelocity;

        PlayerAnimation();
    }
    private void InputDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);

        if (dir.sqrMagnitude > 0)
        {
            // 내가 가려고 한 방향이 지금 향하고 있는 방향의 반대면 
            // 속도를 0으로 초기화를 시킨다
            if (Vector2.Dot(dir, _movementDir) < 0)
            {
                _currentVelocity = 0f;
            }

            // 값을 변경 시킴
            _movementDir = dir.normalized;
            _walkParticle.gameObject.SetActive(true);
        }
        else
        {
            _walkParticle.gameObject.SetActive(false);
        }
        // 값을 변경 시킴

        _movementDir = dir.normalized;

        _currentVelocity = CalcSpeed(dir.normalized);
        // (0,0) == 움직일 방향이 없다면
        // 값 변화가 없다
    }

    private float CalcSpeed(Vector2 dir)
    {
        float velocity = _currentVelocity;

        if (dir.sqrMagnitude > 0)
        {
            velocity += _acceleration * Time.fixedDeltaTime;
        }

        else
        {
            velocity -= _deAcceleration * Time.fixedDeltaTime;
        }

        return Mathf.Clamp(velocity, 0f, _maxSpeed);
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Inst.gameState != EGameState.Game) return;

        if (collision.gameObject.CompareTag("Trigger"))
        {
            if (_isWarping) return;
            WarpZone warpZone = collision.gameObject.GetComponent<WarpZone>();

            Debug.Log(warpZone);
            Debug.Log(warpZone.WarpPoint);
            Vector2 warpPoint = warpZone.WarpPoint;
            _isWarping = true;
            StartCoroutine(WarpPlayer(warpPoint, warpZone.RoomName));
            // 맵 바꿀 때까지는 임시로 주석 해놓을게요
            //if (_movementDir.x == warpZone._offset.x ||
            //    _movementDir.y == warpZone._offset.y)
            //{

            //}
        }
        else if (collision.collider.CompareTag("Closet"))
        {
            GameManager.Inst.coliderState = eColiderState.Closet;
        }
        else if (collision.collider.CompareTag("ObjectBox"))
        {
            GameManager.Inst.coliderState = eColiderState.Box;
        }
        else if (collision.collider.CompareTag("Objectbed"))
        {
            GameManager.Inst.coliderState = eColiderState.Bed;
        }
        else if (collision.collider.CompareTag("ObjectLaker"))
        {
            GameManager.Inst.coliderState = eColiderState.Locker;
        }
        else if (collision.collider.CompareTag("ObjectTable"))
        {
            GameManager.Inst.coliderState = eColiderState.Table;
        }
    }

    private IEnumerator WarpPlayer(Vector2 warpPoint, string roomName)
    {
        GameManager.Inst.UI.FadeScreen(true);
        yield return new WaitForSeconds(0.5f);
        transform.position = warpPoint;
        yield return new WaitForSeconds(0.1f);
        GameManager.Inst.UI.FadeScreen(false);
        yield return new WaitForSeconds(0.5f);
        GameManager.Inst.UI.ActiveRoomText(roomName);
        _isWarping = false;
    }

    private void PlayerAnimation()
    {
        if (_rigid.velocity.x > 0.05f)
        {
            _visualAnimator.Play("RightWalk");
        }

        else if (_rigid.velocity.x < -0.05f)
        {
            _visualAnimator.Play("LeftWalk");
        }

        else if (_rigid.velocity.y > 0.05f)
        {
            _visualAnimator.Play("UpWalk");
        }

        else if (_rigid.velocity.y < -0.05f)
        {
            _visualAnimator.Play("DownWalk");
        }
    }

    // 추후 개선
    public enum WalkType { RightWalk, LeftWalk, UpWalk, DownWalk }
    public void PlayAnimation(int walkType)
    {
        if (_visualAnimator == null) return;
        StopAllCoroutines();

        WalkType type = (WalkType)walkType;
        GameManager.Inst.gameState = EGameState.Timeline;
        StartCoroutine(PlayAnimationCoroutine(type.ToString()));
    }

    private IEnumerator PlayAnimationCoroutine(string walkType)
    {
        while(GameManager.Inst.gameState == EGameState.Timeline)
        {
            _visualAnimator.Play(walkType);

            yield return new WaitForFixedUpdate();
        }
    }
}
