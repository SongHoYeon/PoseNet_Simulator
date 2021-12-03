using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    private WebCamDevice cameraDevice;
    private WebCamTexture cameraTexture;
    public RawImage rawImage;
    private Vector3 rotationVector;

    void Awake()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);
        else
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.Log("No devices cameras found");
                return;
            }
        }

        cameraDevice = WebCamTexture.devices[0];
        cameraTexture = new WebCamTexture(cameraDevice.name);
        cameraTexture.filterMode = FilterMode.Trilinear;

        rawImage.texture = cameraTexture;
        rawImage.material.mainTexture = cameraTexture;

        cameraTexture.requestedFPS = 60;
        cameraTexture.Play();
    }

    void Update()
    {
        if (cameraTexture.width < 100)
            return;

        rotationVector.z = -cameraTexture.videoRotationAngle;
        rawImage.rectTransform.localEulerAngles = rotationVector;
    }
}
