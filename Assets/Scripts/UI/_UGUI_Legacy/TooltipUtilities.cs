// File: Assets/Scripts/UI/_UGUI_Legacy/TooltipUtilities.cs
using UnityEngine;

namespace Abracodabra.UI.Tooltips
{
    /// <summary>
    /// Calculates quality tier for seeds based on their stats.
    /// Used by UISpecSheetController to display quality ratings.
    /// </summary>
    public static class SeedQualityCalculator
    {
        public enum QualityTier { Trash, Poor, Common, Good, Excellent, Legendary }

        public static QualityTier CalculateQuality(SeedTooltipData data)
        {
            if (data == null) return QualityTier.Common;

            float score = 0f;

            // Energy efficiency score
            float efficiencyScore = Mathf.Clamp01((data.energySurplusPerCycle + 10) / 50f);
            score += efficiencyScore * 30f;

            // Maturity speed score
            float maturityScore = 1f - Mathf.Clamp01(data.estimatedMaturityTicks / 100f);
            score += maturityScore * 25f;

            // Yield score
            score += Mathf.Clamp01((data.fruitYieldMultiplier - 1f) / 1.5f) * 25f;

            // Defense score
            score += Mathf.Clamp01(data.defenseMultiplier) * 20f;

            // Penalty for warnings
            if (data.warnings != null)
            {
                score -= data.warnings.Count * 10f;
            }

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
                case QualityTier.Legendary: return new Color(1f, 0.6f, 0.1f);   // Orange
                case QualityTier.Excellent: return new Color(0.2f, 0.6f, 1f);   // Blue
                case QualityTier.Good: return new Color(0.2f, 0.8f, 0.2f);      // Green
                case QualityTier.Common: return Color.white;
                case QualityTier.Poor: return new Color(0.6f, 0.6f, 0.6f);      // Gray
                case QualityTier.Trash: return new Color(0.4f, 0.2f, 0.2f);     // Dark red
                default: return Color.grey;
            }
        }
    }
}