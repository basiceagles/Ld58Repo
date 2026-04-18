/*
MIT License
Copyright (c) 2021 Chillu
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FullscreenGameView
{
    private static readonly Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

    private static readonly PropertyInfo showToolbarProperty =
        gameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly object falseObject = false; 
    private static EditorWindow _instance;

    private const string FULLSCREEN_PREF_KEY = "FullscreenGameView_Enabled";
    private static bool _fullscreenEnabled;

    static FullscreenGameView()
    {
        _fullscreenEnabled = EditorPrefs.GetBool(FULLSCREEN_PREF_KEY, true);
        
        EditorApplication.playModeStateChanged -= ToggleFullScreen;
        if (!_fullscreenEnabled)
            return;
        EditorApplication.playModeStateChanged += ToggleFullScreen;
    }

    [MenuItem("Window/General/Game (Fullscreen) %#&2", priority = 2)]
    public static void Toggle()
    {
        ToggleFullScreen(PlayModeStateChange.EnteredPlayMode);
    }

    [MenuItem("Window/General/Enable Fullscreen On Play")]
    public static void ToggleFullscreenSetting()
    {
        _fullscreenEnabled = !_fullscreenEnabled;
        EditorPrefs.SetBool(FULLSCREEN_PREF_KEY, _fullscreenEnabled);
        
        EditorApplication.playModeStateChanged -= ToggleFullScreen;
        if (_fullscreenEnabled)
        {
            EditorApplication.playModeStateChanged += ToggleFullScreen;
        }
        
        EditorApplication.RepaintHierarchyWindow();
        EditorApplication.RepaintProjectWindow();
    }

    [MenuItem("Window/General/Enable Fullscreen On Play", true)]
    public static bool ValidateToggleFullscreenSetting()
    {
        Menu.SetChecked("Window/General/Enable Fullscreen On Play", _fullscreenEnabled);
        return true;
    }

    public static void ToggleFullScreen(PlayModeStateChange playModeStateChange)
    {
        if (!_fullscreenEnabled)
        {
            CloseGameWindow();
            return;
        }

        if (playModeStateChange == PlayModeStateChange.EnteredEditMode || playModeStateChange == PlayModeStateChange.ExitingEditMode)
        {
            CloseGameWindow();
            return;
        }

        if (gameViewType == null)
        {
            return;
        }

        if (showToolbarProperty == null)
        {
        }

        switch (playModeStateChange)
        {
            case PlayModeStateChange.ExitingPlayMode:
                return;
            case PlayModeStateChange.EnteredPlayMode: 
                if (CloseGameWindow())
                    return;
                break;
        }

        _instance = (EditorWindow) ScriptableObject.CreateInstance(gameViewType);

        showToolbarProperty?.SetValue(_instance, falseObject);

        var desktopResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        var fullscreenRect = new Rect(Vector2.zero, desktopResolution);
        _instance.ShowPopup();
        _instance.position = fullscreenRect;
        _instance.Focus();
    }

    private static bool CloseGameWindow()
    {
        if (_instance != null)
        {
            _instance.Close();
            _instance = null;
            return true;
        }

        return false;
    }
}
#endif