using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class DialoguesSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI npcNameText;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueDataNPC[] dialogues;

    [Header("Settings")]
    [SerializeField] private int startDialogueIndex = 0;
    [SerializeField] private bool dontHideOnClick = false;
    [SerializeField] private bool startAtStart = true;
    [SerializeField] private int dialogueIndexAtStart = 0;

    private int currentDialogueIndex = -1;
    private int currentNpcLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isShowingPlayerResponse = false;

    [Header("Text Effects")]
    [SerializeField] private bool enableTextShake = false;
    [SerializeField][Range(0f, 10f)] private float shakeIntensity = 1f;
    [SerializeField][Range(0.01f, 0.5f)] private float shakeSpeed = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource dialogueAudioSource;
    private Coroutine stopAudioCoroutine;

    private Coroutine typewriterCoroutine;
    private bool isTyping = false;

    [Header("Post-Dialogue Events")]
    [SerializeField] private List<UnityEvent> postDialogueEvents = new List<UnityEvent>();

    // ============ ДОБАВЛЕНО ДЛЯ ИЗОБРАЖЕНИЙ ============
    [Header("Image Display")]
    [SerializeField] private DialogueImageDisplayController imageDisplayController;
    private DialogueImageDisplayController imageController;
    // ===================================================

    public event Action DialogueStarted;
    public event Action DialogueEnded;

    void Start()
    {
        HideDialogue();
        // Ищем контроллер изображений
        imageController = FindObjectOfType<DialogueImageDisplayController>();
        if (imageDisplayController != null && imageController == null)
        {
            imageController = imageDisplayController;
        }

        if (startAtStart)
        {
            StartDialogue(dialogueIndexAtStart);
        }
    }

    void Update()
    {
        if (!isDialogueActive)
            return;

        // ============ ДОБАВЛЕНО ДЛЯ ИЗОБРАЖЕНИЙ ============
        // Блокируем ввод если показывается изображение ИЛИ ожидается клик для изображения
        if (imageController != null && (imageController.IsImageModeActive() || imageController.IsWaitingForImageClick()))
            return;
        // ===================================================

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                CompleteCurrentLineInstantly();
            }
            else
            {
                AdvanceDialogue();
            }
        }
    }

    private void AdvanceDialogue()
    {
        DialogueDataNPC data = dialogues[currentDialogueIndex];

        if (isShowingPlayerResponse)
        {
            isShowingPlayerResponse = false;
            currentNpcLineIndex++;

            if (currentNpcLineIndex >= data.lines.Length)
            {
                TriggerPostDialogueEvents();
                if (!dontHideOnClick)
                {
                    HideDialogue();
                }
            }
            else
            {
                ShowCurrentNpcLine();
            }
        }
        else
        {
            PlayerResponse response = data.GetPlayerResponseAfter(currentNpcLineIndex);

            if (response != null)
            {
                ShowPlayerResponse(response);
            }
            else
            {
                currentNpcLineIndex++;

                if (currentNpcLineIndex >= data.lines.Length)
                {
                    TriggerPostDialogueEvents();
                    if (!dontHideOnClick)
                    {
                        HideDialogue();
                    }
                }
                else
                {
                    ShowCurrentNpcLine();
                }
            }
        }
    }

    public void StartDialogue(int index)
    {
        if (dialogues == null || dialogues.Length == 0)
            return;

        if (index < 0 || index >= dialogues.Length)
            return;

        currentDialogueIndex = index;
        currentNpcLineIndex = 0;
        isShowingPlayerResponse = false;
        isDialogueActive = true;

        ShowDialogue();
        ShowCurrentNpcLine();

        DialogueStarted?.Invoke();
    }

    private void ShowCurrentNpcLine()
    {
        DialogueDataNPC data = dialogues[currentDialogueIndex];

        if (npcNameText != null)
        {
            npcNameText.text = data.npcName.GetLocalizedString();
        }

        UpdateDialogueText();
        PlayCurrentLineAudio();
    }

    private void ShowPlayerResponse(PlayerResponse response)
    {
        isShowingPlayerResponse = true;

        if (npcNameText != null)
        {
            npcNameText.text = response.playerName.GetLocalizedString();
        }

        if (dialogueText != null)
        {
            if (enableTextShake)
            {
                StopAllCoroutines();
            }

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            string playerText = response.playerText.GetLocalizedString();

            if (response.duration > 0f)
            {
                typewriterCoroutine = StartCoroutine(TypeLine(playerText, response.duration));
            }
            else
            {
                dialogueText.text = playerText;
                isTyping = false;
            }

            if (enableTextShake && isActiveAndEnabled)
            {
                StartCoroutine(ShakeText());
            }
        }

        PlayPlayerResponseAudio(response);
    }

    private void PlayPlayerResponseAudio(PlayerResponse response)
    {
        if (dialogueAudioSource == null || response == null)
            return;

        if (stopAudioCoroutine != null)
        {
            StopCoroutine(stopAudioCoroutine);
            stopAudioCoroutine = null;
        }

        dialogueAudioSource.Stop();

        if (response.voiceClip != null)
        {
            dialogueAudioSource.clip = response.voiceClip;
            dialogueAudioSource.Play();
        }

        if (response.duration > 0f)
        {
            stopAudioCoroutine = StartCoroutine(StopAudioAfterDuration(response.duration));
        }
    }

    public void StartDefaultDialogue()
    {
        StartDialogue(startDialogueIndex);
    }

    public void ShowDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
    }

    public void HideDialogue()
    {
        bool wasActive = isDialogueActive;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        isDialogueActive = false;
        currentDialogueIndex = -1;
        currentNpcLineIndex = 0;
        isShowingPlayerResponse = false;

        if (stopAudioCoroutine != null)
        {
            StopCoroutine(stopAudioCoroutine);
            stopAudioCoroutine = null;
        }

        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;

        if (wasActive)
        {
            DialogueEnded?.Invoke();
        }
    }

    private void UpdateDialogueText()
    {
        if (!isDialogueActive || currentDialogueIndex < 0)
            return;

        DialogueDataNPC data = dialogues[currentDialogueIndex];
        if (data == null || data.lines == null || data.lines.Length == 0)
            return;

        currentNpcLineIndex = Mathf.Clamp(currentNpcLineIndex, 0, data.lines.Length - 1);

        if (dialogueText != null)
        {
            if (enableTextShake)
            {
                StopAllCoroutines();
            }

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            var localizedLine = data.lines[currentNpcLineIndex];
            string lineText = localizedLine.GetLocalizedString();

            float duration = 0f;
            if (data.audioClipPlayDurations != null && currentNpcLineIndex >= 0 && currentNpcLineIndex < data.audioClipPlayDurations.Length)
            {
                duration = data.audioClipPlayDurations[currentNpcLineIndex];
            }

            if (duration <= 0f)
            {
                dialogueText.text = lineText;
                isTyping = false;

                // ============ ДОБАВЛЕНО ДЛЯ ИЗОБРАЖЕНИЙ ============
                // Уведомляем контроллер изображений о завершении отображения строки NPC
                if (imageController != null)
                {
                    imageController.CheckForImage();
                }
                // ===================================================
            }
            else
            {
                typewriterCoroutine = StartCoroutine(TypeLine(lineText, duration));
            }

            if (enableTextShake && isActiveAndEnabled)
            {
                StartCoroutine(ShakeText());
            }
        }
    }

    private void CompleteCurrentLineInstantly()
    {
        if (!isDialogueActive || currentDialogueIndex < 0)
            return;

        DialogueDataNPC data = dialogues[currentDialogueIndex];

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (stopAudioCoroutine != null)
        {
            StopCoroutine(stopAudioCoroutine);
            stopAudioCoroutine = null;
        }

        if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
        {
            dialogueAudioSource.Stop();
        }

        if (dialogueText != null)
        {
            if (isShowingPlayerResponse)
            {
                PlayerResponse response = data.GetPlayerResponseAfter(currentNpcLineIndex);
                if (response != null)
                {
                    dialogueText.text = response.playerText.GetLocalizedString();
                }
            }
            else
            {
                if (data.lines != null && data.lines.Length > 0)
                {
                    currentNpcLineIndex = Mathf.Clamp(currentNpcLineIndex, 0, data.lines.Length - 1);
                    dialogueText.text = data.lines[currentNpcLineIndex].GetLocalizedString();

                    // ============ ДОБАВЛЕНО ДЛЯ ИЗОБРАЖЕНИЙ ============
                    // Уведомляем контроллер изображений о завершении отображения строки NPC
                    if (imageController != null)
                    {
                        imageController.CheckForImage();
                    }
                    // ===================================================
                }
            }
        }

        isTyping = false;
    }

    public void SetDialogueAudioSource(AudioSource source)
    {
        dialogueAudioSource = source;
    }

    private void PlayCurrentLineAudio()
    {
        if (dialogueAudioSource == null || !isDialogueActive || currentDialogueIndex < 0)
            return;

        DialogueDataNPC data = dialogues[currentDialogueIndex];
        if (data == null)
            return;

        if (stopAudioCoroutine != null)
        {
            StopCoroutine(stopAudioCoroutine);
            stopAudioCoroutine = null;
        }

        AudioClip clip = null;
        if (data.voiceClips != null && currentNpcLineIndex >= 0 && currentNpcLineIndex < data.voiceClips.Length)
        {
            clip = data.voiceClips[currentNpcLineIndex];
        }

        dialogueAudioSource.Stop();

        if (clip != null)
        {
            dialogueAudioSource.clip = clip;
            dialogueAudioSource.Play();
        }

        float duration = 0f;
        if (data.audioClipPlayDurations != null && currentNpcLineIndex >= 0 && currentNpcLineIndex < data.audioClipPlayDurations.Length)
        {
            duration = data.audioClipPlayDurations[currentNpcLineIndex];
        }

        if (duration > 0f)
        {
            stopAudioCoroutine = StartCoroutine(StopAudioAfterDuration(duration));
        }
    }

    private IEnumerator TypeLine(string line, float duration)
    {
        isTyping = true;
        dialogueText.text = "";
        float timePerChar = duration / line.Length;

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(timePerChar);
        }

        isTyping = false;
        typewriterCoroutine = null;

        // ============ ДОБАВЛЕНО ДЛЯ ИЗОБРАЖЕНИЙ ============
        // Уведомляем контроллер изображений о завершении отображения строки NPC
        // Только если это строка NPC, а не ответ игрока
        if (!isShowingPlayerResponse && imageController != null)
        {
            imageController.CheckForImage();
        }
        // ===================================================
    }

    private IEnumerator ShakeText()
    {
        if (dialogueText == null) yield break;

        RectTransform textRect = dialogueText.rectTransform;
        if (textRect == null) yield break;

        Vector3 originalPos = textRect.localPosition;
        float timer = 0f;

        while (isDialogueActive && dialogueText != null && textRect != null)
        {
            timer += Time.deltaTime * shakeSpeed * 50f;
            float offsetX = (Mathf.PerlinNoise(timer, 0) * 2 - 1) * shakeIntensity;
            float offsetY = (Mathf.PerlinNoise(0, timer) * 2 - 1) * shakeIntensity;

            textRect.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            yield return null;
        }

        if (textRect != null)
        {
            textRect.localPosition = originalPos;
        }
    }

    private IEnumerator StopAudioAfterDuration(float duration)
    {
        int lineIndexAtStart = currentNpcLineIndex;
        yield return new WaitForSeconds(duration);

        if (isDialogueActive && currentDialogueIndex >= 0 && lineIndexAtStart == currentNpcLineIndex && dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }
    }

    private void TriggerPostDialogueEvents()
    {
        foreach (var unityEvent in postDialogueEvents)
        {
            unityEvent?.Invoke();
        }
    }

    // ============ ДОБАВЛЕНО ДЛЯ ИЗОБРАЖЕНИЙ ============
    // Метод для продолжения диалога после закрытия изображения
    private void ContinueDialogueAfterImage()
    {
        if (!isDialogueActive || currentDialogueIndex < 0)
            return;

        DialogueDataNPC data = dialogues[currentDialogueIndex];
        PlayerResponse response = data.GetPlayerResponseAfter(currentNpcLineIndex);

        if (response != null)
        {
            // Если есть ответ игрока, показываем его
            ShowPlayerResponse(response);
        }
        else
        {
            // Если нет ответа игрока, продолжаем диалог
            AdvanceDialogue();
        }
    }

    // Публичные методы для доступа к данным
    public int GetCurrentDialogueIndex()
    {
        return currentDialogueIndex;
    }

    public int GetCurrentNpcLineIndex()
    {
        return currentNpcLineIndex;
    }

    public string GetCurrentNpcId()
    {
        if (currentDialogueIndex >= 0 && currentDialogueIndex < dialogues.Length && dialogues[currentDialogueIndex] != null)
        {
            return dialogues[currentDialogueIndex].npcId;
        }
        return string.Empty;
    }

    public bool IsShowingPlayerResponse()
    {
        return isShowingPlayerResponse;
    }
    // ===================================================
}