// using UnityEditor;
// using UnityEngine;
// using UnityEditor.SceneManagement;
// using System.Collections.Generic;

// public class BatchAddRobotTool
// {
//     [MenuItem("工具/批量添加Robot到场景")]
//     static void AddRobotToScenes()
//     {
//         // 预制体路径
//         string prefabPath = "Assets/Prefabs/Robot.prefab";
//         GameObject robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
//         if(robotPrefab == null)
//         {
//             Debug.LogError("Robot预制体不存在: " + prefabPath);
//             return;
//         }
        
//         // 要处理的场景路径列表
//         List<string> scenePaths = new List<string>
//         {
//             "Assets/Scenes/Scene1.unity",
//             "Assets/Scenes/Scene2.unity",
//             // 添加更多场景
//         };
        
//         foreach(string scenePath in scenePaths)
//         {
//             EditorSceneManager.OpenScene(scenePath);
//             GameObject robot = PrefabUtility.InstantiatePrefab(robotPrefab) as GameObject;
//             EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
//             Debug.Log("已添加Robot到场景: " + scenePath);
//         }
//     }
// }
