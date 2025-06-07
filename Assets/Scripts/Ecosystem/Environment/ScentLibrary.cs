// FILE: Assets/Scripts/Ecosystem/Scents/ScentLibrary.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ScentLibrary", menuName = "Ecosystem/Scent Library")]
public class ScentLibrary : ScriptableObject
{
    public List<ScentDefinition> scents;

    // Helper method to find a scent by its ID (still potentially useful)
    public ScentDefinition GetScentByID(string id)
    {
        if (string.IsNullOrEmpty(id) || scents == null) return null;
        return scents.FirstOrDefault(s => s != null && s.scentID == id);
    }

    // Helper to get the actual list of definitions
    public List<ScentDefinition> GetAllDefinitions()
    {
        // Return a copy or filter out nulls
        return scents?.Where(s => s != null).ToList() ?? new List<ScentDefinition>();
    }

}