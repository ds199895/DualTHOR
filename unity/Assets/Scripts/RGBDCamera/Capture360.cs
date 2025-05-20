using UnityEngine;
using System.IO;

public class Capture360 : MonoBehaviour
{
    public Camera cameraToCapture;
    public RenderTexture cubemap;
    public Shader depthShader;
    private Material depthMaterial;
    public RenderTexture depthCubemap;
    
    public bool saveCubemap = false;
    public string cubemapSavePath;
    
    [Range(0.01f, 100f)]
    public float depthScale = 1.0f; // 控制深度值缩放
    
    void Start()
    {
        // 确保相机存在并准备捕获
        if (cameraToCapture == null)
        {
            cameraToCapture = GetComponent<Camera>();
        }
        
        if (cameraToCapture != null)
        {
            // 确保全景图渲染纹理存在
            if (cubemap == null)
            {
                cubemap = new RenderTexture(2048, 2048, 24);
                cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            }
            
            // 确保深度全景图渲染纹理存在
            if (depthCubemap == null)
            {
                depthCubemap = new RenderTexture(2048, 2048, 24);
                depthCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            }
            
            // 设置相机可以捕获深度
            cameraToCapture.depthTextureMode = DepthTextureMode.Depth;
            
            // 初始化深度着色器
            if (depthShader == null)
            {
                // 尝试内置深度法线着色器
                depthShader = Shader.Find("Hidden/Internal-DepthNormalsTexture");
                if (depthShader == null)
                {
                    depthShader = Shader.Find("Custom/DepthTest");
                }
            }
            
            if (depthShader != null)
            {
                depthMaterial = new Material(depthShader);
            }
            else
            {
                Debug.LogError("Failed to find depth shader for cubemap!");
            }
        }
    }
    
    void Update()
    {
        if (saveCubemap && !string.IsNullOrEmpty(cubemapSavePath))
        {
            CaptureCubemap();
            saveCubemap = false;
        }
    }
    
    public void CaptureCubemap()
    {
        if (cameraToCapture == null || cubemap == null) 
        {
            Debug.LogError("Cannot capture cubemap: missing required components");
            return;
        }
        
        // 确保目录存在
        if (!Directory.Exists(cubemapSavePath))
        {
            Directory.CreateDirectory(cubemapSavePath);
        }
        
        // 渲染颜色全景图
        cameraToCapture.RenderToCubemap(cubemap);
        
        // 保存颜色全景图的每个面
        SaveCubemapFaces(cubemap, "color");
        
        // 捕获深度信息并保存
        CaptureAndSaveDepthCubemap();
        
        Debug.Log($"Captured and saved cubemap to: {cubemapSavePath}");
    }
    
    private void SaveCubemapFaces(RenderTexture cubemap, string prefix)
    {
        string[] faceNames = new string[] { "posx", "negx", "posy", "negy", "posz", "negz" };
        
        for (int face = 0; face < 6; face++)
        {
            // 创建2D渲染纹理来保存单个面，使用sRGB来确保gamma校正
            RenderTexture faceTexture = new RenderTexture(cubemap.width, cubemap.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.SetRenderTarget(faceTexture);
            GL.Clear(true, true, Color.black);
            
            // 从立方体贴图中提取面
            Graphics.CopyTexture(cubemap, face, 0, faceTexture, 0, 0);
            
            // 创建临时渲染纹理用于色彩校正
            RenderTexture tempRT = RenderTexture.GetTemporary(
                faceTexture.width, 
                faceTexture.height, 
                0, 
                RenderTextureFormat.ARGB32, 
                RenderTextureReadWrite.sRGB);
                
            // 应用色彩校正
            Graphics.Blit(faceTexture, tempRT);
            
            // 将渲染纹理转换为Texture2D
            Texture2D tex = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGB24, false);
            RenderTexture.active = tempRT;
            tex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            tex.Apply();
            
            // 保存为PNG
            byte[] bytes = tex.EncodeToPNG();
            string filename = $"{prefix}_{faceNames[face]}.png";
            string filepath = Path.Combine(cubemapSavePath, filename);
            File.WriteAllBytes(filepath, bytes);
            
            // 清理
            RenderTexture.active = null;
            Destroy(tex);
            RenderTexture.ReleaseTemporary(tempRT);
            faceTexture.Release();
            Destroy(faceTexture);
        }
    }
    
