using UnityEngine;

// ScriptableObject — контейнер для одного диалога.
// Создаётся через ПКМ → Create → Narrative → Dialogue Data.
// Каждый диалог (интро, звонок 1, звонок 2) — отдельный ассет.
[CreateAssetMenu(fileName = "DialogueData", menuName = "Narrative/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    // Одна реплика диалога
    [System.Serializable]
    public class Line
    {
        [Tooltip("Имя говорящего — отображается над текстом")]
        public string speakerName;

        [TextArea(2, 5)]
        [Tooltip("Текст реплики")]
        public string text;

        [Tooltip("Озвучка — необязательна. Если не назначена — только текст")]
        public AudioClip voiceClip;
    }

    public Line[] lines;
}
