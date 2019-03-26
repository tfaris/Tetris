using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;


public class TetrisBoard : MonoBehaviour
{
    public int width, height;
    int gridSize = 1, _score = 0;
    public List<GameObject> tetronimoChoices;
    List<Transform> allBlocks, floatBlocks;
    
    GameObject _active, _next, _nextDisplay;
    float _movementTimePassed, _inputTimePassed, _looseBlockPassed, _rotationTimePassed;
    float _movementThresh = 1f;
    float _locktimePassed = 0;
    // Number of frames before a piece is locked
    // into place.
    float _lockDelay = .5f;
    bool _pieceLanded, _lineCleared, _topOut;
    public bool PieceLanded 
    { 
        get => _pieceLanded;
        set
        {
            if (value != _pieceLanded)
            {
                _pieceLanded = value;
                _locktimePassed = 0;
                PlayLanding();
            }
        }
    }
    Bounds _boardBounds;
    public Mesh gridItemMesh;
    public Material gridItemMaterial1;
    public Material gridItemMaterial2;
    public TMPro.TextMeshPro _scoreText;
    public AudioSource _landSound, _music;

    // Start is called before the first frame update
    void Start()
    {
        allBlocks = new List<Transform>();
        floatBlocks = new List<Transform>();
        _nextDisplay = GameObject.Find("Next");
        
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
            var ttb = _active.GetComponent<TetronimoBehavior>();
            ttb.PieceSettled = true;
            ttb.Active = false;
            _active = null;
            _locktimePassed = 0;
            PlayLanding();
        }
    }

    public void PlayLanding()
    {
        _landSound.pitch = UnityEngine.Random.Range(1f, 2f);
        _landSound.Play();
    }

    void CheckLines()
    {
        Dictionary<float, List<Transform>> rowBlockCounts = 
            new Dictionary<float, List<Transform>>();

        foreach (Transform tBlock in allBlocks)
        {
            int row = Mathf.FloorToInt(tBlock.transform.position.y);
            List<Transform> rowCount;
            if (!rowBlockCounts.TryGetValue(row, out rowCount)){
                rowBlockCounts[row] = rowCount = new List<Transform>();
            }
            rowCount.Add(tBlock);
        }

        int linesCleared = 0;
        foreach (var kvp in rowBlockCounts)
        {
            if (kvp.Value.Count >= width)
            {
                // Line has been filled
                foreach (Transform t in kvp.Value)
                {
                    //Rigidbody rb = t.GetComponentsInParent<Rigidbody>()[0];
                    // rb.constraints = RigidbodyConstraints.None;
                    // rb.AddExplosionForce(
                    //     50,
                    //     new Vector3(0, kvp.Key, 0),
                    //     1
                    // );
                    // 
                    var tetronimo = t.parent;
                    if (tetronimo != null)
                    {
                        foreach (Transform leftoverBlock in tetronimo)
                        {
                            leftoverBlock.parent = null;
                        }
                    }
                    allBlocks.Remove(t);
                    Destroy(t.gameObject);
                }
                linesCleared++;
            }
        }

        if (linesCleared > 0)
        {
            Debug.Log(linesCleared + " lines cleared.");
            _lineCleared = true;
            _score += 100 * linesCleared;
            UpdateScore();
        }
    }

    
    void UpdateScore()
    {
        _scoreText.text = _score.ToString();
    }
    
    void UpdateFloatingBlocks()
    {
        //floatBlocks.Clear();
        foreach (Transform block in allBlocks)
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

    int blockCounter = 1;
    void AddTetronimo()
    {
        PieceLanded = false;
        GameObject 
            prefabTmo = tetronimoChoices[UnityEngine.Random.Range(0, tetronimoChoices.Count)],
            nextPrefabTmo = tetronimoChoices[UnityEngine.Random.Range(0, tetronimoChoices.Count)];

        if (_next == null)
        {
            _active = Instantiate(
                prefabTmo,
                Vector3.zero,
                Quaternion.identity
            );
        }
        else
        {
            _active = _next;
        }
        
        _next = Instantiate(
            nextPrefabTmo,
            _nextDisplay.transform
        );
        _next.transform.localPosition = Vector3.zero;

        _active.transform.localRotation = Quaternion.identity;
        _active.transform.parent = transform;

        var ttb = _active.GetComponent<TetronimoBehavior>();
        ttb.Board = this;
        ttb.Active = true;
        _active.name = "tmo" + UnityEngine.Random.Range(0, 500).ToString();
        foreach (Transform block in _active.transform)
        {
            block.gameObject.AddComponent<BoxCollider>();
            block.name = "block" + (++blockCounter);
            allBlocks.Add(block);
        }

        _active.transform.localPosition = Vector3.zero;
        _active.transform.Translate(-.5f, -.5f + (height / 2), 0);

        // Manual collision test since raycast doesn't seem to work on same position.
        foreach (Transform activeBlock in _active.transform)
        {
            foreach (Transform otherBlock in allBlocks)
            {
                if (!otherBlock.transform.IsChildOf(_active.transform))
                {
                    float dist = Vector3.Distance(activeBlock.transform.position, otherBlock.transform.position);
                    if (dist == 0)
                    {
                        _topOut = true;
                        break;
                    }
                }
            }
            if (_topOut)
            {
                break;
            }
        }
    }

    void DoRotation(GameObject obj)
    {
        obj.transform.Rotate(Vector3.forward, -90f);

        // Prevent rotation into other pieces.
        bool rotationCollision = false;
        foreach (Transform blockTrans in obj.transform)
        {
           var overlaps = Physics.OverlapBox(blockTrans.position, new Vector3(1,1,1));
           foreach (Collider x in overlaps)
           {
               if (x.transform.parent != blockTrans.parent)
               {
                    Boundary boundary = x.GetComponent<Boundary>();
                    if (boundary == null)
                    {
                        rotationCollision = true;
                        break;
                    }
               }
           }
        }

        if (rotationCollision)
        {
            obj.transform.Rotate(Vector3.forward, 90f);
        }
        
        WallKick(obj);
    }

    void WallKick(GameObject obj)
    {
        var bounds = BlockUtils.GetBounds(obj);
        float correctionX = 0, correctionY = 0;
        if (bounds.min.x <= _boardBounds.min.x)
        {
            // Over the left edge
            correctionX = _boardBounds.min.x - bounds.min.x + 1;
        }
        if (bounds.max.x >= _boardBounds.max.x)
        {
            // Over the right edge
            correctionX = _boardBounds.max.x - bounds.max.x;
        }
        if (bounds.max.y >= _boardBounds.max.y)
        {
            // Over the top
            correctionY = _boardBounds.max.y - bounds.max.y;
        }
        obj.transform.Translate(
            correctionX, correctionY, 0, Space.World
        );
    }

    float rnext = 0, _fearChange = 0;
    void Update()
    {
        if (_topOut)
        {
            GameOver();
            return;
        }

        float highestY = float.MinValue;
        foreach (Transform block in allBlocks)
        {
            if (block != null && 
                block.transform != null &&
                (_active == null || (_active != null &&!block.transform.IsChildOf(_active.transform))))
            {
                highestY = Mathf.Max(highestY, block.transform.position.y + .5f);
            }
        }

        float fearRatio = Mathf.Max(0,
            Mathf.Clamp(
                (highestY - _boardBounds.min.y) / (_boardBounds.extents.y * 2),
                0,
                1) - .5f);
        _music.pitch = Mathf.Lerp(_music.pitch, 1 + fearRatio, _fearChange);
        _fearChange = Mathf.Clamp(_fearChange + Time.deltaTime * .0005f, 0, 1);

        if (_next != null)
        {
            _next.transform.localRotation = Quaternion.Euler(
                0, 
                Mathf.Lerp(_next.transform.localRotation.y, 360, rnext), 
            0);
            rnext += Time.deltaTime * .05f;
            if (rnext >= 1)
            {
                rnext = 0;
                _next.transform.localRotation = Quaternion.identity;
            }
        }

        if (_lineCleared)
        {
            UpdateFloatingBlocks();
            _lineCleared = false;
        }

        if (floatBlocks.Count > 0)
        {
            // Drop floating blocks.
            _looseBlockPassed += Time.deltaTime;
            if (_looseBlockPassed >= .15f)
            {
                for (int i=0; i < floatBlocks.Count; i++)
                {
                    Transform block = floatBlocks[i];
                    if (!BlockUtils.HitTest(block.transform, Vector3.down, 1))
                    {
                        block.transform.Translate(Vector3.down, Space.World);
                    }
                    else
                    {
                        i--;
                        floatBlocks.Remove(block);
                        PlayLanding();
                    }
                }
                _looseBlockPassed = 0;
            }
            UpdateFloatingBlocks();
        }
        else
        {
            _movementTimePassed += Time.deltaTime;
            _inputTimePassed += Time.deltaTime;
            _rotationTimePassed += Time.deltaTime;
            
            if (PieceLanded)
            {
                NextTetronimo();
            }
            
            // max is top right
            Vector3 start = _boardBounds.max;
            if (_active == null && !_lineCleared)
            {
                AddTetronimo();
            }
            else if (_active != null)
            {
                Bounds ab = BlockUtils.GetBounds(_active);
                if (_rotationTimePassed > .10f)
                {
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        DoRotation(_active);
                    }
                    _rotationTimePassed = 0;
                }
                if (_inputTimePassed > .05f)
                {
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        bool hit = BlockUtils.HitTestChildren(
                            _active.transform, Vector3.right
                        );
                        if (!hit)
                        {
                            _active.transform.Translate(Vector3.right, Space.World);
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
                        _active.transform.Translate(
                            Vector3.down,
                            Space.World
                        );
                    }
                    _movementTimePassed = 0;
                }
            }
        }
    }
    
    void GameOver()
    {
        print("Gameover. Score: " + _score);
        Destroy(_active);
        Destroy(_next);
        _active = null;
        _next = null;
        _topOut = false;
        _score = 0;
        foreach (Transform block in allBlocks)
        {
            Destroy(block.gameObject);
        }
        foreach (Transform block in floatBlocks)
        {
            Destroy(block.gameObject);
        }
        allBlocks.Clear();
        floatBlocks.Clear();
        _lineCleared = false;
        _pieceLanded = false;

        UpdateScore();
    }
    
    void OnDrawGizmos()
    {
        _boardBounds = new Bounds(
            transform.position,
            new Vector3(
                width,
                height,
                0
            )
        );
        Vector3 start = _boardBounds.min;
        for (int i=0; i < width + 2; i++)
        {
            for (int j=0; j < height + 2; j++)
            {
                Gizmos.DrawLine(
                    new Vector3(
                        start.x + i ,
                        start.y + j ,
                        0
                    ),
                    new Vector3(
                        start.x + i + gridSize,
                        start.y + j ,
                        0
                    )
                );
                Gizmos.DrawLine(
                    new Vector3(
                        start.x + i + gridSize ,
                        start.y + j ,
                        0
                    ),
                    new Vector3(
                        start.x + i + gridSize,
                        start.y + j + gridSize,
                        0
                    )
                );
                Gizmos.DrawLine(
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
                Gizmos.DrawLine(
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
