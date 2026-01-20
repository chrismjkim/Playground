using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RecallableObject : MonoBehaviour
{
    public bool rewindAborted { get; private set; }
    public struct KeyframeStatus
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
        public Vector3 linearVel;
        public Vector3 angularVel;

        public KeyframeStatus(Transform transform, Rigidbody rigid)
        {
            pos = transform.position;
            rot = transform.rotation;
            scale = transform.localScale;

            linearVel = rigid.linearVelocity;
            angularVel = rigid.angularVelocity;
        }
    }
    public KeyframeStatus[] keyframes;
    public KeyframeStatus[] capturedKeyframes; // RecallManager로 넘겨주는 keyframe 배열
    public Rigidbody rigid;
    
    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if(GameManager.instance.recall.recallableObjects == null) { }
        else
        {
            GameManager.instance.recall.recallableObjects.Add(this);
        }
        // 모든 키프레임을 오브젝트 최초 생성위치로 초기화함
        keyframes = new KeyframeStatus[GameManager.instance.recall.maxRecallFrames];
        for (int i = 0; i < keyframes.Length; i++)
        {
            keyframes[i] = new KeyframeStatus(transform, rigid);
        }
        keyframes[0] = new KeyframeStatus(transform, rigid);
    }
    private void FixedUpdate()
    {
        // 나중에 안전을 위해서 timeScale이 0일 때 return하도록 예외처리해주면 좋음
        if (Time.timeScale != 0)
        {
            AddKeyFrame();
        }
    }

    // 키프레임은 항상 꽉 차있기 때문에 queue 형태로 밀어내기
    private void AddKeyFrame()
    {
        for (int index = keyframes.Length - 1; index > 0; index--)
        {
            keyframes[index] = keyframes[index - 1];
        }
        keyframes[0] = new KeyframeStatus(transform, rigid);
    }

    public void OnDisable()
    {
        GameManager.instance.recall.recallableObjects.Remove(this);
    }

    public IEnumerator startRewind(int token)
    {
        rewindAborted = false;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        rigid.isKinematic = true;
        // captureKeyFrames()
        capturedKeyframes = new KeyframeStatus[GameManager.instance.recall.maxRecallFrames];
        for (int i = 0; i < keyframes.Length; i++)
        {
            capturedKeyframes[i] = keyframes[i];
        }

        yield return new WaitForSeconds(2f);
        if (!GameManager.instance.recall.IsRecallTokenValid(token))
        {
            if (!GameManager.instance.recall.isRecalling)
            {
                rewindAborted = true;
                rigid.isKinematic = false;
                rigid.linearVelocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
            }
            yield break;
        }
        if (GameManager.instance.recall.isRecalling)
        {
            Debug.Log("오브젝트 되감기 시작");
        }
        int fps = Mathf.FloorToInt(1 / Time.fixedDeltaTime);
        int frames = capturedKeyframes.Length;
        for (int index = 0; index < frames; index++)
        {
            if (!GameManager.instance.recall.IsRecallTokenValid(token))
            {
                if (!GameManager.instance.recall.isRecalling)
                {
                    rewindAborted = true;
                    yield return new WaitForSeconds(1f);
                    rigid.isKinematic = false;
                    rigid.linearVelocity = Vector3.zero;
                    rigid.angularVelocity = Vector3.zero;
                }
                yield break;
            }
            rigid.MovePosition(capturedKeyframes[index].pos);
            rigid.MoveRotation(capturedKeyframes[index].rot);
            if (Mathf.FloorToInt((frames - index) / fps) > 5)
            {
                if (index%fps == 0)
                    AudioManager.instance.PlaySfx(AudioManager.Sfx.Tick);
            }
            else if (Mathf.FloorToInt((frames - index) / fps) > 1)
            {
                if (index % (fps / 2) == 0)
                {
                    AudioManager.instance.PlaySfx(AudioManager.Sfx.Tick);
                }
            }
            else
            {
                if (index % (fps / 4) == 0)
                {
                    AudioManager.instance.PlaySfx(AudioManager.Sfx.Tick);
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
