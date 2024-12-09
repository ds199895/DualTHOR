using UnityEngine;
using System.Collections.Generic;

public class DebugLogger : MonoBehaviour
{
    private Queue<string> logMessages = new Queue<string>(); // 保存日志信息的队列
    public int maxMessages = 10; // 最多显示的日志条数
    public GUIStyle textStyle;   // 可选：自定义字体样式
    private Vector2 scrollPosition; // 用于滚动视图的当前位置

    void Awake()
    {
        // 注册 Unity 日志事件
        Application.logMessageReceived += HandleLog;

        // 初始化样式，确保消息换行
        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.wordWrap = true; // 启用自动换行
            textStyle.normal.textColor = Color.white; // 设置文字颜色为白色
            textStyle.fontSize = 14; // 可选：设置字体大小
        }
    }

    void OnDestroy()
    {
        // 取消注册 Unity 日志事件
        Application.logMessageReceived -= HandleLog;
    }

    // 处理日志信息并添加到队列中
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string message = $"[{type}] {logString}";
        logMessages.Enqueue(message);

        // 限制日志信息数量
        if (logMessages.Count > maxMessages)
        {
            logMessages.Dequeue();
        }
    }

    void OnGUI()
    {
        // 设置 GUI 区域和背景样式
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true)); // 在框内显示日志

        // 添加滚动视图以便查看大量日志
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        foreach (string logMessage in logMessages)
        {
            // 使用启用自动换行的文本区域显示日志
            GUILayout.TextArea(logMessage, textStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }
}