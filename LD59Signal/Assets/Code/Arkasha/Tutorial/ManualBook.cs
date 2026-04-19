using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualBook : MonoBehaviour
{
    // Одна страница — текст и спрайт, заполняется в Inspector
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
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private Image           _pageImage;
    [SerializeField] private GameObject      _imageContainer; // скрывается если спрайта нет
    [SerializeField] private TextMeshProUGUI _pageCounter;    // "1 / 5"

    [Header("Кнопки в мире (коллайдеры)")]
    [SerializeField] private GameObject _btnNextObject; // 3D объект кнопки "Вперёд"
    [SerializeField] private GameObject _btnPrevObject; // 3D объект кнопки "Назад"

    [Header("Взаимодействие")]
    [SerializeField] private float     _interactRange = 3f;
    [SerializeField] private LayerMask _interactLayer;        // слой на котором кнопки
    [SerializeField] private TextMeshProUGUI _hintNext; // текст над кнопкой "Вперёд"
    [SerializeField] private TextMeshProUGUI _hintPrev; // текст над кнопкой "Назад"

    private int    _currentIndex = 0;
    private Camera _mainCam;

    // Храним последний объект под прицелом чтобы не обновлять подсказку каждый кадр
    private GameObject _lastHovered;

    private void Awake()
    {
        _mainCam = Camera.main;
    }

    private void Start()
    {
        ShowPage(_currentIndex);

      if (_hintNext != null) _hintNext.gameObject.SetActive(false);
if (_hintPrev != null) _hintPrev.gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleRaycast();
    }

    // Кидаем луч с камеры. Если попали в кнопку — показываем подсказку.
    // При нажатии E выполняем действие кнопки.
    private void HandleRaycast()
    {
        if (_mainCam == null) return;

        if (Physics.Raycast(_mainCam.transform.position, _mainCam.transform.forward,
                out RaycastHit hit, _interactRange, _interactLayer))
        {
            GameObject hitObj = hit.collider.gameObject;

            // Обновляем подсказку только при смене объекта под прицелом
            if (hitObj != _lastHovered)
            {
                _lastHovered = hitObj;
                ShowHint(hitObj);
            }

            // Игрок нажал E — определяем какую кнопку он смотрит
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (hitObj == _btnNextObject) NextPage();
                else if (hitObj == _btnPrevObject) PrevPage();
            }
        }
        else
        {
            // Прицел не на кнопке — скрываем подсказку
            if (_lastHovered != null)
            {
                _lastHovered = null;
                HideHint();
            }
        }
    }

    // Показывает подсказку с нужным текстом в зависимости от кнопки под прицелом
   private void ShowHint(GameObject hoveredObj)
{
    // Включаем подсказку только той кнопки на которую смотрим
    if (hoveredObj == _btnNextObject && _hintNext != null)
        _hintNext.gameObject.SetActive(true);
    else if (hoveredObj == _btnPrevObject && _hintPrev != null)
        _hintPrev.gameObject.SetActive(true);
}

private void HideHint()
{
    // Скрываем обе подсказки
    if (_hintNext != null) _hintNext.gameObject.SetActive(false);
    if (_hintPrev != null) _hintPrev.gameObject.SetActive(false);
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

    // Обновляет весь UI под текущую страницу
    private void ShowPage(int index)
    {
        if (_pages == null || _pages.Length == 0) return;

        Page page = _pages[index];

        if (_pageText != null)
            _pageText.text = page.text;

        // Контейнер картинки скрываем если спрайт не задан
        bool hasImage = page.image != null;
        if (_imageContainer != null)
            _imageContainer.SetActive(hasImage);
        if (_pageImage != null && hasImage)
            _pageImage.sprite = page.image;

        if (_pageCounter != null)
            _pageCounter.text = $"{index + 1} / {_pages.Length}";
    }
}