using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathHighLow.Views
{
    /// <summary>
    /// 밑장빼기 패널에서만 사용하는 카드 선택 인터랙션입니다.
    /// 씬이나 카드 프리팹을 수정하지 않고 런타임에 카드 프리팹에 추가됩니다.
    /// </summary>
    public sealed class BottomDealCardChoice : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private const float HoverRise = 14f;
        private const float SelectedRise = 28f;
        private const float MoveSpeed = 14f;

        private RectTransform rect;
        private Button button;
        private Outline selectionOutline;
        private Action<BottomDealCardChoice> selectedCallback;
        private Vector2 restingPosition;
        private bool pointerOver;
        private bool selected;
        private bool interactable = true;

        public void Configure(Action<BottomDealCardChoice> onSelected)
        {
            rect = transform as RectTransform;
            button = GetComponent<Button>();
            selectedCallback = onSelected;
            restingPosition = rect != null ? rect.anchoredPosition : Vector2.zero;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Select);
                button.interactable = true;
            }

            Graphic graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                selectionOutline = GetComponent<Outline>();
                if (selectionOutline == null)
                {
                    selectionOutline = gameObject.AddComponent<Outline>();
                }

                selectionOutline.effectColor = new Color(1f, 0.78f, 0.2f, 1f);
                selectionOutline.effectDistance = new Vector2(4f, -4f);
                selectionOutline.enabled = false;
            }
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (selectionOutline != null)
            {
                selectionOutline.enabled = value;
            }
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (button != null)
            {
                button.interactable = value;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (interactable)
            {
                pointerOver = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
        }

        private void Update()
        {
            if (rect == null)
            {
                return;
            }

            float rise = selected ? SelectedRise : pointerOver ? HoverRise : 0f;
            Vector2 target = restingPosition + Vector2.up * rise;
            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                target,
                1f - Mathf.Exp(-MoveSpeed * Time.unscaledDeltaTime));
        }

        private void Select()
        {
            if (interactable)
            {
                selectedCallback?.Invoke(this);
            }
        }
    }
}
