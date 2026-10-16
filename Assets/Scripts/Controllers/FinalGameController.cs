using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MathHighLow.Models;
using MathHighLow.Views;
using UnityEngine;

namespace MathHighLow.Controllers
{
    public sealed class FinalGameController : MonoBehaviour
    {
        public enum GamePhase
        {
            Idle,
            Dealing,
            BuildingExpression,
            Betting,
            Revealing,
            Results,
            FinalShowdownIntro,
            GameOver
        }

        public sealed class PlayerPlacedCard
        {
            public Card Card { get; }
            public bool IsShared { get; }

            public PlayerPlacedCard(Card card, bool isShared)
            {
                Card = card;
                IsShared = isShared;
            }
        }

        [Header("Scene Reference")]
        [SerializeField] private FinalGameView view;

        [Header("Time")]
        [SerializeField] private float roundDuration = 180f;
        [SerializeField] private float dealInterval = 0.05f;

        [Header("Money (KRW)")]
        [SerializeField] private long startingCredits = 2_000_000_000L;
        [SerializeField] private long minBet = 100_000_000L;
        [SerializeField] private long maxBaseBet = 500_000_000L;
        [SerializeField] private long betStep = 100_000_000L;

        [Header("AI Betting")]
        [Tooltip("최상급 패로 분류할 공용 숫자 1~12 평균 목표 거리의 최대값")]
        [Min(0f)]
        [SerializeField] private float topHandMaxAverageDistance = 1f;
        [Tooltip("중급 패로 분류할 평균 목표 거리의 최대값. 이 값보다 크면 하급 패입니다.")]
        [Min(0f)]
        [SerializeField] private float middleHandMaxAverageDistance = 3f;
        [Range(0f, 1f)]
        [SerializeField] private float topHandStrongBetChance = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float middleHandNormalBetChance = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float lowHandBluffChance = 0.1f;

        [Header("Number Deck")]
        [SerializeField] private int minNumber = 1;
        [SerializeField] private int maxNumber = 12;
        [SerializeField] private int copiesPerNumber = 4;

        public GamePhase Phase { get; private set; } = GamePhase.Idle;
        public Hand PlayerHand { get; private set; } = new Hand();
        public Hand AIHand { get; private set; } = new Hand();
        public BlindExpression PlayerExpression => playerExpression;
        public BlindExpression AIExpression => aiExpression;
        public NumberCard SharedCard { get; private set; }
        public bool SharedRevealed { get; private set; }
        public bool IsFinalShowdown { get; private set; }
        public bool IsAllIn { get; private set; }
        public bool AIIsAllIn { get; private set; }
        public bool PlayerFoldedThisRound { get; private set; }
        public bool IsExpressionLocked { get; private set; }
        public bool IsBetConfirmed { get; private set; }
        public long PlayerCredits { get; private set; }
        public long AICredits { get; private set; }
        public long CurrentBet { get; private set; }
        public long PlayerBet => CurrentBet;
        public long AIBet { get; private set; }
        public long TotalPot => PlayerBet + AIBet;
        public float RoundElapsed { get; private set; }
        public float RoundDuration => roundDuration;
        public bool CanSubmitExpression
        {
            get
            {
                if (Phase != GamePhase.BuildingExpression || IsExpressionLocked)
                {
                    return false;
                }

                string validationError;
                return ValidateExpression(playerExpression, PlayerHand, out validationError);
            }
        }
        public string StatusMessage { get; private set; } = "";
        public IReadOnlyList<PlayerPlacedCard> PlayerPlacedCards => playerPlacedCards;

        private readonly System.Random random = new System.Random();
        private readonly List<NumberCard> numberDeck = new List<NumberCard>();
        private readonly HashSet<Card> usedPlayerCards = new HashSet<Card>();
        private readonly List<PlayerPlacedCard> playerPlacedCards =
            new List<PlayerPlacedCard>();
        private BlindExpression playerExpression = new BlindExpression();
        private BlindExpression aiExpression = new BlindExpression();
        private SpecialCard pendingSquareRoot;
        private bool sharedUsed;
        private bool finalIntroConfirmed;
        private bool roundResultConfirmed;
        private bool gameOver;
        private FinalRoundResult lastResult;

        private void Awake()
        {
            EnforceNumberRange();

            if (view == null)
            {
                view = FindFirstObjectByType<FinalGameView>();
            }

            view?.Bind(this);
        }

        private void OnValidate()
        {
            EnforceNumberRange();
        }

