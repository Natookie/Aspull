using UnityEngine;
using System.Collections.Generic;

public class ColumnCompressionMovement : MonoBehaviour
{
    [System.Serializable]
    public struct FallingEntity{
        public Vector2Int position;
        public GridGenerator.TileType type;
        public bool isOutline;
        
        public FallingEntity(Vector2Int pos, GridGenerator.TileType tileType, bool outline = false){
            position = pos;
            type = tileType;
            isOutline = outline;
        }
    }
    
    [Header("REFERENCES")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private SnakeController snakeController;
    [SerializeField] private EncounterGenerator encounterGenerator;
    
    [Header("MOVEMENT SETTINGS")]
    [SerializeField] private bool enableMovement = true;
    
    private bool isInitialized = false;
    
    private readonly List<FallingEntity> entities = new();
    private readonly List<FallingEntity> newPositions = new();
    private readonly HashSet<Vector2Int> cachedSnakePositions = new();
    private readonly HashSet<Vector2Int> cachedBodyTailPositions = new();
    private readonly List<FallingEntity> entitiesToDestroy = new();
    
    void Start(){
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();
        snakeController ??= FindFirstObjectByType<SnakeController>();
        encounterGenerator ??= FindFirstObjectByType<EncounterGenerator>();
        
        Initialize();
        if(TickManager.Instance != null) TickManager.Instance.OnCCMTick += OnCCMTick;
    }
    
    void OnDestroy(){
        if(TickManager.Instance != null) TickManager.Instance.OnCCMTick -= OnCCMTick;
    }
    
    void OnCCMTick(){
        if(!isInitialized || !enableMovement) return;
        if(!GameStateManager.Instance.IsGameActive()) return;
        ShiftAllDown();
    }
    
    void Initialize(){
        entities.Clear();
        newPositions.Clear();
        cachedSnakePositions.Clear();
        cachedBodyTailPositions.Clear();
        entitiesToDestroy.Clear();
        isInitialized = true;
    }
    
    public void AddEntity(Vector2Int pos, GridGenerator.TileType type, bool isOutline = false){
        entities.Add(new FallingEntity(pos, type, isOutline));
        gridGenerator.SetTileType(pos, type);
    }
    
    public void RemoveEntity(Vector2Int pos){
        for(int i = 0; i < entities.Count; i++){
            if(entities[i].position == pos){
                entities.RemoveAt(i);
                return;
            }
        }
    }
    
    void ShiftAllDown(){
        if(!isInitialized) return;

        newPositions.Clear();
        cachedSnakePositions.Clear();
        cachedBodyTailPositions.Clear();
        entitiesToDestroy.Clear();
        
        Vector2Int cachedHeadPosition = Vector2Int.zero;
        GridGenerator.TileType headHitType = GridGenerator.TileType.Empty;
        GridGenerator.TileType bodyHitType = GridGenerator.TileType.Empty;
        bool snakeAtBottom = false;
        
        if(snakeController != null && snakeController.IsAlive()){
            cachedHeadPosition = snakeController.GetCurrentGridPosition();
            List<Vector2Int> snakeSegments = snakeController.GetSegmentPositions();
            foreach(Vector2Int pos in snakeSegments){
                cachedSnakePositions.Add(pos);
                if(pos != cachedHeadPosition) cachedBodyTailPositions.Add(pos);
                if(pos.y == 0) snakeAtBottom = true;
            }
        }
        
        for(int i = 0; i < entities.Count; i++){
            FallingEntity entity = entities[i];
            Vector2Int newPos = new Vector2Int(entity.position.x, entity.position.y - 1);
            
            if(newPos.y >= 0){
                bool wouldHitSnake = cachedSnakePositions.Contains(newPos);
                
                if(wouldHitSnake && snakeAtBottom){
                    entitiesToDestroy.Add(entity);
                    gridGenerator.SetTileType(entity.position, GridGenerator.TileType.Empty);
                    if(bodyHitType == GridGenerator.TileType.Empty) bodyHitType = entity.type;
                }
                else if(newPos == cachedHeadPosition){
                    if(headHitType == GridGenerator.TileType.Empty) headHitType = entity.type;
                    entitiesToDestroy.Add(entity);
                    gridGenerator.SetTileType(entity.position, GridGenerator.TileType.Empty);
                }
                else if(cachedBodyTailPositions.Contains(newPos)){
                    if(bodyHitType == GridGenerator.TileType.Empty) bodyHitType = entity.type;
                    entitiesToDestroy.Add(entity);
                    gridGenerator.SetTileType(entity.position, GridGenerator.TileType.Empty);
                }
            }
            else{
                entitiesToDestroy.Add(entity);
                gridGenerator.SetTileType(entity.position, GridGenerator.TileType.Empty);
            }
        }
        
        foreach(FallingEntity entity in entitiesToDestroy) entities.Remove(entity);
        
        if(headHitType != GridGenerator.TileType.Empty) snakeController?.PushDown(true, headHitType);
        else if(bodyHitType != GridGenerator.TileType.Empty) snakeController?.PushDown(false, bodyHitType);
        
        for(int i = 0; i < entities.Count; i++){
            FallingEntity entity = entities[i];
            Vector2Int newPos = new Vector2Int(entity.position.x, entity.position.y - 1);
            bool isWall = entity.type == GridGenerator.TileType.Wall;
            
            gridGenerator.SetTileType(entity.position, GridGenerator.TileType.Empty);
            
            if(newPos.y >= 0){
                GridGenerator.TileType targetType = gridGenerator.GetTileType(newPos);
                bool isSnake = targetType == GridGenerator.TileType.SnakeHead || 
                               targetType == GridGenerator.TileType.SnakeBody || 
                               targetType == GridGenerator.TileType.SnakeTail;
                
                if(isSnake){
                    if(isWall){
                        newPositions.Add(new FallingEntity(entity.position, entity.type, entity.isOutline));
                        gridGenerator.SetTileType(entity.position, entity.type);
                    }
                    else newPositions.Add(new FallingEntity(newPos, entity.type, entity.isOutline));
                }
                else newPositions.Add(new FallingEntity(newPos, entity.type, entity.isOutline));
            }
        }
        
        entities.Clear();
        
        for(int i = 0; i < newPositions.Count; i++){
            FallingEntity entity = newPositions[i];
            entities.Add(entity);
            gridGenerator.SetTileType(entity.position, entity.type);
        }
        
        for(int i = 0; i < entities.Count; i++){
            if(entities[i].position.y < 0){
                entities.RemoveAt(i);
                i--;
            }
        }
    }
    
    public void ClearAllEntities(){
        if(!isInitialized) return;
        
        for(int i = 0; i < entities.Count; i++)
            gridGenerator.SetTileType(entities[i].position, GridGenerator.TileType.Empty);

        entities.Clear();
    }
    
    public void EnableMovement() => enableMovement = true;
    public void DisableMovement() => enableMovement = false;
    public bool IsInitialized() => isInitialized;
    
    public int GetEntityCount() => entities.Count;
    
    public bool HasEntityAt(Vector2Int pos){
        foreach(var entity in entities){
            if(entity.position == pos) return true;
        }
        return false;
    }
    
    public bool IsOutlineWallAt(Vector2Int pos){
        foreach(var entity in entities){
            if(entity.position == pos && entity.type == GridGenerator.TileType.Wall)
                return entity.isOutline;
        }
        return false;
    }
}