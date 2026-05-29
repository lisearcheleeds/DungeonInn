using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using TMPro;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class SelectedActorInspectorView : MonoBehaviour
    {
        static readonly Color HpFillColor = new(0.70f, 0.20f, 0.18f, 1f);
        static readonly Color MpFillColor = new(0.18f, 0.36f, 0.72f, 1f);

        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text roleText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text lifecycleText;
        [SerializeField] TMP_Text goalText;
        [SerializeField] TMP_Text conditionText;
        [SerializeField] TMP_Text goldText;
        [SerializeField] ValueGaugeView hpGauge;
        [SerializeField] ValueGaugeView mpGauge;
        [SerializeField] GameObject[] statRows;
        [SerializeField] TMP_Text[] statRowTexts;
        [SerializeField] GameObject[] equipmentRows;
        [SerializeField] TMP_Text[] equipmentRowTexts;
        [SerializeField] GameObject[] inventoryRows;
        [SerializeField] TMP_Text[] inventoryRowTexts;
        [SerializeField] GameObject[] effectRows;
        [SerializeField] TMP_Text[] effectRowTexts;

        void Awake()
        {
            ValidateSerializedFields();
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void Clear()
        {
            nameText.text = string.Empty;
            roleText.text = string.Empty;
            levelText.text = string.Empty;
            goalText.text = string.Empty;
            conditionText.text = string.Empty;
            goldText.text = string.Empty;
            hpGauge.SetValue(0, 0);
            mpGauge.SetValue(0, 0);
            SetRows(statRows, statRowTexts, Array.Empty<SelectedActorInspectorStatLineViewData>(), FormatStatLine);
            SetRows(equipmentRows, equipmentRowTexts, Array.Empty<SelectedActorInspectorEquipmentLineViewData>(), FormatEquipmentLine);
            SetRows(inventoryRows, inventoryRowTexts, Array.Empty<SelectedActorInspectorItemViewData>(), FormatInventoryLine);
            SetRows(effectRows, effectRowTexts, Array.Empty<SelectedActorInspectorEffectLineViewData>(), FormatEffectLine);
        }

        public void SetContent(SelectedActorInspectorViewData viewData)
        {
            if (viewData == null)
            {
                Clear();
                return;
            }

            levelText.text = $"Lv.{viewData.Level}";
            nameText.text = viewData.DisplayName;
            roleText.text = $"{viewData.ActorKindText} / {viewData.RoleText}";
            lifecycleText.text = viewData.LifecycleText;
            goalText.text = $"Goal {viewData.GoalText}";
            conditionText.text = $"Fatigue {viewData.Fatigue} / Injury {viewData.InjurySeverity}";
            goldText.text = $"Gold {viewData.Gold:N0} G";
            hpGauge.SetValue(viewData.CurrentHp, viewData.MaxHp);
            mpGauge.SetValue(viewData.CurrentMp, viewData.MaxMp);
            SetRows(statRows, statRowTexts, viewData.Stats, FormatStatLine);
            SetRows(equipmentRows, equipmentRowTexts, viewData.Equipment, FormatEquipmentLine);
            SetRows(inventoryRows, inventoryRowTexts, viewData.InventoryItems, FormatInventoryLine);
            SetRows(effectRows, effectRowTexts, viewData.ActiveEffects, FormatEffectLine);
        }

        static void SetRows<TViewData>(
            GameObject[] rows,
            TMP_Text[] rowTexts,
            IReadOnlyList<TViewData> values,
            Func<TViewData, string> format)
        {
            var count = values?.Count ?? 0;
            var visibleCount = Mathf.Min(count, rows?.Length ?? 0, rowTexts?.Length ?? 0);
            if (rows == null || rowTexts == null)
            {
                return;
            }

            for (var index = 0; index < (rows?.Length ?? 0); index++)
            {
                var isVisible = index < visibleCount;
                if (rows[index] == null)
                {
                    continue;
                }

                rows[index].SetActive(isVisible);
                if (!isVisible || index >= rowTexts.Length || rowTexts[index] == null)
                {
                    continue;
                }

                if (index == visibleCount - 1 && visibleCount < count)
                {
                    rowTexts[index].text = $"... and {count - visibleCount + 1} more";
                }
                else
                {
                    rowTexts[index].text = format(values[index]);
                }
            }
        }

        void ValidateSerializedFields()
        {
            Debug.Assert(nameText != null, $"{nameof(nameText)} is not assigned.", this);
            Debug.Assert(roleText != null, $"{nameof(roleText)} is not assigned.", this);
            Debug.Assert(levelText != null, $"{nameof(levelText)} is not assigned.", this);
            Debug.Assert(goalText != null, $"{nameof(goalText)} is not assigned.", this);
            Debug.Assert(conditionText != null, $"{nameof(conditionText)} is not assigned.", this);
            Debug.Assert(goldText != null, $"{nameof(goldText)} is not assigned.", this);
            Debug.Assert(hpGauge != null, $"{nameof(hpGauge)} is not assigned.", this);
            Debug.Assert(mpGauge != null, $"{nameof(mpGauge)} is not assigned.", this);
            ValidateRows(statRows, statRowTexts, nameof(statRows));
            ValidateRows(equipmentRows, equipmentRowTexts, nameof(equipmentRows));
            ValidateRows(inventoryRows, inventoryRowTexts, nameof(inventoryRows));
            ValidateRows(effectRows, effectRowTexts, nameof(effectRows));
            hpGauge.SetLabel("HP");
            hpGauge.SetFillColor(HpFillColor);
            mpGauge.SetLabel("MP");
            mpGauge.SetFillColor(MpFillColor);
        }

        void ValidateRows(GameObject[] rows, TMP_Text[] rowTexts, string fieldName)
        {
            Debug.Assert(rows != null, $"{fieldName} is not assigned.", this);
            Debug.Assert(rowTexts != null, $"{fieldName} texts are not assigned.", this);
            Debug.Assert(rows == null || rowTexts == null || rows.Length == rowTexts.Length,
                $"{fieldName} and text arrays must have the same length.",
                this);
        }

        static string FormatStatLine(SelectedActorInspectorStatLineViewData stat)
        {
            return $"{stat.Label} {stat.Value}";
        }

        static string FormatEquipmentLine(SelectedActorInspectorEquipmentLineViewData equipment)
        {
            return $"{equipment.SlotText}: {equipment.ItemName}";
        }

        static string FormatInventoryLine(SelectedActorInspectorItemViewData item)
        {
            return $"{item.ItemName} x{item.Count}";
        }

        static string FormatEffectLine(SelectedActorInspectorEffectLineViewData effect)
        {
            return $"{effect.DisplayName} {Mathf.CeilToInt(effect.RemainingSeconds)}s";
        }
    }
}
