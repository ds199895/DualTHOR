using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System.Collections;

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
    }
    string PreprocessJson(string json)
    {
        return json.Replace("\"stateid\"", "\"stateID\"")
                   .Replace("\"objectid\"", "\"objectID\"")
                   .Replace("\"successrate\"", "\"successRate\"")
                   .Replace("\"robottype\"", "\"robotType\"");
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

                    if(actionJson.TrimStart().StartsWith("[")){
                        Debug.Log("Parse Array!");
                        string wrappedJson = "{\"actions\":" + actionJson + "}";
                        ActionDataArray actionDataArray = JsonUtility.FromJson<ActionDataArray>(wrappedJson);
                        Debug.Log("action length: " + actionDataArray.actions.Length);

                        foreach(var data in actionDataArray.actions){
                            await ProcessActionData(data);
                        }
                    }

                    actionJson = PreprocessJson(actionJson);
                    ActionData actionData = JsonUtility.FromJson<ActionData>(actionJson);
                    await ProcessActionData(actionData);
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
                await Task.Delay(2000);
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
        } 
        else {
            // 判断是否为交互类操作(pick、place、toggle、open)
            bool isInteractionAction = IsInteractionAction(actionData.action);
            
            // 如果是交互操作且提供了objectID，设置当前交互物体
            if (isInteractionAction && !string.IsNullOrEmpty(actionData.objectID)) {
                Debug.Log($"设置交互物体: {actionData.objectID} 用于 {actionData.action} 操作");
                agentMovement.SetCurrentInteractingObject(actionData.objectID);
            }
            
            // 执行动作并获取 JsonData 结果
            agentMovement.ExecuteActionWithCallback(actionData, (result) => {
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
}