using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrisBoard : MonoBehaviour
{
    public int width, height;
    int gridSize = 1;
    public List<GameObject> tetronimoChoices;
    List<GameObject> onboard;
    
    GameObject _active;
    float _movementTimePassed, _inputTimePassed;
    float _movementThresh = 1f;
    float _locktimePassed = 0;
    // Number of frames before a piece is locked
    // into place.
    float _lockDelay = .5f;
    public bool PieceLanded { get; set;}
    Bounds _boardBounds;

    public Mesh gridItemMesh;
    public Material gridItemMaterial1;
    public Material gridItemMaterial2;

    // Start is called before the first frame update
    void Start()
    {
        onboard = new List<GameObject>();
        
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
        for (int i=0; i < width; i++)
        {
            for (int j=0; j < height; j++)
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

    void Nudge(GameObject tm)
    {
        Vector3 p = tm.transform.position;
        Bounds b = BlockUtils.GetBounds(tm);
        Vector3 boundsMin = b.min;
        float nx = Mathf.Floor(boundsMin.x),
              ny = Mathf.Floor(boundsMin.y),
              dx = boundsMin.x - nx,
              dy = boundsMin.y - ny;
        tm.transform.Translate(dx, dy, 0);
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
            _active = null;
            _locktimePassed = 0;
        }
    }

    void AddTetronimo()
    {
        PieceLanded = false;
        GameObject prefabTmo = tetronimoChoices[Random.Range(0, tetronimoChoices.Count)];
        _active = Instantiate(
            prefabTmo,
            Vector3.zero,
            Quaternion.identity
        );
        _active.transform.parent = transform;
        _active.transform.Translate(-.5f - 2, -.5f + (height / 2), 0);
        onboard.Add(_active);
        _active.GetComponent<TetronimoBehavior>().Board = this;
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

    // Update is called once per frame
    void Update()
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
                    RaycastHit hit;
                    if (_active.GetComponent<Rigidbody>().SweepTest(Vector3.right, out hit, 1)){
                        //
                    }
                    else
                    {
                        if (ab.max.x + 1 <= _boardBounds.max.x) 
                        {
                            _active.transform.Translate(Vector3.right, Space.World);
                            PieceLanded = false;
                        }
                    }
                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    RaycastHit hit;
                    if (_active.GetComponent<Rigidbody>().SweepTest(Vector3.left, out hit, 1)){
                        //
                    }
                    else
                    {
                        if (ab.min.x - 1 > _boardBounds.min.x - 1)
                        {
                            _active.transform.Translate(Vector3.left, Space.World);
                            PieceLanded = false;
                        }
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
