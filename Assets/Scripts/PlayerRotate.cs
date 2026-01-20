using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRotate : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody rb;

    private void Start()
    {
        // Find the main camera in the scene
        mainCamera = Camera.main;

        // Get the Rigidbody component attached to this GameObject
        rb = GetComponent<Rigidbody>();

        // Enable interpolation for smoother physics-based rotation
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        // Rotate the player towards the camera every physics update
        RotatePlayerTowardsCamera();
    }

    private void RotatePlayerTowardsCamera()
    {
        if (mainCamera != null && rb != null)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0f; // Ignore the y-axis rotation

            if (cameraForward != Vector3.zero)
            {
                Quaternion newRotation = Quaternion.LookRotation(cameraForward);
                rb.MoveRotation(newRotation);
            }
        }
    }
}