        private void EnforceNumberRange()
        {
            minNumber = Mathf.Clamp(minNumber, 1, 12);
            maxNumber = Mathf.Clamp(maxNumber, minNumber, 12);
            copiesPerNumber = Mathf.Max(1, copiesPerNumber);
            topHandMaxAverageDistance = Mathf.Max(0f, topHandMaxAverageDistance);
            middleHandMaxAverageDistance = Mathf.Max(
                topHandMaxAverageDistance,
                middleHandMaxAverageDistance);
            topHandStrongBetChance = Mathf.Clamp01(topHandStrongBetChance);
            middleHandNormalBetChance = Mathf.Clamp01(middleHandNormalBetChance);
            lowHandBluffChance = Mathf.Clamp01(lowHandBluffChance);
        }

        private void Start()
        {
            PlayerCredits = startingCredits;
            AICredits = startingCredits;
            CurrentBet = minBet;
            StartCoroutine(GameLoop());
        }

        public void SelectCard(Card card)
        {
            if (!CanEditExpression() || card == null || IsPlayerCardPlaced(card))
            {
                return;
            }

            if (card is NumberCard numberCard)
            {
                AddPrivateNumber(numberCard);
            }
            else if (card is OperatorCard operatorCard)
            {
                AddOperatorCard(operatorCard);
            }
            else if (card is SpecialCard specialCard)
            {
                AddSpecialCard(specialCard);
            }

            RefreshView(true);
        }

        public void SelectSharedCard()
        {
            if (!CanEditExpression() || sharedUsed || !playerExpression.ExpectingNumber)
            {
                SetStatus("지금은 공용 숫자 카드를 배치할 수 없습니다.");
                return;
            }

            bool withRoot = pendingSquareRoot != null;
            playerExpression.AddSharedNumber(withRoot);
            sharedUsed = true;
            playerPlacedCards.Add(new PlayerPlacedCard(SharedCard, true));
            ConsumePendingRoot();
            view?.PlayHitSound();
            SetStatus("공용 카드 ?를 수식에 배치했습니다.");
            RefreshView(true);
        }

        public void ReturnPlacedCard(Card card, bool sharedCard)
        {
            if (!CanEditExpression())
            {
                return;
            }

            int placedIndex = -1;
            for (int index = 0; index < playerPlacedCards.Count; index++)
            {
                PlayerPlacedCard placed = playerPlacedCards[index];
                if ((sharedCard && placed.IsShared) ||
                    (!sharedCard && !placed.IsShared && ReferenceEquals(placed.Card, card)))
                {
                    placedIndex = index;
                    break;
                }
            }

            if (placedIndex < 0)
            {
                return;
            }

            int returnedCount = playerPlacedCards.Count - placedIndex;
            playerPlacedCards.RemoveRange(placedIndex, returnedCount);
            RebuildPlayerExpressionFromPlacedCards();
            SetStatus(returnedCount == 1
                ? "선택한 카드를 손패로 되돌렸습니다."
                : "수식 순서를 유지하기 위해 선택한 카드와 그 뒤의 카드를 손패로 되돌렸습니다.");
            RefreshView(true);
        }

        public void ResetExpression()
        {
            if (!CanEditExpression())
            {
                return;
            }

            playerExpression.Clear();
            usedPlayerCards.Clear();
            playerPlacedCards.Clear();
            sharedUsed = false;
            pendingSquareRoot = null;
            foreach (SpecialCard special in PlayerHand.SpecialCards)
            {
                special.ResetUsage();
            }

            SetStatus("수식을 초기화했습니다.");
            RefreshView(true);
        }

        public void SubmitExpression()
        {
            if (Phase != GamePhase.BuildingExpression || IsExpressionLocked)
            {
                return;
            }

            string validationError;
            if (!ValidateExpression(playerExpression, PlayerHand, out validationError))
            {
                SetStatus(validationError);
                return;
            }

            IsExpressionLocked = true;
            pendingSquareRoot = null;
            view?.PlayCupOpenSound();

            if (IsFinalShowdown)
            {
                SetStatus("최종 수식이 확정되었습니다. 공용 카드를 공개합니다.");
            }
            else
            {
                SetStatus("수식이 확정되었습니다. 공용 카드를 공개합니다.");
            }

            RefreshView();
        }

        public void CallAIBet()
        {
            if (Phase != GamePhase.Betting || IsBetConfirmed)
            {
                return;
            }

            if (AIBet <= 0 || PlayerCredits < AIBet)
            {
                SetStatus("AI 베팅액을 받을 자금이 부족합니다. 다이를 선택해야 합니다.");
                return;
            }

            CurrentBet = AIBet;
            IsAllIn = false;
            IsBetConfirmed = true;
            Phase = GamePhase.BuildingExpression;
            RoundElapsed = 0f;
            SetStatus(
                $"CALL! 양쪽이 {FormatMoney(AIBet)}씩 걸었습니다. " +
                $"총 판돈 {FormatMoney(TotalPot)} — 이제 수식을 작성하세요.");
            RefreshView();
        }

