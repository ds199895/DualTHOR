using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static UnityEngine.GUILayout;

namespace PrimeTween {
    internal class PrimeTweenInstaller : ScriptableObject {
        [SerializeField] internal SceneAsset demoScene;
        [SerializeField] internal SceneAsset demoSceneUrp;
        [SerializeField] internal Color uninstallButtonColor;

        [ContextMenu(nameof(ResetReviewRequest))] void ResetReviewRequest() => ReviewRequest.ResetReviewRequest();
        [ContextMenu(nameof(DebugReviewRequest))] void DebugReviewRequest() => ReviewRequest.DebugReviewRequest();
    }

    [CustomEditor(typeof(PrimeTweenInstaller), false)]
    internal class InstallerInspector : Editor {
        const string pluginName = "PrimeTween";
        const string pluginPackageId = "com.kyrylokuzyk.primetween";
        const string tgzPath = "Assets/Plugins/" + pluginName + "/internal/" + pluginPackageId + ".tgz";
        const string documentationUrl = "https://github.com/KyryloKuzyk/PrimeTween";
        bool isInstalled;
        GUIStyle boldButtonStyle;
        GUIStyle uninstallButtonStyle;
        GUIStyle wordWrapLabelStyle;

        void OnEnable() => isInstalled = CheckPluginInstalled();

        static bool CheckPluginInstalled() {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted) {
            }
            return listRequest.Result.Any(_ => _.name == pluginPackageId);
        }

