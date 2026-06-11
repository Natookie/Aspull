using NaughtyAttributes;
using UnityEngine;

public class TileVisual : MonoBehaviour
{
    [ReadOnly] public GridGenerator.TileType currentType;
    [ReadOnly] public GridGenerator.TileData tileData;
    [ReadOnly] public Vector2Int gridPosition;
    [ReadOnly] public SpriteRenderer spriteRenderer;
    [ReadOnly] public Color groundColor;
    [ReadOnly] public Color[] wallCheckerColors;
    [ReadOnly] public float colorVariationAmount;
    
    private static readonly Color APPLE_COLOR = new Color(0.898f, 0.325f, 0.294f, 1.000f);
    private static readonly Color POISON_COLOR = new Color(0.306f, 0.788f, 0.690f, 1.000f);
    private static readonly Color SNAKE_HEAD_COLOR = new Color(0.247f, 0.725f, 0.314f, 1.000f);
    private static readonly Color SNAKE_BODY_COLOR = new Color(0.180f, 0.627f, 0.263f, 1.000f);
    private static readonly Color SNAKE_TAIL_COLOR = new Color(0.137f, 0.525f, 0.212f, 1.000f);
    private static readonly Color SNAKE_HEAD_HURT_COLOR = new Color(0.725f, 0.247f, 0.247f, 1.000f);
    private static readonly Color SNAKE_BODY_HURT_COLOR = new Color(0.627f, 0.180f, 0.180f, 1.000f);
    private static readonly Color SNAKE_TAIL_HURT_COLOR = new Color(0.525f, 0.137f, 0.137f, 1.000f);
    
    private bool isSnakeHurt = false;
    private Color wallColor;
    private bool wallColorCached = false;
    private Collider2D tileCollider;
    private ColumnCompressionMovement columnCompression;
    
    private const int LAYER_DEFAULT = 0;
    private const int LAYER_WATER = 4;
    
    public void Initialize(GridGenerator.TileData data, Vector2Int pos, Color baseColor, Color[] wallColors, float variation){
        spriteRenderer = GetComponent<SpriteRenderer>();
        columnCompression = FindFirstObjectByType<ColumnCompressionMovement>();
        tileCollider = GetComponent<Collider2D>();
        tileData = data;
        gridPosition = pos;
        groundColor = baseColor;
        wallCheckerColors = wallColors;
        colorVariationAmount = variation;
        spriteRenderer.color = groundColor;
        currentType = GridGenerator.TileType.Empty;
        wallColorCached = false;
        gameObject.layer = LAYER_DEFAULT;
    }
    
    public void UpdateDisplay(GridGenerator.TileType type, bool hurt = false){
        if(spriteRenderer == null) return;
        if(currentType == type && isSnakeHurt == hurt) return;
        currentType = type;
        isSnakeHurt = hurt;

        switch(type){
            case GridGenerator.TileType.Empty:
                spriteRenderer.color = groundColor;
                gameObject.layer = LAYER_DEFAULT;
                if(tileCollider != null) tileCollider.enabled = false;
                break;
            case GridGenerator.TileType.Wall:
                if(!wallColorCached){
                    Color wallBase = ((gridPosition.x + gridPosition.y) % 2 == 0) ? wallCheckerColors[0] : wallCheckerColors[1];
                    float variation = Random.Range(-colorVariationAmount, colorVariationAmount);
                    wallColor = new Color(
                        Mathf.Clamp01(wallBase.r + variation),
                        Mathf.Clamp01(wallBase.g + variation),
                        Mathf.Clamp01(wallBase.b + variation), 1f);
                    wallColorCached = true;
                }
                spriteRenderer.color = wallColor;
                
                bool isOutline = columnCompression != null && columnCompression.IsOutlineWallAt(gridPosition);
                gameObject.layer = isOutline ? LAYER_WATER : LAYER_DEFAULT;
                
                if(tileCollider != null) tileCollider.enabled = isOutline;
                break;
            case GridGenerator.TileType.Apple:
                spriteRenderer.color = APPLE_COLOR;
                gameObject.layer = LAYER_DEFAULT;
                if(tileCollider != null) tileCollider.enabled = false;
                break;
            case GridGenerator.TileType.Poison:
                spriteRenderer.color = POISON_COLOR;
                if(tileCollider != null) tileCollider.enabled = false;
                break;
            case GridGenerator.TileType.SnakeHead:
            case GridGenerator.TileType.SnakeBody:
            case GridGenerator.TileType.SnakeTail:
                if(type == GridGenerator.TileType.SnakeHead) spriteRenderer.color = hurt ? SNAKE_HEAD_HURT_COLOR : SNAKE_HEAD_COLOR;
                else if(type == GridGenerator.TileType.SnakeBody) spriteRenderer.color = hurt ? SNAKE_BODY_HURT_COLOR : SNAKE_BODY_COLOR;
                else spriteRenderer.color = hurt ? SNAKE_TAIL_HURT_COLOR : SNAKE_TAIL_COLOR;

                gameObject.layer = LAYER_DEFAULT;
                if(tileCollider != null) tileCollider.enabled = false;
                break;
        }
    }
}