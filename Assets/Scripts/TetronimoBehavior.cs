using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetronimoBehavior : MonoBehaviour
{
    public TetrisBoard Board { get; set; }
    public bool PieceSettled { get; set; }

    public bool Active { get;set; }

    void Update()
    {
        if (Active && !PieceSettled)
        {
            var objectDown = BlockUtils.HitTestObjectChildren(transform, Vector3.down);
            if (objectDown != null)
            {
                Boundary boundary = objectDown.GetComponent<Boundary>();
                if (boundary == null || boundary.Type == Boundary.BoundaryType.Floor)
                {
                    Board.PieceLanded = true;
                }
                else
                {
                    Board.PieceLanded = false;
                }
            }
            else
            {
                Board.PieceLanded = false;
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(BlockUtils.GetCenter(gameObject), 1);
    }
}
