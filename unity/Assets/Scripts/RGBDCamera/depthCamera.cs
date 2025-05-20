using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class depthCamera: MonoBehaviour
{
    public Camera depthCam;
    public RenderTexture depthRT;
    public Shader depthShader;
    private Material depthMaterial;
    
    public string depthSavePath;
    public bool saveDepth = false;
    public int depthImageCount = 0;
    
    [Range(0.01f, 100f)]
    public float depthScale = 1.0f; // 控制深度值缩放
    
    // Start is called before the first frame update
    void Start()
    {
        if (depthCam == null)
        {
            depthCam = GetComponent<Camera>();
        }
        
        depthCam.depthTextureMode = DepthTextureMode.Depth;
        
        // 创建深度材质
        if (depthShader != null)
        {
            depthMaterial = new Material(depthShader);
        }
        else
        {
            // 使用内置着色器，这通常更可靠
            depthShader = Shader.Find("Hidden/Internal-DepthNormalsTexture");
            if (depthShader != null)
            {
                depthMaterial = new Material(depthShader);
            }
            else
            {
                // 尝试找自定义着色器
                depthShader = Shader.Find("Custom/DepthTest");
                if (depthShader != null)
                {
                    depthMaterial = new Material(depthShader);
                }
                else
                {
                    Debug.LogError("Failed to find depth shader!");
                }
            }
        }
        
        // 创建深度渲染纹理
        if (depthRT == null)
        {
            depthRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGBFloat);
            depthRT.Create();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (saveDepth && !string.IsNullOrEmpty(depthSavePath))
        {
            CaptureDepth();
        }
    }
    
    public void CaptureDepth()
    {
        if (depthMaterial == null || depthRT == null) return;
        
        // 确保目录存在
        if (!Directory.Exists(depthSavePath))
        {
            Directory.CreateDirectory(depthSavePath);
        }
        
        // 创建渲染纹理和自定义着色器以可视化深度
        RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        
        // 确保相机已经更新了深度纹理
        depthCam.Render();
        
        // 自定义深度可视化（不再使用depthMaterial）
        Material visualizeMaterial = new Material(Shader.Find("Unlit/Texture"));
        
        // 创建临时Texture2D
        Texture2D depthTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        
        // 使用相机的targetTexture来获取正确的深度信息
        RenderTexture currentRT = RenderTexture.active;
        
        // 渲染深度图到渲染纹理
        depthCam.targetTexture = rt;
        depthCam.Render();
        depthCam.targetTexture = null;
        
        // 将深度纹理复制到临时纹理
        RenderTexture.active = rt;
        depthTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        
        // 处理深度数据，应用更好的映射
        Color[] pixels = depthTexture.GetPixels();
        
        // 深度信息通常存储在相机的深度缓冲区中，这个部分需要提取并映射
        // 使用自定义的深度提取和可视化
        for (int i = 0; i < pixels.Length; i++)
        {
            float depth = pixels[i].r;
            // 应用更好的深度映射
            depth = 1.0f - depth; // 翻转深度值，使近距离为亮色
            depth = Mathf.Pow(depth, 0.5f) * depthScale; // 使用幂函数改善可视化
            pixels[i] = new Color(depth, depth, depth, 1);
        }
        
        depthTexture.SetPixels(pixels);
        depthTexture.Apply();
        
        // 恢复原来的RenderTexture
        RenderTexture.active = currentRT;
        
        // 保存为PNG
        byte[] bytes = depthTexture.EncodeToPNG();
        string filename = $"depth_{depthImageCount:D4}.png";
        string filepath = Path.Combine(depthSavePath, filename);
        File.WriteAllBytes(filepath, bytes);
        
        // 增加计数
        depthImageCount++;
        
        // 清理
        RenderTexture.ReleaseTemporary(rt);
        Destroy(depthTexture);
        Destroy(visualizeMaterial);
        
        Debug.Log($"Saved depth image to: {filepath}");
    }
    
    // 直接从相机的深度缓冲区获取深度图
    private Texture2D GetDepthTexture(Camera camera)
    {
        // 创建临时RT来存储深度
        RenderTexture rt = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 24);
        RenderTexture.active = rt;
        camera.targetTexture = rt;
        camera.Render();
        camera.targetTexture = null;
        
        // 创建深度纹理
        Texture2D depthTex = new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        depthTex.ReadPixels(new Rect(0, 0, camera.pixelWidth, camera.pixelHeight), 0, 0);
        depthTex.Apply();
        
        // 处理深度值
        Color[] pixels = depthTex.GetPixels();
        
        // 查找最小和最大深度值以进行归一化
        float minDepth = float.MaxValue;
        float maxDepth = float.MinValue;
        
        for (int i = 0; i < pixels.Length; i++)
        {
            float depth = pixels[i].r;
            if (depth > 0)
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
                float normalizedDepth = (pixels[i].r - minDepth) / depthRange;
                normalizedDepth = 1.0f - normalizedDepth; // 翻转深度值
                pixels[i] = new Color(normalizedDepth, normalizedDepth, normalizedDepth, 1);
            }
        }
        
        depthTex.SetPixels(pixels);
        depthTex.Apply();
        
        // 清理
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return depthTex;
    }
    
    public void ResetImageCount()
    {
        depthImageCount = 0;
    }
    
    public void StartSavingDepth(string path)
    {
        depthSavePath = path;
        saveDepth = true;
        depthImageCount = 0;
        Debug.Log($"Started saving depth images to: {depthSavePath}");
    }
    
    public void StopSavingDepth()
    {
        saveDepth = false;
        Debug.Log("Stopped saving depth images");
    }
}
