using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

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
            SendFeedbackToPython( false,"Error: Missing action in ActionData.");
            return;
        }

        if (actionData.action == "loadstate") {
            if (!string.IsNullOrEmpty(actionData.stateID)) {
                agentMovement.LoadState(actionData.stateID);
                Debug.Log($"Loaded scene state with ID: {actionData.stateID}");
                SendFeedbackToPython( true,$"Loaded state ID: {actionData.stateID}");
            } else {
                Debug.LogError("State ID is missing in ActionData for LoadSceneState.");
                SendFeedbackToPython( false,"Error: Missing state ID for LoadSceneState.");
            }
        } else if (actionData.action == "loadrobot") {

            if (agentMovement == null) {
                Debug.LogWarning("AgentMovement is null, waiting for 2 seconds...");
                await Task.Delay(2000);
                Init();
            }
            var result = agentMovement.LoadRobot(actionData.robotType);
            Debug.Log($"Loaded robot of type: {actionData.robotType}");
            SendFeedbackToPython( result,"load robot feedback");
        } else if (actionData.action == "resetscene") {
    
            var result = agentMovement.LoadScene(actionData.scene,actionData.robotType);
            Debug.Log($"Loaded scene: {actionData.scene},Robot type:{actionData.robotType}");
            Init();
            sceneStateManager.SaveCurrentState();
            SendFeedbackToPython(result);
        } else if (actionData.action=="getcurstate") {
            SendFeedbackToPython(true,"get current scene");
        } else if (actionData.action=="resetpose"){
            Debug.Log("reset pose action!");
            agentMovement.ResetPose();
            SendFeedbackToPython(true,"reset pose");
        } 
        else {
            agentMovement.ExecuteActionWithCallback(actionData, () => {
                bool success = true; // Assume success unless otherwise determined
                string msg = "";

                if (actionData.action == "undo" || actionData.action == "redo") {
                    Debug.Log($"Skipping SaveCurrentState for action: {actionData.action}");
                } else {
                    sceneStateManager.SaveCurrentState();
                    Debug.Log($"Saved current state after action: {actionData.action}");
                }

                SendFeedbackToPython(success, msg);

            });
        }
        // sceneStateManager.camera_ctrl.record=false;
        //  Debug.Log("Finish recording .....");
    }

    public void SendFeedbackToPython( bool success, string msg = "")
    {
        if (client != null && stream != null)
        {
            try
            {
                Vector3 currentPosition = transform.position;
                string feedback ="";

                if(agentMovement.collisionDetected){
                    success=false;
                    msg+=" Collision Detected";

                    sceneStateManager.UpdateLastActionSuccessCollision(agentMovement.collisionA,agentMovement.collisionB);
                }


                if(sceneStateManager) {
                    Debug.Log(sceneStateManager);
                    SceneStateA2T currentSceneState = sceneStateManager.GetCurrentSceneStateA2T();
                    Debug.Log(currentSceneState);
                    string sceneStateJson = JsonUtility.ToJson(currentSceneState);
                    string imagePath = sceneStateManager.ImagePath.Replace("\\", "\\/"); // 转义反斜杠
                    feedback = $"{{\"success\": {(success ? 1 : 0)},\"imgpath\":\"{imagePath}\" ,\"msg\": \"{msg}\", \"x1position\": \"{currentPosition}\", \"sceneState\": {sceneStateJson}}}";
                }
              
                // feedback = $"{{\"success\": {(success ? 1 : 0)}, \"msg\": \"{msg}\", \"x1position\": \"{null}\", \"sceneState\": {null}}}";

                Debug.Log(feedback);
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
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
        sceneStateManager.camera_ctrl.record=false;
        sceneStateManager.camera_ctrl.ResetImageCount();
    }

    public void SendFeedbackToPython( bool success)
    {
        if (client != null && stream != null)
        {
            try
            {
                Vector3 currentPosition = transform.position;
                string feedback ="";
              
                feedback = $"{{\"success\": {(success ? 1 : 0)}}}";

                Debug.Log(feedback);
                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);
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
}