        public void FoldAIBet()
        {
            if (Phase != GamePhase.Betting || IsBetConfirmed)
            {
                return;
            }

            // Fold를 무비용으로 반복하지 못하도록 기존 최소 베팅을 참가금으로 처리한다.
            CurrentBet = Math.Min(minBet, PlayerCredits);
            PlayerFoldedThisRound = true;
            IsBetConfirmed = true;
            SetStatus(
                $"FOLD! 참가금 {FormatMoney(PlayerBet)}을 포기하고 AI가 라운드를 가져갑니다.");
            RefreshView();
        }

        private long ChooseAIBet()
        {
            if (AICredits <= 0)
            {
                return 0;
            }

            float totalDistance = 0f;
            int validCount = 0;
            for (int shared = minNumber; shared <= maxNumber; shared++)
            {
                float value;
                if (TryEvaluate(aiExpression, shared, out value))
                {
                    totalDistance += DistanceToClosestTarget(value);
                    validCount++;
                }
                else
                {
                    totalDistance += 20f;
                }
            }

            float averageDistance = totalDistance / (maxNumber - minNumber + 1);
            int betUnits;
            double roll = random.NextDouble();
            if (validCount > 0 && averageDistance <= topHandMaxAverageDistance)
            {
                // 최상급: 70%는 4~5억, 나머지는 2~3억으로 위장한다.
                betUnits = roll < topHandStrongBetChance
                    ? random.Next(4, 6)
                    : random.Next(2, 4);
            }
            else if (validCount > 0 && averageDistance <= middleHandMaxAverageDistance)
            {
                // 중급: 80%는 2~3억, 나머지는 1억 또는 4억으로 변화를 준다.
                betUnits = roll < middleHandNormalBetChance
                    ? random.Next(2, 4)
                    : (random.NextDouble() < 0.5 ? 1 : 4);
            }
            else
            {
                // 하급: 90%는 1억, 10%는 5억 블러핑이다.
                betUnits = roll < lowHandBluffChance ? 5 : 1;
            }

            long affordable = Math.Min(maxBaseBet, Math.Min(AICredits, PlayerCredits));
            long selected = Math.Min(betUnits * betStep, affordable);
            return Math.Max(minBet, selected);
        }

        public void ConfirmFinalShowdown()
        {
            if (Phase == GamePhase.FinalShowdownIntro)
            {
                finalIntroConfirmed = true;
            }
        }

        public void ConfirmRoundResult()
        {
            if (Phase == GamePhase.Results)
            {
                roundResultConfirmed = true;
            }
        }

        public static string FormatMoney(long won)
        {
            if (won == 0)
            {
                return "0원";
            }

            if (won % 100_000_000L == 0)
            {
                return $"{won / 100_000_000L}억";
            }

            return $"{won:N0}원";
        }

        private IEnumerator GameLoop()
        {
            bool playFinalShowdown = false;

            if (view != null)
            {
                yield return StartCoroutine(view.PlayRulesPanel());
            }

            while (!gameOver)
            {
                yield return StartCoroutine(PlayRound(playFinalShowdown));
                if (lastResult == null)
                {
                    yield break;
                }

                if (playFinalShowdown)
                {
                    string finalWinner = lastResult.Winner;
                    yield return StartCoroutine(WaitForRoundResultConfirmation(lastResult));
                    EndGame(finalWinner);
                    yield break;
                }

                if (lastResult.Winner == "Draw")
                {
                    Phase = GamePhase.FinalShowdownIntro;
                    finalIntroConfirmed = false;
                    view?.ShowFinalShowdownIntro();
                    SetStatus("묻고 더블로 가! 다음 한 판의 승자가 최종 승자입니다.");
                    RefreshView();

                    while (!finalIntroConfirmed)
                    {
                        yield return null;
                    }

                    view?.HideFinalShowdownIntro();
                    playFinalShowdown = true;
                    continue;
                }

                ApplyStandardRoundResult(lastResult);
                yield return StartCoroutine(WaitForRoundResultConfirmation(lastResult));

                if (PlayerCredits <= 0 || AICredits <= 0)
                {
                    EndGame(PlayerCredits > 0 ? "Player" : "AI");
                    yield break;
                }

                playFinalShowdown = false;
            }
        }