    private void CaptureAndSaveDepthCubemap()
    {
        if (cameraToCapture == null) return;
        
        string[] faceNames = new string[] { "posx", "negx", "posy", "negy", "posz", "negz" };
        
        // 保存当前相机设置
        CameraClearFlags originalClearFlags = cameraToCapture.clearFlags;
        RenderTexture originalTargetTexture = cameraToCapture.targetTexture;
        bool originalAllowHDR = cameraToCapture.allowHDR;
        
        // 配置相机捕获深度
        cameraToCapture.clearFlags = CameraClearFlags.Depth;
        cameraToCapture.depthTextureMode = DepthTextureMode.Depth;
        cameraToCapture.allowHDR = true;
        
        // 为每个面创建单独的渲染纹理
        for (int face = 0; face < 6; face++)
        {
            // 创建渲染纹理
            RenderTexture faceRT = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGBFloat);
            
            // 设置相机朝向
            Quaternion[] rotations = new Quaternion[]
            {
                Quaternion.LookRotation(Vector3.right, Vector3.up),    // posx
                Quaternion.LookRotation(Vector3.left, Vector3.up),     // negx
                Quaternion.LookRotation(Vector3.up, Vector3.forward),  // posy
                Quaternion.LookRotation(Vector3.down, Vector3.back),   // negy
                Quaternion.LookRotation(Vector3.forward, Vector3.up),  // posz
                Quaternion.LookRotation(Vector3.back, Vector3.up),     // negz
            };
            
            // 保存原始旋转
            Quaternion originalRotation = cameraToCapture.transform.rotation;
            
            // 设置相机旋转以捕获当前面
            cameraToCapture.transform.rotation = rotations[face];
            
            // 渲染到纹理
            cameraToCapture.targetTexture = faceRT;
            cameraToCapture.Render();
            
            // 创建深度可视化的纹理
            Texture2D depthTexture = new Texture2D(faceRT.width, faceRT.height, TextureFormat.RGBA32, false);
            RenderTexture.active = faceRT;
            depthTexture.ReadPixels(new Rect(0, 0, faceRT.width, faceRT.height), 0, 0);
            depthTexture.Apply();
            
            // 处理深度值
            Color[] pixels = depthTexture.GetPixels();
            
            // 查找最小和最大深度值以进行归一化
            float minDepth = float.MaxValue;
            float maxDepth = float.MinValue;
            
            for (int i = 0; i < pixels.Length; i++)
            {
                // 使用深度值
                float depth = pixels[i].r;
                if (depth > 0.0001f) // 排除0深度值
                {
                    minDepth = Mathf.Min(minDepth, depth);
                    maxDepth = Mathf.Max(maxDepth, depth);
                }
            }
            
            float depthRange = maxDepth - minDepth;
            if (depthRange > 0)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    float depth = pixels[i].r;
                    if (depth > 0.0001f) // 排除0深度值
                    {
                        float normalizedDepth = (depth - minDepth) / depthRange;
                        normalizedDepth = 1.0f - normalizedDepth; // 翻转以便近=白色，远=黑色
                        normalizedDepth = Mathf.Pow(normalizedDepth, 0.5f) * depthScale; // 使用幂函数增强可视化
                        pixels[i] = new Color(normalizedDepth, normalizedDepth, normalizedDepth, 1);
                    }
                    else
                    {
                        pixels[i] = Color.black; // 背景设为黑色
                    }
                }
            }
            else
            {
                // 如果所有深度都相同或没找到有效深度，使用原始深度值
                for (int i = 0; i < pixels.Length; i++)
                {
                    float depth = 1.0f - pixels[i].r; // 翻转深度值
                    pixels[i] = new Color(depth, depth, depth, 1);
                }
            }
            
            depthTexture.SetPixels(pixels);
            depthTexture.Apply();
            
            // 保存深度图像
            byte[] bytes = depthTexture.EncodeToPNG();
            string filename = $"depth_{faceNames[face]}.png";
            string filepath = Path.Combine(cubemapSavePath, filename);
            File.WriteAllBytes(filepath, bytes);
            
            // 恢复相机旋转
            cameraToCapture.transform.rotation = originalRotation;
            
            // 清理
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(faceRT);
            Destroy(depthTexture);
        }
        
        // 恢复相机设置
        cameraToCapture.clearFlags = originalClearFlags;
        cameraToCapture.targetTexture = originalTargetTexture;
        cameraToCapture.allowHDR = originalAllowHDR;
    }
    
    public void StartSavingCubemap(string path)
    {
        cubemapSavePath = path;
        saveCubemap = true;
        Debug.Log($"Started cubemap capture to: {cubemapSavePath}");
    }
}
