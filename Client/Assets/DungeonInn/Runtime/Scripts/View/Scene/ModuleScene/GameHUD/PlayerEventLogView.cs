using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class PlayerEventLogView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
    {
        public const int LineCountPublic = 10;
        const int LineCount = LineCountPublic;
        const float LogDisplaySeconds = 3f;
        const float LogFadeSeconds = 1f;

        [SerializeField] TextMeshProUGUI[] logLines;

        readonly float[] lineTimestamps = new float[LineCount];
        readonly string[] lineTexts = new string[LineCount];
        int scrollOffset;
        bool isMouseOver;

        void Awake()
        {
            for (var i = 0; i < LineCount; i++)
            {
                lineTimestamps[i] = -LogDisplaySeconds;
                lineTexts[i] = string.Empty;
            }
        }

        public void AddEntry(string text)
        {
            for (var i = 0; i < LineCount - 1; i++)
            {
                lineTexts[i] = lineTexts[i + 1];
                lineTimestamps[i] = lineTimestamps[i + 1];
            }
            lineTexts[LineCount - 1] = text;
            lineTimestamps[LineCount - 1] = Time.unscaledTime;
        }

        public void SetEntries(IReadOnlyList<string> entries)
        {
            for (var i = 0; i < LineCount; i++)
            {
                lineTexts[i] = string.Empty;
                lineTimestamps[i] = -LogDisplaySeconds;
            }

            var startLine = LineCount - entries.Count;
            if (startLine < 0)
            {
                startLine = 0;
            }

            var entryStart = entries.Count - LineCount;
            if (entryStart < 0)
            {
                entryStart = 0;
            }

            for (var i = startLine; i < LineCount; i++)
            {
                var entryIndex = entryStart + (i - startLine);
                if (entryIndex < entries.Count)
                {
                    lineTexts[i] = entries[entryIndex];
                }
            }
        }

        public void ScrollUp()
        {
            scrollOffset++;
        }

        public void ScrollDown()
        {
            if (0 < scrollOffset)
            {
                scrollOffset--;
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            isMouseOver = true;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            isMouseOver = false;
            scrollOffset = 0;
        }

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (!isMouseOver)
            {
                return;
            }

            if (0f < eventData.scrollDelta.y)
            {
                ScrollUp();
            }
            else if (eventData.scrollDelta.y < 0f)
            {
                ScrollDown();
            }
        }

        void Update()
        {
            if (logLines == null || logLines.Length < LineCount)
            {
                return;
            }

            for (var i = 0; i < LineCount; i++)
            {
                var sourceIndex = i + scrollOffset;
                if (LineCount <= sourceIndex)
                {
                    sourceIndex = LineCount - 1;
                }

                var line = logLines[i];
                if (line == null)
                {
                    continue;
                }

                line.text = lineTexts[sourceIndex];

                if (isMouseOver)
                {
                    SetAlpha(line, 1f);
                    continue;
                }

                var elapsed = Time.unscaledTime - lineTimestamps[sourceIndex];
                if (elapsed < LogDisplaySeconds)
                {
                    SetAlpha(line, 1f);
                }
                else
                {
                    var fadeProgress = (elapsed - LogDisplaySeconds) / LogFadeSeconds;
                    SetAlpha(line, Mathf.Clamp01(1f - fadeProgress));
                }
            }
        }

        static void SetAlpha(TextMeshProUGUI label, float alpha)
        {
            var color = label.color;
            color.a = alpha;
            label.color = color;
        }
    }
}
