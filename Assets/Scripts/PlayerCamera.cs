using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Pitch")]
    [SerializeField] private float sensitivityY = 0.08f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private float pitch;
    private bool pitchDirty; // 이번 물리틱에 반영할 변경이 있는지

    private void Start()
    {
        pitch = NormalizePitch(transform.localEulerAngles.x);
        pitchDirty = true; // 시작 시 한 번 적용
    }

    // PlayerInput -> Look (UnityEvent) 에서 연결될 함수
    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        float dy = delta.y * sensitivityY * (invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch + dy, minPitch, maxPitch);

        // 여기서는 회전 "적용"은 하지 않고, 값만 갱신했다고 표시
        pitchDirty = true;
    }

    private void LateUpdate()
    {
        if (!pitchDirty) return;

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        pitchDirty = false;
    }

    private static float NormalizePitch(float xEuler)
    {
        if (xEuler > 180f) xEuler -= 360f;
        return xEuler;
    }
}
