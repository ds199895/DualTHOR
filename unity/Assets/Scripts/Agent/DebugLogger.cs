using UnityEngine;
using System.Collections.Generic;

public class DebugLogger : MonoBehaviour
{
    private Queue<string> logMessages = new Queue<string>(); // Queue to store log messages
    public int maxMessages = 10; // Maximum number of log messages to display
    public GUIStyle textStyle;   // Optional: Custom font style
    private Vector2 scrollPosition; // Current position for the scroll view

    public bool isDebugLoggerEnabled = false;
    void Awake()
    {
        // Register Unity log event
        Application.logMessageReceived += HandleLog;

        // Initialize style, ensure messages wrap
        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.wordWrap = true; // Enable auto word wrap
            textStyle.normal.textColor = Color.white; // Set text color to white
            textStyle.fontSize = 14; // Optional: Set font size
        }
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.F1)){
            isDebugLoggerEnabled = !isDebugLoggerEnabled;
        }
    }

    void OnDestroy()
    {
        // Unregister Unity log event
        Application.logMessageReceived -= HandleLog;
    }

    // Process log messages and add them to the queue
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string message = $"[{type}] {logString}";
        logMessages.Enqueue(message);

        // Limit the number of log messages
        if (logMessages.Count > maxMessages)
        {
            logMessages.Dequeue();
        }
    }

    void OnGUI()
    {
        if(!isDebugLoggerEnabled){
            // Set GUI area and background style
            GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true)); // Display logs within a box

            // Add scroll view for viewing large amounts of logs
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (string logMessage in logMessages)
            {
                // Display logs using a text area with auto word wrap enabled
                GUILayout.TextArea(logMessage, textStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}