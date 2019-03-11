using UnityEngine;

public class Boundary : MonoBehaviour
{
    public enum BoundaryType
    {
        Wall,
        Floor,
        Ceiling
    }

    public BoundaryType Type { get; set; } = BoundaryType.Wall;
}
