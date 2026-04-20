using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualBookScreen : MonoBehaviour
{
    [Serializable]
    public struct Page
    {
        [TextArea(3, 8)]
        public string text;
        public Sprite image;
    }

    [Header("Страницы")]
    [SerializeField] private Page[] _pages;

    [Header("UI ссылки")]
    [SerializeField] private GameObject      _screenPanel;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private Image           _pageImage;
    [SerializeField] private GameObject      _imageContainer;
    [SerializeField] private TextMeshProUGUI _pageCounter;
    [SerializeField] private Button          _btnNext;
    [SerializeField] private Button          _btnPrev;

    private int  _currentIndex = 0;
    private bool _isOpen       = false;

    private void Awake()
    {
        _btnNext.onClick.AddListener(NextPage);
        _btnPrev.onClick.AddListener(PrevPage);
    }

    private void Start()
    {
        _screenPanel.SetActive(false);
        ShowPage(_currentIndex);
    }

    private void OnDestroy()
    {
        _btnNext.onClick.RemoveListener(NextPage);
        _btnPrev.onClick.RemoveListener(PrevPage);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_isOpen) CloseScreen();
            else         OpenScreen();
        }

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseScreen();
    }

    private void OpenScreen()
    {
        _isOpen = true;
        _screenPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SetPlayerControl(false);
    }

    private void CloseScreen()
    {
        _isOpen = false;
        _screenPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool state)
    {
        PlayerController pc = FindObjectOfType<PlayerController>();
        CameraController cc = FindObjectOfType<CameraController>();

        if (pc != null) pc.enabled = state;
        if (cc != null) cc.enabled = state;
    }

    private void NextPage()
    {
        if (_currentIndex < _pages.Length - 1)
        {
            _currentIndex++;
            ShowPage(_currentIndex);
        }
    }

    private void PrevPage()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            ShowPage(_currentIndex);
        }
    }

    private void ShowPage(int index)
    {
        if (_pages == null || _pages.Length == 0) return;

        Page page = _pages[index];

        if (_pageText != null)
            _pageText.text = page.text;

        bool hasImage = page.image != null;
        if (_imageContainer != null)
            _imageContainer.SetActive(hasImage);
        if (_pageImage != null && hasImage)
            _pageImage.sprite = page.image;

        if (_pageCounter != null)
            _pageCounter.text = $"{index + 1} / {_pages.Length}";

        _btnPrev.interactable = index > 0;
        _btnNext.interactable = index < _pages.Length - 1;
    }
}