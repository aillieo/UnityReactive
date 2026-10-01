// <copyright file="ReactiveDashboardSample.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace AillieoUtils.Reactive.Samples
{
    using AillieoUtils.Reactive.Unity;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>Self-contained interactive uGUI demonstration; no Inspector wiring required.</summary>
    public sealed class ReactiveDashboardSample : MonoBehaviour
    {
        public ReactiveProperty<int> Health { get; } = Rx.Property(80);

        public ReactiveProperty<int> Gold { get; } = Rx.Property(100);

        public ReactiveCollection<string> Inventory { get; } = Rx.Collection<string>();

        public int RenderCount { get; private set; }

        public string Status => this.status == null ? "" : this.status.text;

        private ReactiveScope scope;
        private GameObject canvasHost;
        private GameObject eventHost;
        private Font font;
        private Sprite fillSprite;
        private UnityEngine.UI.Text healthText;
        private UnityEngine.UI.Text goldText;
        private UnityEngine.UI.Text inventoryText;
        private UnityEngine.UI.Text status;
        private UnityEngine.UI.Text diagnostics;
        private UnityEngine.UI.Slider healthSlider;
        private UnityEngine.UI.Image healthFill;
        private UnityEngine.UI.Button buyButton;
        private readonly Color foreground = new Color32(235, 241, 250, 255);
        private readonly Color muted = new Color32(169, 187, 210, 255);
        private readonly Color accent = new Color32(38, 123, 189, 255);

        private void OnEnable()
        {
            if (this.canvasHost == null)
            {
                this.Build();
            }


            this.canvasHost.SetActive(true);
            if (this.eventHost != null)
            {
                this.eventHost.SetActive(true);
            }


            this.scope = new ReactiveScope();
            var canBuy = this.scope.Add(Rx.Computed(() => this.Gold.Value >= 15));
            var state = this.scope.Add(Rx.Computed(() => this.Health.Value == 0 ? "DEFEATED" : this.Health.Value < 30 ? "LOW HEALTH" : "READY"));
            this.scope.BindInteractable(this.buyButton, canBuy);
            this.scope.Observe(() =>
            {
                this.healthText.text = $"{this.Health.Value} / 100";
                this.goldText.text = $"{this.Gold.Value} GOLD";
                this.inventoryText.text = $"{this.Inventory.Count} POTIONS";
                this.healthSlider.SetValueWithoutNotify(this.Health.Value);
                this.healthFill.fillAmount = this.Health.Value / 100f;
                this.healthFill.color = this.Health.Value < 30 ? new Color32(242, 163, 74, 255) : new Color32(58, 200, 169, 255);
                this.status.text = $"{state.Value}  /  {(canBuy.Value ? "Potion available" : "Not enough gold")}";
                this.diagnostics.text = $"UI refreshes: {++this.RenderCount}   |   Inventory: {string.Join(", ", this.Inventory)}";
            });
        }

        public void Damage()
        {
            this.Health.Value = Mathf.Max(0, this.Health.Value - 10);
        }

        public void Heal()
        {
            this.Health.Value = Mathf.Min(100, this.Health.Value + 10);
        }


        public void BuyPotion()
        {
            if (this.Gold.Value < 15)
            {
                return;
            }


            Rx.Batch(() => { this.Gold.Value -= 15; this.Inventory.Add("Potion"); });
        }

        public void UsePotion()
        {
            if (this.Inventory.Count == 0)
            {
                return;
            }


            Rx.Batch(() => { this.Inventory.RemoveAt(this.Inventory.Count - 1); this.Health.Value = Mathf.Min(100, this.Health.Value + 25); });
        }

        public void ApplyBatch()
        {
            Rx.Batch(() => { this.Health.Value = 30; this.Health.Value = 60; this.Health.Value = 90; this.Gold.Value += 25; });
        }

        public void ResetModel()
        {
            Rx.Batch(() => { this.Health.Value = 80; this.Gold.Value = 100; this.Inventory.Clear(); });
        }


        private void Build()
        {
            this.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            this.canvasHost = new GameObject("Reactive Dashboard Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            this.canvasHost.transform.SetParent(this.transform, false);
            this.canvasHost.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = this.canvasHost.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 900); scaler.matchWidthOrHeight = 1f;
            var background = this.Panel("Background", this.canvasHost.transform, new Color32(15, 23, 38, 255));
            Stretch(background.GetComponent<RectTransform>());
            var content = this.Panel("Content", background.transform, new Color32(15, 23, 38, 255));
            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.06f); rect.anchorMax = new Vector2(0.94f, 0.94f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            var layout = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 12; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            layout.childControlWidth = true; layout.childForceExpandWidth = true;
            this.Label(content.transform, "REACTIVE LAB", 30, this.foreground, 42);
            this.Label(content.transform, "Change the model. See computed state and collection bindings update.", 17, this.muted, 30);
            var cards = this.Row(content.transform, "Metrics", 86);
            this.healthText = this.Card(cards.transform, "HEALTH", "80 / 100");
            this.goldText = this.Card(cards.transform, "WALLET", "100 GOLD");
            this.inventoryText = this.Card(cards.transform, "INVENTORY", "0 POTIONS");
            var bar = this.Panel("HealthBar", content.transform, new Color32(44, 59, 79, 255)); Height(bar, 18);
            var fill = this.Panel("Fill", bar.transform, new Color32(58, 200, 169, 255)); Stretch(fill.GetComponent<RectTransform>());
            this.healthFill = fill.GetComponent<UnityEngine.UI.Image>();
            // Image.Type.Filled requires a sprite. A built-in white texture keeps this sample asset-free.
            this.fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
            this.healthFill.sprite = this.fillSprite;
            this.healthFill.type = UnityEngine.UI.Image.Type.Filled; this.healthFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            this.Label(content.transform, "HEALTH CONTROL   /   drag the slider or use the buttons", 16, this.muted, 26);
            this.healthSlider = this.CreateSlider(content.transform);
            var healthButtons = this.Row(content.transform, "HealthActions", 48);
            this.Button(healthButtons.transform, "Damage -10", this.Damage);
            this.Button(healthButtons.transform, "Heal +10", this.Heal);
            this.Button(healthButtons.transform, "Use potion +25", this.UsePotion);
            var economyButtons = this.Row(content.transform, "EconomyActions", 48);
            this.buyButton = this.Button(economyButtons.transform, "Buy potion / 15 gold", this.BuyPotion);
            this.Button(economyButtons.transform, "Earn 25 gold", () => this.Gold.Value += 25);
            var batchButtons = this.Row(content.transform, "BatchActions", 48);
            this.Button(batchButtons.transform, "Batch: HP 30 > 60 > 90, gold +25", this.ApplyBatch);
            this.Button(batchButtons.transform, "Reset", this.ResetModel);
            this.status = this.Label(content.transform, "READY", 22, this.foreground, 34);
            this.diagnostics = this.Label(content.transform, "UI refreshes: 0", 16, this.muted, 52);
            this.Label(content.transform, "One batch produces one UI refresh. Try spending all your gold: buying disables automatically.\nKeyboard: Tab / Shift+Tab to navigate, arrows for slider, Space to activate buttons.", 16, this.muted, 56);
            if (EventSystem.current == null)
            {
                this.eventHost = new GameObject("Reactive Dashboard EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                this.eventHost.transform.SetParent(this.transform, false);
            }
        }

        private GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false); var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }


        private static void Height(GameObject go, float height)
        {
            var item = go.AddComponent<UnityEngine.UI.LayoutElement>(); item.minHeight = item.preferredHeight = height; item.flexibleWidth = 1;
        }


        private GameObject Row(Transform parent, string name, float height)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup)); row.transform.SetParent(parent, false);
            var layout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>(); layout.spacing = 12; layout.childControlWidth = true;
            layout.childForceExpandWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = true; Height(row, height); return row;
        }

        private UnityEngine.UI.Text Label(Transform parent, string text, int size, Color color, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)); go.transform.SetParent(parent, false);
            var label = go.GetComponent<UnityEngine.UI.Text>(); label.font = this.font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            Height(go, height); return label;
        }

        private UnityEngine.UI.Text Card(Transform parent, string title, string value)
        {
            var card = this.Panel(title, parent, new Color32(28, 42, 61, 255)); Height(card, 86);
            var layout = card.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childControlHeight = true; layout.childForceExpandHeight = false;
            this.Label(card.transform, title, 14, this.muted, 24); return this.Label(card.transform, value, 26, this.foreground, 38);
        }

        private UnityEngine.UI.Button Button(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            var go = this.Panel(text, parent, this.accent); go.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var button = go.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.selectedColor = new Color(1.4f, 1.4f, 1.4f);
            colors.disabledColor = new Color(0.35f, 0.4f, 0.5f); button.colors = colors;
            Height(go, 48); var label = this.Label(go.transform, text, 17, Color.white, 48); label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(8, 0); label.rectTransform.offsetMax = new Vector2(-8, 0);
            button.onClick.AddListener(action); return button;
        }

        private UnityEngine.UI.Slider CreateSlider(Transform parent)
        {
            var go = this.Panel("HealthSlider", parent, new Color32(44, 59, 79, 255)); Height(go, 36);
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var slider = go.AddComponent<UnityEngine.UI.Slider>(); slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;
            var handle = this.Panel("Handle", go.transform, this.foreground); handle.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var rect = handle.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(24, 36);
            slider.handleRect = rect; slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.onValueChanged.AddListener(value => this.Health.Value = Mathf.RoundToInt(value)); return slider;
        }

        private void OnDisable()
        {
            this.scope?.Dispose(); this.scope = null; if (this.canvasHost != null)
            {
                this.canvasHost.SetActive(false);
            }

            if (this.eventHost != null)
            {
                this.eventHost.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (this.canvasHost != null)
            {
                Destroy(this.canvasHost);
            }

            if (this.eventHost != null)
            {
                Destroy(this.eventHost);
            }

            if (this.fillSprite != null)
            {
                Destroy(this.fillSprite);
            }
        }
    }
}
