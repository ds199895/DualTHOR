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
    
    // 添加计时器变量
    private float lastRecordTime = 0f;
    private const float RECORD_INTERVAL = 0.2f; // 200毫秒间隔

    private int imageCounter = 0; // 用于图像编号

    void Start()
    {
        // 确保所有相机启用
        EnableAllCameras();
    }

    void Update()
    {
        HandleEscAndMouseInput();

        // 检查是否启用FreeCam模式
        if (Input.GetKeyDown(KeyCode.Alpha0)) ActivateFreeMode();

        if (isFreeMode && isCameraControlEnabled) FreeCamControl();

        // 按下 'P' 键时保存所有相机图像
        if (Input.GetKeyDown(KeyCode.P)) SaveAllCameraImages();
        
        // 添加自动记录功能
        if (record)
        {
            // Debug.Log("Image path: "+baseDir);
            // imgeDir=baseDir+"/test";
            // 检查是否到达记录间隔
            if (Time.time - lastRecordTime >= RECORD_INTERVAL)
            {
                // 确保保存目录存在
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
                    Debug.LogWarning("记录目录路径为空！请设置 imgeDir");
                    record = false;
                }
                
                // 更新上次记录时间
                lastRecordTime = Time.time;
            }
        }
    }

    private void EnableAllCameras()
    {
        // 启用所有相机，确保同时渲染
        egocentricCam.enabled = true;
        frontCam.enabled = true;
        leftSideCam.enabled = true;
        rightSideCam.enabled = true;
        behindCam.enabled = true;
        freeCam.enabled = false; // 默认情况下不启用FreeCam
        minimapCam.enabled = true;
    }

    void ActivateFreeMode()
    {
        // 仅启用自由相机模式
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

        // 基本移动（前后左右）
        Vector3 move = freeCam.transform.right * Input.GetAxis("Horizontal") +
                       freeCam.transform.forward * Input.GetAxis("Vertical");

        // 添加垂直移动控制
        float verticalMove = 0f;
        if (Input.GetKey(KeyCode.Space)) verticalMove += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) verticalMove -= 1f;
        move += Vector3.up * verticalMove;

        // 应用移动
        freeCam.transform.position += moveSpeed * Time.deltaTime * move * (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f);

        // 相机旋转
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

    // 修改保存所有相机图像到指定目录
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
            Debug.LogWarning($"{cameraName} 没有目标渲染纹理！");
            return;
        }
        
        // 使用编号构建文件名
        string fileName = $"{cameraName}_{imageCounter++}.png";
        
        // 使用自定义路径或默认路径
        string dirPath = !string.IsNullOrEmpty(imgeDir) ? imgeDir : Path.Combine(Application.dataPath, "SavedImages");
        string filePath = Path.Combine(dirPath, fileName);

        // 确保保存目录存在
        Directory.CreateDirectory(dirPath);

        // 将RenderTexture内容保存到PNG文件
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = currentRT;

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);

        Destroy(texture);
        // Debug.Log($"已保存 {cameraName} 图像到 {filePath}");
    }
}