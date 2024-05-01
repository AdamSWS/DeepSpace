using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogicGatesGameController : MonoBehaviour
{

    [SerializeField] private int difficulty = 4;

    [SerializeField] private Texture2D imageA;
    [SerializeField] private Transform levelPanel;
    [SerializeField] private Transform gamePanel;
    [SerializeField] private Image levelPrefab;
    [SerializeField] private Transform gameHolder;
    [SerializeField] private Transform piecePrefab;
    [SerializeField] private GameObject puzzleDoneButton;
    public AudioSource buttonClickSound;
    public AudioSource pieceCorrectSound;
    public AudioSource victorySound;
    private List<Transform> pieces;
    private Vector2Int dimensions;
    private float width;
    private float height;

    private Transform draggingPiece = null;
    private Vector3 offset;

    private int piecesCorrect;

    void Start()
    {
        Image image = Instantiate(levelPrefab, levelPanel);
        image.sprite = Sprite.Create(imageA, new Rect(0, 0, imageA.width, imageA.height), new Vector2(0.5f, 0.5f));
        image.GetComponent<Button>().onClick.AddListener(delegate { StartGame(imageA); });
        buttonClickSound.Play();
    }

    public void StartGame(Texture2D jigsawTexture)
    {

        levelPanel.gameObject.SetActive(false);
        gamePanel.gameObject.SetActive(true);

        pieces = new List<Transform>();

        dimensions = GetDimensions(jigsawTexture, difficulty);

        CreateJigsawPieces(jigsawTexture);

        SetGameHolderPosition();

        Scatter();

        UpdateBorder();

        piecesCorrect = 0;
    }

    private void SetGameHolderPosition()
    {
        Camera cam = Camera.main;
        float orthoHeight = cam.orthographicSize;
        float orthoWidth = orthoHeight * cam.aspect;

        Vector3 centerPosition = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cam.nearClipPlane));

        centerPosition.z = 0;

        gameHolder.position = centerPosition;
    }

    Vector2Int GetDimensions(Texture2D jigsawTexture, int difficulty)
    {
        Vector2Int dimensions = Vector2Int.zero;

        if (jigsawTexture.width < jigsawTexture.height)
        {
            dimensions.x = difficulty;
            dimensions.y = (difficulty * jigsawTexture.height) / jigsawTexture.width;
        }
        else
        {
            dimensions.x = (difficulty * jigsawTexture.width) / jigsawTexture.height;
            dimensions.y = difficulty;
        }
        return dimensions;
    }

    void CreateJigsawPieces(Texture2D jigsawTexture)
    {

        height = 1f / dimensions.y;
        float aspect = (float)jigsawTexture.width / jigsawTexture.height;
        width = aspect / dimensions.x;

        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {

                Transform piece = Instantiate(piecePrefab, gameHolder);
                piece.localPosition = new Vector3(
                  (-width * dimensions.x / 2) + (width * col) + (width / 2),
                  (-height * dimensions.y / 2) + (height * row) + (height / 2),
                  -1);
                piece.localScale = new Vector3(width, height, 1f);

                piece.name = $"Piece {(row * dimensions.x) + col}";
                pieces.Add(piece);

                float width1 = 1f / dimensions.x;
                float height1 = 1f / dimensions.y;

                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(width1 * col, height1 * row);
                uv[1] = new Vector2(width1 * (col + 1), height1 * row);
                uv[2] = new Vector2(width1 * col, height1 * (row + 1));
                uv[3] = new Vector2(width1 * (col + 1), height1 * (row + 1));

                Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                mesh.uv = uv;

                piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);
            }
        }
    }

    private void Scatter()
    {
        Camera cam = Camera.main;
        float orthoHeight = cam.orthographicSize;
        float orthoWidth = orthoHeight * cam.aspect;

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        float buffer = 1f;

        foreach (Transform piece in pieces)
        {
            float x = Random.Range(bottomLeft.x + buffer, topRight.x - buffer);
            float y = Random.Range(bottomLeft.y + buffer, topRight.y - buffer);

            piece.position = new Vector3(x, y, -1);
        }

    }

    private void UpdateBorder()
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();

        float halfWidth = (width * dimensions.x) / 2f;
        float halfHeight = (height * dimensions.y) / 2f;

        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, -1));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, -1));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, -1));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, -1));
        lineRenderer.loop = true;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineRenderer.enabled = true;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {

                draggingPiece = hit.transform;
                offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                offset += Vector3.back;
            }
        }

        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
            SnapAndDisableIfCorrect();
            draggingPiece.position += Vector3.forward;
            draggingPiece = null;
        }

        if (draggingPiece)
        {
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            newPosition += offset;
            draggingPiece.position = newPosition;
        }
    }

    private void SnapAndDisableIfCorrect()
    {
        if (draggingPiece.gameObject.layer == LayerMask.NameToLayer("PuzzlePiece"))
        {

            int pieceIndex = pieces.IndexOf(draggingPiece);

            int col = pieceIndex % dimensions.x;
            int row = pieceIndex / dimensions.x;

            Vector3 targetPosition = new((-width * dimensions.x / 2) + (width * col) + (width / 2),
                                         (-height * dimensions.y / 2) + (height * row) + (height / 2), -1);

            if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 2))
            {

                draggingPiece.localPosition = targetPosition;

                draggingPiece.GetComponent<BoxCollider2D>().enabled = false;

                piecesCorrect++;
                pieceCorrectSound.Play();

                if (piecesCorrect == pieces.Count)
                {
                    puzzleDoneButton.SetActive(true);
                    victorySound.Play();
                }
            }
        }
    }
}