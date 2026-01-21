using System.Collections; // 코루틴
using System.Collections.Generic; // List
using UnityEngine;
using UnityEngine.InputSystem;


public class RecallManager : MonoBehaviour
{
    [Header("리버레코")]
    [SerializeField] public bool isRecallActivated = false;
    [SerializeField] public bool isSelectingRecall = false;
    [SerializeField] public bool isRecalling = false;
    [SerializeField] public float recallTime=10f;
    [SerializeField] public int maxRecallFrames;

    [Header("경로 이펙트")]
    [SerializeField] public Material pathMaterial;
    [SerializeField] public int ghostIntervalSeconds;
    [SerializeField] public int pathUnitLength;


    [Header("클릭 관련 변수")]
    public bool clickStarted;
    public bool clickPerformed;
    public bool clickCanceled;
    public bool isClickHeld;

    [Header("Raycast Controller")]
    public RaycastController raycastController;

    public GameObject detectedRecallableObj = null;
    public List<RecallableObject> recallableObjects = new();
    private int recallToken;
    private void Awake()
    {
        maxRecallFrames = Mathf.FloorToInt(recallTime * (1 / Time.fixedDeltaTime));
    }
    private void LateUpdate()
    {
        clickStarted = false;
        clickPerformed = false;
        clickCanceled = false;
    }
    public void Recall()
    {
        if (!isRecallActivated && !isSelectingRecall && !isRecalling)
        {
            isRecallActivated = true;
            isSelectingRecall = true;
            GameManager.instance.timer.Pause();
            GameManager.instance.cam.setRecallVision();
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Activate);

            // 아웃라인 스크립트 부착하기
            foreach (var obj in recallableObjects)
            {
                if (!obj.TryGetComponent<OutlinePP>(out var outline))
                {
                    obj.gameObject.AddComponent<OutlinePP>();
                    outline = obj.GetComponent<OutlinePP>();
                }
                outline.OutlineColor = GameManager.instance.palette.outlineLooking;
                outline.OutlineWidth = 15f;
                outline.OutlineMode = OutlinePP.Mode.OutlineVisible;
                outline.enabled = false;
            }

        }
        // 오브젝트 선택에서 빠져나오기
        else if (isSelectingRecall)
        {
            GameManager.instance.timer.Resume();
            GameManager.instance.cam.setNormalVision();
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Deactivate);
            ClearDetectedRecallableObj();
            isSelectingRecall = false;
            isRecallActivated = false; 
        }
        // 되감기 중단하기
        else if (isRecalling)
        {
            isRecallActivated = false;
            isRecalling = false;
            ClearDetectedRecallableObj();
            Debug.Log("오브젝트 되감기 중단됨");
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Deactivate);
        }
    }
    public void SelectObject()
    {
        isSelectingRecall = false;
        isRecalling = true;
        GameManager.instance.timer.Resume();
        GameManager.instance.cam.setNormalVision();
        Debug.Log("오브젝트 되감기 준비 중...");
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
        recallToken++;
        RecallableObject obj = detectedRecallableObj ? detectedRecallableObj.GetComponent<RecallableObject>() : null;
        StartCoroutine(RecallObject(obj, recallToken));
    }

    IEnumerator RecallObject(RecallableObject obj, int token)
    {
        if (obj == null)
        {
            isRecallActivated = false;
            isRecalling = false;
            ClearDetectedRecallableObj();
            yield break;
        }
        yield return obj.startRewind(token);

        if (!IsRecallTokenValid(token))
        {
            yield break;
        }
        if (obj.rewindAborted)
        {
            yield break;
        }
        disableOutline(obj.gameObject);
        yield return new WaitForSeconds(1f);
        obj.rigid.isKinematic = false;
        isRecallActivated = false;
        isRecalling = false;
        ClearDetectedRecallableObj();
        Debug.Log("오브젝트 되감기 종료");
    }

    public bool IsRecallTokenValid(int token)
    {
        return isRecalling && token == recallToken;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            clickStarted = true;
            isClickHeld = true;
        }
        if (context.performed)
        {
            clickPerformed = true;
        }
        if (context.canceled)
        {
            clickCanceled = true;
            isClickHeld = false;
            if (detectedRecallableObj != null && isSelectingRecall)
            {
                SelectObject();
            }
        }
    }

    public void SetDetectedRecallableObj(GameObject newObject)
    {
        if (detectedRecallableObj!=null)
        {
            disableOutline(detectedRecallableObj);
            RecallableObject prevObj = detectedRecallableObj.GetComponent<RecallableObject>();
            prevObj.HideRecallPath();
        }
        detectedRecallableObj = newObject;
        // 오브젝트의 이동경로를 생성한다
        RecallableObject obj = detectedRecallableObj.GetComponent<RecallableObject>();
        obj.ShowRecallPath();
        
    }

    public void ClearDetectedRecallableObj()
    {
        if (detectedRecallableObj)
        {
            disableOutline(detectedRecallableObj);
            // 오브젝트의 이동경로를 제거한다
            RecallableObject obj = detectedRecallableObj.GetComponent<RecallableObject>();
            obj.HideRecallPath();
            obj.ClearAllGhosts();
        }
        detectedRecallableObj = null;
    }

    private void disableOutline(GameObject recallableObject)
    {
        OutlinePP lineBefore = recallableObject.GetComponent<OutlinePP>();
        lineBefore.enabled = false;
        lineBefore.OutlineColor = GameManager.instance.palette.outlineLooking;
    }
}
