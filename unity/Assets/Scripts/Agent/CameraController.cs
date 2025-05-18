using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera egocentricCam;
    public Camera frontCam;
    public Camera leftSideCam;
    public Camera rightSideCam;
    public Camera behindCam;
    public Camera freeCam;
    public Camera minimapCam;

    public float moveSpeed = 10.0f;
    public float mouseSensitivity = 2.0f;
    public float verticalRotationLimit = 80.0f;

    private bool isFreeMode = false;
    private bool isCameraControlEnabled = true;
    private float rotationY = 0f;
    private float rotationX = 0f;

    public bool record;
    public string imgeDir;
    private string baseDir=Path.Combine(Application.dataPath, "SavedImages");
    
    // add a timer variable
    private float lastRecordTime = 0f;
    private const float RECORD_INTERVAL = 0.2f; // 200 milliseconds interval

    private int imageCounter = 0; // for image numbering

    void Start()
    {
        // ensure all cameras are enabled
        EnableAllCameras();
    }

    void Update()
    {
        HandleEscAndMouseInput();

        // check if FreeCam mode is enabled
        if (Input.GetKeyDown(KeyCode.Alpha0)) ActivateFreeMode();

        if (isFreeMode && isCameraControlEnabled) FreeCamControl();

        // press 'P' key to save all camera images
        if (Input.GetKeyDown(KeyCode.P)) SaveAllCameraImages();
        
        // add an automatic recording function
        if (record)
        {
            // Debug.Log("Image path: "+baseDir);
            // imgeDir=baseDir+"/test";
            // check if the recording interval is reached
            if (Time.time - lastRecordTime >= RECORD_INTERVAL)
            {
                // ensure the save directory exists
                if (!string.IsNullOrEmpty(imgeDir))
                {
                    if (!Directory.Exists(imgeDir))
                    {
                        Directory.CreateDirectory(imgeDir);
                    }
                    SaveAllCameraImages();
                }
                else
                {
                    Debug.LogWarning("the recording directory path is empty! please set the imgeDir");
                    record = false;
                }
                
                // update the last recording time
                lastRecordTime = Time.time;
            }
        }
    }

    private void EnableAllCameras()
    {
        // enable all cameras, ensure simultaneous rendering
        egocentricCam.enabled = true;
        frontCam.enabled = true;
        leftSideCam.enabled = true;
        rightSideCam.enabled = true;
        behindCam.enabled = true;
        freeCam.enabled = false; // default is not enabled FreeCam
        minimapCam.enabled = true;
    }

    void ActivateFreeMode()
    {
        // only enable the FreeCam mode
        EnableAllCameras();
        freeCam.enabled = true;
        isFreeMode = true;
        isCameraControlEnabled = true;
        SetCursorState(CursorLockMode.Locked, false);
    }

    public void ResetImageCount(){
        imageCounter=0;
    }

    void FreeCamControl()
    {
        HandleCursorVisibility();

        if (Cursor.lockState == CursorLockMode.None) return;

        // basic movement (front, back, left, right)
        Vector3 move = freeCam.transform.right * Input.GetAxis("Horizontal") +
                       freeCam.transform.forward * Input.GetAxis("Vertical");

        // add vertical movement control
        float verticalMove = 0f;
        if (Input.GetKey(KeyCode.Space)) verticalMove += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) verticalMove -= 1f;
        move += Vector3.up * verticalMove;

        // apply movement
        freeCam.transform.position += moveSpeed * Time.deltaTime * move * (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f);

        // camera rotation
        rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationY = Mathf.Clamp(rotationY - Input.GetAxis("Mouse Y") * mouseSensitivity, -verticalRotationLimit, verticalRotationLimit);
        freeCam.transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);
    }

    private void HandleEscAndMouseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) ToggleCameraControl();
        if (!isCameraControlEnabled && Input.GetMouseButtonDown(0)) ToggleCameraControl();
    }

    private void HandleCursorVisibility()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt)) SetCursorState(CursorLockMode.None, true);
        else if (Input.GetKeyUp(KeyCode.LeftAlt)) SetCursorState(CursorLockMode.Locked, false);
    }

    void ToggleCameraControl()
    {
        isCameraControlEnabled = !isCameraControlEnabled;
        SetCursorState(isCameraControlEnabled ? CursorLockMode.Locked : CursorLockMode.None, !isCameraControlEnabled);
    }

    void SetCursorState(CursorLockMode lockMode, bool visible)
    {
        Cursor.lockState = lockMode;
        Cursor.visible = visible;
    }

    // modify the method to save all camera images to the specified directory
    public void SaveAllCameraImages()
    {
        SaveCameraImage(egocentricCam.targetTexture, "EgocentricCam");
        SaveCameraImage(frontCam.targetTexture, "FrontCam");
        SaveCameraImage(leftSideCam.targetTexture, "LeftSideCam");
        SaveCameraImage(rightSideCam.targetTexture, "RightSideCam");
        SaveCameraImage(behindCam.targetTexture, "BehindCam");
        // SaveCameraImage(minimapCam.targetTexture, "Minimap");
    }

    private void SaveCameraImage(RenderTexture renderTexture, string cameraName)
    {
        if (renderTexture == null)
        {
            Debug.LogWarning($"{cameraName} has no target render texture!");
            return;
        }
        
        // use the number to build the file name
        string fileName = $"{cameraName}_{imageCounter++}.png";
        
        // use the custom path or the default path
        string dirPath = !string.IsNullOrEmpty(imgeDir) ? imgeDir : Path.Combine(Application.dataPath, "SavedImages");
        string filePath = Path.Combine(dirPath, fileName);

        // ensure the save directory exists
        Directory.CreateDirectory(dirPath);

        // save the RenderTexture content to the PNG file
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = currentRT;

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);

        Destroy(texture);

    }
}