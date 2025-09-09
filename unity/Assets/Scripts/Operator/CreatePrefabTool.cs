// using UnityEditor;
// using UnityEngine;

// public class CreatePrefabTool
// {
//     [MenuItem("工具/将Robot创建为预制体")]
//     static void CreateRobotPrefab()
//     {
//         GameObject robot = GameObject.Find("Robot");
//         if(robot == null)
//         {
//             Debug.LogError("未找到Robot对象");
//             return;
//         }
        
//         string localPath = "Assets/Prefabs/Robot.prefab";
//         // 确保目录存在
//         System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localPath));
        
//         PrefabUtility.SaveAsPrefabAsset(robot, localPath);
//         Debug.Log("Robot预制体已创建: " + localPath);
//     }
// }
