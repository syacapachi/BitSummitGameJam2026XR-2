using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] Canvas[] canvases;
    LocalCameraSetting ownerSetting;
    public void ResistOwner(LocalCameraSetting cameraSetting)
    {
        ownerSetting = cameraSetting;
        
    }
    public void UnResisiOwner()
    {
        
    }
    private void CameraChange(Camera camera)
    {
        foreach (Canvas canvas in canvases)
        {
            canvas.worldCamera = camera;
        }
    }
}
