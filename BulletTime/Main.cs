using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.GameModes;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.MVVM._VM.Tooltip.Utils;
using Newtonsoft.Json;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BulletTime
{
    // =========================================================================
    //  子弹时间 (Bullet Time)
    //  即时制战斗开始时自动降低游戏时间倍率，战斗结束后恢复。
    //  暂停/继续不会重置倍率（倍率保存在 TimeController.PlayerTimeScale 中，
    //  解除暂停时 TimeController.Activate 会直接用它恢复 Time.timeScale）。
    //  左侧侧边栏按钮用于切换档位。
    // =========================================================================

    public class Main
    {
        private static HudWatcher _watcher;
        private static UnityModManagerNet.UnityModManager.ModEntry _entry;

        /// <summary>mod 安装目录（由 UMM 注入，用于读写设置文件）。</summary>
        public static string ModDir = AppDomain.CurrentDomain.BaseDirectory;

        public const string Version = "1.6.0";

        public static void Load(UnityModManagerNet.UnityModManager.ModEntry modEntry)
        {
            if (modEntry != null && !string.IsNullOrEmpty(modEntry.Path)) ModDir = modEntry.Path;
            _entry = modEntry;

            Config.Load();

            try
            {
                new Harmony("com.custom.bullettime").PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Error("Harmony patch failed", e);
            }

            try
            {
                Runner.Create();
            }
            catch (Exception e)
            {
                Error("runner failed", e);
            }

            try
            {
                _watcher = new HudWatcher();
                EventBus.Subscribe(_watcher);
            }
            catch (Exception e)
            {
                Error("event subscription failed", e);
            }
        }

        /// <summary>初始化失败时写入 UMM 自己的日志（不产生额外文件）。</summary>
        private static void Error(string what, Exception e)
        {
            try
            {
                if (_entry != null && _entry.Logger != null) _entry.Logger.Error("BulletTime: " + what + " - " + e.Message);
            }
            catch { }
        }
    }

    // =========================================================================
    //  设置
    // =========================================================================

    public class SettingsData
    {
        [JsonProperty] public int PresetIndex = 1;
    }

    public static class Config
    {
        /// <summary>可用档位；1.0 = 关闭子弹时间</summary>
        public static readonly float[] Presets = { 0.25f, 0.5f, 0.75f, 1.0f };

        private static int _index = 1;
        private static string SettingsPath
        {
            get { return Path.Combine(Main.ModDir, "settings.json"); }
        }

        public static int Index
        {
            get { return _index; }
            set { _index = Mathf.Clamp(value, 0, Presets.Length - 1); }
        }

        public static float Scale
        {
            get { return Presets[Index]; }
        }

        public static bool Enabled
        {
            get { return Scale < 0.999f; }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var data = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(SettingsPath));
                if (data != null) Index = data.PresetIndex;
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(new SettingsData { PresetIndex = Index }, Formatting.Indented));
            }
            catch { }
        }
    }

    // =========================================================================
    //  本地化（中 / 英）
    //
    //  所有面向玩家的文案都成对提供中文和英文。语言不缓存：
    //  LocalizationManager.CurrentLocale 每次访问都会重新读取游戏设置
    //  (SettingsRoot.Game.Main.Localization)，因此切换语言后下一轮刷新即生效。
    // =========================================================================

    public static class Loc
    {
        /// <summary>当前语言代码；每次实时读取，切换语言后立即变化。</summary>
        public static int Code
        {
            get
            {
                try { return (int)LocalizationManager.CurrentLocale; }
                catch { return -1; }
            }
        }

        public static bool IsChinese
        {
            get { return Code == (int)Locale.zhCN; }
        }

        public static string SetSpeedTitle
        {
            get { return IsChinese ? "子弹时间倍率" : "Bullet Time Speed"; }
        }

        public static string OffLabel
        {
            get { return IsChinese ? "关闭" : "OFF"; }
        }

        /// <summary>按钮悬停说明（随档位与当前语言变化）。</summary>
        public static string SpeedDescription(int index)
        {
            float s = Config.Presets[index];
            bool selected = index == Config.Index;

            if (s >= 0.999f)
            {
                if (IsChinese)
                {
                    return selected
                        ? "子弹时间已关闭，战斗保持正常速度（当前档位）"
                        : "关闭子弹时间，战斗保持正常速度";
                }
                return selected
                    ? "Bullet time is off; combat runs at normal speed (current)"
                    : "Turn off bullet time; combat runs at normal speed";
            }

            string speed = s.ToString("0.##");

            if (IsChinese)
            {
                return "即时制战斗速度：" + speed + " 倍" + (selected ? "（当前档位）" : "（点击切换）");
            }
            return "Real-time combat speed: " + speed + "x" + (selected ? " (current)" : " (click to switch)");
        }
    }

    // =========================================================================
    //  时间倍率
    // =========================================================================

    public static class TimeScaleManager
    {
        private static bool _applied;
        private static float _savedScale = 1f;

        /// <summary>当前是否应该处于子弹时间。</summary>
        public static bool ShouldSlow(Game game)
        {
            if (!Config.Enabled) return false;
            if (game == null || game.Player == null) return false;
            if (!game.Player.IsInCombat) return false;
            if (game.CurrentMode != GameModeType.Default) return false;
            if (game.IsPaused || game.IsFakePause) return false;
            try
            {
                if (TurnBased.Controllers.CombatController.IsInTurnBasedCombat()) return false;
            }
            catch { }
            return true;
        }

        public static void Apply(TimeController timeController)
        {
            try
            {
                var game = Game.Instance;
                if (game == null || timeController == null) return;

                if (ShouldSlow(game))
                {
                    if (!_applied)
                    {
                        _savedScale = timeController.PlayerTimeScale;
                        _applied = true;
                    }
                    if (Math.Abs(timeController.PlayerTimeScale - Config.Scale) > 0.0001f)
                    {
                        timeController.PlayerTimeScale = Config.Scale;
                    }
                }
                else if (_applied)
                {
                    // 只有在 Default 模式才真正恢复；暂停/全屏 UI 等模式下
                    // TimeController 不会运行，保持状态等回到 Default 再恢复。
                    if (!game.IsFakePause && game.CurrentMode == GameModeType.Default)
                    {
                        timeController.PlayerTimeScale = _savedScale;
                        _applied = false;
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(TimeController), "Tick")]
    internal static class Patch_TimeController_Tick
    {
        // 前缀：在 TimeController 用 PlayerTimeScale 计算 Time.timeScale 之前写入倍率
        private static void Prefix(TimeController __instance)
        {
            TimeScaleManager.Apply(__instance);
        }
    }

    // =========================================================================
    //  事件订阅：进入区域后安装 HUD 按钮
    // =========================================================================

    public class HudWatcher : IAreaHandler, IAreaActivationHandler
    {
        public void OnAreaDidLoad()
        {
            HudButtons.EnsureInstalled();
        }

        public void OnAreaActivated()
        {
            HudButtons.EnsureInstalled();
        }

        public void OnAreaBeginUnloading()
        {
            HudButtons.Invalidate();
        }
    }

    // =========================================================================
    //  后台守护：定时检查 HUD 是否重建
    // =========================================================================

    public class Runner : MonoBehaviour
    {
        private float _timer;

        public static void Create()
        {
            var go = new GameObject("BulletTime.Runner");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<Runner>();
        }

        private void Update()
        {
            HudButtons.UpdateAnchors();
            HudButtons.UpdateVisibility();

            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f) return;
            _timer = 0f;

            // 每 0.5s 重试安装（模板/字体/语言/档位也在其中刷新）
            HudButtons.EnsureInstalled();
        }
    }

    // =========================================================================
    //  左侧独立按钮面板
    //
    //  完全自建，不读取、不修改 BubbleBuffs 的任何对象：
    //    - 独立的 ScreenSpaceOverlay Canvas + GraphicRaycaster + CanvasGroup；
    //    - 只从游戏原生侧边栏取一个按钮作为外观模板（模板本身不做任何修改）；
    //    - 4 个按钮横向一行，贴屏幕左边缘、角色头像/动作条上方，
    //      以左下角为锚点，随 CanvasScaler 缩放自适应，显隐跟随 IngameMenuView。
    //
    //  Tooltip 定位：游戏的 UIUtility.SetPopupWindowPosition 会用
    //  Game.Instance.UI.UICamera.WorldToViewportPoint(TooltipPlace.position) 计算落点。
    //  直接把我们 Overlay 画布上的按钮当作 TooltipPlace，坐标空间不一致，
    //  所有候选 pivot 都会被判定为出屏，最终回落到屏幕正中。
    //  因此每个按钮在游戏画布坐标系内配一个不可见锚点，
    //  逐帧同步到按钮的屏幕位置，TooltipPlace 指向该锚点。
    // =========================================================================

    public static class HudButtons
    {
        private const string ButtonNamePrefix = "BulletTimeButton-";
        private const string PanelName = "BulletTimePanel";
        private const string AnchorName = "BulletTimeTooltipAnchor";
        private const string GameContainerPath = "NestedCanvas1/IngameMenuView/ButtonsPart/Container";
        private const string HudViewPath = "NestedCanvas1/IngameMenuView";
        private const float Pad = 6f;

        // 贴屏幕左边缘；该纵向区间在左侧快捷键条下方、角色头像/动作条上方，不会压到原生 UI。
        private const float LeftMargin = 4f;
        private const float BottomMargin = 160f;

        private static GameObject _panel;
        private static CanvasGroup _panelGroup;
        private static RectTransform _gameCanvasRect;
        private static Vector2 _cell = new Vector2(47.7f, 47.7f);
        private static Vector2 _spacing = Vector2.zero;
        private static GameObject _prefab;
        private static Transform _container;
        private static TMP_FontAsset _font;
        private static TMP_FontAsset _cjkFont;
        private static float _nextAttempt;
        private static int _localeCode = int.MinValue;
        private static int _lastIndex = int.MinValue;
        private static readonly List<GameObject> _buttons = new List<GameObject>();
        private static readonly List<RectTransform> _anchors = new List<RectTransform>();
        private static readonly List<TextMeshProUGUI> _labels = new List<TextMeshProUGUI>();
        private static readonly List<TooltipHandler> _tooltips = new List<TooltipHandler>();

        public static void Invalidate()
        {
            DestroyPanel();
        }

        public static void EnsureInstalled()
        {
            if (Time.unscaledTime < _nextAttempt) return;

            try
            {
                var game = Game.Instance;
                if (game == null || game.UI == null || game.UI.Canvas == null) return;

                var canvas = game.UI.Canvas.transform;
                if (canvas == null) return;
                _gameCanvasRect = canvas as RectTransform;

                // 面板是独立的屏幕空间 Canvas，不随游戏 UI 重建而销毁
                if (_panel == null)
                {
                    DestroyPanel();

                    var source = FindGameContainer(canvas);
                    if (source == null)
                    {
                        _nextAttempt = Time.unscaledTime + 5f;
                        return;
                    }

                    var prefab = FindPrefab(source);
                    if (prefab == null)
                    {
                        _nextAttempt = Time.unscaledTime + 5f;
                        return;
                    }

                    _font = FindFont(source);
                    _prefab = prefab;
                    if (!BuildPanel(canvas, source, prefab))
                    {
                        _nextAttempt = Time.unscaledTime + 5f;
                        return;
                    }
                }

                _buttons.RemoveAll(b => b == null);
                _labels.RemoveAll(l => l == null);

                if (_buttons.Count != Config.Presets.Length)
                {
                    ClearButtons();
                    for (int i = 0; i < Config.Presets.Length; i++)
                    {
                        AddButton(_prefab, i, canvas);
                    }
                }

                // 游戏画布重建或锚点丢失时重建锚点（tooltip 定位依赖游戏画布坐标系）
                if (_anchors.Count != _buttons.Count || _anchors.Exists(a => a == null || a.parent != canvas))
                {
                    RebuildAnchors(canvas);
                }

                SyncVisibility(canvas);
                UpdateVisuals();
            }
            catch
            {
                _nextAttempt = Time.unscaledTime + 5f;
            }
        }

        /// <summary>
        /// 创建完全独立的 ScreenSpaceOverlay 面板。
        /// 面板挂在独立 Overlay 画布上，手动定位，不依赖游戏画布的坐标系。
        /// </summary>
        private static bool BuildPanel(Transform gameCanvas, Transform source, GameObject prefab)
        {
            try
            {
                var srcGrid = source.GetComponent<GridLayoutGroup>();
                Vector2 cell = srcGrid != null ? srcGrid.cellSize : new Vector2(47.7f, 47.7f);
                if (cell.x < 1f) cell.x = 47.7f;
                if (cell.y < 1f) cell.y = 47.7f;
                _cell = cell;
                // 间距沿用游戏原生侧边栏的网格间距（实测 (0,0)，按钮彼此相接）
                _spacing = srcGrid != null ? srcGrid.spacing : Vector2.zero;

                int count = Config.Presets.Length;
                float panelW = count * cell.x + (count - 1) * _spacing.x + Pad * 2f;
                float panelH = cell.y + Pad * 2f;

                var go = new GameObject(PanelName, typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(go);

                var cv = go.AddComponent<Canvas>();
                cv.renderMode = RenderMode.ScreenSpaceOverlay;
                cv.sortingOrder = 30000;
                CopyScaler(go.AddComponent<CanvasScaler>(), gameCanvas);
                go.AddComponent<GraphicRaycaster>();

                var cg = go.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;

                var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)panelGo.transform;
                rt.SetParent(go.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(LeftMargin, BottomMargin);
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(panelW, panelH);

                var bg = panelGo.GetComponent<Image>();
                var srcImg = prefab.GetComponentInChildren<Image>(true);
                if (srcImg != null && srcImg.sprite != null)
                {
                    bg.sprite = srcImg.sprite;
                    bg.type = Image.Type.Sliced;
                }
                bg.color = new Color(0.05f, 0.05f, 0.06f, 0.72f);
                bg.raycastTarget = false;

                var layoutGo = new GameObject("Buttons", typeof(RectTransform));
                var layoutRt = (RectTransform)layoutGo.transform;
                layoutRt.SetParent(rt, false);
                layoutRt.anchorMin = Vector2.zero;
                layoutRt.anchorMax = Vector2.one;
                layoutRt.offsetMin = new Vector2(Pad, Pad);
                layoutRt.offsetMax = new Vector2(-Pad, -Pad);

                _panel = go;
                _panelGroup = cg;
                _container = layoutRt;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>复制游戏画布的缩放参数，保证按钮尺寸与游戏 UI 一致。</summary>
        private static void CopyScaler(CanvasScaler dst, Transform gameCanvas)
        {
            try
            {
                var src = gameCanvas != null ? gameCanvas.GetComponent<CanvasScaler>() : null;
                if (src != null)
                {
                    dst.uiScaleMode = src.uiScaleMode;
                    dst.referenceResolution = src.referenceResolution;
                    dst.screenMatchMode = src.screenMatchMode;
                    dst.matchWidthOrHeight = src.matchWidthOrHeight;
                    dst.referencePixelsPerUnit = src.referencePixelsPerUnit;
                    dst.scaleFactor = src.scaleFactor;
                }
                else
                {
                    dst.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    dst.referenceResolution = new Vector2(1920f, 1080f);
                    dst.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    dst.matchWidthOrHeight = 0.5f;
                }
            }
            catch { }
        }

        /// <summary>每帧同步显隐与 tooltip 锚点位置。</summary>
        public static void UpdateVisibility()
        {
            if (_panelGroup == null) return;
            try
            {
                var game = Game.Instance;
                if (game == null || game.UI == null || game.UI.Canvas == null) return;
                SyncVisibility(game.UI.Canvas.transform);
            }
            catch { }
        }

        /// <summary>
        /// 把每个 tooltip 锚点同步到对应按钮的屏幕位置。
        /// 锚点在游戏 UI 画布坐标系内，游戏用 UICamera 把它的位置换算成 tooltip 落点，
        /// 这样 tooltip 才会像原生按钮一样贴着按钮弹出，而不是回落到屏幕正中。
        /// </summary>
        public static void UpdateAnchors()
        {
            if (_gameCanvasRect == null || _anchors.Count == 0) return;
            try
            {
                var game = Game.Instance;
                if (game == null || game.UI == null) return;
                var cam = game.UI.UICamera;
                if (cam == null) return;

                for (int i = 0; i < _anchors.Count && i < _buttons.Count; i++)
                {
                    var anchor = _anchors[i];
                    var button = _buttons[i];
                    if (anchor == null || button == null) continue;

                    var br = button.transform as RectTransform;
                    if (br == null) continue;

                    Vector3 center = br.TransformPoint(br.rect.center);
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, center);

                    Vector3 world;
                    if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_gameCanvasRect, screen, cam, out world))
                    {
                        anchor.position = world;
                    }
                }
            }
            catch { }
        }

        /// <summary>显隐跟随游戏 HUD（IngameMenuView 的 CanvasGroup alpha），不碰 BubbleBuffs。</summary>
        private static void SyncVisibility(Transform canvas)
        {
            if (_panelGroup == null) return;
            bool show = true;
            try
            {
                var game = Game.Instance;
                if (game != null && game.CurrentMode == GameModeType.None) show = false;
            }
            catch { }
            try
            {
                var hud = canvas.Find(HudViewPath);
                if (hud != null)
                {
                    if (!hud.gameObject.activeInHierarchy) show = false;
                    else
                    {
                        var hudGroup = hud.GetComponent<CanvasGroup>();
                        if (hudGroup != null && hudGroup.alpha <= 0.01f) show = false;
                    }
                }
            }
            catch { }

            float alpha = show ? 1f : 0f;
            if (!Mathf.Approximately(_panelGroup.alpha, alpha)) _panelGroup.alpha = alpha;
            _panelGroup.blocksRaycasts = show;
            _panelGroup.interactable = show;
        }

        private static void ClearButtons()
        {
            foreach (var tip in _tooltips)
            {
                try { if (tip != null) tip.Dispose(); } catch { }
            }
            _tooltips.Clear();

            foreach (var anchor in _anchors)
            {
                if (anchor != null) UnityEngine.Object.Destroy(anchor.gameObject);
            }
            _anchors.Clear();

            foreach (var go in _buttons)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _buttons.Clear();
            _labels.Clear();
        }

        private static void DestroyPanel()
        {
            ClearButtons();
            if (_panel != null) UnityEngine.Object.Destroy(_panel);
            _panel = null;
            _panelGroup = null;
            _cell = new Vector2(47.7f, 47.7f);
            _container = null;
            _prefab = null;
        }

        /// <summary>只读地定位游戏原生侧边栏容器（作为按钮外观模板来源）。</summary>
        private static Transform FindGameContainer(Transform canvas)
        {
            var gameContainer = canvas.Find(GameContainerPath);
            if (gameContainer != null) return gameContainer;

            // 层级被额外包装/改名时的兜底：任意 ButtonsPart 下的 Container
            try
            {
                var all = canvas.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    var tr = all[i];
                    if (tr == null || tr.name != "Container") continue;
                    var parent = tr.parent;
                    if (parent != null && parent.name == "ButtonsPart") return tr;
                }
            }
            catch { }
            return null;
        }

        private static TMP_FontAsset FindFont(Transform container)
        {
            try
            {
                var anyTmp = container.GetComponentInChildren<TextMeshProUGUI>(true);
                if (anyTmp != null && anyTmp.font != null) return anyTmp.font;
            }
            catch { }
            try
            {
                if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts != null && fonts.Length > 0) return fonts[0];
            }
            catch { }
            return null;
        }

        /// <summary>找一个包含中文字形的 TMP 字体（游戏中文语言下必然存在）。</summary>
        private static TMP_FontAsset FindCjkFont()
        {
            if (_cjkFont != null) return _cjkFont;
            try
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts != null)
                {
                    for (int i = 0; i < fonts.Length; i++)
                    {
                        var f = fonts[i];
                        if (f == null) continue;
                        if (f.HasCharacter('关', true, false))
                        {
                            _cjkFont = f;
                            break;
                        }
                    }
                }
            }
            catch { }
            return _cjkFont;
        }

        /// <summary>找一个可复制的按钮模板（只读，模板本身不会被改动）。</summary>
        private static GameObject FindPrefab(Transform container)
        {
            GameObject first = null;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (child.name.StartsWith(ButtonNamePrefix)) continue;
                if (child.GetComponentInChildren<OwlcatButton>(true) == null) continue;
                if (first == null) first = child.gameObject;
                // 优先用非激活的模板，避免带上原按钮的运行时状态
                if (!child.gameObject.activeSelf) return child.gameObject;
            }
            return first;
        }

        private static void AddButton(GameObject prefab, int index, Transform canvas)
        {
            // worldPositionStays=false：完全采用模板的局部变换
            var obj = UnityEngine.Object.Instantiate(prefab, _container, false);
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
            obj.name = ButtonNamePrefix + index;
            PlaceButton(obj, index);
            _buttons.Add(obj);
            _anchors.Add(CreateAnchor(canvas));

            // 克隆自游戏按钮，先清掉原按钮的回调，避免触发游戏功能
            TooltipHandler tip = null;
            var owl = obj.GetComponentInChildren<OwlcatButton>(true);
            if (owl != null)
            {
                try { ((UnityEvent)owl.OnLeftClick).RemoveAllListeners(); } catch { }

                int captured = index;
                ((UnityEvent)owl.OnLeftClick).AddListener(new UnityAction(delegate
                {
                    Config.Index = captured;
                    Config.Save();
                    UpdateVisuals();
                }));

                try
                {
                    tip = owl.SetTooltip(new TooltipTemplateSimple(Loc.SetSpeedTitle, Loc.SpeedDescription(index)), TooltipAt(index));
                }
                catch { }
            }
            // 与 _buttons 一一对应（含 null），语言切换时按索引更新 tooltip
            _tooltips.Add(tip);

            var tmp = CreateLabel(obj.transform, SpeedLabel(index));
            _labels.Add(tmp);
        }

        /// <summary>手动定位：四个按钮横向一行，间距沿用游戏原生侧边栏。</summary>
        private static void PlaceButton(GameObject obj, int index)
        {
            try
            {
                var rt = obj.transform as RectTransform;
                if (rt == null) return;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = _cell;
                rt.anchoredPosition = new Vector2(index * (_cell.x + _spacing.x), 0f);
                rt.localScale = Vector3.one;
            }
            catch { }
        }

        private static string SpeedLabel(int index)
        {
            float s = Config.Presets[index];
            if (s >= 0.999f) return Loc.OffLabel;
            return s.ToString("0.##") + "x";
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text)
        {
            try
            {
                var go = new GameObject("bullet-time-label", typeof(RectTransform));
                go.transform.SetParent(parent, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var tmp = go.AddComponent<TextMeshProUGUI>();
                ApplyFont(tmp, text);
                tmp.text = text;
                tmp.fontSize = Mathf.Clamp(_cell.y * 0.42f, 10f, 30f);
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.raycastTarget = false;
                tmp.color = Color.white;
                return tmp;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>含非 ASCII 字符（如中文）时换成带中文形体的字体。</summary>
        private static void ApplyFont(TextMeshProUGUI tmp, string text)
        {
            bool needCjk = false;
            if (!string.IsNullOrEmpty(text))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] > 127) { needCjk = true; break; }
                }
            }
            var font = needCjk ? (FindCjkFont() ?? _font) : _font;
            if (font != null && tmp.font != font) tmp.font = font;
        }

        /// <summary>tooltip 锚点：位于游戏 UI 画布坐标系内，不可见、不参与布局与射线检测。</summary>
        private static RectTransform CreateAnchor(Transform canvas)
        {
            var go = new GameObject(AnchorName, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = _cell;
            rt.localScale = Vector3.one;
            return rt;
        }

        private static void RebuildAnchors(Transform canvas)
        {
            for (int i = 0; i < _anchors.Count; i++)
            {
                if (_anchors[i] != null) UnityEngine.Object.Destroy(_anchors[i].gameObject);
            }
            _anchors.Clear();

            for (int i = 0; i < _buttons.Count; i++)
            {
                _anchors.Add(CreateAnchor(canvas));
            }

            // TooltipPlace 指向旧锚点时替换成新锚点
            RefreshTooltips();
        }

        /// <summary>构造指向对应按钮锚点的 tooltip 配置。</summary>
        private static TooltipConfig TooltipAt(int index)
        {
            var cfg = new TooltipConfig();
            cfg.TooltipPlace = index >= 0 && index < _anchors.Count ? _anchors[index] : null;
            return cfg;
        }

        /// <summary>语言切换 / 档位变化后刷新全部 tooltip（标题、说明、“当前档位”标记）。</summary>
        private static void RefreshTooltips()
        {
            for (int i = 0; i < _tooltips.Count; i++)
            {
                var tip = _tooltips[i];
                if (tip == null) continue;
                try { tip.UpdateTooltip(new TooltipTemplateSimple(Loc.SetSpeedTitle, Loc.SpeedDescription(i)), TooltipAt(i)); }
                catch { }
            }
        }

        private static void UpdateVisuals()
        {
            try
            {
                bool localeChanged = _localeCode != Loc.Code;
                if (localeChanged) _localeCode = Loc.Code;
                bool indexChanged = _lastIndex != Config.Index;
                _lastIndex = Config.Index;

                for (int i = 0; i < _labels.Count; i++)
                {
                    var tmp = _labels[i];
                    if (tmp == null) continue;
                    bool selected = i == Config.Index;
                    string text = SpeedLabel(i);
                    if (tmp.text != text)
                    {
                        tmp.text = text;
                        ApplyFont(tmp, text);
                    }
                    tmp.color = selected ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 1f, 1f, 0.85f);
                }

                if (localeChanged || indexChanged) RefreshTooltips();
            }
            catch { }
        }
    }
}
