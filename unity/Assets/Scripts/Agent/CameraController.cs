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

    public float moveSpeed = 10.0f;
    public float mouseSensitivity = 2.0f;
    public float verticalRotationLimit = 80.0f;

    private bool isFreeMode = false;
    private bool isCameraControlEnabled = true;
    private float rotationY = 0f;
    private float rotationX = 0f;

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

    // 保存所有相机图像到本地
    public void SaveAllCameraImages()
    {
        SaveCameraImage(egocentricCam.targetTexture, "EgocentricCam");
        SaveCameraImage(frontCam.targetTexture, "FrontCam");
        SaveCameraImage(leftSideCam.targetTexture, "LeftSideCam");
        SaveCameraImage(rightSideCam.targetTexture, "RightSideCam");
        SaveCameraImage(behindCam.targetTexture, "BehindCam");
    }

    private void SaveCameraImage(RenderTexture renderTexture, string cameraName)
    {
        // 生成UUID并构建文件名
        string uuid = Guid.NewGuid().ToString();
        string fileName = $"{cameraName}_{uuid}.png";
        string filePath = Path.Combine(Application.dataPath, "SavedImages", fileName);

        // 确保保存目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

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
        Debug.Log($"Saved {cameraName} image to {filePath}");
    }
}