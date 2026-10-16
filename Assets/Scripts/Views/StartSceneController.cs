using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MathHighLow.Views
{
    public sealed class StartSceneController : MonoBehaviour
    {
        [SerializeField] private Sprite startBackground;
        [SerializeField] private string gameSceneName = "MathHighLowGame";
        [SerializeField] private AudioClip startMusicClip;
        [SerializeField, Range(0f, 1f)] private float startMusicVolume = 0.9f;
        [SerializeField] private float blinkInterval = 0.55f;

        private Text pressStartText;
        private AudioSource musicSource;
        private bool acceptingInput;
        private bool startingGame;

        private void Awake()
        {
            BuildStartScreen();
            StartMusic();
            StartCoroutine(BlinkPressStart());
            StartCoroutine(EnableInputAfterShortDelay());
        }

        private void Update()
        {
            if (!acceptingInput || startingGame || !WasPointerPressedThisFrame())
            {
                return;
            }

            startingGame = true;
            if (musicSource != null)
            {
                musicSource.Stop();
            }
            SceneManager.LoadScene(gameSceneName);
        }

        private void BuildStartScreen()
        {
            GameObject canvasObject = new GameObject(
                "StartScreenCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backgroundObject = new GameObject(
                "StartBackground",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(canvasObject.transform, false);
            StretchToParent(backgroundRect);

            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.sprite = startBackground;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;

            GameObject textObject = new GameObject(
                "PressStartText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Outline));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(canvasObject.transform, false);
            textRect.anchorMin = new Vector2(0.15f, 0f);
            textRect.anchorMax = new Vector2(0.85f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 54f);
            textRect.sizeDelta = new Vector2(0f, 80f);

            pressStartText = textObject.GetComponent<Text>();
            pressStartText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pressStartText.text = "<< PRESS START >>";
            pressStartText.fontSize = 45;
            pressStartText.fontStyle = FontStyle.Bold;
            pressStartText.alignment = TextAnchor.MiddleCenter;
            pressStartText.color = new Color32(230, 24, 24, 255);
            pressStartText.raycastTarget = false;

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void StartMusic()
        {
            if (startMusicClip == null)
            {
                startMusicClip = Resources.Load<AudioClip>("Sound/stratScene");
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = startMusicVolume;
            musicSource.clip = startMusicClip;
            if (musicSource.clip != null)
            {
                musicSource.Play();
            }
        }

        private IEnumerator BlinkPressStart()
        {
            bool visible = true;
            while (pressStartText != null)
            {
                pressStartText.enabled = visible;
                visible = !visible;
                yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, blinkInterval));
            }
        }

        private IEnumerator EnableInputAfterShortDelay()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            acceptingInput = true;
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

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(Sprite background)
        {
            startBackground = background;
        }
#endif
    }
}
