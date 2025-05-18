using UnityEngine;

public class Capture360 : MonoBehaviour
{
    public Camera cameraToCapture;
    public RenderTexture cubemap;

    void Start()
    {
        // Ensure the camera exists and is ready to capture
        if (cameraToCapture != null)
        {
            // Create a new stereo rendering cubemap, passing in the camera
            cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            cameraToCapture.RenderToCubemap(cubemap);
        }
    }
}
