using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugConsole : MonoBehaviour 
{
    private const int MaxLogCount = 256;

    private Queue<string> logQueues = new Queue<string>();

    private void Start()
    {
        Application.logMessageReceived += LogCallBack;
    }

    private void OnGUI()
    {
        GUISkin skin = GUI.skin;
        skin.toggle.fontSize = 32;
        skin.window.fontSize = 32;
        skin.button.fontSize = 32;
        skin.label.fontSize = 24;
        skin.textField.fontSize = 32;
        skin.verticalScrollbar.fixedWidth = 50;
        skin.verticalSlider.fixedWidth = 50;
        skin.verticalScrollbarThumb.fixedWidth = 50;
        skin.label.wordWrap = true;
        GUI.skin = skin;

        GUILayout.BeginVertical();
        GUILayout.BeginScrollView(new Vector2(0, 100000f));
        foreach (var item in logQueues)
        {
            GUILayout.Label(item);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void EnqueueLog(string text, string stackTrace)
    {
        if (logQueues.Count >= MaxLogCount)
        {
            logQueues.Dequeue();
        }
        logQueues.Enqueue(text);
        if(!string.IsNullOrEmpty(stackTrace))
        {
            logQueues.Enqueue(stackTrace);
        }
    }

    private void LogCallBack(string condition, string stackTrace, LogType type)
    {
        EnqueueLog(condition, stackTrace);
    }
}
