using UnityEngine;

public static class BlockUtils
{

    public static Vector3 GetCenter(GameObject gameObject)
    {
        Vector3 center = new Vector3();
        foreach (Transform child in gameObject.transform)
        {
            center += child.position;
        }
        center.x /= gameObject.transform.childCount;
        center.y /= gameObject.transform.childCount;
        center.z /= gameObject.transform.childCount;
        return center;
    }
    
    public static Vector3 GetLowestPoint(GameObject gameObject)
    {
        Vector3 lowestBlock = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        foreach (Transform block in gameObject.transform)
        {
            Vector3 blockMin = block.GetComponent<Renderer>().bounds.min;
            if (blockMin.y < lowestBlock.y)
            {
                lowestBlock = blockMin;
            }
        }
        return lowestBlock;
    }

    public static Bounds GetBounds(GameObject gameObject)
    {
        Bounds bounds;
        Renderer childRender;
        bounds = GetRenderBounds(gameObject);
        if (bounds.extents.x == 0)
        {
            bounds = new Bounds(gameObject.transform.position, Vector3.zero);
            foreach (Transform child in gameObject.transform)
            {
                childRender = child.GetComponent<Renderer>();
                if (childRender)
                {
                    bounds.Encapsulate(childRender.bounds);
                }
                else
                {
                    bounds.Encapsulate(GetBounds(child.gameObject));
                }
            }
        }
        return bounds;
    }

    public static Bounds GetRenderBounds(GameObject gameObject)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        Renderer render = gameObject.GetComponent<Renderer>();
        if (render != null)
        {
            return render.bounds;
        }
        return bounds;
    }
    
    public static bool HitTest(Transform castFrom, Vector3 direction)
    {
        foreach (Transform block in castFrom)
        {
            //Debug.Log(block.name);
            RaycastHit[] hits = Physics.RaycastAll(
                block.position,
                direction,
                1
            );
            foreach (var h in hits)
            {
                //Debug.Log(h.collider.gameObject.name);
                if (!h.collider.gameObject.transform.IsChildOf(castFrom))
                {
                    return true;
                }
            }
        }
        return false;
    }
}