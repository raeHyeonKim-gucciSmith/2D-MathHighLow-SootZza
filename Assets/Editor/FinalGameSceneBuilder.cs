using System.IO;
using MathHighLow.Controllers;
using MathHighLow.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FinalGameSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MathHighLowGame.unity";
    private const string PrefabPath = "Assets/Prefabs/EditableCard.prefab";
    private const string UiFontPath = "Assets/Font/BMYEONSUNG_ttf.ttf";

    private static readonly Color Navy = new Color(0.035f, 0.055f, 0.09f, 1f);
    private static readonly Color Panel = new Color(0.08f, 0.11f, 0.17f, 0.94f);
    private static readonly Color Accent = new Color(0.93f, 0.62f, 0.16f, 1f);
    private static readonly Color Pale = new Color(0.86f, 0.9f, 0.96f, 1f);
    private static readonly Color HwatuBlue = new Color(0.20f, 0.39f, 0.72f, 0.96f);
    private static readonly Color MoneyYellow = new Color(1f, 0.72f, 0.04f, 0.98f);
    private static readonly Color ResultGreen = new Color(0.34f, 0.62f, 0.19f, 0.98f);

    static FinalGameSceneBuilder()
    {
        EditorApplication.delayCall += AutoBuildIfMissing;
    }

    [MenuItem("MathHighLow/Rebuild Final Game Scene")]
    public static void BuildScene()
    {
        EnsureFolders();
        GameObject cardPrefab = BuildCardPrefab();
        if (cardPrefab == null)
        {
            throw new MissingReferenceException(
                "EditableCard prefab was created without FinalCardView.");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MathHighLowGame";

        CreateCamera();
        CreateEventSystem();

        GameObject systems = new GameObject("GAME_SYSTEMS");
        FinalGameController controller = systems.AddComponent<FinalGameController>();
        new GameObject("HWATU_LAYOUT_VERSION__1");

        GameObject canvasObject = new GameObject(
            "GAME_UI__EDIT_IMAGES_AND_LAYOUT_HERE",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        FinalGameView view = canvasObject.AddComponent<FinalGameView>();

        Image background = CreateImage(
            canvasObject.transform,
            "ART_SLOT__Background",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            Navy);

        CreateText(canvasObject.transform, "Game_Title", "화투 수식 한판",
            new Vector2(0.39f, 0.955f), new Vector2(0.61f, 0.995f), 28, Color.white);

        // AI 상단 보드: 보유금액 / 최종숫자 / 공개 숫자 2장 / 비공개 액션 3장.
        RectTransform aiArea = CreatePanel(
            canvasObject.transform, "AI_AREA__Numbers_Visible_Actions_Hidden",
            new Vector2(0.03f, 0.70f), new Vector2(0.76f, 0.95f), HwatuBlue);
        RectTransform aiMoney = CreatePanel(aiArea, "AI_Money_Panel",
            new Vector2(0.015f, 0.08f), new Vector2(0.19f, 0.92f), MoneyYellow);
        Text aiCredits = CreateText(aiMoney, "AI_Credits", "AI 보유금액\n20억",
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), 25, Color.black);
        RectTransform aiNumbers = CreateCardContainer(aiArea, "AI_NumberCards__FACE_UP",
            new Vector2(0.225f, 0.08f), new Vector2(0.46f, 0.92f));
        RectTransform aiActions = CreateCardContainer(aiArea, "AI_ActionCards__FACE_DOWN",
            new Vector2(0.465f, 0.08f), new Vector2(0.985f, 0.92f));
        CreateText(aiArea, "AI_Label", "AI 화투패  |  모든 수식패 공개",
            new Vector2(0.34f, -0.16f), new Vector2(0.99f, 0.02f), 18, Color.black);
        // 중앙 좌측: 화투 더미 이미지 슬롯.
        RectTransform drawPile = CreatePanel(
            canvasObject.transform, "HWATU_DRAW_PILE",
            new Vector2(0.42f, 0.39f), new Vector2(0.58f, 0.63f), HwatuBlue);
        CreateImage(drawPile, "ART_SLOT__Hwatu_Draw_Pile",
            new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.90f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.08f, 0.08f, 0.18f));
        CreateText(drawPile, "Draw_Pile_Label", "화투패\n더미",
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.25f), 21, Color.white);

        // 중앙 우측: 미개봉 공용 카드.
        RectTransform sharedFrame = CreatePanel(
            canvasObject.transform, "SEALED_SHARED_CARD_AREA",
            new Vector2(0.74f, 0.35f), new Vector2(0.90f, 0.67f),
            new Color(0.10f, 0.12f, 0.13f, 0.88f));
        CreateImage(sharedFrame, "ART_SLOT__Sealed_Shared_Frame",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.60f, 0.66f, 0.70f, 0.18f));
        RectTransform shared = CreateCardContainer(sharedFrame,
            "SHARED_CARD__FACE_DOWN_UNTIL_REVEAL",
            new Vector2(0.15f, 0.17f), new Vector2(0.85f, 0.90f));
        Image sharedGlass = CreateImage(sharedFrame, "ART_SLOT__Shared_Glass_Cover",
            new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.94f),
            Vector2.zero, Vector2.zero, Color.white);
        sharedGlass.preserveAspect = true;
        sharedGlass.raycastTarget = false;
        CreateText(sharedFrame, "Shared_Card_Label", "미개봉 공용패",
            new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.18f), 20, Color.white);

        Text timer = CreateText(canvasObject.transform, "Round_Timer", "03:00",
            new Vector2(0.44f, 0.64f), new Vector2(0.56f, 0.70f), 34, Color.white);
        Text phase = CreateText(canvasObject.transform, "Phase", "DEALING",
            new Vector2(0.57f, 0.64f), new Vector2(0.72f, 0.70f), 18, Accent);
        RectTransform statusPanel = CreatePanel(
            canvasObject.transform, "Status_Log_Panel",
            new Vector2(0.02f, 0.38f), new Vector2(0.25f, 0.68f),
            new Color(0f, 0f, 0f, 0.68f));
        statusPanel.gameObject.AddComponent<RectMask2D>();
        statusPanel.GetComponent<Image>().raycastTarget = false;
        Text status = CreateText(statusPanel, "Status_Message", "게임 준비 중",
            new Vector2(0.07f, 0.06f), new Vector2(0.955f, 0.94f), 17, Color.white);
        status.alignment = TextAnchor.UpperLeft;
        status.supportRichText = false;
        status.raycastTarget = false;
        Text aiExpression = CreateText(canvasObject.transform, "AI_Expression", "AI 수식: 비공개",
            new Vector2(0.28f, 0.665f), new Vector2(0.68f, 0.70f), 18, Color.black);

        // 하단 플레이어 보드.
        RectTransform playerArea = CreatePanel(
            canvasObject.transform, "PLAYER_AREA",
            new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.31f), HwatuBlue);
        CreateImage(playerArea, "ART_SLOT__Player_Deal_Deck_Fan",
            new Vector2(0.015f, 0.25f), new Vector2(0.24f, 0.94f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.08f, 0.08f, 0.18f));
        RectTransform playerNumbers = CreateCardContainer(playerArea, "Player_NumberCards",
            new Vector2(0.18f, 0.08f), new Vector2(0.40f, 0.92f));
        RectTransform playerActions = CreateCardContainer(playerArea, "Player_ActionCards",
            new Vector2(0.405f, 0.08f), new Vector2(0.685f, 0.92f));
        RectTransform playerMoney = CreatePanel(playerArea, "Player_Money_Panel",
            new Vector2(0.805f, 0.07f), new Vector2(0.985f, 0.93f), MoneyYellow);
        Text playerCredits = CreateText(playerMoney, "Player_Credits", "PLAYER 보유금액\n20억",
            new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.95f), 24, Color.black);
        Text playerExpression = CreateText(canvasObject.transform, "Player_Expression", "수식을 작성하세요",
            new Vector2(0.28f, 0.31f), new Vector2(0.72f, 0.36f), 23, Color.black);
        CreateText(canvasObject.transform, "Player_Label", "Player 화투패",
            new Vector2(0.40f, 0.355f), new Vector2(0.60f, 0.39f), 20, Color.black);

        RectTransform controls = CreatePanel(
            playerArea, "EXPRESSION_CONTROLS",
            new Vector2(0.02f, 0.04f), new Vector2(0.165f, 0.23f), Panel);
        Button resetButton = CreateButton(controls, "Reset_Button", "초기화",
            new Vector2(0.02f, 0.08f), new Vector2(0.47f, 0.92f),
            new Color(0.34f, 0.36f, 0.42f));
        Button submitButton = CreateButton(controls, "Submit_Button", "수식 확정",
            new Vector2(0.53f, 0.08f), new Vector2(0.98f, 0.92f), Accent);

        // 베팅 단계에만 켜지는 전체 투명 오버레이. 중앙 선택부와 하단 금액부를 함께 관리한다.
        RectTransform bettingPanel = CreateContainer(
            canvasObject.transform, "BETTING_PANEL__Enabled_After_Expression",
            Vector2.zero, Vector2.one);
        RectTransform centerBetBox = CreatePanel(
            bettingPanel, "CENTER_BET_OPTIONS",
            new Vector2(0.32f, 0.38f), new Vector2(0.66f, 0.63f), HwatuBlue);
        CreateText(centerBetBox, "Betting_Title", "AI 선제 베팅\n받기 또는 다이를 선택하세요",
            new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.93f), 28, Color.white);
        Button allIn = CreateButton(centerBetBox, "All_In_Button", "ALL IN",
            new Vector2(0.05f, 0.15f), new Vector2(0.26f, 0.36f),
            new Color(0.08f, 0.08f, 0.08f));
        Button dieFuture = CreateButton(centerBetBox, "OPTION_SLOT__Die_Future", "다이 (FOLD)",
            new Vector2(0.12f, 0.12f), new Vector2(0.47f, 0.38f),
            new Color(0.08f, 0.08f, 0.08f));
        Button callFuture = CreateButton(centerBetBox, "OPTION_SLOT__Call_Future", "받기 (CALL)",
            new Vector2(0.53f, 0.12f), new Vector2(0.88f, 0.38f),
            new Color(0.08f, 0.08f, 0.08f));
        Button checkFuture = CreateButton(centerBetBox, "OPTION_SLOT__Check_Future", "CHECK",
            new Vector2(0.74f, 0.15f), new Vector2(0.95f, 0.36f),
            new Color(0.08f, 0.08f, 0.08f));
        dieFuture.interactable = true;
        callFuture.interactable = true;
        checkFuture.interactable = false;
        Text oldOptionHint = CreateText(centerBetBox, "Future_Option_Hint", "",
            new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.13f), 13, Pale);
        oldOptionHint.gameObject.SetActive(false);

        RectTransform playerBetBox = CreatePanel(
            bettingPanel, "PLAYER_BET_AMOUNT",
            new Vector2(0.805f, 0.055f), new Vector2(0.955f, 0.145f), MoneyYellow);
        Button betMinus = CreateButton(playerBetBox, "Bet_Minus", "-",
            new Vector2(0.02f, 0.15f), new Vector2(0.14f, 0.85f),
            new Color(0.85f, 0.85f, 0.85f));
        Text betText = CreateText(playerBetBox, "Bet_Amount", "1억",
            new Vector2(0.15f, 0.08f), new Vector2(0.47f, 0.92f), 22, Color.black);
        Button betPlus = CreateButton(playerBetBox, "Bet_Plus", "+",
            new Vector2(0.48f, 0.15f), new Vector2(0.60f, 0.85f),
            new Color(0.85f, 0.85f, 0.85f));
        Button betConfirm = CreateButton(playerBetBox, "Bet_Confirm_Button", "BET",
            new Vector2(0.63f, 0.12f), new Vector2(0.98f, 0.88f),
            new Color(0.06f, 0.06f, 0.06f));

        RectTransform resultPanel = CreateOverlay(canvasObject.transform, "RESULT_PANEL__Editable");
        Text resultTitle = CreateText(resultPanel, "Result_Title", "ROUND RESULT",
            new Vector2(0.25f, 0.67f), new Vector2(0.75f, 0.83f), 48, Accent);
        Text resultDetail = CreateText(resultPanel, "Result_Detail", "결과 상세",
            new Vector2(0.20f, 0.28f), new Vector2(0.80f, 0.67f), 24, Color.white);
        Button resultClose = CreateButton(resultPanel, "Result_Close", "다음 게임 시작",
            new Vector2(0.40f, 0.16f), new Vector2(0.60f, 0.24f), Accent);

        RectTransform allInPanel = CreateOverlay(canvasObject.transform, "ALL_IN_CONFIRMATION__Editable");
        Text allInText = CreateText(allInPanel, "All_In_Warning", "올인하시겠습니까?",
            new Vector2(0.25f, 0.48f), new Vector2(0.75f, 0.70f), 30, Color.white);
        Button allInConfirm = CreateButton(allInPanel, "Confirm_All_In", "CONFIRM ALL IN",
            new Vector2(0.27f, 0.35f), new Vector2(0.49f, 0.44f),
            new Color(0.72f, 0.16f, 0.16f));
        Button allInCancel = CreateButton(allInPanel, "Cancel_All_In", "CANCEL",
            new Vector2(0.51f, 0.35f), new Vector2(0.73f, 0.44f),
            new Color(0.3f, 0.32f, 0.38f));

        RectTransform finalPanel = CreateOverlay(canvasObject.transform, "FINAL_SHOWDOWN_PANEL__Editable");
        Image finalArt = CreateImage(finalPanel, "ART_SLOT__Final_Showdown",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.16f, 0.035f, 0.025f, 0.97f));
        Text finalTitle = CreateText(finalPanel, "Final_Showdown_Title", "묻고 더블로 가",
            new Vector2(0.17f, 0.60f), new Vector2(0.83f, 0.80f), 64, Accent);
        Text finalDesc = CreateText(finalPanel, "Final_Showdown_Description",
            "다음 한 판의 승자가 최종 승자입니다.",
            new Vector2(0.22f, 0.40f), new Vector2(0.78f, 0.60f), 30, Color.white);
        Button finalContinue = CreateButton(finalPanel, "Start_Final_Showdown", "한방 승부 시작",
            new Vector2(0.36f, 0.24f), new Vector2(0.64f, 0.34f), Accent);

        RectTransform gameOverPanel = CreateOverlay(canvasObject.transform, "GAME_OVER_PANEL__Editable");
        Image gameOverArt = CreateImage(gameOverPanel, "ART_SLOT__Game_Over",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.02f, 0.025f, 0.04f, 0.98f));
        Text gameOverTitle = CreateText(gameOverPanel, "Game_Over_Title", "최종 승리",
            new Vector2(0.20f, 0.58f), new Vector2(0.80f, 0.78f), 70, Accent);
        Text gameOverDesc = CreateText(gameOverPanel, "Game_Over_Description", "최종 자금",
            new Vector2(0.25f, 0.36f), new Vector2(0.75f, 0.58f), 30, Color.white);

        SetReference(controller, "view", view);
        SetReference(view, "cardPrefab", cardPrefab);
        SetReference(view, "playerNumberContainer", playerNumbers);
        SetReference(view, "playerActionContainer", playerActions);
        SetReference(view, "aiNumberContainer", aiNumbers);
        SetReference(view, "aiActionContainer", aiActions);
        SetReference(view, "sharedCardContainer", shared);
        SetReference(view, "sharedGlassCoverImage", sharedGlass);
        SetReference(view, "playerCreditsText", playerCredits);
        SetReference(view, "aiCreditsText", aiCredits);
        SetReference(view, "timerText", timer);
        SetReference(view, "phaseText", phase);
        SetReference(view, "statusText", status);
        SetReference(view, "playerExpressionText", playerExpression);
        SetReference(view, "aiExpressionText", aiExpression);
        SetReference(view, "submitButton", submitButton);
        SetReference(view, "resetButton", resetButton);
        SetReference(view, "bettingPanel", bettingPanel.gameObject);
        SetReference(view, "betText", betText);
        SetReference(view, "betMinusButton", betMinus);
        SetReference(view, "betPlusButton", betPlus);
        SetReference(view, "betConfirmButton", betConfirm);
        SetReference(view, "allInButton", allIn);
        SetReference(view, "callButton", callFuture);
        SetReference(view, "foldButton", dieFuture);
        SetReference(view, "allInConfirmationPanel", allInPanel.gameObject);
        SetReference(view, "allInConfirmationText", allInText);
        SetReference(view, "allInConfirmButton", allInConfirm);
        SetReference(view, "allInCancelButton", allInCancel);
        SetReference(view, "resultPanel", resultPanel.gameObject);
        SetReference(view, "resultTitleText", resultTitle);
        SetReference(view, "resultDetailText", resultDetail);
        SetReference(view, "resultCloseButton", resultClose);
        SetReference(view, "finalShowdownPanel", finalPanel.gameObject);
        SetReference(view, "finalShowdownTitleText", finalTitle);
        SetReference(view, "finalShowdownDescriptionText", finalDesc);
        SetReference(view, "finalShowdownContinueButton", finalContinue);
        SetReference(view, "gameOverPanel", gameOverPanel.gameObject);
        SetReference(view, "gameOverTitleText", gameOverTitle);
        SetReference(view, "gameOverDescriptionText", gameOverDesc);
        SetReference(view, "backgroundArt", background);
        SetReference(view, "finalShowdownArt", finalArt);
        SetReference(view, "gameOverArt", gameOverArt);

        resultPanel.gameObject.SetActive(false);
        allInPanel.gameObject.SetActive(false);
        finalPanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);
        bettingPanel.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = systems;
        Debug.Log($"[MathHighLow] Final game scene generated: {ScenePath}");
    }

    [MenuItem("MathHighLow/Apply BMYEONSUNG Font To All UI Text")]
    public static void ApplyBmyeonsungFontToAllUiText()
    {
        Font uiFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
        if (uiFont == null)
        {
            throw new UnityException($"BMYEONSUNG 폰트를 찾을 수 없습니다: {UiFontPath}");
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = uiFont;
                EditorUtility.SetDirty(text);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        foreach (Text text in prefabRoot.GetComponentsInChildren<Text>(true))
        {
            text.font = uiFont;
            EditorUtility.SetDirty(text);
        }
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        Debug.Log("[MathHighLow] 모든 Legacy UI Text에 BMYEONSUNG 폰트를 적용했습니다.");
    }

    [MenuItem("MathHighLow/Validate Final Game Scene")]
    public static void ValidateScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        FinalGameController controller = Object.FindFirstObjectByType<FinalGameController>();
        FinalGameView view = Object.FindFirstObjectByType<FinalGameView>();
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (controller == null)
            throw new MissingReferenceException("FinalGameController is missing.");
        if (view == null)
            throw new MissingReferenceException("FinalGameView is missing.");
        if (eventSystem == null)
            throw new MissingReferenceException("EventSystem is missing.");
        if (prefab == null)
            throw new MissingReferenceException("EditableCard prefab is missing.");

        SerializedObject controllerObject = new SerializedObject(controller);
        SerializedObject viewObject = new SerializedObject(view);
        RequireReference(controllerObject, "view");
        string[] viewReferences =
        {
            "cardPrefab", "playerNumberContainer", "playerActionContainer",
            "aiNumberContainer", "aiActionContainer", "sharedCardContainer",
            "sharedGlassCoverImage",
            "playerCreditsText", "aiCreditsText", "timerText", "phaseText",
            "statusText", "playerExpressionText", "aiExpressionText",
            "submitButton", "resetButton", "bettingPanel", "betText",
            "betMinusButton", "betPlusButton", "betConfirmButton", "allInButton",
            "callButton", "foldButton",
            "allInConfirmationPanel", "allInConfirmationText",
            "allInConfirmButton", "allInCancelButton", "resultPanel",
            "resultTitleText", "resultDetailText", "resultCloseButton",
            "finalShowdownPanel", "finalShowdownTitleText",
            "finalShowdownDescriptionText", "finalShowdownContinueButton",
            "gameOverPanel", "gameOverTitleText", "gameOverDescriptionText",
            "backgroundArt", "finalShowdownArt", "gameOverArt"
        };
        foreach (string reference in viewReferences)
        {
            RequireReference(viewObject, reference);
        }

        if (!scene.IsValid() || !scene.isLoaded)
            throw new UnityException("Final game scene could not be loaded.");

        Debug.Log("[MathHighLow] Scene validation passed: all required references are connected.");
    }

    [MenuItem("MathHighLow/Remove Final Number UI And Widen Hand")]
    public static void RemoveFinalNumberUiAndWidenHand()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DestroySceneObject(scene, "AI_Final_Number_Panel");
        DestroySceneObject(scene, "Player_Final_Number_Panel");

        SetAnchors(
            FindSceneObject(scene, "AI_NumberCards__FACE_UP"),
            new Vector2(0.225f, 0.08f),
            new Vector2(0.46f, 0.92f));
        SetAnchors(
            FindSceneObject(scene, "AI_ActionCards__FACE_DOWN"),
            new Vector2(0.465f, 0.08f),
            new Vector2(0.985f, 0.92f));
        SetAnchors(
            FindSceneObject(scene, "ART_SLOT__Player_Deal_Deck_Fan"),
            new Vector2(0.015f, 0.25f),
            new Vector2(0.24f, 0.94f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[MathHighLow] Final number UI removed and player hand area widened.");
    }

    [MenuItem("MathHighLow/Apply Shared Glass Cover Layout")]
    public static void ApplySharedGlassCoverLayout()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DestroySceneObject(scene, "ART_SLOT__AI_Deal_Deck_Fan");

        GameObject sharedFrameObject = FindSceneObject(scene, "SEALED_SHARED_CARD_AREA");
        if (sharedFrameObject == null)
        {
            throw new UnityException("SEALED_SHARED_CARD_AREA를 찾을 수 없습니다.");
        }

        GameObject glassObject = FindSceneObject(scene, "ART_SLOT__Shared_Glass_Cover");
        Image glassImage;
        if (glassObject == null)
        {
            glassImage = CreateImage(
                sharedFrameObject.transform,
                "ART_SLOT__Shared_Glass_Cover",
                new Vector2(0.08f, 0.12f),
                new Vector2(0.92f, 0.94f),
                Vector2.zero,
                Vector2.zero,
                Color.white);
        }
        else
        {
            glassImage = glassObject.GetComponent<Image>();
        }

        glassImage.preserveAspect = true;
        glassImage.raycastTarget = false;
        glassImage.transform.SetAsLastSibling();

        FinalGameView view = Object.FindFirstObjectByType<FinalGameView>(
            FindObjectsInactive.Include);
        if (view != null)
        {
            SetReference(view, "sharedGlassCoverImage", glassImage);
            EditorUtility.SetDirty(view);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[MathHighLow] AI 우측 카드 뭉음을 제거하고 공용패 유리잔 슬롯을 추가했습니다.");
    }

    private static void DestroySceneObject(Scene scene, string objectName)
    {
        GameObject target = FindSceneObject(scene, objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static void SetAnchors(
        GameObject target,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AutoBuildIfMissing()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        bool sceneMissing = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null;
        string sceneText = sceneMissing ? string.Empty : File.ReadAllText(ScenePath);
        bool layoutOutdated = !sceneMissing &&
                              !sceneText.Contains("HWATU_LAYOUT_VERSION__1");
        if (sceneMissing || layoutOutdated)
        {
            BuildScene();
        }
        else if (sceneText.Contains("m_Name: AI_Final_Number_Panel") ||
                 sceneText.Contains("m_Name: Player_Final_Number_Panel"))
        {
            RemoveFinalNumberUiAndWidenHand();
        }
        else if (sceneText.Contains("m_Name: ART_SLOT__AI_Deal_Deck_Fan") ||
                 !sceneText.Contains("m_Name: ART_SLOT__Shared_Glass_Cover"))
        {
            ApplySharedGlassCoverLayout();
        }
    }

    private static GameObject BuildCardPrefab()
    {
        GameObject root = new GameObject(
            "EditableCard",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(FinalCardView));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(104f, 164f);
        Image frame = root.GetComponent<Image>();
        frame.color = new Color(0.68f, 0.08f, 0.06f);

        Image artwork = CreateImage(root.transform, "ART_SLOT__Card_Face",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        artwork.preserveAspect = true;
        Image back = CreateImage(root.transform, "ART_SLOT__Card_Back",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        back.preserveAspect = true;
        Text label = CreateText(root.transform, "Card_Value", "?",
            new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.88f), 38, Color.black);
        Text type = CreateText(root.transform, "Card_Type", "CARD",
            new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.20f), 12, Color.black);
        label.gameObject.SetActive(false);
        type.gameObject.SetActive(false);

        FinalCardView view = root.GetComponent<FinalCardView>();
        SetReference(view, "frameImage", frame);
        SetReference(view, "artworkImage", artwork);
        SetReference(view, "backImage", back);
        SetReference(view, "labelText", label);
        SetReference(view, "typeText", type);
        SetReference(view, "button", root.GetComponent<Button>());

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetAsLastSibling();
    }

    private static RectTransform CreatePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax,
            Vector2.zero, Vector2.zero, color);
        return image.rectTransform;
    }

    private static RectTransform CreateContainer(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform CreateOverlay(Transform parent, string name)
    {
        return CreatePanel(parent, name, Vector2.zero, Vector2.one,
            new Color(0.015f, 0.02f, 0.03f, 0.96f));
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string value,
        Vector2 anchorMin,
        Vector2 anchorMax,
        int fontSize,
        Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = gameObject.GetComponent<Text>();
        text.text = value;
        Font uiFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
        text.font = uiFont != null
            ? uiFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject gameObject = new GameObject(
            name, typeof(RectTransform), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        Button button = gameObject.GetComponent<Button>();
        Text text = CreateText(gameObject.transform, "Label", label,
            Vector2.zero, Vector2.one, 20, Color.white);
        text.raycastTarget = false;
        return button;
    }

    private static RectTransform CreateCardContainer(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax,
            Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.035f));
        HorizontalLayoutGroup layout = image.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return image.rectTransform;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Missing serialized property {propertyName} on {target.name}");
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void RequireReference(SerializedObject target, string propertyName)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
        {
            throw new MissingReferenceException(
                $"{target.targetObject.name}.{propertyName} is not connected.");
        }
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }
}
