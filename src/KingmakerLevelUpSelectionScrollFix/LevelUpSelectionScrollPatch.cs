using System;
using System.Text;
using HarmonyLib;
using Kingmaker.UI.LevelUp;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerLevelUpSelectionScrollFix
{
    [HarmonyPatch(typeof(CharBSelectionSwitchFeatures), "SetupFeatureSelection")]
    internal static class LevelUpSelectionScrollPatch
    {
        private const string WrapperName = "KingmakerLevelUpSelectionScrollFix_ScrollRect";
        private const string ViewportName = "KingmakerLevelUpSelectionScrollFix_Viewport";
        private const string ScrollbarName = "KingmakerLevelUpSelectionScrollFix_VerticalScrollbar";

        private static int s_PostfixRunCount;
        private static int s_DumpRunCount;

        private static void Postfix(CharBSelectionSwitchFeatures __instance)
        {
            if (Main.Settings == null || !Main.Settings.EnablePatch)
            {
                return;
            }

            s_PostfixRunCount++;

            try
            {
                Apply(__instance);
            }
            catch (Exception ex)
            {
                Logger.Exception("SetupFeatureSelection postfix failed.", ex);
            }
        }

        private static void Apply(CharBSelectionSwitchFeatures instance)
        {
            if (instance == null)
            {
                Logger.Warning("SetupFeatureSelection postfix received a null instance.");
                return;
            }

            Transform itemsTransform = instance.ItemsContainer;
            if (itemsTransform == null)
            {
                Logger.Warning("ItemsContainer is null; cannot add scrolling.");
                return;
            }

            RectTransform content = itemsTransform as RectTransform;
            if (content == null)
            {
                Logger.Warning("ItemsContainer is not a RectTransform: " + GetPath(itemsTransform));
                return;
            }

            if (s_PostfixRunCount <= 5)
            {
                Logger.Info(
                    "SetupFeatureSelection postfix run " + s_PostfixRunCount
                    + ". ItemsContainer=" + GetPath(content)
                    + ", childCount=" + content.childCount);
            }

            LevelUpSelectionScrollMarker marker = content.GetComponent<LevelUpSelectionScrollMarker>();
            if (marker == null || !marker.IsValid)
            {
                marker = WrapItemsContainer(content, marker);
            }

            if (marker == null || !marker.IsValid)
            {
                Logger.Warning("Could not create a valid scroll wrapper for ItemsContainer.");
                return;
            }

            UpdateWrapper(marker, content);
            DumpDiagnostics(instance, content, marker);
        }

        private static LevelUpSelectionScrollMarker WrapItemsContainer(
            RectTransform content,
            LevelUpSelectionScrollMarker existingMarker)
        {
            Transform originalParent = content.parent;
            if (originalParent == null)
            {
                Logger.Warning("ItemsContainer has no parent; cannot wrap it.");
                return null;
            }

            int siblingIndex = content.GetSiblingIndex();
            RectTransformSnapshot contentSnapshot = RectTransformSnapshot.Capture(content);
            LayoutElement sourceLayoutElement = content.GetComponent<LayoutElement>();

            GameObject wrapperObject = new GameObject(WrapperName, typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
            RectTransform wrapper = wrapperObject.GetComponent<RectTransform>();
            wrapper.SetParent(originalParent, false);
            wrapper.SetSiblingIndex(siblingIndex);
            contentSnapshot.ApplyTo(wrapper);

            LayoutElement wrapperLayoutElement = wrapperObject.GetComponent<LayoutElement>();
            CopyWidthLayout(sourceLayoutElement, wrapperLayoutElement);

            GameObject viewportObject = new GameObject(ViewportName, typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(wrapper, false);
            StretchToParent(viewport);

            content.SetParent(viewport, false);
            ConfigureContentTransform(content);

            bool templateApplied;
            string templatePath;
            Scrollbar verticalScrollbar = CreateVerticalScrollbar(wrapper, content, out templateApplied, out templatePath);

            ScrollRect scrollRect = wrapperObject.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = GetScrollSensitivity();
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.horizontalScrollbar = null;

            LevelUpSelectionScrollMarker marker = existingMarker ?? content.gameObject.AddComponent<LevelUpSelectionScrollMarker>();
            marker.Wrapper = wrapper;
            marker.Viewport = viewport;
            marker.ScrollRect = scrollRect;
            marker.VerticalScrollbar = verticalScrollbar;
            marker.TemplateScrollbarApplied = templateApplied;
            marker.TemplateScrollbarPath = templatePath;
            marker.HasKnownScrollPosition = false;

            Logger.Info(
                "Wrapped ItemsContainer once. Parent=" + GetPath(originalParent)
                + ", wrapper=" + GetPath(wrapper)
                + ", content=" + GetPath(content)
                + ", templateScrollbar=" + (templateApplied ? templatePath : "<fallback>"));

            return marker;
        }

        private static void UpdateWrapper(LevelUpSelectionScrollMarker marker, RectTransform content)
        {
            float verticalPosition = marker.HasKnownScrollPosition
                ? marker.ScrollRect.verticalNormalizedPosition
                : 1f;
            verticalPosition = Mathf.Clamp01(verticalPosition);

            marker.ScrollRect.horizontal = false;
            marker.ScrollRect.vertical = true;
            marker.ScrollRect.scrollSensitivity = GetScrollSensitivity();

            EnsureViewportRaycastTarget(marker.Viewport);
            TryApplyTemplateScrollbar(marker, content);

            if (marker.VerticalScrollbar != null)
            {
                bool showScrollbar = Main.Settings.ShowScrollbar;
                marker.VerticalScrollbar.gameObject.SetActive(showScrollbar);
                marker.ScrollRect.verticalScrollbar = showScrollbar ? marker.VerticalScrollbar : null;
            }

            StretchToParent(marker.Viewport);
            ConfigureContentTransform(content);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float contentHeight = GetContentHeight(content);
            float maxHeight = Mathf.Max(1f, Main.Settings.MaxSelectorHeight);
            float viewportHeight = Mathf.Min(contentHeight, maxHeight);
            if (viewportHeight < 1f)
            {
                viewportHeight = maxHeight;
            }

            LayoutElement layoutElement = marker.Wrapper.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minHeight = viewportHeight;
                layoutElement.preferredHeight = viewportHeight;
                layoutElement.flexibleHeight = 0f;
            }

            marker.Wrapper.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportHeight);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            LayoutRebuilder.ForceRebuildLayoutImmediate(marker.Wrapper);
            marker.ScrollRect.horizontalNormalizedPosition = 0f;
            marker.ScrollRect.verticalNormalizedPosition = verticalPosition;
            marker.LastVerticalNormalizedPosition = verticalPosition;
            marker.HasKnownScrollPosition = true;

            if (marker.LastLoggedContentHeight != contentHeight
                || marker.LastLoggedViewportHeight != viewportHeight
                || marker.LastLoggedChildCount != content.childCount)
            {
                marker.LastLoggedContentHeight = contentHeight;
                marker.LastLoggedViewportHeight = viewportHeight;
                marker.LastLoggedChildCount = content.childCount;

                Logger.Info(
                    "Selection switch wrapper updated. contentHeight=" + contentHeight.ToString("0.0")
                    + ", viewportHeight=" + viewportHeight.ToString("0.0")
                    + ", maxHeight=" + maxHeight.ToString("0.0")
                    + ", childCount=" + content.childCount
                    + ", scrollbar=" + (Main.Settings.ShowScrollbar ? "enabled" : "disabled")
                    + ", verticalPosition=" + verticalPosition.ToString("0.000"));
            }
        }

        private static float GetContentHeight(RectTransform content)
        {
            float height = Mathf.Max(
                LayoutUtility.GetMinHeight(content),
                LayoutUtility.GetPreferredHeight(content));

            if (height <= 1f)
            {
                height = Mathf.Max(height, content.rect.height);
            }

            Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, content);
            if (childBounds.size.y > 1f)
            {
                height = Mathf.Max(height, childBounds.size.y);
            }

            return Mathf.Max(1f, height);
        }

        private static Scrollbar CreateVerticalScrollbar(
            RectTransform wrapper,
            RectTransform content,
            out bool templateApplied,
            out string templatePath)
        {
            Scrollbar template = FindTemplateScrollbar(content);
            Scrollbar clonedScrollbar = CloneScrollbarFromTemplate(template, wrapper);
            if (clonedScrollbar != null)
            {
                templateApplied = true;
                templatePath = GetPath(template.transform);
                Logger.Info("Cloned level-up selector scrollbar style from " + templatePath);
                return clonedScrollbar;
            }

            templateApplied = false;
            templatePath = string.Empty;
            return CreateFallbackVerticalScrollbar(wrapper);
        }

        private static Scrollbar CreateFallbackVerticalScrollbar(RectTransform wrapper)
        {
            GameObject scrollbarObject = new GameObject(ScrollbarName, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.SetParent(wrapper, false);
            ConfigureScrollbarRect(scrollbarRect, 12f);

            Image backgroundImage = scrollbarObject.GetComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.20f);

            GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
            RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
            slidingArea.SetParent(scrollbarRect, false);
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            RectTransform handle = handleObject.GetComponent<RectTransform>();
            handle.SetParent(slidingArea, false);
            StretchToParent(handle);

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(1f, 0.84f, 0.45f, 0.65f);

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handle;

            return scrollbar;
        }

        private static void EnsureViewportRaycastTarget(RectTransform viewport)
        {
            if (viewport == null)
            {
                return;
            }

            Image image = viewport.GetComponent<Image>();
            if (image == null)
            {
                image = viewport.gameObject.AddComponent<Image>();
            }

            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        private static void TryApplyTemplateScrollbar(LevelUpSelectionScrollMarker marker, RectTransform content)
        {
            if (marker.TemplateScrollbarApplied)
            {
                return;
            }

            Scrollbar template = FindTemplateScrollbar(content);
            Scrollbar clonedScrollbar = CloneScrollbarFromTemplate(template, marker.Wrapper);
            if (clonedScrollbar == null)
            {
                return;
            }

            Scrollbar oldScrollbar = marker.VerticalScrollbar;
            marker.VerticalScrollbar = clonedScrollbar;
            marker.ScrollRect.verticalScrollbar = clonedScrollbar;
            marker.TemplateScrollbarApplied = true;
            marker.TemplateScrollbarPath = GetPath(template.transform);

            if (oldScrollbar != null)
            {
                UnityEngine.Object.Destroy(oldScrollbar.gameObject);
            }

            Logger.Info("Replaced fallback scrollbar with cloned level-up selector scrollbar style from " + marker.TemplateScrollbarPath);
        }

        private static Scrollbar FindTemplateScrollbar(RectTransform content)
        {
            Transform abilitiesLeftSide = FindAncestorByName(content, "AbilitiesLeftSide");
            if (abilitiesLeftSide == null)
            {
                return null;
            }

            Scrollbar[] scrollbars = abilitiesLeftSide.GetComponentsInChildren<Scrollbar>(true);
            Scrollbar activeFirstLayer = null;
            Scrollbar inactiveFirstLayer = null;
            Scrollbar activeSelector = null;
            Scrollbar inactiveSelector = null;

            for (int i = 0; i < scrollbars.Length; i++)
            {
                Scrollbar scrollbar = scrollbars[i];
                if (scrollbar == null || IsOwnTransform(scrollbar.transform))
                {
                    continue;
                }

                string path = GetPath(scrollbar.transform);
                bool firstLayer = path.Contains("AbilitiesLeftSide/Selector/Selector/FirstLayer/Scrollbar");
                bool selectorLayer = path.Contains("AbilitiesLeftSide/Selector/Selector/");

                if (firstLayer)
                {
                    if (scrollbar.gameObject.activeInHierarchy)
                    {
                        activeFirstLayer = scrollbar;
                    }
                    else if (inactiveFirstLayer == null)
                    {
                        inactiveFirstLayer = scrollbar;
                    }
                }

                if (selectorLayer)
                {
                    if (scrollbar.gameObject.activeInHierarchy)
                    {
                        activeSelector = scrollbar;
                    }
                    else if (inactiveSelector == null)
                    {
                        inactiveSelector = scrollbar;
                    }
                }
            }

            if (activeFirstLayer != null)
            {
                return activeFirstLayer;
            }

            if (activeSelector != null)
            {
                return activeSelector;
            }

            if (inactiveFirstLayer != null)
            {
                return inactiveFirstLayer;
            }

            return inactiveSelector;
        }

        private static Scrollbar CloneScrollbarFromTemplate(Scrollbar template, RectTransform wrapper)
        {
            if (template == null || wrapper == null)
            {
                return null;
            }

            GameObject scrollbarObject = UnityEngine.Object.Instantiate(template.gameObject);
            scrollbarObject.name = ScrollbarName;

            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            if (scrollbarRect == null || scrollbar == null)
            {
                UnityEngine.Object.Destroy(scrollbarObject);
                return null;
            }

            RectTransform templateRect = template.transform as RectTransform;
            float width = templateRect != null ? Mathf.Max(12f, templateRect.rect.width) : 27f;

            scrollbarRect.SetParent(wrapper, false);
            ConfigureScrollbarRect(scrollbarRect, width);
            scrollbarObject.SetActive(true);

            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            if (scrollbar.handleRect == null)
            {
                Transform handle = FindChildByName(scrollbarRect, "Handle");
                scrollbar.handleRect = handle as RectTransform;
            }

            if (scrollbar.targetGraphic == null && scrollbar.handleRect != null)
            {
                scrollbar.targetGraphic = scrollbar.handleRect.GetComponent<Graphic>();
            }

            return scrollbar;
        }

        private static void ConfigureScrollbarRect(RectTransform scrollbarRect, float width)
        {
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(width, 0f);
        }

        private static Transform FindChildByName(Transform transform, string childName)
        {
            if (transform == null)
            {
                return null;
            }

            if (transform.name == childName)
            {
                return transform;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform found = FindChildByName(transform.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsOwnTransform(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name == WrapperName
                    || transform.name == ViewportName
                    || transform.name == ScrollbarName)
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static void ConfigureContentTransform(RectTransform content)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void CopyWidthLayout(LayoutElement source, LayoutElement target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.minWidth = source.minWidth;
            target.preferredWidth = source.preferredWidth;
            target.flexibleWidth = source.flexibleWidth;
            target.layoutPriority = source.layoutPriority;
        }

        private static float GetScrollSensitivity()
        {
            return Mathf.Max(1f, Main.Settings.ScrollSensitivity);
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static void DumpDiagnostics(
            CharBSelectionSwitchFeatures instance,
            RectTransform content,
            LevelUpSelectionScrollMarker marker)
        {
            if (!Main.Settings.DumpUiHierarchy)
            {
                return;
            }

            int maxRuns = Mathf.Clamp(Main.Settings.DumpUiHierarchyMaxRuns, 1, 20);
            if (s_DumpRunCount >= maxRuns)
            {
                return;
            }

            s_DumpRunCount++;

            Transform root = FindDiagnosticRoot(instance.transform);
            int maxDepth = Mathf.Clamp(Main.Settings.DumpUiHierarchyMaxDepth, 1, 30);
            int maxNodes = Mathf.Clamp(Main.Settings.DumpUiHierarchyMaxNodes, 50, 20000);

            StringBuilder builder = new StringBuilder(64 * 1024);
            builder.AppendLine("Diagnostic UI dump " + s_DumpRunCount + "/" + maxRuns + ".");
            builder.AppendLine("Root=" + GetPath(root) + ", maxDepth=" + maxDepth + ", maxNodes=" + maxNodes);
            builder.AppendLine("CollectionSwitcher=" + GetPath(instance.transform));
            builder.AppendLine("ItemsContainer=" + GetPath(content) + ", childCount=" + content.childCount);
            builder.AppendLine("Wrapper=" + GetPath(marker.Wrapper) + ", viewport=" + GetPath(marker.Viewport));

            AppendScrollComponentSummary(builder, root);

            builder.AppendLine("Hierarchy:");
            int nodeCount = 0;
            bool truncated = false;
            DumpHierarchy(builder, root, 0, maxDepth, maxNodes, ref nodeCount, ref truncated);
            builder.AppendLine("DumpedNodes=" + nodeCount + (truncated ? " (truncated)" : string.Empty));

            Logger.Info(builder.ToString());
        }

        private static Transform FindDiagnosticRoot(Transform transform)
        {
            Transform characterBuild = FindAncestorByName(transform, "CharacterBuild");
            if (characterBuild != null)
            {
                return characterBuild;
            }

            Transform abilitiesLeftSide = FindAncestorByName(transform, "AbilitiesLeftSide");
            if (abilitiesLeftSide != null)
            {
                return abilitiesLeftSide;
            }

            return transform;
        }

        private static Transform FindAncestorByName(Transform transform, string name)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == name)
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static void AppendScrollComponentSummary(StringBuilder builder, Transform root)
        {
            if (root == null)
            {
                return;
            }

            ScrollRect[] scrollRects = root.GetComponentsInChildren<ScrollRect>(true);
            Scrollbar[] scrollbars = root.GetComponentsInChildren<Scrollbar>(true);

            builder.AppendLine("ScrollRects found: " + scrollRects.Length);
            for (int i = 0; i < scrollRects.Length; i++)
            {
                ScrollRect scrollRect = scrollRects[i];
                builder.AppendLine(
                    "  [" + i + "] " + GetPath(scrollRect.transform)
                    + " active=" + scrollRect.gameObject.activeInHierarchy
                    + " vertical=" + scrollRect.vertical
                    + " horizontal=" + scrollRect.horizontal
                    + " content=" + GetPath(scrollRect.content)
                    + " viewport=" + GetPath(scrollRect.viewport)
                    + " vScrollbar=" + GetPath(scrollRect.verticalScrollbar != null ? scrollRect.verticalScrollbar.transform : null)
                    + " hScrollbar=" + GetPath(scrollRect.horizontalScrollbar != null ? scrollRect.horizontalScrollbar.transform : null));
            }

            builder.AppendLine("Scrollbars found: " + scrollbars.Length);
            for (int i = 0; i < scrollbars.Length; i++)
            {
                Scrollbar scrollbar = scrollbars[i];
                RectTransform rectTransform = scrollbar.transform as RectTransform;
                string rectInfo = rectTransform != null ? " rect=" + rectTransform.rect.size.ToString("F1") : string.Empty;
                builder.AppendLine(
                    "  [" + i + "] " + GetPath(scrollbar.transform)
                    + " active=" + scrollbar.gameObject.activeInHierarchy
                    + " direction=" + scrollbar.direction
                    + " handle=" + GetPath(scrollbar.handleRect)
                    + rectInfo);
            }
        }

        private static void DumpHierarchy(
            StringBuilder builder,
            Transform transform,
            int depth,
            int maxDepth,
            int maxNodes,
            ref int nodeCount,
            ref bool truncated)
        {
            if (transform == null || depth > maxDepth || truncated)
            {
                return;
            }

            if (nodeCount >= maxNodes)
            {
                truncated = true;
                builder.AppendLine(new string(' ', depth * 2) + "... truncated after " + maxNodes + " nodes ...");
                return;
            }

            nodeCount++;

            string indent = new string(' ', depth * 2);
            RectTransform rectTransform = transform as RectTransform;
            string rectInfo = rectTransform != null
                ? " rect=" + rectTransform.rect.size.ToString("F1")
                : string.Empty;

            builder.AppendLine(
                indent + transform.name
                + " [" + transform.GetType().Name + "]"
                + " active=" + transform.gameObject.activeInHierarchy
                + rectInfo
                + DescribeInterestingComponents(transform));

            if (depth == maxDepth)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                DumpHierarchy(builder, transform.GetChild(i), depth + 1, maxDepth, maxNodes, ref nodeCount, ref truncated);
            }
        }

        private static string DescribeInterestingComponents(Transform transform)
        {
            StringBuilder builder = new StringBuilder();
            GameObject gameObject = transform.gameObject;

            ScrollRect scrollRect = gameObject.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                builder.Append(" ScrollRect(v=").Append(scrollRect.vertical)
                    .Append(",h=").Append(scrollRect.horizontal)
                    .Append(",content=").Append(GetPath(scrollRect.content))
                    .Append(",viewport=").Append(GetPath(scrollRect.viewport))
                    .Append(")");
            }

            Scrollbar scrollbar = gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
            {
                builder.Append(" Scrollbar(direction=").Append(scrollbar.direction)
                    .Append(",handle=").Append(GetPath(scrollbar.handleRect))
                    .Append(")");
            }

            Image image = gameObject.GetComponent<Image>();
            if (image != null)
            {
                builder.Append(" Image(raycast=").Append(image.raycastTarget)
                    .Append(",sprite=").Append(image.sprite != null ? image.sprite.name : "<null>")
                    .Append(")");
            }

            RectMask2D rectMask = gameObject.GetComponent<RectMask2D>();
            if (rectMask != null)
            {
                builder.Append(" RectMask2D");
            }

            Mask mask = gameObject.GetComponent<Mask>();
            if (mask != null)
            {
                builder.Append(" Mask(showGraphic=").Append(mask.showMaskGraphic).Append(")");
            }

            LayoutGroup layoutGroup = gameObject.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                builder.Append(" LayoutGroup(").Append(layoutGroup.GetType().Name).Append(")");
            }

            ContentSizeFitter contentSizeFitter = gameObject.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                builder.Append(" ContentSizeFitter(h=").Append(contentSizeFitter.horizontalFit)
                    .Append(",v=").Append(contentSizeFitter.verticalFit)
                    .Append(")");
            }

            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                builder.Append(" LayoutElement(minH=").Append(layoutElement.minHeight.ToString("0.0"))
                    .Append(",prefH=").Append(layoutElement.preferredHeight.ToString("0.0"))
                    .Append(",flexH=").Append(layoutElement.flexibleHeight.ToString("0.0"))
                    .Append(")");
            }

            Toggle toggle = gameObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                builder.Append(" Toggle(interactable=").Append(toggle.interactable)
                    .Append(",isOn=").Append(toggle.isOn)
                    .Append(")");
            }

            Button button = gameObject.GetComponent<Button>();
            if (button != null)
            {
                builder.Append(" Button(interactable=").Append(button.interactable).Append(")");
            }

            return builder.ToString();
        }

        private struct RectTransformSnapshot
        {
            private Vector2 m_AnchorMin;
            private Vector2 m_AnchorMax;
            private Vector2 m_AnchoredPosition;
            private Vector2 m_SizeDelta;
            private Vector2 m_Pivot;
            private Vector3 m_LocalScale;
            private Quaternion m_LocalRotation;

            public static RectTransformSnapshot Capture(RectTransform rectTransform)
            {
                return new RectTransformSnapshot
                {
                    m_AnchorMin = rectTransform.anchorMin,
                    m_AnchorMax = rectTransform.anchorMax,
                    m_AnchoredPosition = rectTransform.anchoredPosition,
                    m_SizeDelta = rectTransform.sizeDelta,
                    m_Pivot = rectTransform.pivot,
                    m_LocalScale = rectTransform.localScale,
                    m_LocalRotation = rectTransform.localRotation
                };
            }

            public void ApplyTo(RectTransform rectTransform)
            {
                rectTransform.anchorMin = m_AnchorMin;
                rectTransform.anchorMax = m_AnchorMax;
                rectTransform.anchoredPosition = m_AnchoredPosition;
                rectTransform.sizeDelta = m_SizeDelta;
                rectTransform.pivot = m_Pivot;
                rectTransform.localScale = m_LocalScale;
                rectTransform.localRotation = m_LocalRotation;
            }
        }
    }

    public sealed class LevelUpSelectionScrollMarker : MonoBehaviour
    {
        public RectTransform Wrapper;
        public RectTransform Viewport;
        public ScrollRect ScrollRect;
        public Scrollbar VerticalScrollbar;
        public bool TemplateScrollbarApplied;
        public string TemplateScrollbarPath;
        public bool HasKnownScrollPosition;
        public float LastVerticalNormalizedPosition = 1f;
        public float LastLoggedContentHeight = -1f;
        public float LastLoggedViewportHeight = -1f;
        public int LastLoggedChildCount = -1;

        public bool IsValid
        {
            get
            {
                return Wrapper != null
                    && Viewport != null
                    && ScrollRect != null
                    && ScrollRect.content != null
                    && ScrollRect.viewport != null;
            }
        }
    }
}
