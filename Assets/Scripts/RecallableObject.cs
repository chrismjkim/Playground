using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class RecallableObject : MonoBehaviour
{
    // [SerializeField] private int pathInterval; // path의 점 사이 간격

    // 이동 경로
    private LineRenderer lineRenderer;
    // 이동 경로 중간 메쉬
    public List<GameObject> ghostObjects = new List<GameObject>();
    private Mesh objectMesh;

    private int fps;
    private int ghostInterval;

    public bool rewindAborted { get; private set; }
    public struct KeyframeStatus
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
        public KeyframeStatus(Transform transform, Rigidbody rigid)
        {
            pos = transform.position;
            rot = transform.rotation;
            scale = transform.localScale;
        }
    }
    public KeyframeStatus[] keyframes;
    public KeyframeStatus[] capturedKeyframes; // RecallManager로 넘겨주는 keyframe 배열
    public Rigidbody rigid;
    
    public void CreateRecallPath()
    {
        
    }

    public void ShowRecallPath()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = keyframes.Length;
        for (int index=0; index<keyframes.Length; index++)
        {
            // 시작점을 가장 최근 키프레임으로 역순배치
            lineRenderer.SetPosition(index, keyframes[keyframes.Length - 1 - index].pos);
        }
        int fps = Mathf.FloorToInt(1 / GameManager.instance.timer.baseFixedDeltaTime);
        ghostInterval = fps * GameManager.instance.recall.ghostIntervalSeconds;
        for (int index = ghostInterval-1; index < keyframes.Length; index += ghostInterval)
        {
            CreateGhostMesh(keyframes[index]);
        }
    }
    public void HideRecallPath()
    {
        lineRenderer.enabled = false;
        ClearAllGhosts();
    }
    private void CreateGhostMesh(KeyframeStatus status)
    {
        GameObject ghost = new GameObject("RecallGhost");
        ghost.transform.position = status.pos;
        ghost.transform.rotation = status.rot;
        ghost.transform.localScale = status.scale;

        MeshFilter mf = ghost.AddComponent<MeshFilter>();
        mf.sharedMesh = objectMesh;
        MeshRenderer mr = ghost.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GameManager.instance.recall.pathMaterial;
        
        ghostObjects.Add(ghost);
    }
    private void DeleteGhostMesh(int index)
    {
        GameObject target = ghostObjects[index];
        Destroy(target);
    }

    public void ClearAllGhosts()
    {
        foreach (var g in ghostObjects)
        {
            Destroy(g);
        }
        ghostObjects.Clear();
    }


    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = GameManager.instance.recall.pathMaterial;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
        if (TryGetComponent<MeshFilter>(out var mf)) objectMesh = mf.sharedMesh;

        if (GameManager.instance.recall.recallableObjects == null) { }
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
        int deletedGhosts = 0;
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
            // 라인렌더러 수정하기
            lineRenderer.positionCount--;
            // 고스트 지우기
            if ((index%ghostInterval) == ghostInterval-1)
            {
                // 최근 생성된 고스트부터 삭제
                Debug.Log(ghostObjects.Count - 1 - deletedGhosts);
                DeleteGhostMesh(deletedGhosts);
                deletedGhosts++;
            }
            PlayTickSound(frames, index, fps);
            yield return new WaitForFixedUpdate();
        }
        ClearAllGhosts();
    }
    private void PlayTickSound(int frames, int index, int fps)
    {
        if (Mathf.FloorToInt((frames - index) / fps) > 5)
        {
            if (index % fps == 0)
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
    }

    public void OnDisable()
    {
        GameManager.instance.recall.recallableObjects.Remove(this);
    }
}
