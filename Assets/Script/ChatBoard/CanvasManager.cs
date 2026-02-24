using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] List<Canvas> canvasList = new();
    CameraSetting ownerSetting;
    public void ResistOwner(CameraSetting cameraSetting)
    {
        ownerSetting = cameraSetting;
        cameraSetting.OnCameraChanged += CameraChange;
    }
    public void UnResisiOwner()
    {
        ownerSetting.OnCameraChanged -= CameraChange;
    }
    private void CameraChange(Camera camera)
    {
        foreach (Canvas canvas in canvasList)
        {
            canvas.worldCamera = camera;
        }
    }
}
