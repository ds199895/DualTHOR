using UnityEngine;
using System.Collections.Generic;

public class DebugLogger : MonoBehaviour
{
    private Queue<string> logMessages = new Queue<string>(); // ������־��Ϣ�Ķ���
    public int maxMessages = 10; // �����ʾ����־����
    public GUIStyle textStyle;   // ��ѡ���Զ���������ʽ
    private Vector2 scrollPosition; // ���ڹ�����ͼ�ĵ�ǰλ��

    void Awake()
    {
        // ע�� Unity ��־�¼�
        Application.logMessageReceived += HandleLog;

        // ��ʼ����ʽ��ȷ����Ϣ����
        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.wordWrap = true; // �����Զ�����
            textStyle.normal.textColor = Color.white; // ����������ɫΪ��ɫ
            textStyle.fontSize = 14; // ��ѡ�����������С
        }
    }

    void OnDestroy()
    {
        // ȡ��ע�� Unity ��־�¼�
        Application.logMessageReceived -= HandleLog;
    }

    // ������־��Ϣ����ӵ�������
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string message = $"[{type}] {logString}";
        logMessages.Enqueue(message);

        // ������־��Ϣ����
        if (logMessages.Count > maxMessages)
        {
            logMessages.Dequeue();
        }
    }

    void OnGUI()
    {
        // ���� GUI ����ͱ�����ʽ
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true)); // �ڿ�����ʾ��־

        // ��ӹ�����ͼ�Ա�鿴������־
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        foreach (string logMessage in logMessages)
        {
            // ʹ�������Զ����е��ı�������ʾ��־
            GUILayout.TextArea(logMessage, textStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }
}