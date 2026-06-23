using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PirateBroadside
{
    public enum BattleState
    {
        Menu,
        Playing,
        Victory,
        Defeat
    }

    public sealed class PirateGame : MonoBehaviour
    {
        public static PirateGame Instance { get; private set; }

        public BattleState State { get; private set; }
        public PlayerShip Player { get; private set; }

        private readonly List<EnemyShip> enemies = new List<EnemyShip>();
        private GameObject worldRoot;
        private Canvas activeCanvas;
        private BattleHud hud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (FindFirstObjectByType<PirateGame>() != null)
            {
                return;
            }

            var gameObject = new GameObject("Pirate Broadside");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<PirateGame>();
        }

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            EnsureEventSystem();
            ShowMainMenu();
        }

        private void Update()
        {
            if (State == BattleState.Menu
                && (Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.Space)
                    || Input.GetMouseButtonDown(0)))
            {
                BeginBattle();
                return;
            }

            if (State == BattleState.Playing && Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMainMenu();
            }
        }

        public void BeginBattle()
        {
            ClearCurrentView();
            State = BattleState.Playing;
            enemies.Clear();

            worldRoot = new GameObject("Battle World");
            var result = WorldBuilder.Build(worldRoot.transform);
            Player = result.Player;
            enemies.AddRange(result.Enemies);

            activeCanvas = UiFactory.CreateCanvas("Battle HUD", 20);
            hud = activeCanvas.gameObject.AddComponent<BattleHud>();
            hud.Initialize(Player, enemies.Count);
        }

        public void NotifyEnemySunk(EnemyShip enemy)
        {
            enemies.Remove(enemy);
            if (hud != null)
            {
                hud.SetEnemyCount(enemies.Count);
            }

            if (State == BattleState.Playing && enemies.Count == 0)
            {
                StartCoroutine(FinishAfterDelay(true));
            }
        }

        public void NotifyPlayerSunk()
        {
            if (State == BattleState.Playing)
            {
                StartCoroutine(FinishAfterDelay(false));
            }
        }

        private IEnumerator FinishAfterDelay(bool victory)
        {
            State = victory ? BattleState.Victory : BattleState.Defeat;
            yield return new WaitForSeconds(1.2f);
            ShowResult(victory);
        }

        private void ShowMainMenu()
        {
            StopAllCoroutines();
            ClearCurrentView();
            State = BattleState.Menu;

            activeCanvas = UiFactory.CreateCanvas("Main Menu", 30);
            var root = activeCanvas.transform as RectTransform;

            var background = UiFactory.Image(root, "Title Art", Color.white);
            UiFactory.Stretch(background.rectTransform);
            var titleArt = Resources.Load<Sprite>("TitleArt");
            if (titleArt != null)
            {
                background.sprite = titleArt;
            }

            var shade = UiFactory.Image(root, "Left Shade", new Color(0.015f, 0.035f, 0.055f, 0.78f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.54f, 1f);
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;

            var content = new GameObject("Menu Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(root, false);
            content.anchorMin = new Vector2(0.07f, 0.13f);
            content.anchorMax = new Vector2(0.48f, 0.87f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var eyebrow = UiFactory.Text(content, "TACTICAL NAVAL COMBAT", 20, new Color(0.95f, 0.74f, 0.3f, 1f), TextAnchor.MiddleLeft);
            UiFactory.Place(eyebrow.rectTransform, 0f, 0.84f, 1f, 0.93f);

            var title = UiFactory.Text(content, "PIRATE\nBROADSIDE", 74, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(title.rectTransform, 0f, 0.47f, 1f, 0.86f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 42;
            title.resizeTextMaxSize = 74;

            var subtitle = UiFactory.Text(content, "Command your ship. Break the pirate line.\nRule the turquoise sea.", 24, new Color(0.87f, 0.92f, 0.94f, 1f), TextAnchor.UpperLeft);
            UiFactory.Place(subtitle.rectTransform, 0f, 0.29f, 0.95f, 0.47f);

            var startButton = UiFactory.Button(content, "SET SAIL", new Color(0.86f, 0.42f, 0.12f, 1f));
            UiFactory.Place(startButton.GetComponent<RectTransform>(), 0f, 0.10f, 0.62f, 0.25f);
            startButton.onClick.AddListener(BeginBattle);

            var footer = UiFactory.Text(root, "W/S throttle   A/D steer   Q/E fire broadsides   ESC menu", 18, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter);
            footer.rectTransform.anchorMin = new Vector2(0.15f, 0.025f);
            footer.rectTransform.anchorMax = new Vector2(0.85f, 0.075f);
            footer.rectTransform.offsetMin = Vector2.zero;
            footer.rectTransform.offsetMax = Vector2.zero;
        }

        private void ShowResult(bool victory)
        {
            if (activeCanvas == null)
            {
                activeCanvas = UiFactory.CreateCanvas("Result", 40);
            }

            var root = activeCanvas.transform as RectTransform;
            var panel = UiFactory.Image(root, "Result Shade", new Color(0.01f, 0.025f, 0.04f, 0.88f));
            UiFactory.Place(panel.rectTransform, 0.27f, 0.24f, 0.73f, 0.76f);

            var headline = UiFactory.Text(panel.rectTransform, victory ? "VICTORY" : "SHIP LOST", 62,
                victory ? new Color(1f, 0.76f, 0.28f, 1f) : new Color(1f, 0.38f, 0.28f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(headline.rectTransform, 0.08f, 0.60f, 0.92f, 0.90f);

            var message = UiFactory.Text(panel.rectTransform,
                victory ? "The pirate squadron has been sent beneath the waves." : "The sea claims another captain. Rally your crew and return.",
                22, Color.white, TextAnchor.MiddleCenter);
            UiFactory.Place(message.rectTransform, 0.10f, 0.40f, 0.90f, 0.62f);

            var again = UiFactory.Button(panel.rectTransform, victory ? "SAIL AGAIN" : "TRY AGAIN", new Color(0.86f, 0.42f, 0.12f, 1f));
            UiFactory.Place(again.GetComponent<RectTransform>(), 0.22f, 0.13f, 0.78f, 0.33f);
            again.onClick.AddListener(BeginBattle);
        }

        private void ClearCurrentView()
        {
            if (activeCanvas != null)
            {
                Destroy(activeCanvas.gameObject);
            }

            if (worldRoot != null)
            {
                Destroy(worldRoot);
            }

            activeCanvas = null;
            hud = null;
            Player = null;

            var camera = Camera.main;
            if (camera != null)
            {
                Destroy(camera.gameObject);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }
        }
    }

    public sealed class BattleHud : MonoBehaviour
    {
        private PlayerShip player;
        private Image healthFill;
        private Text healthText;
        private Text enemyText;
        private Text portReload;
        private Text starboardReload;
        private Text speedText;

        public void Initialize(PlayerShip target, int enemyCount)
        {
            player = target;
            var root = transform as RectTransform;

            var topBand = UiFactory.Image(root, "Top Band", new Color(0.01f, 0.025f, 0.04f, 0.72f));
            UiFactory.Place(topBand.rectTransform, 0f, 0.90f, 1f, 1f);

            var objective = UiFactory.Text(topBand.rectTransform, "SINK THE PIRATE SQUADRON", 20, new Color(1f, 0.78f, 0.32f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(objective.rectTransform);

            enemyText = UiFactory.Text(root, string.Empty, 22, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Place(enemyText.rectTransform, 0.72f, 0.91f, 0.96f, 0.98f);

            var status = UiFactory.Image(root, "Status", new Color(0.01f, 0.025f, 0.04f, 0.80f));
            UiFactory.Place(status.rectTransform, 0.025f, 0.035f, 0.40f, 0.17f);

            healthText = UiFactory.Text(status.rectTransform, "HULL", 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(healthText.rectTransform, 0.05f, 0.53f, 0.28f, 0.92f);

            var healthBack = UiFactory.Image(status.rectTransform, "Hull Back", new Color(0.12f, 0.17f, 0.19f, 1f));
            UiFactory.Place(healthBack.rectTransform, 0.28f, 0.61f, 0.94f, 0.82f);
            healthFill = UiFactory.Image(healthBack.rectTransform, "Hull Fill", new Color(0.24f, 0.82f, 0.58f, 1f));
            UiFactory.Stretch(healthFill.rectTransform);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;

            speedText = UiFactory.Text(status.rectTransform, "0 KNOTS", 18, new Color(0.76f, 0.88f, 0.94f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(speedText.rectTransform, 0.05f, 0.10f, 0.42f, 0.48f);

            portReload = UiFactory.Text(status.rectTransform, "Q  PORT READY", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(portReload.rectTransform, 0.42f, 0.08f, 0.68f, 0.50f);
            starboardReload = UiFactory.Text(status.rectTransform, "E  STARBOARD READY", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(starboardReload.rectTransform, 0.68f, 0.08f, 0.97f, 0.50f);

            SetEnemyCount(enemyCount);
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            var health = player.HealthNormalized;
            healthFill.fillAmount = health;
            healthFill.color = Color.Lerp(new Color(0.92f, 0.22f, 0.12f), new Color(0.24f, 0.82f, 0.58f), health);
            healthText.text = $"HULL  {Mathf.CeilToInt(health * 100f)}%";
            speedText.text = $"{Mathf.RoundToInt(player.SpeedKnots)} KNOTS";
            portReload.text = player.PortReady ? "Q  PORT READY" : $"Q  {player.PortReloadRemaining:0.0}s";
            starboardReload.text = player.StarboardReady ? "E  STARBOARD READY" : $"E  {player.StarboardReloadRemaining:0.0}s";
            portReload.color = player.PortReady ? new Color(1f, 0.76f, 0.28f) : Color.white;
            starboardReload.color = player.StarboardReady ? new Color(1f, 0.76f, 0.28f) : Color.white;
        }

        public void SetEnemyCount(int count)
        {
            if (enemyText != null)
            {
                enemyText.text = $"PIRATE SHIPS  {count}";
            }
        }
    }

    internal static class UiFactory
    {
        private static Font font;

        public static Canvas CreateCanvas(string name, int order)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static Image Image(RectTransform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Text(RectTransform parent, string value, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            var gameObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button Button(RectTransform parent, string label, Color color)
        {
            var background = Image(parent, label + " Button", color);
            var button = background.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            button.colors = colors;

            var text = Text(background.rectTransform, label, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