        private IEnumerator WaitForRoundResultConfirmation(FinalRoundResult result)
        {
            roundResultConfirmed = false;
            Phase = GamePhase.Results;
            view?.ShowRoundResult(result);
            RefreshView();

            while (!roundResultConfirmed)
            {
                yield return null;
            }

            view?.HideRoundResult();
        }

        private IEnumerator PlayRound(bool finalShowdown)
        {
            IsFinalShowdown = finalShowdown;
            Phase = GamePhase.Dealing;
            ResetRoundState();
            BuildRoundDecks();
            view?.BeginRound(finalShowdown);

            Card bottomDealCard =
                new OperatorCard(OperatorCard.OperatorType.Add);
            if (view != null)
            {
                SetStatus("AI에게 줄 연산자 또는 특수 카드 한 장을 선택하세요.");
                yield return StartCoroutine(view.PlayBottomDealSelection(
                    selected => bottomDealCard = selected));
            }
            else
            {
                Card[] fallbackCards =
                {
                    new OperatorCard(OperatorCard.OperatorType.Add),
                    new OperatorCard(OperatorCard.OperatorType.Subtract),
                    new OperatorCard(OperatorCard.OperatorType.Divide),
                    new SpecialCard(SpecialCard.SpecialType.Multiply),
                    new SpecialCard(SpecialCard.SpecialType.SquareRoot)
                };
                bottomDealCard =
                    fallbackCards[random.Next(fallbackCards.Length)];
            }

            SetStatus("카드 덱을 섞고 있습니다...");
            if (view != null)
            {
                yield return StartCoroutine(view.PlayDrawPileShuffleAnimation(1.7f));
                view.FinishBottomDealSound();
            }
            else
            {
                yield return new WaitForSeconds(1.7f);
            }

            SetStatus(finalShowdown
                ? "최종 한방 승부 카드를 분배합니다."
                : "카드를 분배합니다.");

            for (int i = 0; i < 2; i++)
            {
                NumberCard dealtCard = DrawNumberCard();
                PlayerHand.AddCard(dealtCard);
                RefreshView(true);
                if (view != null)
                {
                    yield return StartCoroutine(view.PlayCardDealAnimation(dealtCard));
                }
                yield return new WaitForSeconds(dealInterval);
            }

            for (int i = 0; i < 2; i++)
            {
                NumberCard dealtCard = DrawNumberCard();
                AIHand.AddCard(dealtCard);
                RefreshView(true);
                if (view != null)
                {
                    yield return StartCoroutine(view.PlayCardDealAnimation(dealtCard));
                }
                yield return new WaitForSeconds(dealInterval);
            }

            List<Card> actions = BuildActionCards();
            if (bottomDealCard is SpecialCard selectedSpecial &&
                selectedSpecial != null)
            {
                SpecialCard.SpecialType otherSpecialType =
                    selectedSpecial.Type == SpecialCard.SpecialType.Multiply
                        ? SpecialCard.SpecialType.SquareRoot
                        : SpecialCard.SpecialType.Multiply;

                for (int index = 0; index < 3; index++)
                {
                    if (actions[index] is SpecialCard)
                    {
                        actions[index] = new SpecialCard(otherSpecialType);
                        break;
                    }
                }

                for (int index = 3; index < actions.Count; index++)
                {
                    if (actions[index] is SpecialCard)
                    {
                        actions[index] = bottomDealCard;
                        break;
                    }
                }
            }
            else
            {
                actions[random.Next(3, actions.Count)] = bottomDealCard;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                if (i < 3)
                {
                    PlayerHand.AddCard(actions[i]);
                }
                else
                {
                    AIHand.AddCard(actions[i]);
                }

                RefreshView(true);
                if (view != null)
                {
                    yield return StartCoroutine(view.PlayCardDealAnimation(actions[i]));
                }
                yield return new WaitForSeconds(dealInterval);
            }

            // 공용 카드는 가장 마지막에 뒷면으로 놓고 유리잔으로 덮는다.
            SharedCard = DrawNumberCard();
            RefreshView(true);
            if (view != null)
            {
                yield return StartCoroutine(view.PlayCardDealAnimation(SharedCard));
            }
            yield return new WaitForSeconds(dealInterval);
            if (view != null)
            {
                yield return StartCoroutine(view.PlaySharedSealAnimation());
            }

            aiExpression = FindBestBlindExpression(AIHand);

            if (!finalShowdown)
            {
                AIBet = ChooseAIBet();
                AIIsAllIn = false;
                Phase = GamePhase.Betting;
                SetStatus(
                    $"AI가 {FormatMoney(AIBet)}을 선제 베팅했습니다. " +
                    $"받기(CALL) 또는 다이(FOLD)를 선택하세요. 다이 참가금은 {FormatMoney(minBet)}입니다.");
                RefreshView(true);

                while (!IsBetConfirmed)
                {
                    yield return null;
                }

                if (PlayerFoldedThisRound)
                {
                    lastResult = CreateFoldResult();
                    yield break;
                }
            }

            Phase = GamePhase.BuildingExpression;
            SetStatus(finalShowdown
                ? "최종 수식을 작성하세요. 이번 승부의 승자가 게임의 최종 승자입니다."
                : $"CALL 완료: 양쪽 {FormatMoney(AIBet)}씩 베팅했습니다. 공용 카드 ?를 포함해 수식을 완성하세요.");
            RefreshView(true);

            while (RoundElapsed < roundDuration)
            {
                RoundElapsed += Time.deltaTime;
                view?.UpdateTimer(RoundElapsed, roundDuration);

                if (IsExpressionLocked)
                {
                    break;
                }

                yield return null;
            }

            if (!IsExpressionLocked)
            {
                IsExpressionLocked = true;
                SetStatus("시간이 종료되어 현재 수식을 강제 제출합니다.");
            }

            yield return StartCoroutine(RevealSharedCard());
            lastResult = EvaluateRound();

            if (finalShowdown)
            {
                while (lastResult.Winner == "Draw")
                {
                    SetStatus("최종 승부가 다시 동점입니다. 공용 카드만 재추첨합니다.");
                    yield return new WaitForSeconds(1f);
                    SharedCard = DrawNumberCard();
                    SharedRevealed = false;
                    view?.RefreshAll();
                    yield return StartCoroutine(RevealSharedCard());
                    lastResult = EvaluateRound();
                }
            }

        }

