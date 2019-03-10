using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetronimoBehavior : MonoBehaviour
{
    public TetrisBoard Board { get; set; }

    void OnCollisionEnter(Collision c)
    {
        ContactPoint[] contacts = new ContactPoint[c.contactCount];
        c.GetContacts(contacts);
        Bounds myBounds = BlockUtils.GetBounds(gameObject),
               otherBounds = BlockUtils.GetBounds(c.gameObject);

        foreach (ContactPoint cp in contacts)
        {
            if (cp.normal == Vector3.up)
            {
                //Board.NextTetronimo();
                Board.PieceLanded = true;
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(BlockUtils.GetCenter(gameObject), 1);
    }
}
