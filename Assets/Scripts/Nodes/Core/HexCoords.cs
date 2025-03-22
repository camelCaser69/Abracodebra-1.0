using UnityEngine;

[System.Serializable]
public struct HexCoords
{
    public int q; // axial coordinate (column)
    public int r; // axial coordinate (row)

    public HexCoords(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    // Flat top conversion from axial to world coordinates.
    // Using the formula:
    // x = hexSize * (3/2 * q)
    // y = hexSize * ( (sqrt(3)/2 * q) + (sqrt(3) * r) )
    public Vector2 HexToWorld(float hexSize)
    {
        float x = hexSize * (3f / 2f * q);
        float y = hexSize * ((Mathf.Sqrt(3f) / 2f * q) + (Mathf.Sqrt(3f) * r));
        return new Vector2(x, y);
    }

    // Converts a world (UI) position to axial coordinates.
    // Inverse formulas:
    // q = (2/3 * x)/hexSize
    // r = ((-1/3 * x) + (sqrt(3)/3 * y)) / hexSize
    public static HexCoords WorldToHex(Vector2 pos, float hexSize)
    {
        float qf = (2f / 3f * pos.x) / hexSize;
        float rf = ((-1f / 3f * pos.x) + (Mathf.Sqrt(3f) / 3f * pos.y)) / hexSize;
        return RoundAxial(qf, rf);
    }

    public static HexCoords RoundAxial(float qf, float rf)
    {
        float sf = -qf - rf;
        int qi = Mathf.RoundToInt(qf);
        int ri = Mathf.RoundToInt(rf);
        int si = Mathf.RoundToInt(sf);

        float qDiff = Mathf.Abs(qi - qf);
        float rDiff = Mathf.Abs(ri - rf);
        float sDiff = Mathf.Abs(si - sf);

        if (qDiff > rDiff && qDiff > sDiff)
            qi = -ri - si;
        else if (rDiff > sDiff)
            ri = -qi - si;
        return new HexCoords(qi, ri);
    }

    // Fixed neighbor offsets (axial) for flat top grid.
    private static readonly HexCoords[] AxialNeighbors = new HexCoords[] {
        new HexCoords(+1, 0),   // Side1
        new HexCoords(0, -1),   // Side2
        new HexCoords(-1, -1),  // Side3
        new HexCoords(-1, 0),   // Side4
        new HexCoords(0, +1),   // Side5
        new HexCoords(+1, +1)   // Side6
    };

    // Returns the neighbor hex coordinate for a given side index (0 to 5)
    public static HexCoords GetNeighbor(HexCoords origin, int sideIndex)
    {
        return new HexCoords(origin.q + AxialNeighbors[sideIndex].q,
                             origin.r + AxialNeighbors[sideIndex].r);
    }

    public static int OppositeSideIndex(int sideIndex)
    {
        return (sideIndex + 3) % 6;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is HexCoords))
            return false;
        HexCoords other = (HexCoords)obj;
        return this.q == other.q && this.r == other.r;
    }

    public override int GetHashCode()
    {
        unchecked { return (q * 397) ^ r; }
    }
}