        public override void OnInspectorGUI() {
            if (boldButtonStyle == null) {
                boldButtonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            }
            var installer = (PrimeTweenInstaller)target;
            if (uninstallButtonStyle == null) {
                uninstallButtonStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = installer.uninstallButtonColor } };
            }
            if (wordWrapLabelStyle == null) {
                wordWrapLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true, margin = new RectOffset(4, 4, 8, 8) };
            }
            EditorGUI.indentLevel = 5;
            Space(8);
            Label(pluginName, EditorStyles.boldLabel);
            Space(4);
            if (!isInstalled) {
                if (Button("Install " + pluginName)) {
                    installPlugin();
                }
                return;
            }
            if (Button("Documentation", boldButtonStyle)) {
                Application.OpenURL(documentationUrl);
            }

            Space(8);
            if (Button("Open Demo", boldButtonStyle)) {
                var rpAsset = GraphicsSettings.
                    #if UNITY_2019_3_OR_NEWER
                    defaultRenderPipeline;
                    #else
                    renderPipelineAsset;
                    #endif
                bool isUrp = rpAsset != null && rpAsset.GetType().Name.Contains("Universal");
                var demoScene = isUrp ? installer.demoSceneUrp : installer.demoScene;
                if (demoScene == null) {
                    Debug.LogError("Please re-import the plugin from Asset Store and import the 'Demo' folder.\n");
                    return;
                }
                var path = AssetDatabase.GetAssetPath(demoScene);
                EditorSceneManager.OpenScene(path);
            }
            #if UNITY_2019_4_OR_NEWER
            if (Button("Import Basic Examples")) {
                EditorUtility.DisplayDialog(pluginName, $"Please select the '{pluginName}' package in 'Package Manager', then press the 'Samples/Import' button at the bottom of the plugin's description.", "Ok");
                UnityEditor.PackageManager.UI.Window.Open(pluginPackageId);
            }
            #endif
            if (Button("Support")) {
                Application.OpenURL("https://github.com/KyryloKuzyk/PrimeTween#support");
            }

            Space(8);
            if (Button("Uninstall", uninstallButtonStyle)) {
                Client.Remove(pluginPackageId);
                isInstalled = false;
                var msg = $"Please remove the folder manually to uninstall {pluginName} completely: 'Assets/Plugins/{pluginName}'";
                EditorUtility.DisplayDialog(pluginName, msg, "Ok");
                Debug.Log(msg);
            }

            Space(24);
            Label("Updating from PrimeTween [1.1.10 - 1.1.22]", EditorStyles.boldLabel);
            Label("The behaviour of 'Sequence.ChainCallback()' and 'InsertCallback()' was fixed in PrimeTween 1.2.0 so the code written with older versions may work differently in some cases.", wordWrapLabelStyle);
            if (Button("Find potential issues")) {
                ChainInsertCallbackBug.Find();
            }
            BeginHorizontal();
            if (Button("More info")) {
                Application.OpenURL(ChainInsertCallbackBug.moreInfoUrl);
            }
            if (Button("Download version 1.1.22")) {
                Application.OpenURL("https://github.com/KyryloKuzyk/PrimeTween/blob/545dcc52769d52841e282c772e98c8984bfeb243/Benchmarks/Packages/com.kyrylokuzyk.primetween.tgz");
            }
            EndHorizontal();

            Space(24);
            Label("Enjoying PrimeTween?", EditorStyles.boldLabel);
            Label("Consider leaving an <b>honest review</b> and starring PrimeTween on GitHub!\n\n" +
                  "Honest reviews make PrimeTween better and help other developers discover it.", wordWrapLabelStyle);
            if (Button("Leave review!", GUI.skin.button)) {
                ReviewRequest.DisableReviewRequest();
                ReviewRequest.OpenReviewsURL();
            }
        }

        static void installPlugin() {
            ReviewRequest.OnBeforeInstall();
            var path = $"file:../{tgzPath}";
            var addRequest = Client.Add(path);
            while (!addRequest.IsCompleted) {
            }
            if (addRequest.Status == StatusCode.Success) {
                Debug.Log($"{pluginName} installed successfully.\n" +
                          $"Offline documentation is located at Packages/{pluginName}/Documentation.md.\n" +
                          $"Online documentation: {documentationUrl}\n");
            } else {
                Debug.LogError($"Please re-import the plugin from the Asset Store and check that the file exists: [{path}].\n\n{addRequest.Error?.message}\n");
            }
        }

        [InitializeOnLoadMethod]
        static void InitOnLoad() {
            AssetDatabase.importPackageCompleted += name => {
                if (name.Contains(pluginName)) {
                    if (ReviewRequest.PRIME_TWEEN_INSTALLED) {
                        ReviewRequest.TryAskForReview();
                    } else {
                        var installer = AssetDatabase.LoadAssetAtPath<PrimeTweenInstaller>("Assets/Plugins/PrimeTween/PrimeTweenInstaller.asset");
                        EditorUtility.FocusProjectWindow(); // this is important to show the installer object in the Project window
                        Selection.activeObject = installer;
                        EditorGUIUtility.PingObject(installer);
                        EditorApplication.update += InstallAndUnsubscribeFromUpdate;
                        void InstallAndUnsubscribeFromUpdate() {
                            EditorApplication.update -= InstallAndUnsubscribeFromUpdate;
                            installPlugin();
                        }
                    }
                }
            };
        }
    }

    internal static class ReviewRequest {
        internal const string version = "1.2.0";
        const string canAskKey = "PrimeTween.canAskForReview";
        const string versionKey = "PrimeTween.version";

        internal static void TryAskForReview() {
            log("TryAskForReview");
            if (!PRIME_TWEEN_INSTALLED) {
                log("not installed");
                return;
            }
            if (savedVersion == version) {
                log($"same version {version}");
                return;
            }
            bool shouldFindUpdateIssues = Version.TryParse(savedVersion, out var parsedSavedVersion)
                                           && new Version(1, 1, 10) <= parsedSavedVersion
                                           && parsedSavedVersion <= new Version(1, 1, 22);
            log($"updated from version {savedVersion} to {version}, shouldFindUpdateIssues: {shouldFindUpdateIssues}");
            savedVersion = version;
            if (shouldFindUpdateIssues) {
                ChainInsertCallbackBug.Find();
                int updateResponse = EditorUtility.DisplayDialogComplex("PrimeTween 1.2.0",
                    "PrimeTween 1.2.0 fixed a bug in ChainCallback() and InsertCallback() methods.\n" +
                    "This fix may introduce breaking changes in the existing projects. Please see the Console output for more details.",
                    "More info",
                    "Close",
                    "");
                if (updateResponse == 0) {
                    Application.OpenURL(ChainInsertCallbackBug.moreInfoUrl);
                }
                return; // don't ask for review if shouldFindUpdateIssues
            }
            if (!EditorPrefs.GetBool(canAskKey, true)) {
                log("can't ask");
                return;
            }
            DisableReviewRequest();
            var response = EditorUtility.DisplayDialogComplex("Enjoying PrimeTween?",
                "Would you mind to leave an honest review on Asset store? Honest reviews make PrimeTween better and help other developers discover it.",
                "Sure, leave a review!",
                "Never ask again",
                "");
            if (response == 0) {
                OpenReviewsURL();
            }
        }

        internal static bool PRIME_TWEEN_INSTALLED {
            get {
                #if PRIME_TWEEN_INSTALLED
                return true;
                #else
                return false;
                #endif
            }
        }

        internal static void OnBeforeInstall() {
            log($"OnBeforeInstall {version}");
            if (string.IsNullOrEmpty(savedVersion)) {
                savedVersion = version;
            }
        }

        static string savedVersion {
            get => EditorPrefs.GetString(versionKey);
            set => EditorPrefs.SetString(versionKey, value);
        }

        internal static void DisableReviewRequest() => EditorPrefs.SetBool(canAskKey, false);
        internal static void OpenReviewsURL() => Application.OpenURL("https://assetstore.unity.com/packages/slug/252960#reviews");

        internal static void ResetReviewRequest() {
            Debug.Log(nameof(ResetReviewRequest));
            EditorPrefs.DeleteKey(versionKey);
            EditorPrefs.DeleteKey(canAskKey);
        }

        internal static void DebugReviewRequest() {
            Debug.Log(nameof(DebugReviewRequest));
            savedVersion = "1.1.22";
            EditorPrefs.SetBool("PrimeTween.canAskForReview", false);
            // TryAskForReview();
        }

        [System.Diagnostics.Conditional("_")]
        static void log(string msg) {
            Debug.Log($"ReviewRequest: {msg}");
        }
    }

    internal static class ChainInsertCallbackBug {
        internal const string moreInfoUrl = "https://github.com/KyryloKuzyk/PrimeTween/discussions/112";
        static Dictionary<short, OpCode> OpcodeDict;
        static MethodInfo[] methodsWithBug;
        static MethodInfo[] groupMethods;

        internal static void Find() {
            OpcodeDict = typeof(OpCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(x => (OpCode)x.GetValue(null))
                .ToDictionary(x => x.Value, x => x);
            #if PRIME_TWEEN_INSTALLED
            methodsWithBug = typeof(Sequence).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(methodInfo => methodInfo.Name == nameof(Sequence.ChainCallback) || methodInfo.Name == nameof(Sequence.InsertCallback))
                .Select(methodInfo => methodInfo.IsGenericMethod ? methodInfo.GetGenericMethodDefinition() : methodInfo)
                .ToArray();
            Assert.AreEqual(4, methodsWithBug.Length);
            groupMethods = typeof(Sequence).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(methodInfo => methodInfo.Name == nameof(Sequence.Group))
                .ToArray();
            #endif
            Assert.AreEqual(2, groupMethods.Length);

            string methodAssemblyName = methodsWithBug[0].Module.Assembly.FullName;
            const BindingFlags findAll = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            int numPotentialIssues = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetReferencedAssemblies().Any(dependency => dependency.FullName == methodAssemblyName))
                .Where(assembly => !assembly.GetName().Name.StartsWith("PrimeTween.", StringComparison.Ordinal))
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(findAll).Cast<MethodBase>().Union(type.GetConstructors(findAll)))
                .Count(method => FindInMethod(method));
            if (numPotentialIssues == 0) {
                Debug.Log($"PrimeTween updated to version {ReviewRequest.version}: no potential issues found in ChainCallback() and InsertCallback() usages.\n" +
                          $"More info: {moreInfoUrl}\n");
            }
        }

        /// https://stackoverflow.com/a/33034906/1951038
        static bool FindInMethod(MethodBase method) {
            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null) {
                return false;
            }
            bool bugFound = false;
            using (var br = new BinaryReader(new MemoryStream(il))) {
                while (br.BaseStream.Position < br.BaseStream.Length) {
                    byte firstByte = br.ReadByte();
                    short opCodeValue = firstByte == 0xFE ? BitConverter.ToInt16(new[] { br.ReadByte(), firstByte }, 0) : firstByte;
                    OpCode opCode = OpcodeDict[opCodeValue];
                    switch (opCode.OperandType) {
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.ShortInlineVar:
                        case OperandType.ShortInlineI:
                            br.ReadByte();
                            break;
                        case OperandType.InlineVar:
                            br.ReadInt16();
                            break;
                        case OperandType.InlineField:
                        case OperandType.InlineType:
                        case OperandType.ShortInlineR:
                        case OperandType.InlineString:
                        case OperandType.InlineSig:
                        case OperandType.InlineI:
                        case OperandType.InlineBrTarget:
                            br.ReadInt32();
                            break;
                        case OperandType.InlineI8:
                        case OperandType.InlineR:
                            br.ReadInt64();
                            break;
                        case OperandType.InlineSwitch:
                            var size = (int)br.ReadUInt32();
                            br.ReadBytes(size * 4);
                            break;
                        case OperandType.InlineTok:
                            br.ReadUInt32();
                            break;
                        case OperandType.InlineMethod:
                            int token = (int)br.ReadUInt32();
                            if (method.Module.ResolveMethod(token) is MethodInfo resolvedMethod) {
                                if (bugFound) {
                                    if (groupMethods.Contains(resolvedMethod)) {
                                        Debug.LogError($"PrimeTween updated to version {ReviewRequest.version}: potential breaking change found in the '{method.DeclaringType}.{method.Name}()' method.\n" +
                                                       "Please double-check the behavior if Group() is called immediately after the ChainCallback() or InsertCallback() and apply the fix manually if necessary.\n" +
                                                       "Or use ChainCallbackObsolete/InsertCallbackObsolete() instead to preserve the old incorrect behavior.\n" +
                                                       $"More info: {moreInfoUrl}\n");
                                        return true;
                                    }
                                } else {
                                    bugFound = isMethodWithBug(resolvedMethod);
                                }
                            }
                            break;
                        case OperandType.InlineNone:
                            break;
                        default:
                            throw new Exception();
                    }
                }
            }
            return false;
        }

        static bool isMethodWithBug(MethodInfo method) {
            foreach (var methodWithBug in methodsWithBug) {
                if (methodWithBug.IsGenericMethodDefinition && method.IsGenericMethod) {
                    if (methodWithBug == method.GetGenericMethodDefinition()) {
                        return true;
                    }
                } else if (methodWithBug == method) {
                    return true;
                }
            }
            return false;
        }
    }
}
