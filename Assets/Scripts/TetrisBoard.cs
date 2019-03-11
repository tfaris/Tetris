using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;


public class TetrisBoard : MonoBehaviour
{
    public int width, height;
    int gridSize = 1;
    public List<GameObject> tetronimoChoices;
    List<GameObject> onboard;
    List<GameObject> looseBlocks, floatBlocks;
    
    GameObject _active;
    float _movementTimePassed, _inputTimePassed, _looseBlockPassed;
    float _movementThresh = 1f;
    float _locktimePassed = 0;
    // Number of frames before a piece is locked
    // into place.
    float _lockDelay = .5f;
    public bool PieceLanded { get; set; }
    Bounds _boardBounds;

    public Mesh gridItemMesh;
    public Material gridItemMaterial1;
    public Material gridItemMaterial2;

    // Start is called before the first frame update
    void Start()
    {
        onboard = new List<GameObject>();
        looseBlocks = new List<GameObject>();
        floatBlocks = new List<GameObject>();
        
        //_boardBounds = GetComponent<Renderer>().bounds;
        _boardBounds = new Bounds(
            transform.position,
            new Vector3(
                width,
                height,
                0
            )
        );
        
        // min is bottom left
        Vector3 start = _boardBounds.min;
        for (int i=0; i < width + 2; i++)
        {
            for (int j=0; j < height + 2; j++)
            {
                if (i == 0 || i == width + 1 || j == 0 || j == height + 1)
                {
                    GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Boundary boundaryComp = boundary.AddComponent<Boundary>();
                    if (j == 0)
                    {
                        boundaryComp.Type = Boundary.BoundaryType.Floor;
                    }
                    else if (j == height + 1)
                    {
                        boundaryComp.Type = Boundary.BoundaryType.Ceiling;
                    }
                    else
                    {
                        boundaryComp.Type = Boundary.BoundaryType.Wall;
                    }
                    boundary.name = "Boundary";
                    boundary.transform.parent = this.gameObject.transform;
                    boundary.transform.position = new Vector3(
                        start.x + i +.5f,
                        start.y + j + .5f,
                        .5f
                    );
                    boundary.transform.localScale = new Vector3(1, 1, 2);
                    boundary.AddComponent<BoxCollider>();
                    var renderer = boundary.GetComponent<MeshRenderer>();
                    renderer.material = gridItemMaterial1;
                }
                else
                {
                    GameObject gridPanel = new GameObject();
                    gridPanel.transform.parent = this.gameObject.transform;
                    gridPanel.transform.position = new Vector3(
                        start.x + i +.5f,
                        start.y + j + .5f,
                        1
                    );
                    var meshFilter = gridPanel.AddComponent<MeshFilter>();
                    meshFilter.mesh = gridItemMesh;
                    var renderer = gridPanel.AddComponent<MeshRenderer>();
                    if ((i + j) % 2 == 0){
                        renderer.material = gridItemMaterial1;
                    }
                    else {
                        renderer.material = gridItemMaterial2;
                    }
                }
            }
        }
    }

    //
    // Locks the current piece into place and allows
    // for the next piece to be added.
    //
    public void NextTetronimo()
    {
        _locktimePassed += Time.deltaTime;
        if (_locktimePassed >= _lockDelay)
        {
            CheckLines();
            // The piece is finally in its place.
            _active.GetComponent<TetronimoBehavior>().PieceSettled = true;
            _active = null;
            _locktimePassed = 0;
        }
    }

