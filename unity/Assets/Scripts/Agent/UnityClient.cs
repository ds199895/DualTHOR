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
        public string execution_mode = "sequential"; // "sequential" or "parallel"
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

                await client.ConnectAsync("127.0.0.1", 5678); // Asynchronous connection
                stream = client.GetStream();

                Debug.Log("Connected to server successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Connection error: {e.Message}");
                await Task.Delay(5000); // 5 seconds later retry
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
                        Debug.Log("Parse action array!");
                        await ProcessActionArray(actionJson);
                    }
                    else 
                    {
                        Debug.Log("Parse single action!");
                        // 单个动作处理
                        actionJson = PreprocessJson(actionJson);
                        Debug.Log("Parse single action: "+actionJson);
                        ActionData actionData = JsonUtility.FromJson<ActionData>(actionJson);
                        Debug.Log("Parse single action: "+actionData.ToString());
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
        
        string imagePath = Path.Combine(Application.dataPath, "SavedImages")+"/"+actionData.action+ Guid.NewGuid().ToString();
        sceneStateManager.camera_ctrl.imgeDir = imagePath;
        Debug.Log("Set image save path: "+sceneStateManager.camera_ctrl.imgeDir);
        sceneStateManager.camera_ctrl.record = true;
        
        // 设置深度相机开始保存
        depthCamera depthCam = FindObjectOfType<depthCamera>();
        if (depthCam != null)
        {
            // 在相同路径下创建depth子文件夹
            string depthPath = Path.Combine(imagePath, "depth");
            depthCam.StartSavingDepth(depthPath);
            Debug.Log("Depth camera started recording to: " + depthPath);
        }
        else
        {
            Debug.LogWarning("Depth camera not found, cannot save depth images");
        }
        
        // 尝试使用Capture360捕获全景图
        Capture360 capture360 = FindObjectOfType<Capture360>();
        if (capture360 != null)
        {
            // 在相同路径下创建cubemap子文件夹
            string cubemapPath = Path.Combine(imagePath, "cubemap");
            capture360.StartSavingCubemap(cubemapPath);
            Debug.Log("Cubemap capture started to: " + cubemapPath);
        }

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
            // No longer directly execute LoadState, but start a coroutine to handle it
            StartCoroutine(ResetStateWithScreenshots());
        } else if (actionData.action=="lift"){
            Debug.Log("lift action!");
            
            // Create image save path
            string imageDir = Path.Combine(Application.dataPath, "SavedImages") + "/lift_" + Guid.NewGuid().ToString();
            sceneStateManager.camera_ctrl.imgeDir = imageDir;
            sceneStateManager.camera_ctrl.record = true;
            
            // Record original position, for later detection
            GameObject targetObject = null;
            Vector3 originalPosition = Vector3.zero;
            if (sceneStateManager.SimObjectsDict.TryGetValue(actionData.objectID, out targetObject) && targetObject != null) {
                originalPosition = targetObject.transform.position;
            }
            
            // Start coroutine to execute Lift operation
            StartCoroutine(LiftWithCallback(actionData.objectID, originalPosition));
        }
        else {
            // Determine if it is an interaction operation (pick, place, toggle, open)
            bool isInteractionAction = IsInteractionAction(actionData.action);
            
            // If it is an interaction operation and provides an objectID, set the current interacting object
            if (isInteractionAction && !string.IsNullOrEmpty(actionData.objectID)) {
                Debug.Log($"Set interacting object: {actionData.objectID} for {actionData.action} operation");
                agentMovement.SetCurrentInteractingObject(actionData.objectID);
                
                // Add object to ignored collision list to avoid collision in interaction
                agentMovement.AddIgnoredCollisionObject(actionData.objectID);
            }
            
            Debug.Log("Execute action: "+actionData);
            // Record the start time, for performance analysis
            float startTime = Time.realtimeSinceStartup;
            
            // Execute action and get JsonData result
            agentMovement.ExecuteActionWithCallback(actionData, (result) => {
                // Calculate execution time
                float executionTime = Time.realtimeSinceStartup - startTime;
                Debug.Log($"Action {actionData.action} execution time: {executionTime:F3} seconds");
                
                // Send feedback based on action result
                Debug.Log($"Action result: success={result.success}, msg={result.msg}");
                
                if (actionData.action == "undo" || actionData.action == "redo") {
                    Debug.Log($"Skipping SaveCurrentState for action: {actionData.action}");
                } else {
                    sceneStateManager.SaveCurrentState();
                    Debug.Log($"Saved current state after action: {actionData.action}");
                }

                // Send feedback based on result information
                SendActionFeedbackToPython(result.success, result.msg);
                
                // After operation, clear the interacting object ID and ignored collision list
                if (isInteractionAction) {
                    agentMovement.ClearIgnoredCollisionObjects();
                    Debug.Log($"Cleared interacting object ID and ignored collision list, operation: {actionData.action}");
                }
            });
        }
    }

    // Helper method to determine if it is an interaction operation
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
                    string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // Escape backslash
                    
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"imgpath\":\"{imagePath}\", \"msg\": \"{msg}\", \"x1position\": \"{currentPosition}\", \"sceneState\": {sceneStateJson}}}";
                }
                else
                {
                    // Simplified feedback
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"msg\": \"{msg}\"}}";
                }

                Debug.Log(feedback);
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
                
                // Clean up state
                Debug.Log("Feedback sent, now cleaning up state");
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
        
        // 停止深度相机保存
        depthCamera depthCam = FindObjectOfType<depthCamera>();
        if (depthCam != null)
        {
            depthCam.StopSavingDepth();
            depthCam.ResetImageCount();
        }
        
        // 确保Capture360已完成捕获
        Capture360 capture360 = FindObjectOfType<Capture360>();
        if (capture360 != null)
        {
            capture360.saveCubemap = false;
            Debug.Log("Ensured cubemap capture is stopped");
        }
    }

    public void SendFeedbackToPython(bool success)
    {
        SendFeedbackToPython(success, success ? "operation success" : "operation failed");
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
                    string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // Escape backslash
                    
                    // Simplified feedback, no longer check collision information
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"imgpath\":\"{imagePath}\", \"msg\": \"{msg}\", \"x1position\": \"{currentPosition}\", \"sceneState\": {sceneStateJson}}}";
                }
                else
                {
                    // Simplified feedback
                    feedback = $"{{\"success\": {(success ? 1 : 0)}, \"msg\": \"{msg}\"}}";
                }

                Debug.Log(feedback);
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
                
                // Clean up state
                Debug.Log("Feedback sent, now cleaning up state");
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
            
            // 停止深度相机保存
            depthCamera depthCam = FindObjectOfType<depthCamera>();
            if (depthCam != null)
            {
                depthCam.StopSavingDepth();
                depthCam.ResetImageCount();
            }
            
            // 确保Capture360已完成捕获
            Capture360 capture360 = FindObjectOfType<Capture360>();
            if (capture360 != null)
            {
                capture360.saveCubemap = false;
                Debug.Log("Ensured cubemap capture is stopped");
            }
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

    // Add a new coroutine to handle screenshots before and after resetting state
    private IEnumerator ResetStateWithScreenshots()
    {
        Debug.Log("Start resetting state process - capture initial screenshot");
        
        // Set image save path, using resetstate prefix and unique ID
        string imagePath = Path.Combine(Application.dataPath, "SavedImages") + "/resetstate_" + Guid.NewGuid().ToString();
        sceneStateManager.camera_ctrl.imgeDir = imagePath;
        
        // Ensure camera controller starts recording
        sceneStateManager.camera_ctrl.record = true;
        
        // 设置深度相机开始保存
        depthCamera depthCam = FindObjectOfType<depthCamera>();
        if (depthCam != null)
        {
            // 在相同路径下创建depth子文件夹
            string depthPath = Path.Combine(imagePath, "depth");
            depthCam.StartSavingDepth(depthPath);
            Debug.Log("Depth camera started recording to: " + depthPath);
        }
        
        // 尝试使用Capture360捕获全景图
        Capture360 capture360 = FindObjectOfType<Capture360>();
        if (capture360 != null)
        {
            // 在相同路径下创建cubemap子文件夹
            string cubemapPath = Path.Combine(imagePath, "cubemap");
            capture360.StartSavingCubemap(cubemapPath);
            Debug.Log("Cubemap capture started to: " + cubemapPath);
        }
        
        // Wait for a few frames to ensure the screenshot is complete
        yield return new WaitForSeconds(0.5f);
        
        // Execute state reset
        Debug.Log("Execute state reset operation");
        bool result = agentMovement.LoadState("0");
        
        // Wait for state loading and rendering to complete
        yield return new WaitForSeconds(1.0f);
        
        // Wait for a few more frames to ensure the scene is stable
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // Result feedback
        if(result){
            SendActionFeedbackToPython(result, "reset state success");
        }else{
            SendActionFeedbackToPython(result, "reset state failed");
        }
    }

    // New method: process action array
    private async Task ProcessActionArray(string actionJson)
    {
        try
        {
            // Set recording path - ensure unique ID for each dual arm operation
            string imagePath = Path.Combine(Application.dataPath, "SavedImages") + "/dualarm_action_" + Guid.NewGuid().ToString();
            sceneStateManager.camera_ctrl.imgeDir = imagePath;
            sceneStateManager.camera_ctrl.record = true;
            
            // 设置深度相机开始保存
            depthCamera depthCam = FindObjectOfType<depthCamera>();
            if (depthCam != null)
            {
                // 在相同路径下创建depth子文件夹
                string depthPath = Path.Combine(imagePath, "depth");
                depthCam.StartSavingDepth(depthPath);
                Debug.Log("Depth camera started recording to: " + depthPath);
            }
            
            // 尝试使用Capture360捕获全景图
            Capture360 capture360 = FindObjectOfType<Capture360>();
            if (capture360 != null)
            {
                // 在相同路径下创建cubemap子文件夹
                string cubemapPath = Path.Combine(imagePath, "cubemap");
                capture360.StartSavingCubemap(cubemapPath);
                Debug.Log("Cubemap capture started to: " + cubemapPath);
            }
            
            Debug.Log("Start dual arm action recording...");
            
            // Parse JSON array to ActionData array
            // string wrappedJson = "{\"actions\":" + actionJson + "}";
            ActionDataArray actionDataArray = JsonUtility.FromJson<ActionDataArray>(actionJson);
            
            if (actionDataArray == null || actionDataArray.actions == null || actionDataArray.actions.Length == 0)
            {
                Debug.LogError("Cannot parse action array or array is empty");
                SendFeedbackToPython(false, "Cannot parse action array or array is empty");
                return;
            }
            
            Debug.Log($"Successfully parsed action array, containing {actionDataArray.actions.Length} actions");
            
            // Get execution mode directly from ActionDataArray
            string executionMode = actionDataArray.executionMode?.ToLower() ?? "sequential";
            Debug.Log($"Get execution mode from JSON: {executionMode}");
            
            // If still not found execution mode, try to extract from original JSON
            if (string.IsNullOrEmpty(executionMode) && actionJson.Contains("executionMode"))
            {
                try
                {
                    // Try to extract execution mode directly from original JSON
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
                                Debug.Log("Extracted execution mode: parallel");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error extracting execution mode: {ex.Message}, using default sequential execution mode");
                }
            }
            
            // Update the last executed action in the state manager
            if (sceneStateManager != null)
            {
                // Set the current action as dual arm action
                sceneStateManager.UpdateLastAction("dualarm_" + (executionMode == "parallel" ? "parallel" : "sequential"));
                Debug.Log($"Set last action to: dualarm_{executionMode}");
            }
            
            // Prepare result list
            List<ActionResult> results = new List<ActionResult>();
            
            // Process actions based on execution mode
            if (executionMode == "parallel")
            {
                // Parallel execution mode
                Debug.Log("Using parallel execution mode");
                await ExecuteActionsInParallel(actionDataArray.actions, results);
            }
            else
            {
                // Sequential execution mode
                Debug.Log("Using sequential execution mode");
                await ExecuteActionsSequentially(actionDataArray.actions, results);
            }
            
            // Send feedback containing all results
            SendMultiActionFeedbackToPython(results);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing action array: {ex.Message}");
            SendFeedbackToPython(false, $"Error processing action array: {ex.Message}");
            
            // Ensure recording stops
            sceneStateManager.camera_ctrl.record = false;
            sceneStateManager.camera_ctrl.ResetImageCount();
            
            // 停止深度相机保存
            depthCamera depthCam = FindObjectOfType<depthCamera>();
            if (depthCam != null)
            {
                depthCam.StopSavingDepth();
                depthCam.ResetImageCount();
            }
            
            // 确保Capture360已完成捕获
            Capture360 capture360 = FindObjectOfType<Capture360>();
            if (capture360 != null)
            {
                capture360.saveCubemap = false;
                Debug.Log("Ensured cubemap capture is stopped");
            }
        }
    }
    
    // Sequential execution of actions
    private async Task ExecuteActionsSequentially(ActionData[] actions, List<ActionResult> results)
    {
        foreach (var actionData in actions)
        {
            try
            {
                // Process each action and wait for completion
                var result = await ProcessActionWithResult(actionData);
                results.Add(result);
                
                Debug.Log($"Sequential execution: completed action {actionData.action}, result: {(result.success ? "success" : "failed")}");
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing action {actionData.action}: {ex.Message}");
                results.Add(new ActionResult(false, $"Error executing action: {ex.Message}", actionData.arm, actionData.action));
                
                // Continue to execute the next action even if an error occurs
            }
        }
        
        // After all actions are completed, update the last executed action name
        if (actions.Length > 0 && sceneStateManager != null)
        {
            var lastAction = actions[actions.Length - 1];
            sceneStateManager.UpdateLastAction(lastAction.action);
        }
    }
    
    // Parallel execution of actions
    private async Task ExecuteActionsInParallel(ActionData[] actions, List<ActionResult> results)
    {
        // Create task list
        List<Task<ActionResult>> tasks = new List<Task<ActionResult>>();
        
        foreach (var actionData in actions)
        {
            // Add task to process each action
            tasks.Add(ProcessActionWithResult(actionData));
        }
        
        // Wait for all tasks to complete
        ActionResult[] taskResults = await Task.WhenAll(tasks);
        
        // Add results to result list
        results.AddRange(taskResults);
        
        Debug.Log($"Parallel execution: completed {taskResults.Length} actions");
        
        // After parallel execution, update the last executed action (use the first action)
        if (actions.Length > 0 && sceneStateManager != null)
        {
            sceneStateManager.UpdateLastAction("dualarm_action");
        }
    }
    
    // Process a single action and return the result
    private async Task<ActionResult> ProcessActionWithResult(ActionData actionData)
    {
        TaskCompletionSource<ActionResult> tcs = new TaskCompletionSource<ActionResult>();
        
        try
        {
            // Special processing for actions that do not need to save state
            string actionLower = actionData.action.ToLower();
            bool isSpecialAction = actionLower == "undo" || 
                                  actionLower == "redo" || 
                                  actionLower == "loadstate" || 
                                  actionLower == "resetstate" ||
                                  actionLower == "getcurstate";
            
            if (isSpecialAction)
            {
                Debug.Log($"Processing special action: {actionData.action}");
                bool success = false;
                string msg = "";
                
                // Direct execution of special operations
                switch (actionLower)
                {
                    case "undo":
                        success = agentMovement.Undo();
                        msg = success ? "Undo operation success" : "Undo operation failed";
                        Debug.Log($"Executing Undo operation: {(success ? "success" : "failed")}");
                        break;
                        
                    case "redo":
                        success = agentMovement.Redo();
                        msg = success ? "Redo operation success" : "Redo operation failed";
                        Debug.Log($"Executing Redo operation: {(success ? "success" : "failed")}");
                        break;
                        
                    case "loadstate":
                        if (!string.IsNullOrEmpty(actionData.stateID))
                        {
                            success = agentMovement.LoadState(actionData.stateID);
                            msg = success ? $"Load state {actionData.stateID} success" : $"Load state {actionData.stateID} failed";
                            Debug.Log($"Executing LoadState operation: {(success ? "success" : "failed")}");
                        }
                        else
                        {
                            success = false;
                            msg = "Missing stateID parameter";
                            Debug.LogError("LoadState operation missing stateID parameter");
                        }
                        break;
                        
                    case "resetstate":
                        success = agentMovement.LoadState("0");
                        msg = success ? "Reset state success" : "Reset state failed";
                        Debug.Log($"Executing ResetState operation: {(success ? "success" : "failed")}");
                        break;
                        
                    case "getcurstate":
                        success = true;
                        msg = "Get current state";
                        Debug.Log("Executing GetCurrentState operation");
                        break;
                }
                
                // Immediately return the result
                return new ActionResult(
                    success,
                    msg,
                    actionData.arm,
                    actionData.action
                );
            }
            
            // Determine if it is an interaction operation (pick, place, toggle, open)
            bool isInteractionAction = IsInteractionAction(actionData.action);
            
            // If it is an interaction operation and provides an objectID, set the current interacting object
            if (isInteractionAction && !string.IsNullOrEmpty(actionData.objectID)) {
                Debug.Log($"Setting interacting object: {actionData.objectID} for {actionData.action} operation");
                agentMovement.SetCurrentInteractingObject(actionData.objectID);
                
                // Add object to ignored collision list to avoid collision in interaction
                agentMovement.AddIgnoredCollisionObject(actionData.objectID);
            }
            
            // Set result callback
            agentMovement.ExecuteActionWithCallback(actionData, (jsonData) => {
                // Create result object
                ActionResult result = new ActionResult(
                    jsonData.success,
                    jsonData.msg,
                    actionData.arm,
                    actionData.action
                );
                
                // After operation, clear the interacting object ID and ignored collision list
                if (isInteractionAction) {
                    agentMovement.ClearIgnoredCollisionObjects();
                    Debug.Log($"Cleared interacting object ID and ignored collision list, operation: {actionData.action}");
                }
                
                // Note: Here we do not save state, we will save it after all actions are completed
                
                // Complete the task
                tcs.SetResult(result);
            });
            
            // Wait for the task to complete
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing action {actionData.action}: {ex.Message}");
            return new ActionResult(false, $"Error processing action: {ex.Message}", actionData.arm, actionData.action);
        }
    }
    
    // Send multi-action feedback
    private void SendMultiActionFeedbackToPython(List<ActionResult> results)
    {
        if (client != null && stream != null)
        {
            try
            {
                // Save current scene state before sending feedback
                if (sceneStateManager != null)
                {
                    // Ensure saving the final state after all actions are completed
                    sceneStateManager.SaveCurrentState();
                    Debug.Log("Saved scene state after dual arm action execution");
                }
                
                // Get current scene state
                SceneStateA2T currentSceneState = sceneStateManager.GetCurrentSceneStateA2T();
                string sceneStateJson = JsonUtility.ToJson(currentSceneState);
                string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // Escape backslash
                
                // Calculate overall success status (all actions must be successful to be considered successful)
                bool overallSuccess = results.All(r => r.success);
                
                // Build result JSON array
                string resultsJson = "[" + string.Join(",", results.Select(r => JsonUtility.ToJson(r))) + "]";
                
                // Build complete feedback
                string feedback = $"{{\"success\": {(overallSuccess ? 1 : 0)}, \"imgpath\":\"{imagePath}\", \"sceneState\": {sceneStateJson}, \"results\": {resultsJson}}}";
                
                Debug.Log($"Sending multi-action feedback: {feedback}");
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
                
                // Clean up state
                Debug.Log("Feedback sent, now cleaning up state");
                agentMovement.ClearCollisions();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending multi-action feedback: {ex.GetType().Name} - {ex.Message}");
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

    // Add new coroutine to handle Lift operation
    private IEnumerator LiftWithCallback(string objectID, Vector3 originalPosition)
    {
        bool success = false;
        string message = "lift operation not completed";
        
        // Create image save path
        string imageDir = Path.Combine(Application.dataPath, "SavedImages") + "/lift_" + Guid.NewGuid().ToString();
        sceneStateManager.camera_ctrl.imgeDir = imageDir;
        sceneStateManager.camera_ctrl.record = true;
        
        // 设置深度相机开始保存
        depthCamera depthCam = FindObjectOfType<depthCamera>();
        if (depthCam != null)
        {
            // 在相同路径下创建depth子文件夹
            string depthPath = Path.Combine(imageDir, "depth");
            depthCam.StartSavingDepth(depthPath);
            Debug.Log("Depth camera started recording to: " + depthPath);
        }
        
        // 尝试使用Capture360捕获全景图
        Capture360 capture360 = FindObjectOfType<Capture360>();
        if (capture360 != null)
        {
            // 在相同路径下创建cubemap子文件夹
            string cubemapPath = Path.Combine(imageDir, "cubemap");
            capture360.StartSavingCubemap(cubemapPath);
            Debug.Log("Cubemap capture started to: " + cubemapPath);
        }
        
        // Execute lift operation (put it outside the try block)
        yield return StartCoroutine(agentMovement.Lift(objectID));
        
        try {
            // Check if the operation is successful
            GameObject targetObject = null;
            if (sceneStateManager.SimObjectsDict.TryGetValue(objectID, out targetObject) && targetObject != null) {
                // Check height change
                Vector3 currentPosition = targetObject.transform.position;
                bool heightIncreased = currentPosition.y > originalPosition.y + 0.05f; // Height increased by at least 5 cm
                
                // Check if it is held
                bool isHeld = targetObject.transform.parent != null && 
                             (targetObject.transform.parent.CompareTag("Hand") || 
                              targetObject.transform.parent.name.Contains("Gripper"));
                
                success = heightIncreased || isHeld;
                if (success) {
                    message = "lift operation completed successfully";
                    Debug.Log($"Lift success: object {objectID} height increased from {originalPosition.y} to {currentPosition.y}");
                } else {
                    message = "lift operation failed: object not lifted correctly";
                    Debug.LogWarning($"Lift failed: object {objectID} height from {originalPosition.y} to {currentPosition.y}, gripper state: {isHeld}");
                }
            } else {
                success = false;
                message = $"lift operation failed: object {objectID} not found";
                Debug.LogError($"Object {objectID} not found");
            }
        } catch (Exception ex) {
            success = false;
            message = $"lift operation failed: {ex.Message}";
            Debug.LogError($"Lift operation failed: {ex.Message}");
        }
        
        // Update the action result in the state manager
        if (sceneStateManager != null) {
            sceneStateManager.UpdateLastActionSuccess("lift");
            if (!success && sceneStateManager.GetCurrentSceneStateA2T() != null && 
                sceneStateManager.GetCurrentSceneStateA2T().agent != null) {
                sceneStateManager.GetCurrentSceneStateA2T().agent.errorMessage = message;
            }
            
            // Save current state
            sceneStateManager.SaveCurrentState();
        }
        
        // Send result to Python
        SendFeedbackToPython(success, message);
        
        // Ensure recording stops
        sceneStateManager.camera_ctrl.record = false;
        sceneStateManager.camera_ctrl.ResetImageCount();
        
        // 停止深度相机保存
        if (depthCam != null)
        {
            depthCam.StopSavingDepth();
            depthCam.ResetImageCount();
        }
        
        // 确保Capture360已完成捕获
        if (capture360 != null)
        {
            capture360.saveCubemap = false;
            Debug.Log("Ensured cubemap capture is stopped");
        }
    }
}