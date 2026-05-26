using System.Collections.Generic;
using System.Text;
using LighthouseExtends.ScreenStack;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GuildManagementWindow :
        GameHudScreenStackWindowBase,
        IScreenStackSetup<GuildManagementWindowData>
    {
        public void Setup(GuildManagementWindowData screenStackData)
        {
            ApplyContent(
                "Guild Management",
                Format(screenStackData.ViewData));
            Debug.Log(
                $"[GameHUD.ScreenStack] GuildManagementWindow displayed. " +
                $"Facilities={screenStackData.ViewData.Facilities.Count} Inventory={screenStackData.ViewData.Inventory.Count}");
        }

        static string Format(GuildManagementWindowViewData viewData)
        {
            var builder = new StringBuilder();
            AppendSection(builder, "KPI", viewData.Kpis);
            AppendSection(builder, "Facilities", viewData.Facilities);
            AppendSection(builder, "Combined Inventory", viewData.Inventory);
            AppendSection(builder, "Transactions", viewData.Transactions);
            return builder.ToString();
        }

        static void AppendSection(StringBuilder builder, string title, IReadOnlyList<string> lines)
        {
            builder.AppendLine(title);
            if (lines.Count == 0)
            {
                builder.AppendLine("- None");
                builder.AppendLine();
                return;
            }

            foreach (var line in lines)
            {
                builder.AppendLine($"- {line}");
            }

            builder.AppendLine();
        }
    }
}