    void CheckLines()
    {
        bool cleared = false;
        Dictionary<float, List<Transform>> rowBlockCounts = 
            new Dictionary<float, List<Transform>>();
        
        List<Transform> allBlocks = new List<Transform>();
        foreach (GameObject tetronimo in onboard)
        {
            foreach (Transform tBlock in tetronimo.transform)
            {
                allBlocks.Add(tBlock);
            }
        }
        foreach (GameObject go in looseBlocks)
        {
            if (!allBlocks.Contains(go.transform)){
                allBlocks.Add(go.transform);
            }
        }

        foreach (Transform tBlock in allBlocks)
        {
            int row = Mathf.FloorToInt(tBlock.transform.position.y);
            List<Transform> rowCount;
            if (!rowBlockCounts.TryGetValue(row, out rowCount)){
                rowBlockCounts[row] = rowCount = new List<Transform>();
            }
            rowCount.Add(tBlock);
        }

        foreach (var kvp in rowBlockCounts)
        {
            if (kvp.Value.Count == width)
            {
                foreach (Transform t in kvp.Value)
                {
                    //Rigidbody rb = t.GetComponentsInParent<Rigidbody>()[0];
                    // rb.constraints = RigidbodyConstraints.None;
                    // rb.AddExplosionForce(
                    //     50,
                    //     new Vector3(0, kvp.Key, 0),
                    //     1
                    // );
                    if (t.parent == null)
                    {
                        continue;
                    }
                    foreach (Transform leftoverBlock in t.parent)
                    {
                        if (leftoverBlock != t) {
                            leftoverBlock.parent = null;
                            looseBlocks.Add(leftoverBlock.gameObject);
                        }
                    }
                    t.parent = null;
                    Destroy(t.gameObject);
                    cleared = true;
                }
            }
        }
        if (cleared)
        {
            UpdateFloatingBlocks();
        }
    }
    
    void UpdateFloatingBlocks()
    {
        foreach (GameObject block in looseBlocks)
        {
            if (!BlockUtils.HitTest(
                block.transform,
                Vector3.down,
                1
            ))
            {
                floatBlocks.Add(block);
            }
        }
    }

    void AddTetronimo()
    {
        PieceLanded = false;
        GameObject prefabTmo = tetronimoChoices[UnityEngine.Random.Range(0, tetronimoChoices.Count)];
        _active = Instantiate(
            prefabTmo,
            Vector3.zero,
            Quaternion.identity
        );
        _active.transform.parent = transform;
        _active.transform.Translate(-.5f - 2, -.5f + (height / 2), 0);
        onboard.Add(_active);
        _active.GetComponent<TetronimoBehavior>().Board = this;
        _active.name = "tmo" + UnityEngine.Random.Range(0, 500).ToString();

        foreach (Transform block in _active.transform)
        {
            block.gameObject.AddComponent<BoxCollider>();
            block.name = "block" + UnityEngine.Random.Range(0, 500).ToString();
        }
    }

    void DoRotation(GameObject obj)
    {
        obj.transform.Rotate(Vector3.forward, -90f);
        WallKick(obj);
        // TODO: Prevent rotation into other pieces.
        // RaycastHit hit;
        // if (gameObject.GetComponent<Rigidbody>().SweepTest(Vector3.zero, out hit, 0))
        // {
            
        // }
    }

    void WallKick(GameObject obj)
    {
        var bounds = BlockUtils.GetBounds(obj);
        float correctionX = 0, correctionY = 0;
        if (bounds.min.x < _boardBounds.min.x)
        {
            // Over the left edge
            correctionX = _boardBounds.min.x - bounds.min.x;
        }
        if (bounds.max.x > _boardBounds.max.x)
        {
            // Over the right edge
            correctionX = _boardBounds.max.x - bounds.max.x;
        }
        if (bounds.max.y > _boardBounds.max.y)
        {
            // Over the top
            correctionY = _boardBounds.max.y - bounds.max.y;
        }
        obj.transform.Translate(
            correctionX, correctionY, 0, Space.World
        );
    }