        private void ResetRoundState()
        {
            PlayerHand.Clear();
            AIHand.Clear();
            playerExpression = new BlindExpression();
            aiExpression = new BlindExpression();
            usedPlayerCards.Clear();
            playerPlacedCards.Clear();
            pendingSquareRoot = null;
            sharedUsed = false;
            SharedCard = null;
            SharedRevealed = false;
            IsExpressionLocked = false;
            IsBetConfirmed = IsFinalShowdown;
            IsAllIn = false;
            AIIsAllIn = false;
            PlayerFoldedThisRound = false;
            CurrentBet = 0;
            AIBet = 0;
            RoundElapsed = 0f;
            lastResult = null;
        }

        private void BuildRoundDecks()
        {
            numberDeck.Clear();
            for (int value = minNumber; value <= maxNumber; value++)
            {
                for (int copy = 0; copy < copiesPerNumber; copy++)
                {
                    numberDeck.Add(new NumberCard(value));
                }
            }

            Shuffle(numberDeck);
        }

        private List<Card> BuildActionCards()
        {
            List<Card> specials = new List<Card>
            {
                new SpecialCard(SpecialCard.SpecialType.Multiply),
                new SpecialCard(SpecialCard.SpecialType.SquareRoot)
            };
            Shuffle(specials);

            List<Card> playerCards = new List<Card>
            {
                specials[0],
                CreateRandomBasicOperatorCard(),
                CreateRandomBasicOperatorCard()
            };
            List<Card> aiCards = new List<Card>
            {
                specials[1],
                CreateRandomBasicOperatorCard(),
                CreateRandomBasicOperatorCard()
            };

            Shuffle(playerCards);
            Shuffle(aiCards);
            playerCards.AddRange(aiCards);
            return playerCards;
        }

        private OperatorCard CreateRandomBasicOperatorCard()
        {
            OperatorCard.OperatorType[] basics =
            {
                OperatorCard.OperatorType.Add,
                OperatorCard.OperatorType.Subtract,
                OperatorCard.OperatorType.Divide
            };
            return new OperatorCard(basics[random.Next(basics.Length)]);
        }

        private NumberCard DrawNumberCard()
        {
            if (numberDeck.Count == 0)
            {
                BuildRoundDecks();
            }

            int index = numberDeck.Count - 1;
            NumberCard card = numberDeck[index];
            numberDeck.RemoveAt(index);
            return card;
        }

        private void AddPrivateNumber(NumberCard card)
        {
            if (!playerExpression.ExpectingNumber || !PlayerHand.NumberCards.Contains(card))
            {
                SetStatus("지금은 연산 카드를 선택할 차례입니다.");
                return;
            }

            bool withRoot = pendingSquareRoot != null;
            playerExpression.AddPrivateNumber(card, withRoot);
            usedPlayerCards.Add(card);
            playerPlacedCards.Add(new PlayerPlacedCard(card, false));
            ConsumePendingRoot();
            view?.PlayHitSound();
            SetStatus("숫자를 배치했습니다.");
        }

