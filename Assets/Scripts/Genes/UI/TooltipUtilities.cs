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
        
        public static string ColorizeValue(float value, float baseline, string text, bool higherIsBetter = true)
        {
            Color goodColor = new Color(0.5f, 1f, 0.5f); // Light Green
            Color badColor = new Color(1f, 0.5f, 0.5f);  // Light Red
            
            bool isGood = higherIsBetter ? (value > baseline) : (value < baseline);
            
            if (Mathf.Approximately(value, baseline))
                return text;

            Color finalColor = isGood ? goodColor : badColor;
            return $"<color=#{ColorUtility.ToHtmlStringRGB(finalColor)}>{text}</color>";
        }
    }

    public static class SeedQualityCalculator
    {
        public enum QualityTier { Trash, Poor, Common, Good, Excellent, Legendary }

        public static QualityTier CalculateQuality(SeedTooltipData data)
        {
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
            score -= data.warnings.Count * 10f;

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