// File: Assets/Scripts/UI/ITooltipDataProvider.cs
using UnityEngine;

/// <summary>
/// Interface for objects that can provide tooltip data.
/// Implemented by GeneBase, ToolDefinition, and other ScriptableObjects.
/// </summary>
public interface ITooltipDataProvider
{
    string GetTooltipTitle();
    string GetTooltipDescription();
    string GetTooltipDetails(object source = null);
}