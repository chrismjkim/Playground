using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
public class CameraManager : MonoBehaviour
{
    [Header("Volumes")]
    public Volume gloabalVolume;
    public VolumeProfile normalProfile;
    public VolumeProfile bwProfile;
    
    private CinemachineBrain brain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
    }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void setRecallVision()
    {
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        gloabalVolume.profile = bwProfile;
    }

    public void setNormalVision()
    {
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
        gloabalVolume.profile = normalProfile;
    }
}