        private void AddOperatorCard(OperatorCard card)
        {
            if (playerExpression.ExpectingNumber || playerExpression.IsEmpty ||
                playerExpression.Operands.Count >= 3)
            {
                SetStatus("지금은 숫자 카드를 선택할 차례입니다.");
                return;
            }

            playerExpression.AddOperator(card.Operator);
            usedPlayerCards.Add(card);
            playerPlacedCards.Add(new PlayerPlacedCard(card, false));
            view?.PlayHitSound();
            SetStatus("연산자를 배치했습니다.");
        }

        private void AddSpecialCard(SpecialCard card)
        {
            if (card.Type == SpecialCard.SpecialType.SquareRoot)
            {
                if (!playerExpression.ExpectingNumber || pendingSquareRoot != null)
                {
                    SetStatus("√는 숫자를 놓기 직전에 선택하세요.");
                    return;
                }

                pendingSquareRoot = card;
                playerPlacedCards.Add(new PlayerPlacedCard(card, false));
                view?.PlayHitSound();
                SetStatus("다음 숫자 또는 공용 ?에 √가 적용됩니다.");
                return;
            }

            if (playerExpression.ExpectingNumber || playerExpression.IsEmpty ||
                playerExpression.Operands.Count >= 3)
            {
                SetStatus("×는 두 숫자 사이에 배치해야 합니다.");
                return;
            }

            playerExpression.AddOperator(OperatorCard.OperatorType.Multiply);
            card.Consume();
            usedPlayerCards.Add(card);
            playerPlacedCards.Add(new PlayerPlacedCard(card, false));
            view?.PlayHitSound();
            SetStatus("×를 배치했습니다.");
        }

        private void ConsumePendingRoot()
        {
            if (pendingSquareRoot == null)
            {
                return;
            }

            pendingSquareRoot.Consume();
            usedPlayerCards.Add(pendingSquareRoot);
            pendingSquareRoot = null;
        }

        private bool CanEditExpression()
        {
            return Phase == GamePhase.BuildingExpression && !IsExpressionLocked;
        }

        public bool IsPlayerCardUsed(Card card)
        {
            return IsPlayerCardPlaced(card);
        }

        public bool IsPlayerCardPlaced(Card card)
        {
            return playerPlacedCards.Any(placed =>
                !placed.IsShared && ReferenceEquals(placed.Card, card));
        }

        public bool IsSharedCardPlaced()
        {
            return playerPlacedCards.Any(placed => placed.IsShared);
        }

        private void RebuildPlayerExpressionFromPlacedCards()
        {
            playerExpression.Clear();
            usedPlayerCards.Clear();
            sharedUsed = false;
            pendingSquareRoot = null;

            foreach (SpecialCard special in PlayerHand.SpecialCards)
            {
                special.ResetUsage();
            }

            foreach (PlayerPlacedCard placed in playerPlacedCards)
            {
                if (placed.IsShared)
                {
                    bool withRoot = pendingSquareRoot != null;
                    playerExpression.AddSharedNumber(withRoot);
                    sharedUsed = true;
                    ConsumePendingRoot();
                    continue;
                }

                if (placed.Card is NumberCard numberCard)
                {
                    bool withRoot = pendingSquareRoot != null;
                    playerExpression.AddPrivateNumber(numberCard, withRoot);
                    usedPlayerCards.Add(numberCard);
                    ConsumePendingRoot();
                }
                else if (placed.Card is OperatorCard operatorCard)
                {
                    playerExpression.AddOperator(operatorCard.Operator);
                    usedPlayerCards.Add(operatorCard);
                }
                else if (placed.Card is SpecialCard specialCard)
                {
                    if (specialCard.Type == SpecialCard.SpecialType.SquareRoot)
                    {
                        pendingSquareRoot = specialCard;
                    }
                    else
                    {
                        playerExpression.AddOperator(OperatorCard.OperatorType.Multiply);
                        specialCard.Consume();
                        usedPlayerCards.Add(specialCard);
                    }
                }
            }
        }

