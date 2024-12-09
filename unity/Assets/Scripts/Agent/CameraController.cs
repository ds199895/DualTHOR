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
        // ȷ�������������
        EnableAllCameras();
    }

    void Update()
    {
        HandleEscAndMouseInput();

        // ����Ƿ�����FreeCamģʽ
        if (Input.GetKeyDown(KeyCode.Alpha0)) ActivateFreeMode();

        if (isFreeMode && isCameraControlEnabled) FreeCamControl();

        // ���� 'P' ��ʱ�����������ͼ��
        if (Input.GetKeyDown(KeyCode.P)) SaveAllCameraImages();
    }

    private void EnableAllCameras()
    {
        // �������������ȷ��ͬʱ��Ⱦ
        egocentricCam.enabled = true;
        frontCam.enabled = true;
        leftSideCam.enabled = true;
        rightSideCam.enabled = true;
        behindCam.enabled = true;
        freeCam.enabled = false; // Ĭ������²�����FreeCam
    }

    void ActivateFreeMode()
    {
        // �������������ģʽ
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

        // �����ƶ���ǰ�����ң�
        Vector3 move = freeCam.transform.right * Input.GetAxis("Horizontal") +
                       freeCam.transform.forward * Input.GetAxis("Vertical");

        // ��Ӵ�ֱ�ƶ�����
        float verticalMove = 0f;
        if (Input.GetKey(KeyCode.Space)) verticalMove += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) verticalMove -= 1f;
        move += Vector3.up * verticalMove;

        // Ӧ���ƶ�
        freeCam.transform.position += moveSpeed * Time.deltaTime * move * (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f);

        // �����ת
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

    // �����������ͼ�񵽱���
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
        // ����UUID�������ļ���
        string uuid = Guid.NewGuid().ToString();
        string fileName = $"{cameraName}_{uuid}.png";
        string filePath = Path.Combine(Application.dataPath, "SavedImages", fileName);

        // ȷ������Ŀ¼����
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // ��RenderTexture���ݱ��浽PNG�ļ�
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