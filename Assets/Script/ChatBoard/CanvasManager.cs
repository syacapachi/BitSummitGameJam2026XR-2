using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] List<Canvas> canvasList = new();
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
        foreach (Canvas canvas in canvasList)
        {
            canvas.worldCamera = camera;
        }
    }
}