    void Update()
    {
        if (floatBlocks.Count > 0)
        {
            // Drop floating blocks.
            _looseBlockPassed += Time.deltaTime;
            if (_looseBlockPassed >= .5f)
            {
                for (int i=0; i < floatBlocks.Count; i++)
                {
                    GameObject block = floatBlocks[i];
                    if (!BlockUtils.HitTest(block.transform, Vector3.down, 1))
                    {
                        block.transform.Translate(Vector3.down, Space.World);
                    }
                    else
                    {
                        i--;
                        floatBlocks.Remove(block);
                    }
                }
                UpdateFloatingBlocks();
                _looseBlockPassed = 0;
            }
        }
        else
        {
            _movementTimePassed += Time.deltaTime;
            _inputTimePassed += Time.deltaTime;
            
            if (PieceLanded)
            {
                NextTetronimo();
            }
            
            // max is top right
            Vector3 start = _boardBounds.max;
            if (_active == null)
            {
                AddTetronimo();
            }
            else
            {
                Bounds ab = BlockUtils.GetBounds(_active);
                if (_inputTimePassed > .05f)
                {
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        DoRotation(_active);
                        PieceLanded = false;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        bool hit = BlockUtils.HitTestChildren(
                            _active.transform, Vector3.right
                        );
                        if (!hit)
                        {
                            _active.transform.Translate(Vector3.right, Space.World);
                            PieceLanded = false;
                        }
                    }
                    else if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        bool hit = BlockUtils.HitTestChildren(
                            _active.transform, Vector3.left
                        );
                        if (!hit)
                        {
                            _active.transform.Translate(Vector3.left, Space.World);
                            // TODO: Pieces can be moved around infinitely if they
                            // have room to move left and right because this keeps
                            // setting piecelanded = false
                            PieceLanded = false;
                        }
                    }
                    
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        _movementThresh = .05f;
                    }
                    else
                    {
                        _movementThresh = 1f;
                    }
                    _inputTimePassed = 0;
                }

                if (_movementTimePassed >= _movementThresh)
                {
                    if (!PieceLanded)
                    {
                        // Vector3 pos = _active.transform.position;
                        // pos.y -= 1f;
                        // _active.transform.position = pos;
                        _active.transform.Translate(
                            Vector3.down,
                            Space.World
                        );

                        Bounds activeBounds = BlockUtils.GetBounds(_active);
                        if (activeBounds.min.y <= _boardBounds.min.y)
                        {
                            // Hit the bottom of the board.
                            //float correctionDistance = _boardBounds.min.y - activeBounds.min.y;
                            //_active.transform.Translate(0, correctionDistance, 0);
                            
                            Vector3 lowestPoint = BlockUtils.GetLowestPoint(_active);
                            float correctionDistance = _boardBounds.min.y - lowestPoint.y;
                            _active.transform.Translate(0, correctionDistance, 0, Space.World);

                            PieceLanded = true;
                        }
                    }
                    _movementTimePassed = 0;
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Vector3 start = _boardBounds.min;
        for (int i=0; i < width; i++)
        {
            for (int j=0; j < height; j++)
            {
                Gizmos.DrawLine(
                    new Vector3(
                        start.x + i,
                        start.y + j,
                        0
                    ),
                    new Vector3(
                        start.x + i + gridSize,
                        start.y + j,
                        0
                    )
                );
                Debug.DrawLine(
                    new Vector3(
                        start.x + i + gridSize,
                        start.y + j,
                        0
                    ),
                    new Vector3(
                        start.x + i + gridSize,
                        start.y + j + gridSize,
                        0
                    )
                );
                Debug.DrawLine(
                    new Vector3(
                        start.x + i + gridSize,
                        start.y + j + gridSize,
                        0
                    ),
                    new Vector3(
                        start.x + i,
                        start.y + j + gridSize,
                        0
                    )
                );
                Debug.DrawLine(
                    new Vector3(
                        start.x + i,
                        start.y + j + gridSize,
                        0
                    ),
                    new Vector3(
                        start.x + i,
                        start.y + j,
                        0
                    )
                );
            }
        }
    }
}