        public bool ValidateExpression(BlindExpression expression, Hand hand, out string error)
        {
            error = "";
            if (expression == null || !expression.IsComplete)
            {
                error = "개인 숫자 2장과 공용 ?를 포함한 완성된 수식이 필요합니다.";
                return false;
            }

            if (expression.Operands.Count(o => o.IsShared) != 1)
            {
                error = "공용 숫자 ?를 정확히 한 번 사용해야 합니다.";
                return false;
            }

            List<NumberCard> remainingNumbers = new List<NumberCard>(hand.NumberCards);
            foreach (BlindOperand operand in expression.Operands.Where(o => !o.IsShared))
            {
                if (operand.Card == null || !remainingNumbers.Remove(operand.Card))
                {
                    error = "지급받지 않은 숫자 카드를 사용했습니다.";
                    return false;
                }
            }

            if (remainingNumbers.Count != 0)
            {
                error = "개인 숫자 카드 2장을 모두 사용해야 합니다.";
                return false;
            }

            Dictionary<OperatorCard.OperatorType, int> available =
                hand.OperatorCards.GroupBy(c => c.Operator)
                    .ToDictionary(g => g.Key, g => g.Count());
            int requiredMultiply = hand.GetMultiplyCount();
            int usedMultiply = expression.Operators.Count(o =>
                o == OperatorCard.OperatorType.Multiply);

            if (usedMultiply != requiredMultiply)
            {
                error = "지급받은 × 특수 카드를 반드시 사용해야 합니다.";
                return false;
            }

            foreach (IGrouping<OperatorCard.OperatorType, OperatorCard.OperatorType> group in
                     expression.Operators.Where(o => o != OperatorCard.OperatorType.Multiply)
                         .GroupBy(o => o))
            {
                int count;
                if (!available.TryGetValue(group.Key, out count) || group.Count() > count)
                {
                    error = "지급받지 않은 기본 연산자를 사용했습니다.";
                    return false;
                }
            }

            int requiredRoots = hand.GetSquareRootCount();
            int usedRoots = expression.Operands.Count(o => o.HasSquareRoot);
            if (usedRoots != requiredRoots)
            {
                error = "지급받은 √ 특수 카드를 반드시 사용해야 합니다.";
                return false;
            }

            return true;
        }

        private BlindExpression FindBestBlindExpression(Hand hand)
        {
            List<BlindExpression> candidates = GenerateCandidates(hand);
            BlindExpression best = candidates.FirstOrDefault();
            float bestScore = float.PositiveInfinity;

            foreach (BlindExpression candidate in candidates)
            {
                float total = 0f;
                for (int shared = minNumber; shared <= maxNumber; shared++)
                {
                    float value;
                    bool success = TryEvaluate(candidate, shared, out value);
                    total += success ? DistanceToClosestTarget(value) : 1000f;
                }

                float average = total / (maxNumber - minNumber + 1);
                if (average < bestScore)
                {
                    bestScore = average;
                    best = candidate;
                }
            }

            return best ?? new BlindExpression();
        }

