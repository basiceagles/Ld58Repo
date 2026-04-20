using UnityEngine;
using UnityEngine.Localization;

[System.Serializable]
public class PlayerResponse
{
    public int afterNpcLineIndex = 0;

    public LocalizedString playerName;

    public LocalizedString playerText;

    [Header("Audio")]
    public AudioClip voiceClip;

    public float duration = 0f;
}

[CreateAssetMenu(fileName = "DialogueDataNPC", menuName = "DialogueNPC/Dialogue Data")]
public class DialogueDataNPC : ScriptableObject
{
    [Header("NPC Identification")]
    public string npcId;

    [Header("Localization")]
    [SerializeField] private LocalizedString _npcName = new LocalizedString();
    [SerializeField] private LocalizedString[] _lines = new LocalizedString[0];

    [Header("Player Responses")]
    public PlayerResponse[] playerResponses = new PlayerResponse[0];

    [Header("Audio")]
    public AudioClip[] voiceClips;
    public float[] audioClipPlayDurations;

    // ============ Public Properties ============

    public LocalizedString npcName
    {
        get
        {
            if (_npcName == null)
                _npcName = new LocalizedString();
            return _npcName;
        }
        set { _npcName = value; }
    }

    public LocalizedString[] lines
    {
        get
        {
            if (_lines == null)
                _lines = new LocalizedString[0];
            return _lines;
        }
        set { _lines = value; }
    }

    public PlayerResponse GetPlayerResponseAfter(int npcLineIndex)
    {
        if (playerResponses == null) return null;

        foreach (var response in playerResponses)
        {
            if (response != null && response.afterNpcLineIndex == npcLineIndex)
            {
                return response;
            }
        }
        return null;
    }

    public bool HasPlayerResponseAfter(int npcLineIndex)
    {
        return GetPlayerResponseAfter(npcLineIndex) != null;
    }
}
