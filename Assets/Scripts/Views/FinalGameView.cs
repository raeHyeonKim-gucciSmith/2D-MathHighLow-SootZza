using System;
using System.Collections;
using System.Collections.Generic;
using MathHighLow.Controllers;
using MathHighLow.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MathHighLow.Views
{
    public sealed class FinalGameView : MonoBehaviour
    {
        [Header("Card Prefab")]
        [SerializeField] private GameObject cardPrefab;

        [Header("Card Containers")]
        [SerializeField] private Transform playerNumberContainer;
        [SerializeField] private Transform playerActionContainer;
        [SerializeField] private Transform aiNumberContainer;
        [SerializeField] private Transform aiActionContainer;
        [SerializeField] private Transform sharedCardContainer;

        [Header("Shared Card Glass Cover")]
        [Tooltip("미개봉 공용카드를 덮는 유리잔 이미지. 여기에 유리잔 Sprite를 넣으세요.")]
        [SerializeField] private Image sharedGlassCoverImage;
        [SerializeField] private float glassMoveDuration = 0.55f;
        [SerializeField] private float glassRaisedDistance = 230f;
        [SerializeField] private float sharedCardFlipDuration = 1.2f;

        [Header("Card Deal Animation")]
        [SerializeField] private float cardDealDuration = 0.24f;
        [SerializeField] private float cardDealArcHeight = 5f;
        [SerializeField] private float cardDealRotation = 180f;

        [Header("Header")]
        [SerializeField] private Text playerCreditsText;
        [SerializeField] private Text aiCreditsText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text statusText;

        [Header("Status Log")]
        [SerializeField, Min(2)] private int statusLogCapacity = 7;

        [Header("Audio")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip hitSoundClip;
        [SerializeField] private AudioClip cupDownClip;
        [SerializeField] private AudioClip cupOpenClip;
        [SerializeField] private AudioClip showdownClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip gameOverClip;
        [SerializeField] private AudioClip removeTheBottomClip;
        [SerializeField] private AudioClip shuffleSoundClip;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float cupDownVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float cupOpenVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float showdownVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float victoryVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float gameOverVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float removeTheBottomVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float shuffleSoundVolume = 0.9f;

        [Header("Expressions")]
        [SerializeField] private Text playerExpressionText;
        [SerializeField] private Text aiExpressionText;

        [Header("Expression Buttons")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button resetButton;

        [Header("Betting")]
        [SerializeField] private GameObject bettingPanel;
        [SerializeField] private Text betText;
        [SerializeField] private Button betMinusButton;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button betConfirmButton;
        [SerializeField] private Button allInButton;
        [SerializeField] private Button callButton;
        [SerializeField] private Button foldButton;

        [Header("All In Confirmation")]
        [SerializeField] private GameObject allInConfirmationPanel;
        [SerializeField] private Text allInConfirmationText;
        [SerializeField] private Button allInConfirmButton;
        [SerializeField] private Button allInCancelButton;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDetailText;
        [SerializeField] private Button resultCloseButton;

        [Header("Final Showdown Panel")]
        [SerializeField] private GameObject finalShowdownPanel;
        [SerializeField] private Text finalShowdownTitleText;
        [SerializeField] private Text finalShowdownDescriptionText;
        [SerializeField] private Button finalShowdownContinueButton;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverTitleText;
        [SerializeField] private Text gameOverDescriptionText;

        [Header("Editable Full-Screen Art Slots")]
        [SerializeField] private Image backgroundArt;
        [SerializeField] private Image finalShowdownArt;
        [SerializeField] private Image gameOverArt;

        [Header("Final Win / Lose Sprites")]
        [Tooltip("플레이어가 최종 승리했을 때 표시할 이미지")]
        [SerializeField] private Sprite gameOverWinSprite;
        [Tooltip("플레이어가 최종 패배했을 때 표시할 이미지")]
        [SerializeField] private Sprite gameOverLoseSprite;

        private readonly List<FinalCardView> spawnedCards = new List<FinalCardView>();
        private readonly Dictionary<Card, FinalCardView> dealTargetViews =
            new Dictionary<Card, FinalCardView>();
        private FinalGameController controller;
        private Transform playerHandContainer;
        private Transform playerPlacedCardContainer;
        private Text aiBetTitleText;
        private FinalCardView sharedDisplayCardView;
        private CanvasGroup sharedDisplayCanvasGroup;
        private RectTransform sharedGlassRect;
        private Vector2 sharedGlassCoveredPosition;
        private bool sharedGlassPositionInitialized;
        private bool sharedCardCovered;
        private readonly List<string> statusMessages = new List<string>();
        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private AudioSource bottomDealSource;
        private AudioSource shuffleSoundSource;
        private bool bottomDealSoundPlayedForCurrentRound;
        private bool cupOpenPlayedForCurrentReveal;
        private bool showdownSoundPlayed;
        private readonly List<Image> gameOverComicFrames = new List<Image>();
        private Coroutine gameOverComicSequence;
        private bool finalOutcomeSoundPlayed;
        private bool canReturnToStartScene;
        private float returnToStartSceneTime;
        private bool rulesPanelShown;

        private const string AIBackFanSlotName = "ART_SLOT__AI_Deal_Deck_Fan";
        private const string DrawPileSlotName = "ART_SLOT__Hwatu_Draw_Pile";
        private const string VictoryConditionText =
            "승리 조건: 수식 결과를 1 또는 20에 더 가깝게 만드세요.";
        private const string RequiredExpressionText =
            "필수 수식: 개인 숫자 2장·공용 숫자 1장·연산자 2장과 받은 ×/√를 모두 사용하세요.";
        private const string SharedCardRuleText =
            "공용패 규칙: 미개봉 공용패는 숫자패입니다.";
        private static readonly Color VictoryConditionColor = new Color32(255, 145, 36, 255);
        private const float GlassSealDuration = 0.18f;
        private const float GlassRevealMinimumDuration = 1.2f;
        private const float GlassOffscreenMargin = 24f;
        private const float GameOverComicFrameInterval = 1.7f;
        private const float GameOverClickDelay = 0.35f;
        private const float BottomDealPanelFadeDuration = 0.22f;
        private const float BottomDealCardTravelDuration = 0.5f;
        private const float BottomDealSpecialAppearanceChance = 0.25f;

        public void Bind(FinalGameController gameController)
        {
            controller = gameController;
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<FinalGameController>();
            }

            EnsureCallFoldControls();
            StartCoroutine(PositionDrawPileBelowTimerAfterLayout());
            ConfigureCenterPileAndStatusLog();
            InitializeAudio();

            submitButton?.onClick.AddListener(() => controller?.SubmitExpression());
            resetButton?.onClick.AddListener(() => controller?.ResetExpression());
            callButton?.onClick.AddListener(() => controller?.CallAIBet());
            foldButton?.onClick.AddListener(() => controller?.FoldAIBet());
            allInCancelButton?.onClick.AddListener(HideAllInConfirmation);
            finalShowdownContinueButton?.onClick.AddListener(() =>
                controller?.ConfirmFinalShowdown());
            resultCloseButton?.onClick.AddListener(() => controller?.ConfirmRoundResult());

            HideAllInConfirmation();
            HideRoundResult();
            HideFinalShowdownIntro();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            EnsurePlayerCardInteractionAreas();
            EnsureSharedGlassCoverSlot();
            HideAIBackFan();
            BuildDecorativeCardBackGroups();
            HideTextsOutsidePanelsAtRuntime();
            EnsureTimerVisible();
            IncreaseFontSizes(transform);
            if (statusText != null)
            {
                statusText.text = BuildStatusText();
            }
        }

        private void Update()
        {
            if (!canReturnToStartScene ||
                Time.unscaledTime < returnToStartSceneTime ||
                !WasPointerPressedThisFrame())
            {
                return;
            }

            canReturnToStartScene = false;
            SceneManager.LoadScene("StartScene");
        }

        public void BeginRound(bool finalShowdown)
        {
            EnsureTimerVisible();
            bottomDealSoundPlayedForCurrentRound = false;
            if (bottomDealSource != null && bottomDealSource.isPlaying)
            {
                bottomDealSource.Stop();
            }
            StopShuffleSound();

            HideAllInConfirmation();
            HideRoundResult();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            ResetSharedCoverForDeal();
            cupOpenPlayedForCurrentReveal = false;
            if (controller != null)
            {
                UpdateTimer(0f, controller.RoundDuration);
            }
            RebuildCards();
            RefreshAll();
        }

        public IEnumerator PlayRulesPanel()
        {
            if (rulesPanelShown)
            {
                yield break;
            }
            rulesPanelShown = true;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                yield break;
            }

            GameObject overlay = CreateRulesOverlay(rootCanvas.transform);
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            Button closeButton = FindDescendant(overlay.transform, "Rules_Close")
                ?.GetComponent<Button>();
            bool closeRequested = false;
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => closeRequested = true);
            }
            else
            {
                closeRequested = true;
            }

            yield return FadeCanvasGroup(
                group,
                0f,
                1f,
                BottomDealPanelFadeDuration);
            while (!closeRequested)
            {
                yield return null;
            }

            if (closeButton != null)
            {
                closeButton.interactable = false;
            }
            group.interactable = false;
            group.blocksRaycasts = false;
            yield return FadeCanvasGroup(
                group,
                1f,
                0f,
                BottomDealPanelFadeDuration);
            Destroy(overlay);
        }

        public IEnumerator PlayDrawPileShuffleAnimation(float duration)
        {
            PlayShuffleSoundLoop();
            Transform drawPile = FindDescendant(transform, DrawPileSlotName);
            if (drawPile == null)
            {
                yield return new WaitForSeconds(duration);
                StopShuffleSound();
                yield break;
            }

            List<RectTransform> cards = new List<RectTransform>();
            List<Vector2> positions = new List<Vector2>();
            List<Quaternion> rotations = new List<Quaternion>();
            for (int index = 0; index < drawPile.childCount; index++)
            {
                RectTransform card = drawPile.GetChild(index) as RectTransform;
                if (card == null)
                {
                    continue;
                }

                cards.Add(card);
                positions.Add(card.anchoredPosition);
                rotations.Add(card.localRotation);
            }

            if (cards.Count == 0)
            {
                yield return new WaitForSeconds(duration);
                StopShuffleSound();
                yield break;
            }

            float safeDuration = Mathf.Max(0.01f, duration);
            int transferCount = Mathf.Min(9, cards.Count);
            float transferDuration = safeDuration / transferCount;
            List<RectTransform> cardOrder = new List<RectTransform>(cards);

            for (int transfer = 0; transfer < transferCount; transfer++)
            {
                RectTransform bottomCard = cardOrder[0];
                Vector2 start = positions[0];
                Vector2 pulledDown = start + new Vector2(-34f, -68f);
                Vector2 raisedAboveDeck =
                    positions[positions.Count - 1] + new Vector2(22f, 42f);
                Quaternion startRotation = rotations[0];
                Quaternion raisedRotation = Quaternion.Euler(0f, 0f, -12f);
                Quaternion endRotation = rotations[rotations.Count - 1];
                bool movedToTopLayer = false;
                float elapsed = 0f;

                while (elapsed < transferDuration)
                {
                    elapsed += Time.deltaTime;
                    float normalized = Mathf.Clamp01(
                        elapsed / transferDuration);

                    if (normalized < 0.32f)
                    {
                        float pull = Mathf.SmoothStep(
                            0f,
                            1f,
                            normalized / 0.32f);
                        bottomCard.anchoredPosition =
                            Vector2.LerpUnclamped(start, pulledDown, pull);
                        bottomCard.localRotation = Quaternion.Slerp(
                            startRotation,
                            Quaternion.Euler(0f, 0f, 8f),
                            pull);
                    }
                    else if (normalized < 0.76f)
                    {
                        float rise = Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(0.32f, 0.76f, normalized));
                        Vector2 position = Vector2.LerpUnclamped(
                            pulledDown,
                            raisedAboveDeck,
                            rise);
                        position.x += Mathf.Sin(rise * Mathf.PI) * -16f;
                        bottomCard.anchoredPosition = position;
                        bottomCard.localRotation = Quaternion.Slerp(
                            Quaternion.Euler(0f, 0f, 8f),
                            raisedRotation,
                            rise);
                    }
                    else
                    {
                        if (!movedToTopLayer)
                        {
                            movedToTopLayer = true;
                            bottomCard.SetAsLastSibling();
                        }

                        float settle = Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(0.76f, 1f, normalized));
                        bottomCard.anchoredPosition = Vector2.LerpUnclamped(
                            raisedAboveDeck,
                            positions[positions.Count - 1],
                            settle);
                        bottomCard.localRotation = Quaternion.Slerp(
                            raisedRotation,
                            endRotation,
                            settle);
                    }

                    float stackShift = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.48f, 1f, normalized));
                    for (int orderIndex = 1;
                         orderIndex < cardOrder.Count;
                         orderIndex++)
                    {
                        RectTransform stackedCard = cardOrder[orderIndex];
                        stackedCard.anchoredPosition = Vector2.LerpUnclamped(
                            positions[orderIndex],
                            positions[orderIndex - 1],
                            stackShift);
                        stackedCard.localRotation = Quaternion.Slerp(
                            rotations[orderIndex],
                            rotations[orderIndex - 1],
                            stackShift);
                    }

                    yield return null;
                }

                cardOrder.RemoveAt(0);
                cardOrder.Add(bottomCard);

                for (int orderIndex = 0;
                     orderIndex < cardOrder.Count;
                     orderIndex++)
                {
                    RectTransform card = cardOrder[orderIndex];
                    card.anchoredPosition = positions[orderIndex];
                    card.localRotation = rotations[orderIndex];
                    card.SetSiblingIndex(orderIndex);
                }
            }

            StopShuffleSound();
        }

        public IEnumerator PlayBottomDealSelection(
            Action<Card> selectionCompleted)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Transform drawPile = FindDescendant(transform, DrawPileSlotName);
            if (rootCanvas == null || cardPrefab == null || drawPile == null)
            {
                selectionCompleted?.Invoke(
                    new OperatorCard(OperatorCard.OperatorType.Add));
                yield break;
            }

            GameObject overlay = CreateBottomDealOverlay(rootCanvas.transform);
            PlayBottomDealSoundOnce();
            CanvasGroup overlayGroup = overlay.GetComponent<CanvasGroup>();
            RectTransform panel = overlay.transform.GetChild(0) as RectTransform;
            Button confirmButton = FindDescendant(panel, "BottomDeal_Confirm")
                ?.GetComponent<Button>();

            List<Card> candidateCards = new List<Card>
            {
                new OperatorCard(OperatorCard.OperatorType.Add),
                new OperatorCard(OperatorCard.OperatorType.Subtract),
                new OperatorCard(OperatorCard.OperatorType.Divide)
            };
            if (UnityEngine.Random.value < BottomDealSpecialAppearanceChance)
            {
                candidateCards.RemoveAt(
                    UnityEngine.Random.Range(0, candidateCards.Count));
                SpecialCard.SpecialType specialType =
                    UnityEngine.Random.value < 0.5f
                        ? SpecialCard.SpecialType.Multiply
                        : SpecialCard.SpecialType.SquareRoot;
                candidateCards.Add(new SpecialCard(specialType));
            }

            for (int index = candidateCards.Count - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                Card temp = candidateCards[index];
                candidateCards[index] = candidateCards[swapIndex];
                candidateCards[swapIndex] = temp;
            }

            List<BottomDealCardChoice> choices = new List<BottomDealCardChoice>();
            BottomDealCardChoice selectedChoice = null;
            Card selectedCard = candidateCards[0];
            bool confirmed = false;

            for (int index = 0; index < 3; index++)
            {
                Card candidateCard = candidateCards[index];
                GameObject cardObject = Instantiate(cardPrefab, panel);
                cardObject.name =
                    $"BottomDeal_Choice_{candidateCard.GetDisplayText()}";
                RectTransform cardRect = cardObject.transform as RectTransform;
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(132f, 208f);
                cardRect.anchoredPosition = new Vector2((index - 1) * 180f, 18f);
                cardRect.localScale = Vector3.one;
                cardRect.localRotation = Quaternion.identity;

                FinalCardView cardView = cardObject.GetComponent<FinalCardView>();
                cardView.Configure(
                    controller,
                    candidateCard,
                    false,
                    false,
                    false,
                    false,
                    true,
                    true);

                BottomDealCardChoice choice =
                    cardObject.AddComponent<BottomDealCardChoice>();
                choice.Configure(clickedChoice =>
                {
                    selectedChoice = clickedChoice;
                    selectedCard = candidateCard;
                    foreach (BottomDealCardChoice item in choices)
                    {
                        item.SetSelected(item == clickedChoice);
                    }

                    if (confirmButton != null)
                    {
                        confirmButton.interactable = true;
                    }
                });
                choices.Add(choice);
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
                confirmButton.onClick.AddListener(() =>
                {
                    if (selectedChoice != null)
                    {
                        confirmed = true;
                    }
                });
            }

            yield return FadeCanvasGroup(
                overlayGroup,
                0f,
                1f,
                BottomDealPanelFadeDuration);
            while (!confirmed)
            {
                yield return null;
            }

            foreach (BottomDealCardChoice choice in choices)
            {
                choice.SetInteractable(false);
                if (choice != selectedChoice)
                {
                    choice.gameObject.SetActive(false);
                }
            }
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            RectTransform selectedRect = selectedChoice.transform as RectTransform;
            Vector3 startPosition = selectedRect.position;
            Vector3 endPosition = drawPile.position;
            selectedRect.SetParent(rootCanvas.transform, true);
            selectedRect.SetAsLastSibling();

            float elapsed = 0f;
            Vector3 startScale = selectedRect.localScale;
            while (elapsed < BottomDealCardTravelDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(
                    elapsed / BottomDealCardTravelDuration);
                float movement = Mathf.SmoothStep(0f, 1f, normalized);
                Vector3 position =
                    Vector3.LerpUnclamped(startPosition, endPosition, movement);
                position.y += Mathf.Sin(normalized * Mathf.PI) * 45f;
                selectedRect.position = position;
                selectedRect.localScale =
                    Vector3.Lerp(startScale, startScale * 0.72f, movement);
                selectedRect.localRotation =
                    Quaternion.Euler(0f, 0f, movement * 180f);
                overlayGroup.alpha = 1f - movement;
                yield return null;
            }

            Destroy(selectedChoice.gameObject);
            Destroy(overlay);
            selectionCompleted?.Invoke(selectedCard);
        }

        private GameObject CreateBottomDealOverlay(Transform canvasTransform)
        {
            GameObject overlay = new GameObject(
                "BOTTOM_DEAL_OVERLAY__RUNTIME",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            overlay.layer = canvasTransform.gameObject.layer;
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.SetParent(canvasTransform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            Image dim = overlay.GetComponent<Image>();
            dim.color = new Color(0.025f, 0.018f, 0.012f, 0.82f);
            dim.raycastTarget = true;
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject panelObject = new GameObject(
                "BOTTOM_DEAL_PANEL__RUNTIME",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.layer = overlay.layer;
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(overlayRect, false);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(720f, 500f);
            panel.anchoredPosition = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.14f, 0.075f, 0.035f, 0.98f);
            panelImage.raycastTarget = true;

            CreateBottomDealText(
                panel,
                "BottomDeal_Title",
                "밑장빼기",
                new Vector2(0f, 205f),
                new Vector2(620f, 55f),
                40,
                new Color(1f, 0.78f, 0.26f));
            CreateBottomDealText(
                panel,
                "BottomDeal_Guide",
                "AI에게 몰래 넣을 연산자 또는 특수 카드 한 장을 선택하세요.",
                new Vector2(0f, 158f),
                new Vector2(650f, 38f),
                22,
                Color.white);

            CreateBottomDealButton(panel);
            return overlay;
        }

        private GameObject CreateRulesOverlay(Transform canvasTransform)
        {
            GameObject overlay = new GameObject(
                "RULES_OVERLAY__RUNTIME",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            overlay.layer = canvasTransform.gameObject.layer;
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.SetParent(canvasTransform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            Image dim = overlay.GetComponent<Image>();
            dim.color = new Color(0.018f, 0.012f, 0.008f, 0.9f);
            dim.raycastTarget = true;
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject panelObject = new GameObject(
                "RULES_PANEL__RUNTIME",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline));
            panelObject.layer = overlay.layer;
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(overlayRect, false);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(920f, 980f);
            panel.anchoredPosition = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.12f, 0.06f, 0.025f, 0.99f);
            panelImage.raycastTarget = true;
            Outline outline = panelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.92f, 0.62f, 0.18f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            CreateBottomDealText(
                panel,
                "Rules_Title",
                "게임 규칙",
                new Vector2(0f, 450f),
                new Vector2(820f, 55f),
                42,
                new Color(1f, 0.78f, 0.26f));

            CreateRuleLine(
                panel,
                "1. 밑장빼기",
                "무작위 카드 3장 중 하나를 골라 AI의 행동 카드로 넣습니다.",
                390f);
            SpawnRuleCard(
                panel,
                new OperatorCard(OperatorCard.OperatorType.Add),
                new Vector2(-96f, 320f));
            SpawnRuleCard(
                panel,
                new SpecialCard(SpecialCard.SpecialType.Multiply),
                new Vector2(0f, 320f));
            SpawnRuleCard(
                panel,
                new SpecialCard(SpecialCard.SpecialType.SquareRoot),
                new Vector2(96f, 320f));
            CreateBottomDealText(
                panel,
                "Rules_AI_Arrow",
                "→  AI",
                new Vector2(210f, 320f),
                new Vector2(120f, 42f),
                24,
                Color.white);

            CreateRuleLine(
                panel,
                "2. 카드 배부",
                "플레이어와 AI는 각각 개인 숫자 2장과 행동 카드 3장을 받습니다.",
                245f);
            CreateRuleLine(
                panel,
                "3. 공용 숫자",
                "테이블의 컵 아래에 숨겨져 있으며, 양쪽 모두 수식에 한 번 사용하고 제출 후 공개됩니다.",
                190f);
            CreateSharedNumberRuleVisual(panel, new Vector2(0f, 35f));
            CreateRuleLine(
                panel,
                "4. 베팅",
                "CALL은 같은 금액으로 계속 진행, FOLD는 이번 라운드를 포기합니다.",
                -120f);
            CreateRuleLine(
                panel,
                "5. 수식 완성",
                "개인 숫자 2장과 공용 숫자 1장을 모두 사용해 3분 안에 수식을 만드세요.",
                -175f);

            CreateBottomDealText(
                panel,
                "Rules_Expression_Example_Label",
                "예시)",
                new Vector2(-282f, -255f),
                new Vector2(90f, 40f),
                21,
                Color.white);
            SpawnRuleCard(
                panel,
                new NumberCard(4),
                new Vector2(-192f, -255f));
            SpawnRuleCard(
                panel,
                new OperatorCard(OperatorCard.OperatorType.Add),
                new Vector2(-96f, -255f));
            SpawnRuleCard(
                panel,
                new NumberCard(1),
                new Vector2(0f, -255f),
                true);
            SpawnRuleCard(
                panel,
                new OperatorCard(OperatorCard.OperatorType.Divide),
                new Vector2(96f, -255f));
            SpawnRuleCard(
                panel,
                new NumberCard(8),
                new Vector2(192f, -255f));

            CreateRuleLine(
                panel,
                "6. 특수카드",
                "×는 곱셈, √는 다음 숫자에 적용하며 받은 특수카드는 반드시 사용합니다.",
                -335f);
            CreateRuleLine(
                panel,
                "7. 승리",
                "결과가 1 또는 20에 더 가까운 쪽이 승리하며, 상대의 돈을 모두 잃게 만들면 최종 승리합니다.",
                -385f);

            CreateRulesCloseButton(panel);
            return overlay;
        }

        private void CreateSharedNumberRuleVisual(
            Transform parent,
            Vector2 position)
        {
            SpawnRuleCard(
                parent,
                new NumberCard(1),
                position + new Vector2(-250f, 15f),
                true);
            CreateBottomDealText(
                parent,
                "Rules_Shared_Card_Label",
                "공용 숫자 카드",
                position + new Vector2(-250f, -50f),
                new Vector2(140f, 32f),
                18,
                Color.white);

            GameObject tableObject = new GameObject(
                "Rules_Shared_Number_Table",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline));
            tableObject.layer = parent.gameObject.layer;
            RectTransform table = tableObject.GetComponent<RectTransform>();
            table.SetParent(parent, false);
            table.anchorMin = new Vector2(0.5f, 0.5f);
            table.anchorMax = new Vector2(0.5f, 0.5f);
            table.pivot = new Vector2(0.5f, 0.5f);
            table.anchoredPosition = position + new Vector2(90f, 0f);
            table.sizeDelta = new Vector2(420f, 160f);

            Image tableImage = tableObject.GetComponent<Image>();
            tableImage.color = new Color(0.16f, 0.31f, 0.2f, 0.96f);
            tableImage.raycastTarget = false;
            Outline tableOutline = tableObject.GetComponent<Outline>();
            tableOutline.effectColor = new Color(0.52f, 0.3f, 0.12f, 1f);
            tableOutline.effectDistance = new Vector2(3f, -3f);

            Sprite cupSprite = sharedGlassCoverImage != null
                ? sharedGlassCoverImage.sprite
                : null;
            if (cupSprite != null)
            {
                GameObject cupObject = new GameObject(
                    "Rules_Shared_Cup",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                cupObject.layer = tableObject.layer;
                RectTransform cupRect = cupObject.GetComponent<RectTransform>();
                cupRect.SetParent(table, false);
                cupRect.anchorMin = new Vector2(0.5f, 0.5f);
                cupRect.anchorMax = new Vector2(0.5f, 0.5f);
                cupRect.pivot = new Vector2(0.5f, 0.5f);
                cupRect.anchoredPosition = new Vector2(-135f, 15f);
                cupRect.sizeDelta = new Vector2(92f, 118f);

                Image cupImage = cupObject.GetComponent<Image>();
                cupImage.sprite = cupSprite;
                cupImage.color = Color.white;
                cupImage.preserveAspect = true;
                cupImage.raycastTarget = false;
            }
            else
            {
                CreateBottomDealText(
                    table,
                    "Rules_Shared_Cup_Fallback",
                    "컵",
                    new Vector2(-135f, 15f),
                    new Vector2(92f, 118f),
                    22,
                    Color.white);
            }

            CreateBottomDealText(
                table,
                "Rules_Shared_Reveal_Arrow",
                "→  공개  →",
                new Vector2(0f, 15f),
                new Vector2(120f, 36f),
                18,
                Color.white);
            SpawnRuleCard(
                table,
                new NumberCard(7),
                new Vector2(135f, 15f));
            CreateBottomDealText(
                table,
                "Rules_Shared_Example_Number_Label",
                "예시 숫자 카드",
                new Vector2(135f, -58f),
                new Vector2(140f, 32f),
                18,
                Color.white);
        }

        private void CreateRuleLine(
            Transform parent,
            string heading,
            string description,
            float y)
        {
            Text line = CreateBottomDealText(
                parent,
                $"Rules_{heading}",
                $"{heading}  {description}",
                new Vector2(0f, y),
                new Vector2(800f, 44f),
                22,
                Color.white);
            line.alignment = TextAnchor.MiddleLeft;
            line.resizeTextMinSize = 16;
        }

        private void SpawnRuleCard(
            Transform parent,
            Card card,
            Vector2 position,
            bool sharedCard = false)
        {
            if (cardPrefab == null)
            {
                return;
            }

            GameObject cardObject = Instantiate(cardPrefab, parent);
            cardObject.name = $"Rules_Card_{card.GetDisplayText()}";
            RectTransform rect = cardObject.transform as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(52f, 82f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            FinalCardView cardView = cardObject.GetComponent<FinalCardView>();
            cardView.Configure(
                controller,
                card,
                false,
                false,
                sharedCard,
                false,
                true,
                true);

            Button button = cardObject.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        private void CreateRulesCloseButton(Transform parent)
        {
            GameObject buttonObject = new GameObject(
                "Rules_Close",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -445f);
            rect.sizeDelta = new Vector2(250f, 62f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.76f, 0.28f, 0.08f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.86f, 0.64f);
            colors.pressedColor = new Color(0.84f, 0.68f, 0.45f);
            button.colors = colors;

            CreateBottomDealText(
                rect,
                "Label",
                "기리하기",
                Vector2.zero,
                rect.sizeDelta,
                28,
                Color.white);
        }

        private Text CreateBottomDealText(
            Transform parent,
            string objectName,
            string message,
            Vector2 position,
            Vector2 size,
            int fontSize,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.layer = parent.gameObject.layer;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text textComponent = textObject.GetComponent<Text>();
            textComponent.text = message;
            textComponent.font = statusText != null && statusText.font != null
                ? statusText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = color;
            textComponent.raycastTarget = false;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = Mathf.Max(12, fontSize - 8);
            textComponent.resizeTextMaxSize = fontSize;
            return textComponent;
        }

        private void CreateBottomDealButton(Transform parent)
        {
            GameObject buttonObject = new GameObject(
                "BottomDeal_Confirm",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -192f);
            rect.sizeDelta = new Vector2(230f, 62f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.76f, 0.28f, 0.08f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.64f);
            colors.pressedColor = new Color(0.84f, 0.68f, 0.45f);
            colors.disabledColor = new Color(0.34f, 0.34f, 0.34f, 0.65f);
            button.colors = colors;

            CreateBottomDealText(
                rect,
                "Label",
                "밑장빼기",
                Vector2.zero,
                rect.sizeDelta,
                28,
                Color.white);
        }

        private static IEnumerator FadeCanvasGroup(
            CanvasGroup group,
            float from,
            float to,
            float duration)
        {
            if (group == null)
            {
                yield break;
            }

            group.alpha = from;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, normalized));
                yield return null;
            }

            group.alpha = to;
        }

        public void RebuildCards()
        {
            foreach (FinalCardView cardView in spawnedCards)
            {
                if (cardView != null)
                {
                    Destroy(cardView.gameObject);
                }
            }
            spawnedCards.Clear();
            dealTargetViews.Clear();

            if (controller == null || cardPrefab == null)
            {
                return;
            }

            EnsurePlayerCardInteractionAreas();
            List<RectTransform> handCardRects = new List<RectTransform>();
            foreach (NumberCard card in controller.PlayerHand.NumberCards)
            {
                if (!controller.IsPlayerCardPlaced(card))
                {
                    FinalCardView handView = SpawnCard(
                        card, playerHandContainer, true, false);
                    if (handView != null)
                        handCardRects.Add(handView.transform as RectTransform);
                }
            }
            foreach (OperatorCard card in controller.PlayerHand.OperatorCards)
            {
                if (!controller.IsPlayerCardPlaced(card))
                {
                    FinalCardView handView = SpawnCard(
                        card, playerHandContainer, true, false);
                    if (handView != null)
                        handCardRects.Add(handView.transform as RectTransform);
                }
            }
            foreach (SpecialCard card in controller.PlayerHand.SpecialCards)
            {
                if (!controller.IsPlayerCardPlaced(card))
                {
                    FinalCardView handView = SpawnCard(
                        card, playerHandContainer, true, false);
                    if (handView != null)
                        handCardRects.Add(handView.transform as RectTransform);
                }
            }

            if (controller.SharedCard != null && !controller.IsSharedCardPlaced())
            {
                FinalCardView sharedHandView = SpawnSharedCard(
                    playerHandContainer, false);
                if (sharedHandView != null)
                    handCardRects.Add(sharedHandView.transform as RectTransform);
            }

            LayoutPlayerHand(handCardRects);

            if (controller.SharedCard != null && sharedCardContainer != null)
            {
                sharedDisplayCardView = SpawnSharedDisplayCard();
            }

            foreach (FinalGameController.PlayerPlacedCard placed in
                     controller.PlayerPlacedCards)
            {
                if (placed.IsShared)
                {
                    SpawnSharedCard(playerPlacedCardContainer, true);
                }
                else
                {
                    SpawnCard(
                        placed.Card,
                        playerPlacedCardContainer,
                        true,
                        false,
                        true);
                }
            }

            // AI 숫자 카드는 공개, 연산·특수 카드는 뒷면으로 표시한다.
            foreach (NumberCard card in controller.AIHand.NumberCards)
            {
                SpawnCard(card, aiNumberContainer, false, false);
            }
            foreach (OperatorCard card in controller.AIHand.OperatorCards)
            {
                SpawnCard(card, aiActionContainer, false, false);
            }
            foreach (SpecialCard card in controller.AIHand.SpecialCards)
            {
                SpawnCard(card, aiActionContainer, false, false);
            }

        }

        public void RefreshAll()
        {
            if (controller == null)
            {
                return;
            }

            if (playerCreditsText != null)
                playerCreditsText.text = $"PLAYER  {FinalGameController.FormatMoney(controller.PlayerCredits)}";
            if (aiCreditsText != null)
                aiCreditsText.text = $"AI  {FinalGameController.FormatMoney(controller.AICredits)}";
            if (phaseText != null)
                phaseText.text = controller.IsFinalShowdown
                    ? "FINAL SHOWDOWN"
                    : controller.Phase.ToString();
            UpdateStatus(controller.StatusMessage);

            if (playerExpressionText != null)
            {
                playerExpressionText.text = string.IsNullOrEmpty(
                    controller.PlayerExpression.ToDisplayString(
                        controller.SharedRevealed,
                        controller.SharedCard != null ? controller.SharedCard.Value : 0))
                    ? "수식을 작성하세요"
                    : controller.PlayerExpression.ToDisplayString(
                        controller.SharedRevealed,
                        controller.SharedCard != null ? controller.SharedCard.Value : 0);
            }

            if (aiExpressionText != null)
            {
                aiExpressionText.text = controller.SharedRevealed
                    ? controller.AIExpression.ToDisplayString(true, controller.SharedCard.Value)
                    : "AI 수식: 제출 완료 / 비공개";
            }

            bool building = controller.Phase ==
                            FinalGameController.GamePhase.BuildingExpression;
            if (submitButton != null)
            {
                submitButton.interactable = building && controller.CanSubmitExpression;
            }
            if (resetButton != null)
            {
                resetButton.interactable = building && !controller.IsExpressionLocked;
            }

            bool betting = controller.Phase == FinalGameController.GamePhase.Betting;
            if (bettingPanel != null)
            {
                bettingPanel.SetActive(betting);
            }
            if (betText != null)
            {
                betText.text = $"AI BET\n{FinalGameController.FormatMoney(controller.AIBet)}";
            }
            if (aiBetTitleText != null)
            {
                aiBetTitleText.text =
                    $"AI 선제 베팅  {FinalGameController.FormatMoney(controller.AIBet)}\n" +
                    "받기 또는 다이를 선택하세요";
            }
            if (callButton != null)
                callButton.interactable = betting && !controller.IsBetConfirmed &&
                                          controller.PlayerCredits >= controller.AIBet;
            if (foldButton != null)
                foldButton.interactable = betting && !controller.IsBetConfirmed;

            foreach (FinalCardView cardView in spawnedCards)
            {
                cardView.Refresh();
            }
        }

        public void UpdateTimer(float elapsed, float duration)
        {
            if (timerText == null)
            {
                return;
            }

            float remaining = Mathf.Max(0f, duration - elapsed);
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
            timerText.color = remaining <= 30f ? new Color(1f, 0.35f, 0.3f) : Color.white;
            EnsureTimerVisible();

            if (submitButton != null && controller != null)
            {
                submitButton.interactable = controller.CanSubmitExpression;
            }
        }

        public void UpdateStatus(string message)
        {
            if (statusText == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string normalizedMessage = message.Trim();
            if (statusMessages.Count > 0 &&
                statusMessages[statusMessages.Count - 1] == normalizedMessage)
            {
                return;
            }

            statusMessages.Add(normalizedMessage);
            while (statusMessages.Count > Mathf.Max(2, statusLogCapacity))
            {
                statusMessages.RemoveAt(0);
            }

            statusText.text = BuildStatusText();
            EnsureStatusTextVisible();
        }

        private string BuildStatusText()
        {
            string orange = ColorUtility.ToHtmlStringRGB(VictoryConditionColor);
            string logText = string.Join("\n", statusMessages.ConvertAll(FormatStatusEntry));
            int victoryConditionFontSize = statusText != null
                ? statusText.fontSize + 1
                : 1;
            return
                $"<size={victoryConditionFontSize}><color=#{orange}>" +
                $"{VictoryConditionText}\n{RequiredExpressionText}\n{SharedCardRuleText}" +
                $"</color></size>\n\n{logText}";
        }

        private static string FormatStatusEntry(string entry)
        {
            string indentedEntry = entry.Replace("\r\n", "\n").Replace("\r", "\n")
                .Replace("\n", "\n  ");
            return $"> {indentedEntry}";
        }

        private static void IncreaseFontSizes(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = Mathf.Max(1, text.fontSize + 1);
            }
        }

        private void ConfigureCenterPileAndStatusLog()
        {
            if (statusText == null)
            {
                return;
            }

            RectTransform statusRect = statusText.rectTransform;
            RectTransform panelRect = statusRect.parent as RectTransform;
            bool createdPanel = false;
            if (panelRect == null || panelRect.name != "Status_Log_Panel")
            {
                Transform originalParent = statusRect.parent;
                int siblingIndex = statusRect.GetSiblingIndex();
                GameObject panelObject = new GameObject(
                    "Status_Log_Panel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.SetParent(originalParent, false);
                panelRect.SetSiblingIndex(siblingIndex);
                statusRect.SetParent(panelRect, false);
                createdPanel = true;
            }

            if (createdPanel)
            {
                panelRect.anchorMin = new Vector2(0.02f, 0.38f);
                panelRect.anchorMax = new Vector2(0.25f, 0.68f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                Image panelImage = panelRect.GetComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.68f);
                panelImage.raycastTarget = false;

                statusRect.anchorMin = new Vector2(0.07f, 0.06f);
                statusRect.anchorMax = new Vector2(0.955f, 0.94f);
                statusRect.offsetMin = Vector2.zero;
                statusRect.offsetMax = Vector2.zero;
                statusText.alignment = TextAnchor.UpperLeft;
                statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
                statusText.verticalOverflow = VerticalWrapMode.Truncate;
                statusText.raycastTarget = false;
            }

            panelRect.anchorMax = new Vector2(
                Mathf.Min(1f, panelRect.anchorMax.x + 0.07f),
                panelRect.anchorMax.y);

            statusText.fontSize = Mathf.Max(1, statusText.fontSize + 1);
            statusText.supportRichText = true;

            if (panelRect.GetComponent<RectMask2D>() == null)
            {
                panelRect.gameObject.AddComponent<RectMask2D>();
            }

            string initialMessage = statusText.text;
            statusText.text = string.Empty;
            UpdateStatus(initialMessage);
        }

        private IEnumerator PositionDrawPileBelowTimerAfterLayout()
        {
            yield return null;

            Transform drawPileSlot = FindDescendant(transform, DrawPileSlotName);
            RectTransform drawPilePanel = drawPileSlot != null
                ? drawPileSlot.parent as RectTransform
                : null;
            RectTransform timerRect = timerText != null ? timerText.rectTransform : null;
            if (drawPilePanel == null || timerRect == null)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();
            Vector3[] drawPileCorners = new Vector3[4];
            Vector3[] timerCorners = new Vector3[4];
            drawPilePanel.GetWorldCorners(drawPileCorners);
            timerRect.GetWorldCorners(timerCorners);

            Vector3 drawPileCenter = (drawPileCorners[0] + drawPileCorners[2]) * 0.5f;
            float drawPileTop = Mathf.Max(drawPileCorners[1].y, drawPileCorners[2].y);
            Vector3 timerCenter = (timerCorners[0] + timerCorners[2]) * 0.5f;
            float timerBottom = Mathf.Min(timerCorners[0].y, timerCorners[3].y);
            const float gap = 12f;
            drawPilePanel.position += new Vector3(
                timerCenter.x - drawPileCenter.x,
                timerBottom - gap - drawPileTop,
                0f);
        }

        private void EnsureStatusTextVisible()
        {
            if (statusText == null)
            {
                return;
            }

            statusText.gameObject.SetActive(true);
            statusText.enabled = true;
            statusText.canvasRenderer.SetAlpha(1f);
            statusText.transform.SetAsLastSibling();
            statusText.SetAllDirty();
        }

        private void EnsureTimerVisible()
        {
            if (timerText == null)
            {
                return;
            }

            timerText.gameObject.SetActive(true);
            timerText.enabled = true;
            timerText.canvasRenderer.SetAlpha(1f);
            timerText.raycastTarget = false;
            timerText.transform.SetAsLastSibling();
            timerText.SetAllDirty();

            Outline outline = timerText.GetComponent<Outline>();
            if (outline == null)
            {
                outline = timerText.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void HideTimer()
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
        }

        private void HideTextsOutsidePanelsAtRuntime()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            foreach (Text textElement in texts)
            {
                if (textElement == null ||
                    textElement == timerText ||
                    IsInsideVisualPanel(textElement.transform))
                {
                    continue;
                }

                textElement.enabled = false;
            }
        }

        private bool IsInsideVisualPanel(Transform textTransform)
        {
            Transform current = textTransform.parent;
            while (current != null && current != transform)
            {
                if (current.GetComponent<Image>() != null)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void InitializeAudio()
        {
            if (bgmClip == null)
            {
                bgmClip = Resources.Load<AudioClip>("Sound/bgm");
            }
            if (hitSoundClip == null)
            {
                hitSoundClip = Resources.Load<AudioClip>("Sound/hitSound");
            }
            if (cupDownClip == null)
            {
                cupDownClip = Resources.Load<AudioClip>("Sound/cupDown");
            }
            if (cupOpenClip == null)
            {
                cupOpenClip = Resources.Load<AudioClip>("Sound/cupOpen");
            }
            if (showdownClip == null)
            {
                showdownClip = Resources.Load<AudioClip>("Sound/Showdown");
            }
            if (victoryClip == null)
            {
                victoryClip = Resources.Load<AudioClip>("Sound/Victory");
            }
            if (gameOverClip == null)
            {
                gameOverClip = Resources.Load<AudioClip>("Sound/gameover");
            }
            if (removeTheBottomClip == null)
            {
                removeTheBottomClip =
                    Resources.Load<AudioClip>("Sound/removeTheBottom");
            }
            if (shuffleSoundClip == null)
            {
                shuffleSoundClip =
                    Resources.Load<AudioClip>("Sound/ShuffleSound");
            }

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.clip = bgmClip;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 1f;

            bottomDealSource = gameObject.AddComponent<AudioSource>();
            bottomDealSource.playOnAwake = false;
            bottomDealSource.loop = false;
            bottomDealSource.volume = removeTheBottomVolume;
            bottomDealSource.clip = removeTheBottomClip;

            shuffleSoundSource = gameObject.AddComponent<AudioSource>();
            shuffleSoundSource.playOnAwake = false;
            shuffleSoundSource.loop = true;
            shuffleSoundSource.volume = shuffleSoundVolume;
            shuffleSoundSource.clip = shuffleSoundClip;

            if (bgmSource.clip != null)
            {
                bgmSource.Play();
            }
        }

        private void PlayBottomDealSoundOnce()
        {
            if (bottomDealSoundPlayedForCurrentRound)
            {
                return;
            }

            bottomDealSoundPlayedForCurrentRound = true;
            if (bottomDealSource == null || removeTheBottomClip == null)
            {
                return;
            }

            bottomDealSource.clip = removeTheBottomClip;
            bottomDealSource.volume = removeTheBottomVolume;
            bottomDealSource.Play();
        }

        public void FinishBottomDealSound()
        {
            if (bottomDealSource != null && bottomDealSource.isPlaying)
            {
                bottomDealSource.Stop();
            }
        }

        private void PlayShuffleSoundLoop()
        {
            if (shuffleSoundSource == null || shuffleSoundClip == null)
            {
                return;
            }

            shuffleSoundSource.Stop();
            shuffleSoundSource.clip = shuffleSoundClip;
            shuffleSoundSource.volume = shuffleSoundVolume;
            shuffleSoundSource.loop = true;
            shuffleSoundSource.Play();
        }

        private void StopShuffleSound()
        {
            if (shuffleSoundSource != null && shuffleSoundSource.isPlaying)
            {
                shuffleSoundSource.Stop();
            }
        }

        public void PlayHitSound()
        {
            if (sfxSource != null && hitSoundClip != null)
            {
                sfxSource.PlayOneShot(hitSoundClip, hitSoundVolume);
            }
        }

        // Keep the existing Inspector timing values serialized and available even though
        // the shared-card reveal now follows its fixed cinematic timing sequence.
        public float ConfiguredGlassMoveDuration => glassMoveDuration;
        public float ConfiguredSharedCardFlipDuration => sharedCardFlipDuration;

        private void PlayCupDownSound()
        {
            if (sfxSource != null && cupDownClip != null)
            {
                sfxSource.PlayOneShot(cupDownClip, cupDownVolume);
            }
        }

        public void PlayCupOpenSound()
        {
            if (cupOpenPlayedForCurrentReveal)
            {
                return;
            }

            cupOpenPlayedForCurrentReveal = true;
            if (sfxSource != null && cupOpenClip != null)
            {
                sfxSource.PlayOneShot(cupOpenClip, cupOpenVolume);
            }
        }

        public void ShowAllInConfirmation(long amount)
        {
            if (allInConfirmationText != null)
            {
                allInConfirmationText.text =
                    $"PLAYER {FinalGameController.FormatMoney(amount)}을 올인하시겠습니까?\n" +
                    "AI는 자기 패를 기준으로 별도 베팅합니다.\n" +
                    "패배하면 보유 금액이 0원이 되어 게임이 종료됩니다.";
            }
            if (allInConfirmationPanel != null)
            {
                allInConfirmationPanel.SetActive(true);
            }
        }

        public void HideAllInConfirmation()
        {
            if (allInConfirmationPanel != null)
            {
                allInConfirmationPanel.SetActive(false);
            }
        }

        public IEnumerator PlaySharedSealAnimation()
        {
            EnsureSharedGlassCoverSlot();
            if (sharedGlassRect == null)
            {
                yield break;
            }

            sharedGlassCoverImage.gameObject.SetActive(true);
            SetSharedDisplayAlpha(1f);
            sharedCardCovered = false;
            Vector2 start = GetGlassOffscreenPosition();
            Vector2 end = sharedGlassCoveredPosition;
            sharedGlassRect.anchoredPosition = start;
            yield return MoveRect(sharedGlassRect, start, end, GlassSealDuration);
            PlayCupDownSound();

            // 유리잔이 완전히 덮인 뒤에는 카드 뒷면도 렌더링하지 않아 유리잔만 보인다.
            sharedCardCovered = true;
            SetSharedDisplayAlpha(0f);
        }

        public IEnumerator PlayCardDealAnimation(Card dealtCard)
        {
            if (dealtCard == null)
            {
                yield break;
            }

            FinalCardView targetView;
            if (!dealTargetViews.TryGetValue(dealtCard, out targetView) || targetView == null)
            {
                yield break;
            }

            Transform drawPile = FindDescendant(transform, DrawPileSlotName);
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Sprite backSprite = GetCardBackSprite();
            RectTransform targetRect = targetView.transform as RectTransform;
            RectTransform drawPileRect = drawPile as RectTransform;
            if (rootCanvas == null || backSprite == null || targetRect == null || drawPileRect == null)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();

            CanvasGroup targetGroup = targetView.GetComponent<CanvasGroup>();
            if (targetGroup == null)
            {
                targetGroup = targetView.gameObject.AddComponent<CanvasGroup>();
            }
            float targetFinalAlpha = targetGroup.alpha;
            bool targetFinalInteractable = targetGroup.interactable;
            bool targetFinalBlocksRaycasts = targetGroup.blocksRaycasts;
            targetGroup.alpha = 0f;
            targetGroup.interactable = false;
            targetGroup.blocksRaycasts = false;

            GameObject movingObject = new GameObject(
                $"DEALING_CARD_BACK__{dealtCard.GetCardType()}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            movingObject.layer = rootCanvas.gameObject.layer;
            RectTransform movingRect = movingObject.GetComponent<RectTransform>();
            movingRect.SetParent(rootCanvas.transform, false);
            movingRect.anchorMin = new Vector2(0.5f, 0.5f);
            movingRect.anchorMax = new Vector2(0.5f, 0.5f);
            movingRect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 targetSize = targetRect.rect.size;
            movingRect.sizeDelta = new Vector2(
                targetSize.x > 1f ? targetSize.x : 104f,
                targetSize.y > 1f ? targetSize.y : 164f);
            movingRect.position = drawPileRect.position;
            movingRect.SetAsLastSibling();

            Image movingImage = movingObject.GetComponent<Image>();
            movingImage.sprite = backSprite;
            movingImage.color = Color.white;
            movingImage.preserveAspect = true;
            movingImage.raycastTarget = false;

            Vector3 startPosition = drawPileRect.position;
            Vector3 endPosition = targetRect.position;
            float targetAngle = targetRect.eulerAngles.z;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, cardDealDuration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                float movement = Mathf.SmoothStep(0f, 1f, normalized);
                Vector3 position = Vector3.LerpUnclamped(startPosition, endPosition, movement);
                position.y += Mathf.Sin(normalized * Mathf.PI) * cardDealArcHeight;
                movingRect.position = position;
                movingRect.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(0f, targetAngle + cardDealRotation, movement));
                yield return null;
            }

            movingRect.position = endPosition;
            PlayHitSound();
            targetGroup.alpha = targetFinalAlpha;
            targetGroup.interactable = targetFinalInteractable;
            targetGroup.blocksRaycasts = targetFinalBlocksRaycasts;
            Destroy(movingObject);
        }

        public IEnumerator PlaySharedRevealAnimation(Action revealAtFlipMidpoint)
        {
            UpdateStatus("유리잔을 들어 올리고 공용 카드를 공개합니다...");
            EnsureSharedGlassCoverSlot();
            PlayCupOpenSound();
            yield return new WaitForSeconds(2f);

            RectTransform cardRect = sharedDisplayCardView != null
                ? sharedDisplayCardView.transform as RectTransform
                : null;
            yield return AnimateCupRiseAndCardReveal(cardRect, revealAtFlipMidpoint);
            sharedCardCovered = false;
            cupOpenPlayedForCurrentReveal = false;
        }

        private IEnumerator AnimateCupRiseAndCardReveal(
            RectTransform cardRect,
            Action revealCard)
        {
            const float totalDuration = 7f;
            const float slowSpinDuration = 6f;
            const float revealMoment = totalDuration - 0.2f;

            Vector2 glassStart = sharedGlassRect != null
                ? sharedGlassRect.anchoredPosition
                : Vector2.zero;
            Vector2 glassEnd = sharedGlassRect != null
                ? GetGlassOffscreenPosition()
                : Vector2.zero;
            Quaternion originalCardRotation = cardRect != null
                ? cardRect.localRotation
                : Quaternion.identity;
            bool revealed = false;
            float elapsed = 0f;
            SetSharedDisplayAlpha(1f);

            while (elapsed < totalDuration)
            {
                elapsed = Mathf.Min(totalDuration, elapsed + Time.deltaTime);
                float normalized = elapsed / totalDuration;

                if (sharedGlassRect != null)
                {
                    float glassMovement = Mathf.SmoothStep(0f, 1f, normalized);
                    sharedGlassRect.anchoredPosition = Vector2.LerpUnclamped(
                        glassStart, glassEnd, glassMovement);
                }

                if (cardRect != null)
                {
                    float angle;
                    if (elapsed <= slowSpinDuration)
                    {
                        angle = Mathf.SmoothStep(0f, 360f, elapsed / slowSpinDuration);
                    }
                    else
                    {
                        float fastSpin = Mathf.SmoothStep(
                            0f,
                            1f,
                            (elapsed - slowSpinDuration) /
                            (totalDuration - slowSpinDuration));
                        angle = Mathf.LerpUnclamped(360f, 720f, fastSpin);
                    }

                    cardRect.localRotation = originalCardRotation *
                                             Quaternion.Euler(0f, angle, 0f);
                }

                if (!revealed && elapsed >= revealMoment)
                {
                    revealed = true;
                    revealCard?.Invoke();
                }

                yield return null;
            }

            if (sharedGlassRect != null)
            {
                sharedGlassRect.anchoredPosition = glassEnd;
            }
            if (cardRect != null)
            {
                cardRect.localRotation = originalCardRotation;
            }
            SetSharedDisplayAlpha(1f);
            if (!revealed)
            {
                revealCard?.Invoke();
            }
        }

        public void ShowRoundResult(FinalRoundResult result)
        {
            if (resultPanel == null || result == null)
            {
                return;
            }

            resultPanel.SetActive(true);
            if (resultTitleText != null)
            {
                resultTitleText.text = result.WasFolded
                    ? "PLAYER FOLD"
                    : result.Winner == "Player"
                    ? "PLAYER WIN"
                    : result.Winner == "AI" ? "AI WIN" : "DRAW";
            }

            if (resultDetailText != null)
            {
                if (result.WasFolded)
                {
                    resultDetailText.text =
                        $"AI 선제 베팅: {FinalGameController.FormatMoney(result.AIBet)}\n\n" +
                        "플레이어가 다이를 선택했습니다.\n" +
                        $"포기한 참가금: {FinalGameController.FormatMoney(result.PlayerBet)}\n" +
                        "AI가 참가금을 획득하고 다음 라운드로 진행합니다.";
                    return;
                }

                string playerLine = result.PlayerValid
                    ? $"PLAYER  {result.PlayerExpression.ToDisplayString(true, result.SharedNumber)} = {result.PlayerValue:0.##}\n목표 {result.PlayerClosestTarget} / 거리 {result.PlayerDistance:0.###}"
                    : "PLAYER  무효 수식";
                string aiLine = result.AIValid
                    ? $"AI  {result.AIExpression.ToDisplayString(true, result.SharedNumber)} = {result.AIValue:0.##}\n목표 {result.AIClosestTarget} / 거리 {result.AIDistance:0.###}"
                    : "AI  무효 수식";
                resultDetailText.text =
                    $"공용 숫자: {result.SharedNumber}\n\n{playerLine}\n\n{aiLine}\n\n" +
                    $"PLAYER BET: {FinalGameController.FormatMoney(result.PlayerBet)}" +
                    $"{(result.WasAllIn ? "  (ALL IN)" : "")}\n" +
                    $"AI BET: {FinalGameController.FormatMoney(result.AIBet)}" +
                    $"{(result.AIWasAllIn ? "  (ALL IN)" : "")}\n" +
                    $"TOTAL POT: {FinalGameController.FormatMoney(result.TotalPot)}";
            }
        }

        public void HideRoundResult()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        public void ShowFinalShowdownIntro()
        {
            HideTimer();
            if (finalShowdownTitleText != null)
                finalShowdownTitleText.text = "묻고 더블로 가";
            if (finalShowdownDescriptionText != null)
                finalShowdownDescriptionText.text =
                    "일반 라운드가 동점입니다.\n다음 한 판의 승자가 게임의 최종 승자가 됩니다.";
            if (finalShowdownPanel != null)
                finalShowdownPanel.SetActive(true);

            if (!showdownSoundPlayed && sfxSource != null && showdownClip != null)
            {
                showdownSoundPlayed = true;
                sfxSource.PlayOneShot(showdownClip, showdownVolume);
            }
        }

        public void HideFinalShowdownIntro()
        {
            if (finalShowdownPanel != null)
            {
                finalShowdownPanel.SetActive(false);
            }
        }

        public void ShowGameOver(string winner)
        {
            HideTimer();
            HideAllInConfirmation();
            HideFinalShowdownIntro();
            bool playerWon = winner == "Player";
            bool lossComicStarted = false;
            canReturnToStartScene = false;
            PlayFinalOutcomeSound(playerWon);
            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = playerWon ? "최종 승리" : "최종 패배";
                gameOverTitleText.gameObject.SetActive(!playerWon);
            }
            if (gameOverDescriptionText != null)
            {
                gameOverDescriptionText.text =
                    $"PLAYER {FinalGameController.FormatMoney(controller.PlayerCredits)}\n" +
                    $"AI {FinalGameController.FormatMoney(controller.AICredits)}";
            }
            if (gameOverArt != null)
            {
                if (gameOverComicSequence != null)
                {
                    StopCoroutine(gameOverComicSequence);
                    gameOverComicSequence = null;
                }

                SetGameOverComicFramesVisible(false);
                if (playerWon)
                {
                    ShowSingleGameOverSprite(gameOverWinSprite, true);
                }
                else if (PrepareGameOverComicFrames())
                {
                    if (gameOverTitleText != null)
                    {
                        gameOverTitleText.gameObject.SetActive(false);
                    }
                    gameOverArt.sprite = null;
                    gameOverArt.color = Color.black;
                    gameOverArt.enabled = true;
                    gameOverComicSequence = StartCoroutine(PlayGameOverComicSequence());
                    lossComicStarted = true;
                }
                else
                {
                    ShowSingleGameOverSprite(gameOverLoseSprite, false);
                }
            }
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (playerWon)
            {
                StartCoroutine(ShowVictoryTitleAfterSound());
            }
            else if (!lossComicStarted)
            {
                ShowFinalTitleAndEnableReturn();
            }
        }

        private IEnumerator ShowVictoryTitleAfterSound()
        {
            float victoryDuration = victoryClip != null ? victoryClip.length : 0f;
            if (victoryDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(victoryDuration);
            }

            ShowFinalTitleAndEnableReturn();
        }

        private void ShowFinalTitleAndEnableReturn()
        {
            if (gameOverTitleText != null)
            {
                gameOverTitleText.gameObject.SetActive(true);
            }

            returnToStartSceneTime = Time.unscaledTime + GameOverClickDelay;
            canReturnToStartScene = true;
        }

        private void PlayFinalOutcomeSound(bool playerWon)
        {
            if (finalOutcomeSoundPlayed)
            {
                return;
            }

            finalOutcomeSoundPlayed = true;
            if (playerWon)
            {
                if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
                {
                    bgmSource.Play();
                }
                if (sfxSource != null && victoryClip != null)
                {
                    sfxSource.PlayOneShot(victoryClip, victoryVolume);
                }
                return;
            }

            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
            if (sfxSource != null)
            {
                sfxSource.Stop();
                if (gameOverClip != null)
                {
                    sfxSource.PlayOneShot(gameOverClip, gameOverVolume);
                }
            }
        }

        private void ShowSingleGameOverSprite(Sprite sprite, bool playerWon)
        {
            if (gameOverArt == null)
            {
                return;
            }

            if (sprite != null)
            {
                gameOverArt.sprite = sprite;
                gameOverArt.color = Color.white;
                gameOverArt.preserveAspect = false;
                gameOverArt.enabled = true;
                return;
            }

            Debug.LogWarning(
                playerWon
                    ? "FinalGameView의 Game Over Win Sprite가 비어 있습니다."
                    : "FinalGameView의 Game Over Lose Sprite와 네 컷 이미지가 비어 있습니다.");
        }

        private bool PrepareGameOverComicFrames()
        {
            if (gameOverArt == null)
            {
                return false;
            }

            if (gameOverComicFrames.Count == 4)
            {
                return true;
            }

            gameOverComicFrames.Clear();
            Vector2[] anchorMins =
            {
                new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0f)
            };
            Vector2[] anchorMaxs =
            {
                new Vector2(0.5f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(1f, 0.5f)
            };

            for (int index = 0; index < 4; index++)
            {
                Sprite frameSprite = Resources.Load<Sprite>(
                    $"Art/GameOver/gameover_{index + 1}");
                if (frameSprite == null)
                {
                    return false;
                }

                GameObject frameObject = new GameObject(
                    $"GameOver_Comic_Frame_{index + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform frameRect = frameObject.GetComponent<RectTransform>();
                frameRect.SetParent(gameOverArt.rectTransform, false);
                frameRect.anchorMin = anchorMins[index];
                frameRect.anchorMax = anchorMaxs[index];
                frameRect.offsetMin = new Vector2(2f, 2f);
                frameRect.offsetMax = new Vector2(-2f, -2f);

                Image frameImage = frameObject.GetComponent<Image>();
                frameImage.sprite = frameSprite;
                frameImage.color = Color.white;
                frameImage.preserveAspect = false;
                frameImage.raycastTarget = false;
                frameImage.gameObject.SetActive(false);
                gameOverComicFrames.Add(frameImage);
            }

            return true;
        }

        private IEnumerator PlayGameOverComicSequence()
        {
            for (int index = 0; index < gameOverComicFrames.Count; index++)
            {
                if (gameOverComicFrames[index] != null)
                {
                    gameOverComicFrames[index].gameObject.SetActive(true);
                }

                if (index < gameOverComicFrames.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(GameOverComicFrameInterval);
                }
            }

            ShowFinalTitleAndEnableReturn();

            gameOverComicSequence = null;
        }

        private void SetGameOverComicFramesVisible(bool visible)
        {
            foreach (Image frame in gameOverComicFrames)
            {
                if (frame != null)
                {
                    frame.gameObject.SetActive(visible);
                }
            }
        }

        private static bool WasPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            bool mousePressed = Mouse.current != null &&
                                Mouse.current.leftButton.wasPressedThisFrame;
            bool touchPressed = Touchscreen.current != null &&
                                Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            return mousePressed || touchPressed;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private void EnsureCallFoldControls()
        {
            Transform titleTransform = FindDescendant(transform, "Betting_Title");
            aiBetTitleText = titleTransform != null ? titleTransform.GetComponent<Text>() : null;

            if (foldButton == null)
            {
                Transform foldTransform = FindDescendant(transform, "OPTION_SLOT__Die_Future");
                foldButton = foldTransform != null ? foldTransform.GetComponent<Button>() : null;
            }
            if (callButton == null)
            {
                Transform callTransform = FindDescendant(transform, "OPTION_SLOT__Call_Future");
                callButton = callTransform != null ? callTransform.GetComponent<Button>() : null;
            }

            ConfigureDecisionButton(foldButton, "다이 (FOLD)", 0.12f, 0.47f);
            ConfigureDecisionButton(callButton, "받기 (CALL)", 0.53f, 0.88f);

            SetObjectActive(betMinusButton, false);
            SetObjectActive(betPlusButton, false);
            SetObjectActive(betConfirmButton, false);
            SetObjectActive(allInButton, false);

            Transform check = FindDescendant(transform, "OPTION_SLOT__Check_Future");
            if (check != null)
            {
                check.gameObject.SetActive(false);
            }

            Transform oldHint = FindDescendant(transform, "Future_Option_Hint");
            if (oldHint != null)
            {
                oldHint.gameObject.SetActive(false);
            }

            if (betText != null)
            {
                RectTransform rect = betText.rectTransform;
                rect.anchorMin = new Vector2(0.05f, 0.05f);
                rect.anchorMax = new Vector2(0.95f, 0.95f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                betText.fontSize = 20;
            }
        }

        private static void ConfigureDecisionButton(
            Button button,
            string label,
            float anchorMinX,
            float anchorMaxX)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.interactable = true;
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(anchorMinX, 0.12f);
                rect.anchorMax = new Vector2(anchorMaxX, 0.38f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Text labelText = button.GetComponentInChildren<Text>(true);
            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        private static void SetObjectActive(Button button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }

        private FinalCardView SpawnCard(
            Card card,
            Transform parent,
            bool playerCard,
            bool hidden,
            bool placedCard = false)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject cardObject = Instantiate(cardPrefab, parent);
            IncreaseFontSizes(cardObject.transform);
            FinalCardView cardView = cardObject.GetComponent<FinalCardView>();
            cardView.name = placedCard
                ? $"Placed_{card.GetCardType()}_{card.GetDisplayText()}"
                : $"{(playerCard ? "Player_Hand" : "AI")}_{card.GetCardType()}_{card.GetDisplayText()}";
            cardView.Configure(controller, card, playerCard, hidden, false, placedCard);
            if (!placedCard)
            {
                dealTargetViews[card] = cardView;
            }
            spawnedCards.Add(cardView);
            return cardView;
        }

        private FinalCardView SpawnSharedCard(Transform parent, bool placedCard)
        {
            if (parent == null || controller.SharedCard == null)
            {
                return null;
            }

            GameObject sharedObject = Instantiate(cardPrefab, parent);
            IncreaseFontSizes(sharedObject.transform);
            FinalCardView sharedView = sharedObject.GetComponent<FinalCardView>();
            sharedView.name = placedCard
                ? "Placed_Shared_Unopened_Card"
                : "Player_Hand_Shared_Unopened_Card";
            sharedView.Configure(
                controller,
                controller.SharedCard,
                true,
                false,
                true,
                placedCard);
            spawnedCards.Add(sharedView);
            return sharedView;
        }

        private FinalCardView SpawnSharedDisplayCard()
        {
            GameObject sharedObject = Instantiate(cardPrefab, sharedCardContainer);
            IncreaseFontSizes(sharedObject.transform);
            FinalCardView sharedView = sharedObject.GetComponent<FinalCardView>();
            sharedView.name = "Sealed_Shared_Physical_Card";
            sharedView.Configure(
                controller,
                controller.SharedCard,
                false,
                false,
                true,
                false,
                false,
                true);

            RectTransform rect = sharedView.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(112f, 176f);
                rect.localScale = Vector3.one;
            }

            sharedDisplayCanvasGroup = sharedObject.GetComponent<CanvasGroup>();
            if (sharedDisplayCanvasGroup == null)
            {
                sharedDisplayCanvasGroup = sharedObject.AddComponent<CanvasGroup>();
            }
            sharedDisplayCanvasGroup.alpha = sharedCardCovered ? 0f : 1f;
            sharedDisplayCanvasGroup.interactable = false;
            sharedDisplayCanvasGroup.blocksRaycasts = false;

            dealTargetViews[controller.SharedCard] = sharedView;

            spawnedCards.Add(sharedView);
            return sharedView;
        }

        private void EnsurePlayerCardInteractionAreas()
        {
            if (playerHandContainer == null)
            {
                playerHandContainer = FindDescendant(
                    transform,
                    "ART_SLOT__Player_Deal_Deck_Fan");
            }

            if (playerHandContainer != null)
            {
                Image oldFanArtwork = playerHandContainer.GetComponent<Image>();
                if (oldFanArtwork != null)
                {
                    oldFanArtwork.enabled = false;
                }
            }

            if (playerNumberContainer != null)
            {
                playerNumberContainer.gameObject.SetActive(false);
            }
            if (playerActionContainer != null)
            {
                playerActionContainer.gameObject.SetActive(false);
            }

            if (sharedCardContainer != null && sharedCardContainer.parent != null)
            {
                sharedCardContainer.parent.gameObject.SetActive(true);
            }

            if (playerPlacedCardContainer != null)
            {
                return;
            }

            Transform playerArea = FindDescendant(transform, "PLAYER_AREA");
            if (playerArea == null)
            {
                return;
            }

            Transform existing = FindDescendant(
                playerArea,
                "PLAYER_EXPRESSION_CARD_SLOTS__AUTO");
            if (existing != null)
            {
                playerPlacedCardContainer = existing;
                return;
            }

            GameObject containerObject = new GameObject(
                "PLAYER_EXPRESSION_CARD_SLOTS__AUTO",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            containerObject.layer = playerArea.gameObject.layer;

            RectTransform rect = containerObject.GetComponent<RectTransform>();
            rect.SetParent(playerArea, false);
            rect.anchorMin = new Vector2(0.24f, 0.08f);
            rect.anchorMax = new Vector2(0.79f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout =
                containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            playerPlacedCardContainer = rect;
        }

        private static void LayoutPlayerHand(IReadOnlyList<RectTransform> cards)
        {
            int count = cards.Count;
            for (int index = 0; index < count; index++)
            {
                RectTransform card = cards[index];
                if (card == null)
                {
                    continue;
                }

                float normalized = count <= 1 ? 0.5f : index / (float)(count - 1);
                float offset = normalized - 0.5f;
                float fanDistance = Mathf.Abs(offset) * 2f;
                float fanDrop = fanDistance * fanDistance * 22f;
                card.anchorMin = new Vector2(0.5f, 0.5f);
                card.anchorMax = new Vector2(0.5f, 0.5f);
                card.pivot = new Vector2(0.5f, 0.15f);
                card.sizeDelta = new Vector2(104f, 164f);
                card.anchoredPosition = new Vector2(offset * 300f, -fanDrop);
                card.localRotation = Quaternion.Euler(0f, 0f, offset * -26f);
                card.localScale = Vector3.one;
            }
        }

        private void BuildDecorativeCardBackGroups()
        {
            Sprite backSprite = GetCardBackSprite();
            if (backSprite == null)
            {
                Debug.LogWarning(
                    "EditableCard의 ART_SLOT__Card_Back에 카드 뒷면 Sprite가 없습니다. " +
                    "AI 패 묶음과 중앙 화투 더미를 생성하지 못했습니다.");
                return;
            }

            Transform drawPileSlot = FindDescendant(transform, DrawPileSlotName);

            BuildBackStack(drawPileSlot, backSprite, 7);
        }

        private void HideAIBackFan()
        {
            Transform aiFanSlot = FindDescendant(transform, AIBackFanSlotName);
            if (aiFanSlot != null)
            {
                aiFanSlot.gameObject.SetActive(false);
            }
        }

        private void EnsureSharedGlassCoverSlot()
        {
            if (sharedGlassCoverImage == null)
            {
                Transform existing = FindDescendant(transform, "ART_SLOT__Shared_Glass_Cover");
                sharedGlassCoverImage = existing != null
                    ? existing.GetComponent<Image>()
                    : null;
            }

            if (sharedGlassCoverImage == null && sharedCardContainer != null &&
                sharedCardContainer.parent != null)
            {
                GameObject glassObject = new GameObject(
                    "ART_SLOT__Shared_Glass_Cover",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                glassObject.layer = sharedCardContainer.gameObject.layer;
                RectTransform rect = glassObject.GetComponent<RectTransform>();
                rect.SetParent(sharedCardContainer.parent, false);
                rect.anchorMin = new Vector2(0.08f, 0.12f);
                rect.anchorMax = new Vector2(0.92f, 0.94f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                sharedGlassCoverImage = glassObject.GetComponent<Image>();
                sharedGlassCoverImage.color = Color.white;
                sharedGlassCoverImage.preserveAspect = true;
                sharedGlassCoverImage.raycastTarget = false;
            }

            if (sharedGlassCoverImage != null)
            {
                sharedGlassRect = sharedGlassCoverImage.rectTransform;
                sharedGlassCoverImage.enabled = sharedGlassCoverImage.sprite != null;
                if (!sharedGlassPositionInitialized)
                {
                    sharedGlassCoveredPosition = sharedGlassRect.anchoredPosition;
                    sharedGlassPositionInitialized = true;
                }
                sharedGlassCoverImage.transform.SetAsLastSibling();
            }
        }

        private void ResetSharedCoverForDeal()
        {
            sharedCardCovered = false;
            sharedDisplayCanvasGroup = null;
            EnsureSharedGlassCoverSlot();
            if (sharedGlassRect == null)
            {
                return;
            }

            sharedGlassCoverImage.gameObject.SetActive(true);
            sharedGlassRect.anchoredPosition = GetGlassOffscreenPosition();
        }

        private Vector2 GetGlassOffscreenPosition()
        {
            if (sharedGlassRect == null)
            {
                return sharedGlassCoveredPosition + Vector2.up * glassRaisedDistance;
            }

            Canvas canvas = sharedGlassRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            RectTransform parentRect = sharedGlassRect.parent as RectTransform;
            if (canvasRect == null || parentRect == null)
            {
                return sharedGlassCoveredPosition + Vector2.up * glassRaisedDistance;
            }

            Vector2 originalPosition = sharedGlassRect.anchoredPosition;
            sharedGlassRect.anchoredPosition = sharedGlassCoveredPosition;
            Canvas.ForceUpdateCanvases();

            Vector3[] canvasCorners = new Vector3[4];
            Vector3[] glassCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            sharedGlassRect.GetWorldCorners(glassCorners);

            float canvasTop = canvasCorners[0].y;
            float glassBottom = glassCorners[0].y;
            for (int index = 1; index < 4; index++)
            {
                canvasTop = Mathf.Max(canvasTop, canvasCorners[index].y);
                glassBottom = Mathf.Min(glassBottom, glassCorners[index].y);
            }

            float worldDistance = Mathf.Max(0f, canvasTop - glassBottom + GlassOffscreenMargin);
            float localDistance = Mathf.Abs(parentRect.InverseTransformVector(
                new Vector3(0f, worldDistance, 0f)).y);
            sharedGlassRect.anchoredPosition = originalPosition;
            return sharedGlassCoveredPosition + Vector2.up * localDistance;
        }

        private void SetSharedDisplayAlpha(float alpha)
        {
            if (sharedDisplayCanvasGroup != null)
            {
                sharedDisplayCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private IEnumerator MoveGlassAndRevealCard(
            RectTransform glassRect,
            Vector2 start,
            Vector2 end,
            float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            SetSharedDisplayAlpha(0f);
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                float movement = Mathf.SmoothStep(0f, 1f, normalized);
                glassRect.anchoredPosition = Vector2.LerpUnclamped(start, end, movement);

                // 유리잔이 충분히 들린 뒤 뒷면 카드가 서서히 나타난다.
                float cardReveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.85f, normalized));
                SetSharedDisplayAlpha(cardReveal);
                yield return null;
            }

            glassRect.anchoredPosition = end;
            SetSharedDisplayAlpha(1f);
        }

        private static IEnumerator MoveRect(
            RectTransform rect,
            Vector2 start,
            Vector2 end,
            float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                float t = Mathf.SmoothStep(0f, 1f, normalized);
                rect.anchoredPosition = Vector2.LerpUnclamped(start, end, t);

                if (normalized >= 1f)
                {
                    break;
                }

                yield return null;
            }
            rect.anchoredPosition = end;
        }

        private static IEnumerator ScaleCardX(
            RectTransform rect,
            float startX,
            float endX,
            float duration)
        {
            float elapsed = 0f;
            Vector3 scale = rect.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                scale.x = Mathf.LerpUnclamped(startX, endX, t);
                rect.localScale = scale;
                yield return null;
            }
            scale.x = endX;
            rect.localScale = scale;
        }

        private Sprite GetCardBackSprite()
        {
            if (cardPrefab == null)
            {
                return null;
            }

            Transform backSlot = FindDescendant(cardPrefab.transform, "ART_SLOT__Card_Back");
            Image backImage = backSlot != null ? backSlot.GetComponent<Image>() : null;
            return backImage != null ? backImage.sprite : null;
        }

        private static void BuildBackFan(Transform parent, Sprite sprite, int count)
        {
            if (parent == null)
            {
                return;
            }

            PrepareDecorativeContainer(parent);
            for (int index = 0; index < count; index++)
            {
                float normalized = count <= 1 ? 0f : index / (float)(count - 1);
                float offset = normalized - 0.5f;
                RectTransform card = CreateDecorativeBack(
                    parent,
                    sprite,
                    $"AI_Card_Back_{index + 1:00}");
                card.sizeDelta = new Vector2(104f, 164f);
                card.anchoredPosition = new Vector2(offset * 210f, -Mathf.Abs(offset) * 24f);
                card.localRotation = Quaternion.Euler(0f, 0f, offset * -38f);
            }
        }

        private static void BuildBackStack(Transform parent, Sprite sprite, int count)
        {
            if (parent == null)
            {
                return;
            }

            PrepareDecorativeContainer(parent);
            for (int index = 0; index < count; index++)
            {
                RectTransform card = CreateDecorativeBack(
                    parent,
                    sprite,
                    $"Draw_Pile_Card_Back_{index + 1:00}");
                card.sizeDelta = new Vector2(104f, 164f);
                card.anchoredPosition = new Vector2(index * 2.5f - 7.5f, index * 2.5f - 7.5f);
                card.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? -1.5f : 1.5f);
            }
        }

        private static void PrepareDecorativeContainer(Transform parent)
        {
            Image placeholder = parent.GetComponent<Image>();
            if (placeholder != null)
            {
                placeholder.enabled = false;
            }

            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (child.name.StartsWith("AI_Card_Back_") ||
                    child.name.StartsWith("Draw_Pile_Card_Back_"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private static RectTransform CreateDecorativeBack(
            Transform parent,
            Sprite sprite,
            string objectName)
        {
            GameObject cardObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            cardObject.layer = parent.gameObject.layer;

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;

            Image image = cardObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant.name == objectName)
                {
                    return descendant;
                }
            }

            return null;
        }
    }
}
