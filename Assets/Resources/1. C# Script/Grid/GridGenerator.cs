using UnityEngine;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public enum TileType { Empty, Wall, Apple, Poison, SnakeHead, SnakeBody, SnakeTail }
    public class TileData
    {
        public TileType type;
        public GameObject groundVisual;
        public TileVisual tileVisualScript;
        public Vector2Int gridPosition;
        
        public TileData(Vector2Int pos){ gridPosition = pos; type = TileType.Empty; }
    }
    
    [Header("REFERENCES")] 
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ColumnCompressionMovement ccm;
    [SerializeField] private Material spriteLitMaterial;
    
    [Header("GRID SIZE")] 
    [SerializeField, Range(0.1f, 1f)] private float gridWidthPercentage = 0.8f;
    [SerializeField, Range(0.1f, 1f)] private float gridHeightPercentage = 0.8f;
    [Space(10)]
    [SerializeField, MinValue(0f), MaxValue(1f)] private Vector2 gridOffset = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2Int fixedGridSize = new Vector2Int(20, 20);
    [SerializeField] private bool useFixedSize = false;
    
    [Header("GRID")] [SerializeField] private float tileSize = 1f;
    [SerializeField] private Transform gridParent;
    
    [Header("VISUAL")] [SerializeField] private Sprite tileSprite;
    [SerializeField] private Color[] checkerColors = new Color[2];
    [SerializeField] private Color[] wallCheckerColors = new Color[2];
    [SerializeField] private float colorVariationAmount = 0.15f;
    private bool isSnakeHurt = false;
    
    [Header("SORTING")] [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int groundSortingOrder = 0;
    
    [Header("DEBUG")] 
    [SerializeField] private bool displayTileType = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.yellow;
    
    private TileData[,] gridData;
    private Vector2Int calculatedGridSize;
    private Vector3 bottomLeftPosition, topRightPosition;
    private bool isInitialized = false;
    private float actualTileSize;
    
    void OnValidate(){
        if(tileSize <= 0) tileSize = 0.1f;
        gridWidthPercentage = Mathf.Clamp(gridWidthPercentage, 0.1f, 1f);
        gridHeightPercentage = Mathf.Clamp(gridHeightPercentage, 0.1f, 1f);
        if(fixedGridSize.x < 1) fixedGridSize.x = 1;
        if(fixedGridSize.y < 1) fixedGridSize.y = 1;
        if(checkerColors == null || checkerColors.Length < 2) checkerColors = new Color[]{ new Color(0.2f, 0.2f, 0.2f, 1f), new Color(0.3f, 0.3f, 0.3f, 1f) };
        if(wallCheckerColors == null || wallCheckerColors.Length < 2) wallCheckerColors = new Color[]{ new Color(0.4f, 0.4f, 0.4f, 1f), new Color(0.5f, 0.5f, 0.5f, 1f) };
    }
    
    void Start(){
        ccm ??= FindFirstObjectByType<ColumnCompressionMovement>();
        if(targetCamera == null) targetCamera = Camera.main;
        if(!isInitialized){ CalculateGridFromCamera(); InitializeGridData(); GenerateVisualTiles(); isInitialized = true; }
    }
    
    void OnDrawGizmos(){
        if(!showGizmos || targetCamera == null) return;
        if(targetCamera == null) targetCamera = Camera.main;
        if(targetCamera == null) return;
        
        CalculateGridBoundsForGizmos();
        
        float gridWidth = topRightPosition.x - bottomLeftPosition.x;
        float gridHeight = topRightPosition.y - bottomLeftPosition.y;
        Vector3 center = bottomLeftPosition + new Vector3(gridWidth * 0.5f, gridHeight * 0.5f, 0);
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, new Vector3(gridWidth, gridHeight, 0));
        
        Gizmos.color = useFixedSize ? Color.green : Color.cyan;
        int tilesX = useFixedSize ? fixedGridSize.x : calculatedGridSize.x;
        int tilesY = useFixedSize ? fixedGridSize.y : calculatedGridSize.y;
        float tileW = gridWidth / tilesX;
        float tileH = gridHeight / tilesY;
        
        for(int x = 0; x <= tilesX; x++){
            Vector3 start = bottomLeftPosition + new Vector3(x * tileW, 0, 0);
            Vector3 end = bottomLeftPosition + new Vector3(x * tileW, gridHeight, 0);
            Gizmos.DrawLine(start, end);
        }
        for(int y = 0; y <= tilesY; y++){
            Vector3 start = bottomLeftPosition + new Vector3(0, y * tileH, 0);
            Vector3 end = bottomLeftPosition + new Vector3(gridWidth, y * tileH, 0);
            Gizmos.DrawLine(start, end);
        }
        
        Gizmos.color = Color.red;
        Vector3 br = new Vector3(topRightPosition.x, bottomLeftPosition.y, 0);
        Vector3 tl = new Vector3(bottomLeftPosition.x, topRightPosition.y, 0);
        Gizmos.DrawLine(bottomLeftPosition, br);
        Gizmos.DrawLine(br, topRightPosition);
        Gizmos.DrawLine(topRightPosition, tl);
        Gizmos.DrawLine(tl, bottomLeftPosition);
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(center, 0.1f);
    }
    
    void CalculateGridBoundsForGizmos(){
        if(targetCamera == null) return;

        float camH = targetCamera.orthographicSize * 2f;
        float camW = camH * targetCamera.aspect;
        Vector3 camBL = targetCamera.transform.position - new Vector3(camW * 0.5f, camH * 0.5f, 0);
        float usedW = camW * gridWidthPercentage;
        float usedH = camH * gridHeightPercentage;
        float offX = (camW - usedW) * gridOffset.x;
        float offY = (camH - usedH) * gridOffset.y;
        bottomLeftPosition = camBL + new Vector3(offX, offY, 0);
        
        if(useFixedSize){
            float tile = Mathf.Min(usedW / fixedGridSize.x, usedH / fixedGridSize.y);
            float totalW = fixedGridSize.x * tile;
            float totalH = fixedGridSize.y * tile;
            bottomLeftPosition += new Vector3((usedW - totalW) * 0.5f, (usedH - totalH) * 0.5f, 0);
            topRightPosition = bottomLeftPosition + new Vector3(totalW, totalH, 0);
        }
        else{
            int tilesX = Mathf.FloorToInt(usedW / tileSize);
            int tilesY = Mathf.FloorToInt(usedH / tileSize);
            float totalW = tilesX * tileSize;
            float totalH = tilesY * tileSize;
            bottomLeftPosition += new Vector3((usedW - totalW) * 0.5f, (usedH - totalH) * 0.5f, 0);
            topRightPosition = bottomLeftPosition + new Vector3(totalW, totalH, 0);
        }
    }
    
    public void CalculateGridFromCamera(){
        if(targetCamera == null) return;

        float camH = targetCamera.orthographicSize * 2f;
        float camW = camH * targetCamera.aspect;
        Vector3 camBL = targetCamera.transform.position - new Vector3(camW * 0.5f, camH * 0.5f, 0);
        float usedW = camW * gridWidthPercentage;
        float usedH = camH * gridHeightPercentage;
        float offX = (camW - usedW) * gridOffset.x;
        float offY = (camH - usedH) * gridOffset.y;
        bottomLeftPosition = camBL + new Vector3(offX, offY, 0);
        
        if(useFixedSize){
            calculatedGridSize = fixedGridSize;
            actualTileSize = Mathf.Min(usedW / fixedGridSize.x, usedH / fixedGridSize.y);
            float totalW = calculatedGridSize.x * actualTileSize;
            float totalH = calculatedGridSize.y * actualTileSize;
            bottomLeftPosition += new Vector3((usedW - totalW) * 0.5f, (usedH - totalH) * 0.5f, 0);
            topRightPosition = bottomLeftPosition + new Vector3(totalW, totalH, 0);
        }
        else{
            int tilesX = Mathf.FloorToInt(usedW / tileSize);
            int tilesY = Mathf.FloorToInt(usedH / tileSize);
            calculatedGridSize = new Vector2Int(tilesX, tilesY);
            actualTileSize = tileSize;
            float totalW = calculatedGridSize.x * actualTileSize;
            float totalH = calculatedGridSize.y * actualTileSize;
            bottomLeftPosition += new Vector3((usedW - totalW) * 0.5f, (usedH - totalH) * 0.5f, 0);
            topRightPosition = bottomLeftPosition + new Vector3(totalW, totalH, 0);
        }
        isInitialized = true;
    }
    
    void InitializeGridData(){
        gridData = new TileData[calculatedGridSize.x, calculatedGridSize.y];
        for(int x = 0; x < calculatedGridSize.x; x++){
            for(int y = 0; y < calculatedGridSize.y; y++){
                gridData[x, y] = new TileData(new Vector2Int(x, y));
            }
        }
    }
    
    void GenerateVisualTiles(){
        if(gridParent == null){
            GameObject parentObj = new GameObject("GridParent");
            gridParent = parentObj.transform;
            gridParent.SetParent(transform);
        }

        ClearGridChildren();
        for(int x = 0; x < calculatedGridSize.x; x++){
            for(int y = 0; y < calculatedGridSize.y; y++){
                CreateGroundTile(x, y);
            }
        }
    }
    
    void ClearGridChildren(){
        if(gridParent == null) return;
        
        for(int i = gridParent.childCount - 1; i >= 0; i--){
            if(Application.isPlaying) Destroy(gridParent.GetChild(i).gameObject);
            else DestroyImmediate(gridParent.GetChild(i).gameObject);
        }
    }

    public void ClearAllTilesAndGameOver(System.Action onComplete) => StartCoroutine(ClearRowsCoroutine(onComplete));
    IEnumerator ClearRowsCoroutine(System.Action onComplete){
        for(int row = 0; row < calculatedGridSize.y; row++){
            for(int x = 0; x < calculatedGridSize.x; x++){
                Vector2Int pos = new Vector2Int(x, row);
                
                SetTileType(pos, TileType.Empty);
                ccm?.RemoveEntity(pos);
            }
            AudioManager.Instance.PlayClearBlock();
            yield return new WaitForSecondsRealtime(0.075f);
        }
        
        ccm?.ClearAllEntities();
        onComplete?.Invoke();
    }
    
    [Button("Generate Grid", EButtonEnableMode.Editor)] 
    void GenerateGrid(){
        if(targetCamera == null) return;
        CalculateGridFromCamera();
        InitializeGridData();
        GenerateVisualTiles();
        isInitialized = true;
    }
    
    [Button("Clear Grid", EButtonEnableMode.Editor)] 
    void ClearGrid(){
        ClearGridChildren();
        if(gridData == null) return;
        for(int x = 0; x < calculatedGridSize.x; x++){
            for(int y = 0; y < calculatedGridSize.y; y++){
                if(gridData[x, y] != null){
                    gridData[x, y].type = TileType.Empty;
                    gridData[x, y].groundVisual = null;
                    gridData[x, y].tileVisualScript = null;
                }
            }
        }
    }
    
    void CreateGroundTile(int x, int y){
        GameObject tile = new GameObject($"Ground_{x}_{y}");
        tile.transform.SetParent(gridParent);
        tile.transform.position = new Vector3(
            bottomLeftPosition.x + (x * actualTileSize) + (actualTileSize * 0.5f),
            bottomLeftPosition.y + (y * actualTileSize) + (actualTileSize * 0.5f), 0);
        tile.transform.localScale = new Vector3(actualTileSize, actualTileSize, 1f);
        
        tile.AddComponent<BoxCollider2D>();
        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = groundSortingOrder;
        
        Color baseColor = ((x + y) % 2 == 0) ? checkerColors[0] : checkerColors[1];
        float variation = Random.Range(-colorVariationAmount, colorVariationAmount);
        Color finalBaseColor = new Color(
            Mathf.Clamp01(baseColor.r + variation),
            Mathf.Clamp01(baseColor.g + variation),
            Mathf.Clamp01(baseColor.b + variation), 1f);
        
        sr.color = finalBaseColor;
        
        TileVisual tv = tile.AddComponent<TileVisual>();
        tv.Initialize(gridData[x, y], new Vector2Int(x, y), finalBaseColor, wallCheckerColors, colorVariationAmount, spriteLitMaterial);
        gridData[x, y].groundVisual = tile;
        gridData[x, y].tileVisualScript = tv;
    }
    
    void UpdateTileVisuals(){
        for(int x = 0; x < calculatedGridSize.x; x++){
            for(int y = 0; y < calculatedGridSize.y; y++){
                if(gridData[x, y]?.tileVisualScript != null)
                    gridData[x, y].tileVisualScript.UpdateDisplay(gridData[x, y].type, isSnakeHurt);
            }
        }
    }
    
    public void SetTileType(Vector2Int gridPos, TileType type){
        if(!IsValidPosition(gridPos)) return;
        
        TileData tile = gridData[gridPos.x, gridPos.y];
        if(tile.type == type) return;
        
        tile.type = type;
        
        if(displayTileType && tile.tileVisualScript != null)
            tile.tileVisualScript.UpdateDisplay(type);
    }
    
    public TileType GetTileType(Vector2Int gridPos) => IsValidPosition(gridPos) ? gridData[gridPos.x, gridPos.y].type : TileType.Empty;
    public string GetTileName(Vector2Int gridPos) => IsValidPosition(gridPos) ? $"{gridData[gridPos.x, gridPos.y].type}({gridPos.x},{gridPos.y})" : "Invalid";
    
    public void ClearTileType(Vector2Int gridPos) => SetTileType(gridPos, TileType.Empty);
    public bool IsTileEmpty(Vector2Int gridPos) => IsValidPosition(gridPos) && gridData[gridPos.x, gridPos.y].type == TileType.Empty;
    public bool IsTileWalkable(Vector2Int gridPos){
        if(!IsValidPosition(gridPos)) return false;

        TileType t = gridData[gridPos.x, gridPos.y].type;
        return t == TileType.Empty || t == TileType.Apple || t == TileType.Poison;
    }
    public bool IsValidPosition(Vector2Int p) => p.x >= 0 && p.x < calculatedGridSize.x && p.y >= 0 && p.y < calculatedGridSize.y;
    
    public List<Vector2Int> GetAllTilesOfType(TileType type){
        List<Vector2Int> result = new List<Vector2Int>();
        for(int x = 0; x < calculatedGridSize.x; x++){
            for(int y = 0; y < calculatedGridSize.y; y++){
                if(gridData[x, y].type == type) result.Add(new Vector2Int(x, y));
            }
        }
        return result;
    }
    
    public Vector3 GridToWorldPosition(Vector2Int p) => new Vector3(
        bottomLeftPosition.x + (p.x * actualTileSize) + (actualTileSize * 0.5f),
        bottomLeftPosition.y + (p.y * actualTileSize) + (actualTileSize * 0.5f), 0);
    
    public Vector2Int WorldToGridPosition(Vector3 world) => new Vector2Int(
        Mathf.RoundToInt((world.x - bottomLeftPosition.x - actualTileSize * 0.5f) / actualTileSize),
        Mathf.RoundToInt((world.y - bottomLeftPosition.y - actualTileSize * 0.5f) / actualTileSize));
    
    public Vector2Int GetGridSize() => calculatedGridSize;
    public float GetTileSize() => actualTileSize;
    public Vector3 GetBottomLeftPosition() => bottomLeftPosition;
    public bool IsInitialized() => isInitialized;
    public TileData GetTileData(Vector2Int pos) => IsValidPosition(pos) ? gridData[pos.x, pos.y] : null;
    public void SetSnakeHurt(bool value){
        isSnakeHurt = value;
        UpdateTileVisuals();
    }
}