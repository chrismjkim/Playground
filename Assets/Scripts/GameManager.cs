using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Player player;
    public PauseManager timer;
    public RecallManager recall;
    public CameraManager cam;
    public ColorManager palette;
    public AudioManager sound;
    void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }
    // Update is called once per frame
    void Update() { }
}
