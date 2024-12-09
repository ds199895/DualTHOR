using UnityEngine;
using UnityEditor;

public class RemoveScripts: MonoBehaviour
{
    [MenuItem("Tools/Remove All Scripts and Missing Scripts in Scene")]
    public static void RemoveAllScriptsAndMissingScriptsInScene()
    {
        // ���ҳ��������е� GameObject
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int removedCountScripts = 0; // ͳ���Ƴ��Ľű�����
        int removedCountMissing = 0; // ͳ���Ƴ��ġ�Missing Script������

        // ����ÿ�� GameObject
        foreach (GameObject obj in allObjects)
        {
            // �Ƴ���Missing Script��
            int missingCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            removedCountMissing += missingCount;

            // ��ȡ���й��� GameObject �ϵ����
            Component[] components = obj.GetComponents<Component>();

            // ����ÿ�������ɾ�����м̳��� MonoBehaviour �Ľű�
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