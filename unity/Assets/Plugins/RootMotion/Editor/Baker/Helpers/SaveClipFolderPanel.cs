<<<<<<< HEAD
﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace RootMotion
{
    public class SaveClipFolderPanel : EditorWindow
    {
        public static string Apply(string currentPath)
        {
            string path = EditorUtility.SaveFolderPanel("Save clip(s) to folder", currentPath, "");

            if (path.Length != 0)
            {
                return path.Substring(path.IndexOf("Assets/"));
            }

            return currentPath;
        }
    }
=======
﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace RootMotion
{
    public class SaveClipFolderPanel : EditorWindow
    {
        public static string Apply(string currentPath)
        {
            string path = EditorUtility.SaveFolderPanel("Save clip(s) to folder", currentPath, "");

            if (path.Length != 0)
            {
                return path.Substring(path.IndexOf("Assets/"));
            }

            return currentPath;
        }
    }
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
}