using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class UnityClient : MonoBehaviour
{
    private static UnityClient instance;
    public static UnityClient Instance => instance;

    TcpClient client;
    NetworkStream stream;
    public AgentMovement agentMovement;
    public SceneStateManager sceneStateManager;

    [Serializable]
    public class ActionData
    {
        public string action;
        public float magnitude;
        public string arm;  
        public string objectID;
        public float successRate;
        public string stateID;
        public string robotType;
        public string scene;
    }
    
    [Serializable]
    public class ActionDataArray
    {
        public ActionData[] actions;
        public string executionMode;
    }
    
    [Serializable]
    public class ActionResult
    {
        public bool success;
        public string msg;
        public string arm;
        public string action;
        
        public ActionResult(bool success, string msg, string arm, string action)
        {
            this.success = success;
            this.msg = msg;
            this.arm = arm;
            this.action = action;
        }
    }
    
    [Serializable]
    public class DualArmActionMetadata
    {
        public string execution_mode = "sequential"; // "sequential" 或 "parallel"
    }
    
    string PreprocessJson(string json)
    {
        return json.Replace("\"stateid\"", "\"stateID\"")
                   .Replace("\"objectid\"", "\"objectID\"")
                   .Replace("\"successrate\"", "\"successRate\"")
                   .Replace("\"robottype\"", "\"robotType\"")
                   .Replace("\"execution_mode\"", "\"executionMode\"");
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        Init();
        ConnectToServerAsync();
    }

    void Init(){
        
        sceneStateManager = GameObject.Find("SceneManager").GetComponent<SceneStateManager>();
        Debug.Log("Set scenestate manager ",sceneStateManager);
        // Debug.Log(sceneStateManager);
        agentMovement=FindAnyObjectByType<AgentMovement>();
        Debug.Log("Set scenestate manager ",agentMovement);
    }

    async void ConnectToServerAsync()
    {
        while (client == null || !client.Connected)
        {
            try
            {
                client = new TcpClient();
                Debug.Log("Attempting to connect to server at 127.0.0.1:5678...");

                await client.ConnectAsync("127.0.0.1", 5678); // 异步连接
                stream = client.GetStream();

                Debug.Log("Connected to server successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Connection error: {e.Message}");
                await Task.Delay(5000); // 5 秒后重试
            }
        }
    }

    async void Update()
    {
        if (client != null && stream != null && stream.DataAvailable)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string actionJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"Received action from Python: {actionJson}");

                    // 检查是否是动作数组
                    if(actionJson.TrimStart().Contains("["))
                    {
                        Debug.Log("解析动作数组!");
                        await ProcessActionArray(actionJson);
                    }
                    else 
                    {
                        Debug.Log("解析单个动作!");
                        // 单个动作处理
                        actionJson = PreprocessJson(actionJson);
                        Debug.Log("解析单个动作: "+actionJson);
                        ActionData actionData = JsonUtility.FromJson<ActionData>(actionJson);
                        Debug.Log("解析单个动作: "+actionData.ToString());
                        await ProcessActionData(actionData);
                    }
                }
                else
                {
                    Debug.LogWarning("Received empty data from stream.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred while reading stream: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }

    private async Task ProcessActionData(ActionData actionData) {
        Debug.Log("Start recording .....");
        
        sceneStateManager.camera_ctrl.imgeDir=Path.Combine(Application.dataPath, "SavedImages")+"/"+actionData.action+ Guid.NewGuid().ToString();
        Debug.Log("设置图像保存路径: "+sceneStateManager.camera_ctrl.imgeDir);
        sceneStateManager.camera_ctrl.record=true;

        Debug.Log("Parsed Action Data: "+actionData.ToString());
        if (string.IsNullOrEmpty(actionData.action)) {
            Debug.LogError("ActionData does not contain a valid action.");
            SendFeedbackToPython(false,"Error: Missing action in ActionData.");
            return;
        }

        if (actionData.action == "loadstate") {
            if (!string.IsNullOrEmpty(actionData.stateID)) {
                var result = agentMovement.LoadState(actionData.stateID);
                Debug.Log($"Loaded scene state with ID: {actionData.stateID}");
                SendFeedbackToPython(result,"load state feedback");
            } else {
                Debug.LogError("State ID is missing in ActionData for LoadSceneState.");
                SendFeedbackToPython(false,"Error: Missing state ID for LoadSceneState.");
            }
        } else if (actionData.action == "loadrobot") {

            if (agentMovement == null) {
                Debug.LogWarning("AgentMovement is null, waiting for 2 seconds...");
                await Task.Delay(5000);
                Init();
            }
            var result = agentMovement.LoadRobot(actionData.robotType);
            Debug.Log($"Loaded robot of type: {actionData.robotType}");
            SendFeedbackToPython(result,"load robot feedback");
        } else if (actionData.action == "resetscene") {
            var result = agentMovement.LoadScene(actionData.scene,actionData.robotType);
            Debug.Log($"Loaded scene: {actionData.scene},Robot type:{actionData.robotType}");
            Init();
            sceneStateManager.SaveCurrentState();
            SendFeedbackToPython(result,"reset scene feedback");
        } else if (actionData.action=="getcurstate") {
            SendFeedbackToPython(true,"get current scene");
        } else if (actionData.action=="resetpose"){
            Debug.Log("reset pose action!");
            var result = agentMovement.ResetPose();
            SendFeedbackToPython(result,"reset pose");
        } else if (actionData.action=="resetstate"){
            Debug.Log("reset state action!");
            // 不再直接执行LoadState，而是启动协程处理
            StartCoroutine(ResetStateWithScreenshots());
        } else if (actionData.action=="lift"){
            Debug.Log("lift action!");
            
            // 创建图像保存路径
            string imageDir = Path.Combine(Application.dataPath, "SavedImages") + "/lift_" + Guid.NewGuid().ToString();
            sceneStateManager.camera_ctrl.imgeDir = imageDir;
            sceneStateManager.camera_ctrl.record = true;
            
            // 记录原始位置，用于后续检测
            GameObject targetObject = null;
            Vector3 originalPosition = Vector3.zero;
            if (sceneStateManager.SimObjectsDict.TryGetValue(actionData.objectID, out targetObject) && targetObject != null) {
                originalPosition = targetObject.transform.position;
            }
            
            // 启动协程执行Lift操作
            StartCoroutine(LiftWithCallback(actionData.objectID, originalPosition));
        }
        else {
            // 判断是否为交互类操作(pick、place、toggle、open)
            bool isInteractionAction = IsInteractionAction(actionData.action);
            
            // 如果是交互操作且提供了objectID，设置当前交互物体
            if (isInteractionAction && !string.IsNullOrEmpty(actionData.objectID)) {
                Debug.Log($"设置交互物体: {actionData.objectID} 用于 {actionData.action} 操作");
                agentMovement.SetCurrentInteractingObject(actionData.objectID);
                
                // 添加物体到忽略碰撞列表，避免交互中的碰撞被视为失败
                agentMovement.AddIgnoredCollisionObject(actionData.objectID);
            }
            
            Debug.Log("执行动作: "+actionData);
            // 记录动作开始时间，用于性能分析
            float startTime = Time.realtimeSinceStartup;
            
            // 执行动作并获取 JsonData 结果
            agentMovement.ExecuteActionWithCallback(actionData, (result) => {
                // 计算执行时间
                float executionTime = Time.realtimeSinceStartup - startTime;
                Debug.Log($"动作 {actionData.action} 执行时间: {executionTime:F3} 秒");
                
                // 根据动作结果发送反馈
                Debug.Log($"Action result: success={result.success}, msg={result.msg}");
                
                if (actionData.action == "undo" || actionData.action == "redo") {
                    Debug.Log($"Skipping SaveCurrentState for action: {actionData.action}");
                } else {
                    sceneStateManager.SaveCurrentState();
                    Debug.Log($"Saved current state after action: {actionData.action}");
                }

                // 发送反馈，根据 result 中的信息
                SendActionFeedbackToPython(result.success, result.msg);
                
                // 操作完成后清除交互物体ID和忽略碰撞列表
                if (isInteractionAction) {
                    agentMovement.ClearIgnoredCollisionObjects();
                    Debug.Log($"已清除交互物体ID和忽略碰撞列表，操作: {actionData.action}");
                }
            });
        }
    }

    // 判断是否为交互类操作的辅助方法
    private bool IsInteractionAction(string action)
    {
        string lowerAction = action.ToLower();
        return lowerAction.Contains("pick") || 
               lowerAction.Contains("place") || 
               lowerAction.Contains("toggle") || 
               lowerAction.Contains("open");
    }

    public void SendActionFeedbackToPython(bool success, string msg)
    {
        if (client != null && stream != null)
        {
            try
            {
                Vector3 currentPosition = transform.position;
                string feedback = "";
                
                if (sceneStateManager)
                {
                    SceneStateA2T currentSceneState = sceneStateManager.GetCurrentSceneStateA2T();
                    string sceneStateJson = JsonUtility.ToJson(currentSceneState);
                    string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // 转义反斜杠
                    
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"imgpath\":\"{imagePath}\", \"msg\": \"{msg}\", \"x1position\": \"{currentPosition}\", \"sceneState\": {sceneStateJson}}}";
                }
                else
                {
                    // 简化版反馈
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"msg\": \"{msg}\"}}";
                }

                Debug.Log(feedback);
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
                
                // 清理状态
                Debug.Log("反馈已发送，现在清理状态");
                agentMovement.ClearCollisions();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred while sending feedback: {ex.GetType().Name} - {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot send feedback: client or stream is null.");
        }
        
        Debug.Log("Stop recording");
        sceneStateManager.camera_ctrl.record = false;
        sceneStateManager.camera_ctrl.ResetImageCount();
    }

    public void SendFeedbackToPython(bool success)
    {
        SendFeedbackToPython(success, success ? "操作成功" : "操作失败");
    }

    public void SendFeedbackToPython(bool success, string msg = "")
    {
        if (client != null && stream != null)
        {
            try
            {
                Vector3 currentPosition = transform.position;
                string feedback = "";
                
                if (sceneStateManager)
                {
                    SceneStateA2T currentSceneState = sceneStateManager.GetCurrentSceneStateA2T();
                    string sceneStateJson = JsonUtility.ToJson(currentSceneState);
                    string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // 转义反斜杠
                    
                    // 简化后的反馈，不再检查碰撞信息
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"imgpath\":\"{imagePath}\", \"msg\": \"{msg}\", \"x1position\": \"{currentPosition}\", \"sceneState\": {sceneStateJson}}}";
                }
                else
                {
                    // 简化版反馈
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"msg\": \"{msg}\"}}";
                }

                Debug.Log(feedback);
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
                
                // 清理状态
                Debug.Log("反馈已发送，现在清理状态");
                agentMovement.ClearCollisions();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred while sending feedback: {ex.GetType().Name} - {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot send feedback: client or stream is null.");
        }
        
        Debug.Log("Stop recording");
        if (sceneStateManager != null && sceneStateManager.camera_ctrl != null)
        {
            sceneStateManager.camera_ctrl.record = false;
            sceneStateManager.camera_ctrl.ResetImageCount();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit - Closing client and stream.");

        try
        {
            if (stream != null)
            {
                stream.Close();
                //Debug.Log("Stream closed successfully.");
            }

            if (client != null)
            {
                client.Close();
                //Debug.Log("Client closed successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception occurred while closing client/stream: {ex.GetType().Name} - {ex.Message}");
        }
    }

    // 添加一个新的协程来处理重置状态前后的截图
    private IEnumerator ResetStateWithScreenshots()
    {
        Debug.Log("开始重置状态过程 - 捕获初始截图");
        
        // 设置图像保存路径，使用resetstate前缀和唯一ID
        sceneStateManager.camera_ctrl.imgeDir = Path.Combine(Application.dataPath, "SavedImages") + "/resetstate_" + Guid.NewGuid().ToString();
        
        // 确保相机控制器开始记录
        sceneStateManager.camera_ctrl.record = true;
        
        // 等待几帧以确保截图完成
        yield return new WaitForSeconds(0.5f);
        
        // 执行状态重置
        Debug.Log("执行状态重置操作");
        bool result = agentMovement.LoadState("0");
        
        // 等待状态加载和渲染完成
        yield return new WaitForSeconds(1.0f);
        
        // 再等待几帧确保场景稳定
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // 结果反馈
        if(result){
            SendActionFeedbackToPython(result, "reset state success");
        }else{
            SendActionFeedbackToPython(result, "reset state failed");
        }
    }

    // 新方法：处理动作数组
    private async Task ProcessActionArray(string actionJson)
    {
        try
        {
            // 设置录制路径 - 确保每次双臂操作都有唯一ID
            sceneStateManager.camera_ctrl.imgeDir = Path.Combine(Application.dataPath, "SavedImages") + "/dualarm_action_" + Guid.NewGuid().ToString();
            sceneStateManager.camera_ctrl.record = true;
            
            Debug.Log("开始双臂动作录制...");
            
            // 将JSON数组解析为ActionData数组
            // string wrappedJson = "{\"actions\":" + actionJson + "}";
            ActionDataArray actionDataArray = JsonUtility.FromJson<ActionDataArray>(actionJson);
            
            if (actionDataArray == null || actionDataArray.actions == null || actionDataArray.actions.Length == 0)
            {
                Debug.LogError("无法解析动作数组或数组为空");
                SendFeedbackToPython(false, "动作数组解析失败或为空");
                return;
            }
            
            Debug.Log($"成功解析动作数组，包含 {actionDataArray.actions.Length} 个动作");
            
            // 从ActionDataArray中直接获取执行模式
            string executionMode = actionDataArray.executionMode?.ToLower() ?? "sequential";
            Debug.Log($"从JSON中获取执行模式: {executionMode}");
            
            // 如果仍未找到执行模式，尝试从原始JSON提取
            if (string.IsNullOrEmpty(executionMode) && actionJson.Contains("executionMode"))
            {
                try
                {
                    // 尝试直接从原始JSON中提取执行模式
                    int modeIndex = actionJson.IndexOf("executionMode");
                    if (modeIndex > 0)
                    {
                        int valueStart = actionJson.IndexOf(":", modeIndex) + 1;
                        int valueEnd = actionJson.IndexOf(",", valueStart);
                        if (valueEnd < 0) valueEnd = actionJson.IndexOf("}", valueStart);
                        
                        if (valueStart > 0 && valueEnd > valueStart)
                        {
                            string modeValue = actionJson.Substring(valueStart, valueEnd - valueStart).Trim();
                            // 去除可能的引号
                            modeValue = modeValue.Replace("\"", "").Replace("'", "").Trim();
                            
                            if (modeValue == "parallel")
                            {
                                executionMode = "parallel";
                                Debug.Log("已从JSON中提取执行模式: parallel");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"提取执行模式时出错: {ex.Message}，使用默认顺序执行模式");
                }
            }
            
            // 更新状态管理器中的最后执行动作
            if (sceneStateManager != null)
            {
                // 设置当前动作为双臂动作
                sceneStateManager.UpdateLastAction("dualarm_" + (executionMode == "parallel" ? "parallel" : "sequential"));
                Debug.Log($"已设置最后动作为: dualarm_{executionMode}");
            }
            
            // 准备结果列表
            List<ActionResult> results = new List<ActionResult>();
            
            // 根据执行模式处理动作
            if (executionMode == "parallel")
            {
                // 并行执行模式
                Debug.Log("使用并行执行模式");
                await ExecuteActionsInParallel(actionDataArray.actions, results);
            }
            else
            {
                // 顺序执行模式
                Debug.Log("使用顺序执行模式");
                await ExecuteActionsSequentially(actionDataArray.actions, results);
            }
            
            // 发送包含所有结果的反馈
            SendMultiActionFeedbackToPython(results);
        }
        catch (Exception ex)
        {
            Debug.LogError($"处理动作数组时发生错误: {ex.Message}");
            SendFeedbackToPython(false, $"处理动作数组错误: {ex.Message}");
            
            // 确保停止录制
            sceneStateManager.camera_ctrl.record = false;
            sceneStateManager.camera_ctrl.ResetImageCount();
        }
    }
    
    // 顺序执行动作
    private async Task ExecuteActionsSequentially(ActionData[] actions, List<ActionResult> results)
    {
        foreach (var actionData in actions)
        {
            try
            {
                // 处理每个动作并等待完成
                var result = await ProcessActionWithResult(actionData);
                results.Add(result);
                
                Debug.Log($"顺序执行：完成动作 {actionData.action}，结果: {(result.success ? "成功" : "失败")}");
                
                // 对于顺序执行，每个动作完成后都保存一次状态
                // 注释掉这里，因为我们将在所有动作完成后统一保存状态
                // if (sceneStateManager != null && actionData.action != "undo" && actionData.action != "redo")
                // {
                //     sceneStateManager.SaveCurrentState();
                //     Debug.Log($"已保存动作 {actionData.action} 执行后的状态");
                // }
            }
            catch (Exception ex)
            {
                Debug.LogError($"执行动作 {actionData.action} 时发生错误: {ex.Message}");
                results.Add(new ActionResult(false, $"执行错误: {ex.Message}", actionData.arm, actionData.action));
                
                // 即使出错，也继续执行下一个动作
            }
        }
        
        // 所有动作完成后，更新最后一个执行的动作名称
        if (actions.Length > 0 && sceneStateManager != null)
        {
            var lastAction = actions[actions.Length - 1];
            sceneStateManager.UpdateLastAction(lastAction.action);
        }
    }
    
    // 并行执行动作
    private async Task ExecuteActionsInParallel(ActionData[] actions, List<ActionResult> results)
    {
        // 创建任务列表
        List<Task<ActionResult>> tasks = new List<Task<ActionResult>>();
        
        foreach (var actionData in actions)
        {
            // 添加处理每个动作的任务
            tasks.Add(ProcessActionWithResult(actionData));
        }
        
        // 等待所有任务完成
        ActionResult[] taskResults = await Task.WhenAll(tasks);
        
        // 将结果添加到结果列表
        results.AddRange(taskResults);
        
        Debug.Log($"并行执行：完成 {taskResults.Length} 个动作");
        
        // 并行执行完成后，更新最后一个动作（使用第一个动作）
        if (actions.Length > 0 && sceneStateManager != null)
        {
            sceneStateManager.UpdateLastAction("dualarm_action");
        }
    }
    
    // 处理单个动作并返回结果
    private async Task<ActionResult> ProcessActionWithResult(ActionData actionData)
    {
        TaskCompletionSource<ActionResult> tcs = new TaskCompletionSource<ActionResult>();
        
        try
        {
            // 特殊处理不需要保存状态的操作
            string actionLower = actionData.action.ToLower();
            bool isSpecialAction = actionLower == "undo" || 
                                  actionLower == "redo" || 
                                  actionLower == "loadstate" || 
                                  actionLower == "resetstate" ||
                                  actionLower == "getcurstate";
            
            if (isSpecialAction)
            {
                Debug.Log($"处理特殊动作: {actionData.action}");
                bool success = false;
                string msg = "";
                
                // 直接执行特殊操作
                switch (actionLower)
                {
                    case "undo":
                        success = agentMovement.Undo();
                        msg = success ? "撤销操作成功" : "撤销操作失败";
                        Debug.Log($"执行Undo操作: {(success ? "成功" : "失败")}");
                        break;
                        
                    case "redo":
                        success = agentMovement.Redo();
                        msg = success ? "重做操作成功" : "重做操作失败";
                        Debug.Log($"执行Redo操作: {(success ? "成功" : "失败")}");
                        break;
                        
                    case "loadstate":
                        if (!string.IsNullOrEmpty(actionData.stateID))
                        {
                            success = agentMovement.LoadState(actionData.stateID);
                            msg = success ? $"加载状态 {actionData.stateID} 成功" : $"加载状态 {actionData.stateID} 失败";
                            Debug.Log($"执行LoadState操作: {(success ? "成功" : "失败")}");
                        }
                        else
                        {
                            success = false;
                            msg = "缺少stateID参数";
                            Debug.LogError("LoadState操作缺少stateID参数");
                        }
                        break;
                        
                    case "resetstate":
                        success = agentMovement.LoadState("0");
                        msg = success ? "重置状态成功" : "重置状态失败";
                        Debug.Log($"执行ResetState操作: {(success ? "成功" : "失败")}");
                        break;
                        
                    case "getcurstate":
                        success = true;
                        msg = "获取当前状态";
                        Debug.Log("执行GetCurrentState操作");
                        break;
                }
                
                // 立即返回结果
                return new ActionResult(
                    success,
                    msg,
                    actionData.arm,
                    actionData.action
                );
            }
            
            // 判断是否为交互类操作(pick、place、toggle、open)
            bool isInteractionAction = IsInteractionAction(actionData.action);
            
            // 如果是交互操作且提供了objectID，设置当前交互物体
            if (isInteractionAction && !string.IsNullOrEmpty(actionData.objectID)) {
                Debug.Log($"设置交互物体: {actionData.objectID} 用于 {actionData.action} 操作");
                agentMovement.SetCurrentInteractingObject(actionData.objectID);
                
                // 添加物体到忽略碰撞列表，避免交互中的碰撞被视为失败
                agentMovement.AddIgnoredCollisionObject(actionData.objectID);
            }
            
            // 设置结果回调
            agentMovement.ExecuteActionWithCallback(actionData, (jsonData) => {
                // 创建结果对象
                ActionResult result = new ActionResult(
                    jsonData.success,
                    jsonData.msg,
                    actionData.arm,
                    actionData.action
                );
                
                // 操作完成后清除交互物体ID和忽略碰撞列表
                if (isInteractionAction) {
                    agentMovement.ClearIgnoredCollisionObjects();
                    Debug.Log($"已清除交互物体ID和忽略碰撞列表，操作: {actionData.action}");
                }
                
                // 注意：这里不保存状态，我们将在所有动作完成后统一保存
                
                // 完成任务
                tcs.SetResult(result);
            });
            
            // 等待任务完成
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Debug.LogError($"处理动作 {actionData.action} 发生异常: {ex.Message}");
            return new ActionResult(false, $"处理异常: {ex.Message}", actionData.arm, actionData.action);
        }
    }
    
    // 发送多动作反馈
    private void SendMultiActionFeedbackToPython(List<ActionResult> results)
    {
        if (client != null && stream != null)
        {
            try
            {
                // 在发送反馈前保存当前场景状态
                if (sceneStateManager != null)
                {
                    // 确保在完成所有动作后保存最终状态
                    sceneStateManager.SaveCurrentState();
                    Debug.Log("已保存双臂动作执行后的场景状态");
                }
                
                // 获取当前场景状态
                SceneStateA2T currentSceneState = sceneStateManager.GetCurrentSceneStateA2T();
                string sceneStateJson = JsonUtility.ToJson(currentSceneState);
                string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // 转义反斜杠
                
                // 计算整体成功状态（所有动作都成功才算成功）
                bool overallSuccess = results.All(r => r.success);
                
                // 构建结果JSON数组
                string resultsJson = "[" + string.Join(",", results.Select(r => JsonUtility.ToJson(r))) + "]";
                
                // 构建完整反馈
                string feedback = $"{{\"success\": {(overallSuccess ? 1 : 0)}, \"imgpath\":\"{imagePath}\", \"sceneState\": {sceneStateJson}, \"results\": {resultsJson}}}";
                
                Debug.Log($"发送多动作反馈: {feedback}");
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
                
                // 清理状态
                Debug.Log("反馈已发送，现在清理状态");
                agentMovement.ClearCollisions();
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送多动作反馈时发生异常: {ex.GetType().Name} - {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("无法发送反馈: client 或 stream 为 null.");
        }
        
        Debug.Log("停止录制");
        sceneStateManager.camera_ctrl.record = false;
        sceneStateManager.camera_ctrl.ResetImageCount();
    }

    // 添加新的协程处理Lift操作
    private IEnumerator LiftWithCallback(string objectID, Vector3 originalPosition)
    {
        bool success = false;
        string message = "lift操作未完成";
        
        // 执行lift操作（放在try块外）
        yield return StartCoroutine(agentMovement.Lift(objectID));
        
        try {
            // 检查操作是否成功
            GameObject targetObject = null;
            if (sceneStateManager.SimObjectsDict.TryGetValue(objectID, out targetObject) && targetObject != null) {
                // 检查高度变化
                Vector3 currentPosition = targetObject.transform.position;
                bool heightIncreased = currentPosition.y > originalPosition.y + 0.05f; // 高度增加至少5厘米
                
                // 检查是否被抓取
                bool isHeld = targetObject.transform.parent != null && 
                             (targetObject.transform.parent.CompareTag("Hand") || 
                              targetObject.transform.parent.name.Contains("Gripper"));
                
                success = heightIncreased || isHeld;
                if (success) {
                    message = "lift操作成功完成";
                    Debug.Log($"Lift成功: 物体{objectID}高度从{originalPosition.y}增加到{currentPosition.y}");
                } else {
                    message = "lift操作失败：物体未被正确抬起";
                    Debug.LogWarning($"Lift失败: 物体{objectID}高度从{originalPosition.y}到{currentPosition.y}，抓取状态：{isHeld}");
                }
            } else {
                success = false;
                message = $"lift操作失败：找不到物体{objectID}";
                Debug.LogError($"找不到物体{objectID}");
            }
        } catch (Exception ex) {
            success = false;
            message = $"lift操作异常: {ex.Message}";
            Debug.LogError($"Lift操作发生异常: {ex.Message}");
        }
        
        // 更新状态管理器中的动作结果
        if (sceneStateManager != null) {
            sceneStateManager.UpdateLastActionSuccess("lift");
            if (!success && sceneStateManager.GetCurrentSceneStateA2T() != null && 
                sceneStateManager.GetCurrentSceneStateA2T().agent != null) {
                sceneStateManager.GetCurrentSceneStateA2T().agent.errorMessage = message;
            }
            
            // 保存当前状态
            sceneStateManager.SaveCurrentState();
        }
        
        // 发送结果到Python
        SendFeedbackToPython(success, message);
        
        // 确保停止录制
        sceneStateManager.camera_ctrl.record = false;
        sceneStateManager.camera_ctrl.ResetImageCount();
    }
}