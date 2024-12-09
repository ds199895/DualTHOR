using UnityEngine;
using UnityEditor;

public class RemoveScripts: MonoBehaviour
{
    [MenuItem("Tools/Remove All Scripts and Missing Scripts in Scene")]
    public static void RemoveAllScriptsAndMissingScriptsInScene()
    {
        // 查找场景中所有的 GameObject
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int removedCountScripts = 0; // 统计移除的脚本数量
        int removedCountMissing = 0; // 统计移除的“Missing Script”数量

        // 遍历每个 GameObject
        foreach (GameObject obj in allObjects)
        {
            // 移除“Missing Script”
            int missingCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            removedCountMissing += missingCount;

            // 获取所有挂在 GameObject 上的组件
            Component[] components = obj.GetComponents<Component>();

            // 遍历每个组件，删除所有继承自 MonoBehaviour 的脚本
            foreach (Component component in components)
            {
                if (component is MonoBehaviour && !(component is Transform))
                {
                    DestroyImmediate(component);
                    removedCountScripts++;
                }
            }
        }

        Debug.Log($"Removed all scripts and missing scripts from scene objects. Total removed scripts: {removedCountScripts}, Total removed missing scripts: {removedCountMissing}");
    }
}