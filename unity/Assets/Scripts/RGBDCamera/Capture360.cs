using UnityEngine;

public class Capture360 : MonoBehaviour
{
    public Camera cameraToCapture;
    public RenderTexture cubemap;

    void Start()
    {
        // 确保相机存在并准备好进行捕获
        if (cameraToCapture != null)
        {
            // 创建一个新的立体渲染立方图，传入相机
            cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            cameraToCapture.RenderToCubemap(cubemap);
        }
    }
}