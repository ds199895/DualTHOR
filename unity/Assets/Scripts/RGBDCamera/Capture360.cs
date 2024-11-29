using UnityEngine;

public class Capture360 : MonoBehaviour
{
    public Camera cameraToCapture;
    public RenderTexture cubemap;

    void Start()
    {
        // ȷ��������ڲ�׼���ý��в���
        if (cameraToCapture != null)
        {
            // ����һ���µ�������Ⱦ����ͼ���������
            cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            cameraToCapture.RenderToCubemap(cubemap);
        }
    }
}
