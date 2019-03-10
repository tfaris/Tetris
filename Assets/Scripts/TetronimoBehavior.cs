using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetronimoBehavior : MonoBehaviour
{
    public TetrisBoard Board { get; set; }
    public bool PieceSettled { get; set; }

    void Update()
    {
        if (!PieceSettled && BlockUtils.HitTest(transform, Vector3.down))
        {
            Debug.Log("HIT TEST DOWN");
            Board.PieceLanded = true;
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(BlockUtils.GetCenter(gameObject), 1);
    }
}
