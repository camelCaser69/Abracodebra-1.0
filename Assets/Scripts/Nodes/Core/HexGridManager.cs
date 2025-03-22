using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    public float hexSize = 50f;
    public float pinRadiusMultiplier = 1.0f; // Pin radius = hexSize * multiplier

    // Flat top hexagon grid only – no orientation toggle needed.
    public static HexGridManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }
}