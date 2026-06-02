using System.Collections.Generic;
using DungeonInn.Application.Economy;
using DungeonInn.Application.World;
using TMPro;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class InnStatusPanelView : MonoBehaviour
    {
        const int GuestDisplayLimit = 5;

        [SerializeField] GameObject[] guestRows;
        [SerializeField] TMP_Text[] guestNameTexts;
        [SerializeField] TMP_Text[] guestHpTexts;
        [SerializeField] TMP_Text[] guestRecoveryTexts;
        [SerializeField] TMP_Text emptyGuestText;
        [SerializeField] TMP_Text dayText;
        [SerializeField] TMP_Text salesText;
        [SerializeField] TMP_Text guestCountText;
        [SerializeField] TMP_Text occupancyText;
        [SerializeField] TMP_Text guildGoldText;

        public void SetGuests(IReadOnlyList<InnGuestSummary> guests)
        {
            var count = guests?.Count ?? 0;
            if (emptyGuestText != null)
            {
                emptyGuestText.gameObject.SetActive(count == 0);
            }

            for (var i = 0; i < GuestDisplayLimit; i++)
            {
                var isVisible = i < count;
                if (TryGetGuestRow(i, out var guestRow))
                {
                    guestRow.SetActive(isVisible);
                }

                if (!isVisible || guests == null)
                {
                    continue;
                }

                var guest = guests[i];
                SetText(guestNameTexts, i, guest.Name);
                SetText(guestHpTexts, i, $"HP {Mathf.RoundToInt(guest.HpRatio * 100f)}%");
                SetText(guestRecoveryTexts, i, FormatRecoveryTime(guest.RecoveryRemainingSeconds));
            }
        }

        public void SetEconomySummary(InnEconomyStatus status)
        {
            SetText(dayText, $"Day {status.CurrentDay + 1}");
            SetText(salesText, $"Sales {status.Current.Sales:N0} G");
            SetText(guestCountText, $"Guests {status.Current.Guests} / Rejected {status.Current.RejectedGuests}");
            SetText(occupancyText, $"Rooms {status.Current.OccupiedRooms}/{status.Current.RoomCapacity} ({status.Current.OccupancyPercent}%)");
            SetText(guildGoldText, $"Guild Gold {status.Current.GuildGold:N0} G");
        }

        static string FormatRecoveryTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return "Recovery --";
            }

            var minutes = Mathf.CeilToInt(seconds / 60f);
            return $"Recovery {minutes}m";
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        static void SetText(TMP_Text[] texts, int index, string value)
        {
            if (texts == null || texts.Length <= index || texts[index] == null)
            {
                return;
            }

            texts[index].text = value;
        }

        bool TryGetGuestRow(int index, out GameObject guestRow)
        {
            guestRow = null;
            if (guestRows == null || guestRows.Length <= index)
            {
                return false;
            }

            guestRow = guestRows[index];
            return guestRow != null;
        }
    }
}

