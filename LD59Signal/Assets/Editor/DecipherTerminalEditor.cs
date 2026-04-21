#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DecipherTerminal))]
public class DecipherTerminalEditor : Editor
{
    private SerializedProperty _interactionRadiusProp;
    private SerializedProperty _playerTransformProp;
    private SerializedProperty _interactPromptProp;
    private SerializedProperty _decipherScreenProp;
    private SerializedProperty _fragmentsProp;

    private bool[] _foldouts = Array.Empty<bool>();

    private void OnEnable()
    {
        _interactionRadiusProp = serializedObject.FindProperty("_interactionRadius");
        _playerTransformProp   = serializedObject.FindProperty("_playerTransform");
        _interactPromptProp    = serializedObject.FindProperty("_interactPrompt");
        _decipherScreenProp    = serializedObject.FindProperty("_decipherScreen");
        _fragmentsProp         = serializedObject.FindProperty("_fragments");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Взаимодействие", DrawInteractionSection);
        DrawSection("Экран",          DrawScreenSection);
        DrawSection("Фрагменты",      DrawFragmentsSection);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInteractionSection()
    {
        EditorGUILayout.PropertyField(_interactionRadiusProp, new GUIContent("Радиус"));
        EditorGUILayout.PropertyField(_playerTransformProp,   new GUIContent("Transform игрока"));
        EditorGUILayout.PropertyField(_interactPromptProp,    new GUIContent("Подсказка I"));
    }

    private void DrawScreenSection()
    {
        EditorGUILayout.PropertyField(_decipherScreenProp, new GUIContent("Decipher Screen"));
    }

    private void DrawFragmentsSection()
    {
        int count = _fragmentsProp.arraySize;

        if (_foldouts.Length != count)
        {
            bool[] resized = new bool[count];
            for (int i = 0; i < Mathf.Min(_foldouts.Length, count); i++)
                resized[i] = _foldouts[i];
            _foldouts = resized;
        }

        for (int i = 0; i < count; i++)
        {
            DrawFragmentFoldout(_fragmentsProp.GetArrayElementAtIndex(i), i);
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Добавить фрагмент"))
        {
            _fragmentsProp.InsertArrayElementAtIndex(count);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            ApplyDefaults(_fragmentsProp.GetArrayElementAtIndex(count));
        }

        GUI.enabled = count > 0;
        if (GUILayout.Button("− Удалить последний"))
            _fragmentsProp.DeleteArrayElementAtIndex(count - 1);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFragmentFoldout(SerializedProperty frag, int index)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        _foldouts[index] = EditorGUILayout.Foldout(
            _foldouts[index], $"  Фрагмент {index + 1}", true, EditorStyles.foldoutHeader);

        if (GUILayout.Button("Сброс", GUILayout.Width(60)))
        {
            ApplyDefaults(frag);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();

        if (!_foldouts[index])
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.indentLevel++;

        Subheader("Волна-цель");
        Slider(frag, "targetAmplitude", "Амплитуда",  0.1f, 2f);
        Slider(frag, "targetFrequency", "Частота",    1f,   20f);
        Slider(frag, "targetPhase",     "Фаза",       0f,   6.28f);

        Subheader("Управление игрока");
        Slider(frag, "ampAdjustSpeed",   "Скорость амплитуды", 0.1f, 3f);
        Slider(frag, "freqAdjustSpeed",  "Скорость частоты",   0.1f, 5f);
        Slider(frag, "phaseAdjustSpeed", "Скорость фазы",      0.1f, 5f);
        EditorGUILayout.PropertyField(
            frag.FindPropertyRelative("hasPhaseControl"),
            new GUIContent("Управление фазой (Q/E)"));

        Subheader("Дрейф");
        EditorGUILayout.PropertyField(
            frag.FindPropertyRelative("hasDrift"),
            new GUIContent("Включить дрейф"));
        Slider(frag, "driftSpeed", "Скорость дрейфа", 0f, 2f);

        Subheader("Помехи");
        Slider(frag, "noiseStrength", "Сила помех",     0f,   0.5f);
        Slider(frag, "noiseInterval", "Интервал (сек)", 0.1f, 5f);

        Subheader("Инверсия");
        EditorGUILayout.PropertyField(
            frag.FindPropertyRelative("hasInversion"),
            new GUIContent("Включить инверсию"));
        Slider(frag, "inversionInterval", "Интервал (сек)", 1f, 10f);

        Subheader("Текст фрагмента");
        EditorGUILayout.PropertyField(
            frag.FindPropertyRelative("revealedText"),
            new GUIContent("Текст при расшифровке"));

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);
        EditorGUILayout.EndVertical();
    }

    private static void DrawSection(string title, Action drawContent)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        drawContent();
        EditorGUI.indentLevel--;
    }

    private static void Subheader(string label)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
    }

    private static void Slider(SerializedProperty parent, string propName, string label, float min, float max)
    {
        SerializedProperty prop = parent.FindPropertyRelative(propName);
        if (prop == null) return;
        prop.floatValue = EditorGUILayout.Slider(label, prop.floatValue, min, max);
    }

    private static void ApplyDefaults(SerializedProperty frag)
    {
        frag.FindPropertyRelative("targetAmplitude").floatValue   = 1f;
        frag.FindPropertyRelative("targetFrequency").floatValue   = 3f;
        frag.FindPropertyRelative("targetPhase").floatValue       = 0f;
        frag.FindPropertyRelative("ampAdjustSpeed").floatValue    = 0.5f;
        frag.FindPropertyRelative("freqAdjustSpeed").floatValue   = 1f;
        frag.FindPropertyRelative("phaseAdjustSpeed").floatValue  = 1f;
        frag.FindPropertyRelative("hasPhaseControl").boolValue    = false;
        frag.FindPropertyRelative("hasDrift").boolValue           = false;
        frag.FindPropertyRelative("driftSpeed").floatValue        = 0.2f;
        frag.FindPropertyRelative("noiseStrength").floatValue     = 0f;
        frag.FindPropertyRelative("noiseInterval").floatValue     = 2f;
        frag.FindPropertyRelative("hasInversion").boolValue       = false;
        frag.FindPropertyRelative("inversionInterval").floatValue = 5f;
        frag.FindPropertyRelative("revealedText").stringValue     = string.Empty;
    }
}
#endif