using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] public float baseFixedDeltaTime;
    void Start()
    {
        baseFixedDeltaTime = Time.fixedDeltaTime;
    }
    public void Pause()
    {
        Time.timeScale = 0;
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
    }

    public void Resume()
    {
        Time.timeScale = 1;
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
    }
}
