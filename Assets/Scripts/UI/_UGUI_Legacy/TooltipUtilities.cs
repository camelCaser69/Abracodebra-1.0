using UnityEngine;
using Abracodabra.Genes.Core;
using System.Collections.Generic;

namespace Abracodabra.UI.Tooltips
{
    public static class PassiveStatTypeExtensions
    {
        public static string GetDisplayName(this PassiveStatType stat)
        {
            switch (stat)
            {
                case PassiveStatType.None: return "Special"; // Kept new change
                case PassiveStatType.GrowthSpeed: return "Growth Speed";
                case PassiveStatType.EnergyGeneration: return "Energy Generation";
                case PassiveStatType.EnergyStorage: return "Energy Storage";
                case PassiveStatType.FruitYield: return "Fruit Yield";
                case PassiveStatType.Defense: return "Defense";
                default: return stat.ToString();
            }
        }
    }

    public static class TooltipFormatting
    {
        public static string FormatPercentage(float value, bool showSign = true)
        {
            float percentage = (value - 1f) * 100f;
            string sign = percentage >= 0 && showSign ? "+" : "";
            return $"{sign}{percentage:F0}%";
        }

        // Restored method required by SeedEditorTooltipPanel
        public static string ColorizeValue(float value, float baseline, string text, bool higherIsBetter = true)
        {
            Color goodColor = new Color(0.5f, 1f, 0.5f); // Light Green
            Color badColor = new Color(1f, 0.5f, 0.5f);  // Light Red
            
            bool isGood = higherIsBetter ? (value > baseline) : (value < baseline);
            
            // If roughly equal, return white/default text
            if (Mathf.Approximately(value, baseline))
                return text;

            Color finalColor = isGood ? goodColor : badColor;
            return $"<color=#{ColorUtility.ToHtmlStringRGB(finalColor)}>{text}</color>";
        }

        // Kept new method from friend's update
        public static string GetColorForValue(float value)
        {
            if (value > 1f) return "#90EE90"; // Light green for buffs
            if (value < 1f) return "#FF6B6B"; // Light red for debuffs
            return "#FFFFFF"; // White for neutral
        }

        // Kept new method from friend's update
        public static string FormatStatChange(PassiveStatType stat, float value)
        {
            if (stat == PassiveStatType.None) return ""; // No stat change to display
            
            string color = GetColorForValue(value);
            string statName = stat.GetDisplayName();
            string change = FormatPercentage(value);
            return $"<color={color}>{change} {statName}</color>";
        }
    }

    // Restored class required by UISpecSheetController, InventoryTooltipPanel, etc.
    public static class SeedQualityCalculator
    {
        public enum QualityTier { Trash, Poor, Common, Good, Excellent, Legendary }

        public static QualityTier CalculateQuality(SeedTooltipData data)
        {
            if (data == null) return QualityTier.Common;

            float score = 0f;

            // Energy Surplus (0-30 points)
            float efficiencyScore = Mathf.Clamp01((data.energySurplusPerCycle + 10) / 50f); // Normalize around a surplus of 40 being max
            score += efficiencyScore * 30f;
            
            // Growth Speed (0-25 points)
            float maturityScore = 1f - Mathf.Clamp01(data.estimatedMaturityTicks / 100f); // 100 ticks is slow
            score += maturityScore * 25f;

            // Fruit Yield (0-25 points)
            score += Mathf.Clamp01((data.fruitYieldMultiplier - 1f) / 1.5f) * 25f; // 150% bonus is max
            
            // Defense (0-20 points)
            score += Mathf.Clamp01(data.defenseMultiplier) * 20f;
            
            // Penalize for warnings
            if (data.warnings != null)
            {
                score -= data.warnings.Count * 10f;
            }

            // Determine tier
            if (score >= 90) return QualityTier.Legendary;
            if (score >= 70) return QualityTier.Excellent;
            if (score >= 50) return QualityTier.Good;
            if (score >= 30) return QualityTier.Common;
            if (score >= 15) return QualityTier.Poor;
            return QualityTier.Trash;
        }

        public static string GetQualityDescription(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Legendary: return "★★★★★ Legendary";
                case QualityTier.Excellent: return "★★★★☆ Excellent";
                case QualityTier.Good: return "★★★☆☆ Good";
                case QualityTier.Common: return "★★☆☆☆ Common";
                case QualityTier.Poor: return "★☆☆☆☆ Poor";
                case QualityTier.Trash: return "☆☆☆☆☆ Trash";
                default: return "Unknown";
            }
        }
        
        public static Color GetQualityColor(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Legendary: return new Color(1f, 0.6f, 0.1f);
                case QualityTier.Excellent: return new Color(0.2f, 0.6f, 1f);
                case QualityTier.Good: return new Color(0.2f, 0.8f, 0.2f);
                case QualityTier.Common: return Color.white;
                case QualityTier.Poor: return new Color(0.6f, 0.6f, 0.6f);
                case QualityTier.Trash: return new Color(0.4f, 0.2f, 0.2f);
                default: return Color.grey;
            }
        }
    }
}