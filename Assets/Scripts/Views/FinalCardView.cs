using MathHighLow.Controllers;
using MathHighLow.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MathHighLow.Views
{
    [RequireComponent(typeof(Button))]
    public sealed class FinalCardView : MonoBehaviour
    {
        [Header("Editable Image Slots")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image backImage;

        [Header("Card Face Sprites - Numbers 1~12")]
        [SerializeField] private Sprite number1Sprite;
        [SerializeField] private Sprite number2Sprite;
        [SerializeField] private Sprite number3Sprite;
        [SerializeField] private Sprite number4Sprite;
        [SerializeField] private Sprite number5Sprite;
        [SerializeField] private Sprite number6Sprite;
        [SerializeField] private Sprite number7Sprite;
        [SerializeField] private Sprite number8Sprite;
        [SerializeField] private Sprite number9Sprite;
        [SerializeField] private Sprite number10Sprite;
        [SerializeField] private Sprite number11Sprite;
        [SerializeField] private Sprite number12Sprite;

        [Header("Card Face Sprites - Operators / Specials")]
        [SerializeField] private Sprite addSprite;
        [SerializeField] private Sprite subtractSprite;
        [SerializeField] private Sprite divideSprite;
        [SerializeField] private Sprite multiplySprite;
        [SerializeField] private Sprite squareRootSprite;

        [Header("Shared Unopened Card")]
        [Tooltip("AI와 플레이어가 함께 사용하는 공용 ? 카드의 공개 전 이미지")]
        [SerializeField] private Sprite unopenedCardSprite;

        [Header("Text")]
        [SerializeField] private Text labelText;
        [SerializeField] private Text typeText;

        [Header("Interaction")]
        [SerializeField] private Button button;

        [Header("Fallback Colors")]
        [SerializeField] private Color numberColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color operatorColor = new Color(0.65f, 0.82f, 0.95f);
        [SerializeField] private Color specialColor = new Color(0.98f, 0.72f, 0.24f);
        [SerializeField] private Color sharedColor = new Color(0.64f, 0.48f, 0.82f);
        [SerializeField] private Color usedColor = new Color(0.35f, 0.35f, 0.35f);

        private FinalGameController controller;
        private Card card;
        private bool isShared;
        private bool faceDown;
        private bool isPlayerCard;
        private bool isPlacedCard;
        private bool useSharedUnopenedArtwork;
        private bool displayOnly;
        private Image[] cornerMasks;
        private static Sprite cornerMaskSprite;
        private static readonly Color CardBorderColor =
            new Color32(232, 16, 26, 255);

        public void Configure(
            FinalGameController gameController,
            Card cardData,
            bool playerCard,
            bool hidden,
            bool sharedCard = false,
            bool placedCard = false,
            bool sharedUsesUnopenedArtwork = true,
            bool displayOnlyCard = false)
        {
            controller = gameController;
            card = cardData;
            isPlayerCard = playerCard;
            faceDown = hidden;
            isShared = sharedCard;
            isPlacedCard = placedCard;
            useSharedUnopenedArtwork = sharedUsesUnopenedArtwork;
            displayOnly = displayOnlyCard;

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            EnsureCornerMasks();
            Refresh();
        }

        public void Refresh()
        {
            if (controller == null)
            {
                return;
            }

            bool sharedHidden = isShared && !controller.SharedRevealed;
            bool shouldHide = faceDown || sharedHidden;
            if (isShared && controller.SharedRevealed)
            {
                shouldHide = false;
            }

            if (backImage != null)
            {
                if (isShared && useSharedUnopenedArtwork && unopenedCardSprite != null)
                {
                    backImage.sprite = unopenedCardSprite;
                    backImage.color = Color.white;
                    backImage.preserveAspect = true;
                }

                backImage.gameObject.SetActive(shouldHide);
            }

            bool used = card != null && isPlayerCard && !isPlacedCard &&
                        controller.IsPlayerCardUsed(card);
            SetCornerMasksVisible(!shouldHide, used);
            if (artworkImage != null)
            {
                artworkImage.sprite = GetCardFaceSprite();
                artworkImage.color = used ? usedColor : Color.white;
                artworkImage.preserveAspect = true;
                artworkImage.gameObject.SetActive(!shouldHide);
            }

            if (frameImage != null)
            {
                frameImage.color = used ? usedColor : GetBaseColor();
            }

            if (labelText != null)
            {
                labelText.gameObject.SetActive(false);
            }

            if (typeText != null)
            {
                typeText.gameObject.SetActive(false);
            }

            bool canClick = controller.Phase ==
                            FinalGameController.GamePhase.BuildingExpression &&
                            !controller.IsExpressionLocked &&
                            !displayOnly &&
                            (isPlacedCard || ((isPlayerCard || isShared) && !used));
            button.interactable = canClick;
        }

        private void EnsureCornerMasks()
        {
            if (cornerMasks != null && cornerMasks.Length == 4)
            {
                return;
            }

            if (cornerMaskSprite == null)
            {
                cornerMaskSprite = CreateCornerMaskSprite();
            }

            cornerMasks = new Image[4];
            Vector2[] anchorMins =
            {
                new Vector2(0f, 0.93f),
                new Vector2(0.89f, 0.93f),
                new Vector2(0.89f, 0f),
                new Vector2(0f, 0f)
            };
            Vector2[] anchorMaxs =
            {
                new Vector2(0.11f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0.07f),
                new Vector2(0.11f, 0.07f)
            };
            float[] rotations = { 0f, -90f, 180f, 90f };

            for (int index = 0; index < cornerMasks.Length; index++)
            {
                GameObject maskObject = new GameObject(
                    $"Card_Corner_Mask_{index + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                maskObject.layer = gameObject.layer;
                RectTransform rect = maskObject.GetComponent<RectTransform>();
                rect.SetParent(transform, false);
                rect.anchorMin = anchorMins[index];
                rect.anchorMax = anchorMaxs[index];
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, rotations[index]);
                rect.SetAsLastSibling();

                Image maskImage = maskObject.GetComponent<Image>();
                maskImage.sprite = cornerMaskSprite;
                maskImage.color = CardBorderColor;
                maskImage.preserveAspect = false;
                maskImage.raycastTarget = false;
                cornerMasks[index] = maskImage;
            }
        }

        private void SetCornerMasksVisible(bool visible, bool used)
        {
            if (cornerMasks == null)
            {
                return;
            }

            Color color = used ? usedColor : CardBorderColor;
            foreach (Image mask in cornerMasks)
            {
                if (mask == null)
                {
                    continue;
                }

                mask.gameObject.SetActive(visible);
                mask.color = color;
                mask.transform.SetAsLastSibling();
            }
        }

        private static Sprite CreateCornerMaskSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);
            texture.name = "Runtime_Card_Corner_Mask";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color transparent = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedX = x / (float)(size - 1);
                    float normalizedY = y / (float)(size - 1);
                    float distanceFromInnerCorner = Vector2.Distance(
                        new Vector2(normalizedX, normalizedY),
                        new Vector2(1f, 0f));
                    texture.SetPixel(
                        x,
                        y,
                        distanceFromInnerCorner > 1f
                            ? Color.white
                            : transparent);
                }
            }
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        private void HandleClick()
        {
            if (isPlacedCard)
            {
                controller.ReturnPlacedCard(card, isShared);
            }
            else if (isShared)
            {
                controller.SelectSharedCard();
            }
            else if (isPlayerCard)
            {
                controller.SelectCard(card);
            }
        }

        private Color GetBaseColor()
        {
            if (isShared)
            {
                return sharedColor;
            }

            if (card is NumberCard)
            {
                return numberColor;
            }

            if (card is SpecialCard)
            {
                return specialColor;
            }

            return operatorColor;
        }

        private Sprite GetCardFaceSprite()
        {
            if (card is NumberCard numberCard)
            {
                return GetNumberSprite(numberCard.Value);
            }

            if (card is OperatorCard operatorCard)
            {
                return operatorCard.Operator switch
                {
                    OperatorCard.OperatorType.Add => addSprite,
                    OperatorCard.OperatorType.Subtract => subtractSprite,
                    OperatorCard.OperatorType.Divide => divideSprite,
                    OperatorCard.OperatorType.Multiply => multiplySprite,
                    _ => null
                };
            }

            if (card is SpecialCard specialCard)
            {
                return specialCard.Type switch
                {
                    SpecialCard.SpecialType.Multiply => multiplySprite,
                    SpecialCard.SpecialType.SquareRoot => squareRootSprite,
                    _ => null
                };
            }

            return null;
        }

        private Sprite GetNumberSprite(int value)
        {
            return value switch
            {
                1 => number1Sprite,
                2 => number2Sprite,
                3 => number3Sprite,
                4 => number4Sprite,
                5 => number5Sprite,
                6 => number6Sprite,
                7 => number7Sprite,
                8 => number8Sprite,
                9 => number9Sprite,
                10 => number10Sprite,
                11 => number11Sprite,
                12 => number12Sprite,
                _ => null
            };
        }

    }
}
