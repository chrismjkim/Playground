using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class RaycastController : MonoBehaviour
{
    public float rayLength = 100f;
    private int mask;

    [SerializeField] public RecallManager recallManager;

    private void Start()
    {
        mask = LayerMask.GetMask("Default");
    }

    private void Update()
    {
        if (!recallManager.isSelectingRecall)
        {
            return;
        }
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        // Raycast가 맞춘 물체가 있을 때,
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, mask)) {
            // 맞춘 물체가 RecallableObject인 경우
            if (hit.transform.gameObject.TryGetComponent(out OutlinePP line)) {
                // 이전 프레임에 감지된 물체가 있는 경우
                if (recallManager.detectedRecallableObj != null)
                {
                    // 맞춘 물체가 이전 프레임과 다를 경우
                    if (recallManager.detectedRecallableObj != hit.transform.gameObject)
                    {
                        recallManager.SetDetectedRecallableObj(hit.transform.gameObject);
                        line.enabled = true;
                        AudioManager.instance.PlaySfx(AudioManager.Sfx.Look);
                    }
                    if (recallManager.clickStarted || recallManager.isClickHeld)
                    {
                        line.OutlineColor = GameManager.instance.palette.outlineSelected;
                    }
                }
                // 이전 프레임에 감지된 물체가 없는 경우
                else
                {
                    recallManager.SetDetectedRecallableObj(hit.transform.gameObject);
                    line.enabled = true;
                    AudioManager.instance.PlaySfx(AudioManager.Sfx.Look);
                }
            }
            // 맞춘 물체가 RecallableObject가 아니고 detectedRecallableObj가 있는 경우
            else if (recallManager.detectedRecallableObj) 
            {
                recallManager.ClearDetectedRecallableObj();
            }
        }
        // Raycast가 맞춘 물체가 없을 때,
        else
        {
            // detectedRecallableObj가 있는 경우
            if (recallManager.detectedRecallableObj != null)
            {
                recallManager.ClearDetectedRecallableObj();
            }
        }
    }
    // OnDrawGizmos는 생명 주기 함수
    private void OnDrawGizmos()
    {
        if (!recallManager.isSelectingRecall)
        {
            return;
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * rayLength);
    }
}
