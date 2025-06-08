using UnityEngine;

public class DepthShaderManager : MonoBehaviour
{
    public Shader depthShader;
    public Material depthMaterial;
    
    void Awake()
    {
        // 尝试查找或加载深度着色器
        if (depthShader == null)
        {
            depthShader = Shader.Find("Custom/DepthTest");
            
            if (depthShader == null)
            {
                Debug.LogError("Could not find Custom/DepthTest shader! Please ensure the shader is included in the project.");
            }
            else
            {
                Debug.Log("Successfully found Custom/DepthTest shader.");
                depthMaterial = new Material(depthShader);
            }
        }
        else
        {
            depthMaterial = new Material(depthShader);
            Debug.Log("Using provided Custom/DepthTest shader.");
        }
        
        // 添加查找场景中的所有深度相机，并设置着色器
        depthCamera[] depthCameras = FindObjectsOfType<depthCamera>();
        foreach (depthCamera cam in depthCameras)
        {
            if (cam.depthShader == null && depthShader != null)
            {
                cam.depthShader = depthShader;
                Debug.Log($"Assigned depth shader to camera: {cam.name}");
            }
        }
    }
} 