using UnityEngine;

public class StructureCollisionDetector : MonoBehaviour
{
    public bool CollideStructure { get; private set; }
    private int structureLayer;

    private void Start()
    {
        // 获取structure层的索引
        structureLayer = LayerMask.NameToLayer("Structure");
        CollideStructure = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = true;
            Debug.Log($"与Structure层物体发生碰撞: {collision.gameObject.name}");
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = false;
            Debug.Log($"与Structure层物体结束碰撞: {collision.gameObject.name}");
        }
    }
} 