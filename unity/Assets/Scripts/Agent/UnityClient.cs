using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UnityClient : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;
    AgentMovement agentMovement;
    SceneManager sceneManager;

    [Serializable]
    public class ActionData
    {
        public string action;
        public float Magnitude;
        public string arm;  
        public string objectID;
        public float successRate;
        public string stateID;  
    }
    string PreprocessJson(string json)
    {
        return json.Replace("\"stateid\"", "\"stateID\"")
                   .Replace("\"objectid\"", "\"objectID\"")
                   .Replace("\"successrate\"", "\"successRate\"");
    }

    void Start()
    {
        agentMovement = GetComponent<AgentMovement>();
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneManager>();
        ConnectToServerAsync();
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
            catch (SocketException se)
            {
                switch (se.SocketErrorCode)
                {
                    case SocketError.ConnectionRefused:
                        Debug.LogError("Connection refused by server. Is the server running?");
                        break;
                    case SocketError.HostNotFound:
                        Debug.LogError("Host not found. Please check the IP address.");
                        break;
                    case SocketError.TimedOut:
                        Debug.LogError("Connection timed out. Server might be unreachable.");
                        break;
                    case SocketError.NetworkUnreachable:
                        Debug.LogError("Network unreachable. Please check your network connection.");
                        break;
                    case SocketError.AddressFamilyNotSupported:
                        Debug.LogError("Address family not supported. Verify IPv4/IPv6 compatibility.");
                        break;
                    case SocketError.AccessDenied:
                        Debug.LogError("Access denied. Check if port 5678 is blocked by a firewall.");
                        break;
                    default:
                        Debug.LogError($"SocketException occurred: {se.SocketErrorCode} - {se.Message}");
                        break;
                }

                Debug.LogError($"Detailed exception: {se.GetType().Name} - {se.Message}");
                await Task.Delay(5000); // 5 秒后重试
            }
            catch (ArgumentException ae)
            {
                Debug.LogError($"Invalid IP address or port number: {ae.Message}. Please check your settings.");
            }
            catch (ObjectDisposedException ode)
            {
                Debug.LogError($"Attempted to use a disposed TcpClient: {ode.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Unexpected exception occurred: {e.GetType().Name} - {e.Message}");
                if (e.InnerException != null)
                {
                    Debug.LogError($"Inner Exception: {e.InnerException.GetType().Name} - {e.InnerException.Message}");
                }
                await Task.Delay(5000); // 5 秒后重试
            }
        }
    }

    void Update()
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

                    // 解析 JSON 数据
                    actionJson = PreprocessJson(actionJson);
                    ActionData actionData = JsonUtility.FromJson<ActionData>(actionJson);

                    // 确保 action 有效
                    if (string.IsNullOrEmpty(actionData.action))
                    {
                        Debug.LogError("ActionData does not contain a valid action.");
                        SendFeedbackToPython("Error: Missing action in ActionData.");
                        return;
                    }

                    // 特殊处理 LoadSceneState 动作
                    if (actionData.action == "loadstate")
                    {
                        if (!string.IsNullOrEmpty(actionData.stateID))
                        {
                            agentMovement.LoadState(actionData.stateID);
                            Debug.Log($"Loaded scene state with ID: {actionData.stateID}");
                            SendFeedbackToPython($"Loaded state ID: {actionData.stateID}");
                        }
                        else
                        {
                            Debug.LogError("State ID is missing in ActionData for LoadSceneState.");
                            SendFeedbackToPython("Error: Missing state ID for LoadSceneState.");
                        }
                    }
                    else
                    {
                        // 处理其他常规动作
                        agentMovement.ExecuteActionWithCallback(actionData, () =>
                        {
                            if (actionData.action == "undo" || actionData.action == "redo")
                            {
                                Debug.Log($"Skipping SaveCurrentState for action: {actionData.action}");
                            }
                            else
                            {
                                sceneManager.SaveCurrentState();
                                Debug.Log($"Saved current state after action: {actionData.action}");
                            }

                            SendFeedbackToPython(actionData.action);
                        });
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

    public void SendFeedbackToPython(string action)
    {
        if (client != null && stream != null)
        {
            try
            {
                Vector3 currentPosition = transform.position;
                SceneStateA2T currentSceneState = sceneManager.GetCurrentSceneStateA2T();

                // 确保 objectsStates 被正确序列化
                string sceneStateJson = JsonUtility.ToJson(currentSceneState);
                string feedback = $"Executed {action}, x1position: {currentPosition}, sceneState: {sceneStateJson}";

                byte[] feedbackData = Encoding.UTF8.GetBytes(feedback + "\n");
                stream.Write(feedbackData, 0, feedbackData.Length);

                //Debug.Log($"Sent feedback to Python: {feedback} (Size: {feedbackData.Length} bytes)");
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