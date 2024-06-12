using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ItemAPI;

namespace Mod
{
    public static class KGUI
    {
        public static Font font;
        public static Transform KGUIRoot, KGUIController;
        private static Canvas m_canvas;

        public static readonly Dictionary<TextAnchor, Vector2> AnchorMap = new Dictionary<TextAnchor, Vector2>()
        {
            { TextAnchor.LowerLeft,     new Vector2(0.0f, 0.0f) },
            { TextAnchor.LowerCenter,   new Vector2(0.5f, 0.0f) },
            { TextAnchor.LowerRight,    new Vector2(1.0f, 0.0f) },
            { TextAnchor.MiddleLeft,    new Vector2(0.0f, 0.5f) },
            { TextAnchor.MiddleCenter,  new Vector2(0.5f, 0.5f) },
            { TextAnchor.MiddleRight,   new Vector2(1.0f, 0.5f) },
            { TextAnchor.UpperLeft,     new Vector2(0.0f, 1.0f) },
            { TextAnchor.UpperCenter,   new Vector2(0.5f, 1.0f) },
            { TextAnchor.UpperRight,    new Vector2(1.0f, 1.0f) },
        };

        public static bool Toggle()
        {
            var active = !KGUIRoot.gameObject.activeSelf;
            KGUIRoot.gameObject.SetActive(active);
            return active;
        }

        public static void SetVisible(bool visible)
        {
            KGUIRoot.gameObject.SetActive(visible);
        }

        public static void Init()
        {
            KGUIController = new GameObject("KGUIController").transform;
            GameObject.DontDestroyOnLoad(KGUIController.gameObject);
            CreateCanvas();
            KGUIRoot = m_canvas.transform;
            KGUIRoot.SetParent(KGUIController);
        }

        public static void CreateCanvas()
        {
            GameObject canvas = new GameObject("Canvas");
            GameObject.DontDestroyOnLoad(canvas);
            m_canvas = canvas.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 100000;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }

        static Color defaultTextColor = new Color(1, 1, 1, .5f);
        public static Text CreateText(Transform parent, Vector2 offset, string text, TextAnchor anchor = TextAnchor.MiddleCenter, int font_size=20)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent != null ? parent : KGUIRoot);

            RectTransform trans = textObject.AddComponent<RectTransform>();
            trans.SetTextAnchor(anchor);
            trans.anchoredPosition = offset;

            Tools.LogPropertiesAndFields(Tools.sharedAuto1.LoadAsset<Font>("04b_03__"));
            Text textComponent = textObject.AddComponent<Text>();
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.alignment = anchor;
            textComponent.text = text;
            textComponent.font = Tools.sharedAuto1.LoadAsset<Font>("04b_03__");
            textComponent.fontSize = font_size;
            textComponent.color = defaultTextColor;

            return textComponent;
        }

        public static void SetTextAnchor(this RectTransform r, TextAnchor anchor)
        {
            r.anchorMin = AnchorMap[anchor];
            r.anchorMax = AnchorMap[anchor];
            r.pivot = AnchorMap[anchor];
        }

    }
}