        private List<BlindExpression> GenerateCandidates(Hand hand)
        {
            List<BlindExpression> results = new List<BlindExpression>();
            if (hand.NumberCards.Count != 2)
            {
                return results;
            }

            int[][] permutations =
            {
                new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
            };

            List<OperatorCard.OperatorType> binaryCards =
                hand.OperatorCards.Select(c => c.Operator).ToList();
            for (int i = 0; i < hand.GetMultiplyCount(); i++)
            {
                binaryCards.Add(OperatorCard.OperatorType.Multiply);
            }

            int rootCount = hand.GetSquareRootCount();
            IEnumerable<int> rootPositions = rootCount == 1
                ? new[] { 0, 1, 2 }
                : new[] { -1 };

            foreach (int[] permutation in permutations)
            {
                foreach (int rootPosition in rootPositions)
                {
                    for (int first = 0; first < binaryCards.Count; first++)
                    {
                        for (int second = 0; second < binaryCards.Count; second++)
                        {
                            if (first == second)
                            {
                                continue;
                            }

                            BlindExpression expression = new BlindExpression();
                            for (int position = 0; position < 3; position++)
                            {
                                int token = permutation[position];
                                bool hasRoot = position == rootPosition;
                                if (token == 2)
                                {
                                    expression.AddSharedNumber(hasRoot);
                                }
                                else
                                {
                                    expression.AddPrivateNumber(hand.NumberCards[token], hasRoot);
                                }

                                if (position == 0)
                                {
                                    expression.AddOperator(binaryCards[first]);
                                }
                                else if (position == 1)
                                {
                                    expression.AddOperator(binaryCards[second]);
                                }
                            }

                            string error;
                            if (ValidateExpression(expression, hand, out error))
                            {
                                results.Add(expression);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private IEnumerator RevealSharedCard()
        {
            Phase = GamePhase.Revealing;
            SetStatus("공용 숫자 카드를 공개합니다.");
            if (view != null)
            {
                yield return StartCoroutine(view.PlaySharedRevealAnimation(() =>
                {
                    SharedRevealed = true;
                    RefreshView();
                }));
            }
            else
            {
                SharedRevealed = true;
                RefreshView();
            }
            yield return new WaitForSeconds(1.25f);
        }

        private FinalRoundResult EvaluateRound()
        {
            FinalRoundResult result = new FinalRoundResult
            {
                PlayerExpression = playerExpression.Clone(),
                AIExpression = aiExpression.Clone(),
                SharedNumber = SharedCard.Value,
                PlayerBet = PlayerBet,
                AIBet = AIBet,
                TotalPot = TotalPot,
                Bet = TotalPot,
                WasAllIn = IsAllIn,
                AIWasAllIn = AIIsAllIn,
                IsFinalShowdown = IsFinalShowdown
            };

            string playerError;
            string aiError;
            result.PlayerValid = ValidateExpression(playerExpression, PlayerHand, out playerError) &&
                                 TryEvaluate(playerExpression, SharedCard.Value, out result.PlayerValue);
            result.AIValid = ValidateExpression(aiExpression, AIHand, out aiError) &&
                             TryEvaluate(aiExpression, SharedCard.Value, out result.AIValue);

            if (result.PlayerValid)
            {
                FindClosestTarget(result.PlayerValue, out result.PlayerClosestTarget,
                    out result.PlayerDistance);
            }
            else
            {
                result.PlayerDistance = float.PositiveInfinity;
            }

            if (result.AIValid)
            {
                FindClosestTarget(result.AIValue, out result.AIClosestTarget,
                    out result.AIDistance);
            }
            else
            {
                result.AIDistance = float.PositiveInfinity;
            }

            if (!result.PlayerValid && !result.AIValid)
            {
                result.Winner = "Draw";
            }
            else if (Mathf.Approximately(result.PlayerDistance, result.AIDistance))
            {
                result.Winner = "Draw";
            }
            else
            {
                result.Winner = result.PlayerDistance < result.AIDistance ? "Player" : "AI";
            }

            return result;
        }

        private FinalRoundResult CreateFoldResult()
        {
            return new FinalRoundResult
            {
                PlayerExpression = playerExpression.Clone(),
                AIExpression = aiExpression.Clone(),
                SharedNumber = 0,
                PlayerBet = PlayerBet,
                AIBet = AIBet,
                TotalPot = TotalPot,
                Bet = TotalPot,
                Winner = "AI",
                WasFolded = true,
                IsFinalShowdown = false
            };
        }

        private static bool TryEvaluate(BlindExpression expression, int sharedValue, out float value)
        {
            value = 0f;
            if (expression == null || !expression.IsComplete)
            {
                return false;
            }

            Expression resolved = new Expression();
            for (int i = 0; i < expression.Operands.Count; i++)
            {
                BlindOperand operand = expression.Operands[i];
                resolved.AddNumber(operand.ResolveValue(sharedValue), operand.HasSquareRoot);
                if (i < expression.Operators.Count)
                {
                    resolved.AddOperator(expression.Operators[i]);
                }
            }

            MathHighLow.Models.ExpressionEvaluator.EvaluationResult evaluation =
                MathHighLow.Models.ExpressionEvaluator.Evaluate(resolved);
            value = evaluation.Value;
            return evaluation.Success && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float DistanceToClosestTarget(float value)
        {
            return Mathf.Min(Mathf.Abs(value - 1f), Mathf.Abs(value - 20f));
        }

        private static void FindClosestTarget(float value, out int target, out float distance)
        {
            float toOne = Mathf.Abs(value - 1f);
            float toTwenty = Mathf.Abs(value - 20f);
            if (toOne <= toTwenty)
            {
                target = 1;
                distance = toOne;
            }
            else
            {
                target = 20;
                distance = toTwenty;
            }
        }

        private void ApplyStandardRoundResult(FinalRoundResult result)
        {
            if (result.Winner == "Player")
            {
                PlayerCredits += result.AIBet;
                AICredits -= result.AIBet;
            }
            else if (result.Winner == "AI")
            {
                PlayerCredits -= result.PlayerBet;
                AICredits += result.PlayerBet;
            }

            PlayerCredits = Math.Max(0, PlayerCredits);
            AICredits = Math.Max(0, AICredits);
        }

        private void EndGame(string winner)
        {
            gameOver = true;
            Phase = GamePhase.GameOver;
            view?.ShowGameOver(winner);
            SetStatus(winner == "Player" ? "최종 승리!" : "최종 패배");
            RefreshView();
        }

        private void SetStatus(string message)
        {
            StatusMessage = message;
            view?.UpdateStatus(message);
        }

        private void RefreshView(bool rebuildCards = false)
        {
            if (rebuildCards)
            {
                view?.RebuildCards();
            }

            view?.RefreshAll();
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }
    }
}
