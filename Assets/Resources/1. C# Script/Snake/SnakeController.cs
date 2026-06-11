using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

public class SnakeController : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private EncounterGenerator encounterGenerator;
    [SerializeField] private ColumnCompressionMovement columnCompression;
    [SerializeField] private SnakeStatus status;
    
    [Header("STUCK PREVENTION")]
    [SerializeField] private int maxStuckTicks = 10;
    [SerializeField] private bool enableStuckPrevention = true;
    
    [Header("DEBUG")]
    [SerializeField] private bool enableDebugInput = true;
    [SerializeField] private bool isMovementPaused = false;
    [ReadOnly] public int inputCount;
    [ReadOnly] public int stuckTickCount = 0;
    
    private static readonly Vector2Int[] ALL_DIRECTIONS = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };
    
    private Vector2Int[] positionBuffer;
    private int headIndex;
    private int tailIndex;
    private int bufferSize;
    private int currentLengthCache = 3;
    
    private Vector2Int[] inputBuffer = new Vector2Int[3];
    private Vector2Int moveDirection;
    private Vector2Int lastEnqueuedDirection;
    private Vector2Int nextHeadPosition;
    private Vector2Int lastHeadPosition;

    private bool isAlive => status.isAlive;
    private bool isPushingDown = false;
    private bool pendingGrowth = false;
    private bool pendingShrink = false;
    
    private Vector2Int gridSize;
    private List<Vector2Int> availableDirectionsCache = new List<Vector2Int>(4);
    private List<Vector2Int> tempPositions = new List<Vector2Int>();
    private List<Vector2Int> oldPositionsBuffer = new List<Vector2Int>();
    private List<Vector2Int> newPositionsBuffer = new List<Vector2Int>();
    private HashSet<Vector2Int> occupiedBuffer = new HashSet<Vector2Int>();
    
    void Start(){
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();
        columnCompression ??= FindFirstObjectByType<ColumnCompressionMovement>();
        encounterGenerator ??= FindFirstObjectByType<EncounterGenerator>();
        
        InitializeSnake();
        if(TickManager.Instance != null) TickManager.Instance.OnSnakeTick += OnSnakeTick;
    }
    
    void OnDestroy(){
        if(TickManager.Instance != null) TickManager.Instance.OnSnakeTick -= OnSnakeTick;
    }

    public void ResetSnake(){
        inputCount = 0;
        stuckTickCount = 0;
        isMovementPaused = true;
    }

    void Update(){
        if(!isAlive) return;
        if(!GameStateManager.Instance.IsGameActive()) return;
        
        HandleInput();
        if(enableDebugInput) HandleDebugInput();
    }
    
    void OnSnakeTick(){
        if(!isAlive) return;
        if(isMovementPaused) return;
        
        MoveSnake();
    }
    
    void InitializeSnake(){
        gridSize = gridGenerator.GetGridSize();
        
        bufferSize = 1000;
        positionBuffer = new Vector2Int[bufferSize];
        inputCount = 0;
        stuckTickCount = 0;
        lastEnqueuedDirection = Vector2Int.right;
        
        Vector2Int startPos = new Vector2Int(gridSize.x / 2, gridSize.y / 2);
        
        headIndex = 2;
        tailIndex = 0;
        
        positionBuffer[0] = new Vector2Int(startPos.x - 2, startPos.y);
        positionBuffer[1] = new Vector2Int(startPos.x - 1, startPos.y);
        positionBuffer[2] = startPos;
        
        for(int i = 0; i <= 2; i++) gridGenerator.SetTileType(positionBuffer[i], GridGenerator.TileType.SnakeBody);
        gridGenerator.SetTileType(startPos, GridGenerator.TileType.SnakeHead);
        
        moveDirection = Vector2Int.right;
        isMovementPaused = false;
        pendingGrowth = false;
        pendingShrink = false;
        currentLengthCache = 3;
        lastHeadPosition = startPos;
    }
    
    public Vector2Int GetCurrentGridPosition(){
        if(!isAlive) return Vector2Int.zero;
        return positionBuffer[headIndex];
    }
    
    public Vector2Int GetNextHeadPosition(){
        return nextHeadPosition;
    }
    
    Vector2Int GetPositionAtOffset(int offsetFromHead){
        int index = headIndex - offsetFromHead;
        if(index < 0) index += bufferSize;
        return positionBuffer[index];
    }
    
    void HandleInput(){
        if(isMovementPaused) return;

        bool upPressed    = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool downPressed  = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        bool leftPressed  = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightPressed = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

        if(upPressed)    EnqueueInput(Vector2Int.up);
        if(downPressed)  EnqueueInput(Vector2Int.down);
        if(leftPressed)  EnqueueInput(Vector2Int.left);
        if(rightPressed) EnqueueInput(Vector2Int.right);
    }

    void EnqueueInput(Vector2Int newInput){
        if(newInput == lastEnqueuedDirection) return;
        if(inputCount < 3){
            inputBuffer[inputCount] = newInput;
            inputCount++;
            lastEnqueuedDirection = newInput;
        }
    }
    
    #if UNITY_EDITOR
    void HandleDebugInput(){
        if(Input.GetMouseButtonDown(0)) pendingGrowth = true;
        if(Input.GetMouseButtonDown(1) && currentLengthCache > 3) pendingShrink = true;
        if(Input.GetKeyDown(KeyCode.Space)) ToggleMovementPause();
    }

    void ToggleMovementPause() => isMovementPaused = !isMovementPaused;
    #endif
    
    bool IsValidMove(Vector2Int gridPos){
        if(!gridGenerator.IsValidPosition(gridPos)) return false;
        
        GridGenerator.TileType tileType = gridGenerator.GetTileType(gridPos);
        
        if(tileType == GridGenerator.TileType.Wall) return false;

        if(tileType == GridGenerator.TileType.SnakeHead || 
            tileType == GridGenerator.TileType.SnakeBody || 
            tileType == GridGenerator.TileType.SnakeTail){
            if(pendingGrowth){
                Vector2Int tailPos = GetPositionAtOffset(currentLengthCache - 1);
                if(gridPos == tailPos) return true;
            }
            return false;
        }
        
        return true;
    }
    
    void EmergencyUnstuck(){
        if(encounterGenerator == null || !enableStuckPrevention){
            status.Die();
            return;
        }
        
        int targetX = Mathf.RoundToInt(encounterGenerator.GetCorridorCenter());
        targetX = Mathf.Clamp(targetX, 1, gridSize.x - 2);
        
        Vector2Int currentHeadPos = positionBuffer[headIndex];
        int shiftAmount = targetX - currentHeadPos.x;
        
        if(shiftAmount == 0){
            status.Die();
            return;
        }
        
        tempPositions.Clear();
        
        for(int i = 0; i < currentLengthCache; i++){
            Vector2Int oldPos = GetPositionAtOffset(i);
            Vector2Int newPos = new Vector2Int(oldPos.x + shiftAmount, oldPos.y);
            
            if(!gridGenerator.IsValidPosition(newPos)){
                status.Die();
                return;
            }
            
            tempPositions.Add(newPos);
        }
        
        for(int i = 0; i < currentLengthCache; i++){
            Vector2Int oldPos = GetPositionAtOffset(i);
            gridGenerator.SetTileType(oldPos, GridGenerator.TileType.Empty);
        }
        
        for(int i = 0; i < tempPositions.Count; i++){
            Vector2Int newPos = tempPositions[i];
            
            GridGenerator.TileType type;
            if(i == 0) type = GridGenerator.TileType.SnakeHead;
            else if(i == tempPositions.Count - 1) type = GridGenerator.TileType.SnakeTail;
            else type = GridGenerator.TileType.SnakeBody;
            
            gridGenerator.SetTileType(newPos, type);
            
            int bufferIndex = headIndex - i;
            if(bufferIndex < 0) bufferIndex += bufferSize;
            positionBuffer[bufferIndex] = newPos;
        }
        
        inputCount = 0;
        lastEnqueuedDirection = Vector2Int.zero;
        stuckTickCount = 0;
        
        AudioManager.Instance.PlaySnakeUnstuck();
        status.TakeDamage(2);
    }
    
    void MoveSnake(){
        Vector2Int currentHeadPos = positionBuffer[headIndex];
        if(currentHeadPos == lastHeadPosition){
            stuckTickCount++;
            AudioManager.Instance.PlaySnakeStuck();

            if(enableStuckPrevention && stuckTickCount >= maxStuckTicks){
                EmergencyUnstuck();
                stuckTickCount = 0;
                return;
            }
        }
        else{
            stuckTickCount = 0;
            lastHeadPosition = currentHeadPos;
        }
        
        if(inputCount > 0){
            int consumeIndex = -1;
            
            for(int i = 0; i < inputCount; i++){
                if(inputBuffer[i] != -moveDirection){
                    consumeIndex = i;
                    break;
                }
            }
            
            if(consumeIndex >= 0){
                Vector2Int attemptedDirection = inputBuffer[consumeIndex];
                Vector2Int testPos = positionBuffer[headIndex] + attemptedDirection;
                
                if(IsValidMove(testPos)){
                    moveDirection = attemptedDirection;
                    
                    for(int i = consumeIndex; i < inputCount - 1; i++) inputBuffer[i] = inputBuffer[i + 1];
                    inputCount--;
                    
                    if(inputCount == 0) lastEnqueuedDirection = moveDirection;
                }
                else{
                    for(int i = consumeIndex; i < inputCount - 1; i++) inputBuffer[i] = inputBuffer[i + 1];
                    inputCount--;
                }
            }
        }
        
        nextHeadPosition = positionBuffer[headIndex] + moveDirection;
        Vector2Int newHeadPos = nextHeadPosition;
        
        bool invalidMove = !IsValidMove(newHeadPos);
        bool outOfBounds = !gridGenerator.IsValidPosition(newHeadPos);
        bool hitWall = false;
        bool hitSelf = false;

        if(!outOfBounds){
            GridGenerator.TileType t = gridGenerator.GetTileType(newHeadPos);
            hitWall = t == GridGenerator.TileType.Wall;
            hitSelf = t == GridGenerator.TileType.SnakeBody || t == GridGenerator.TileType.SnakeTail;
        }
        bool shouldRedirect = outOfBounds || (hitWall && moveDirection != Vector2Int.up) || hitSelf;
        
        if(invalidMove){
            if(hitWall && moveDirection == Vector2Int.up){
                PushDown(true, GridGenerator.TileType.Wall);
                return;
            }

            if(shouldRedirect){
                availableDirectionsCache.Clear();

                foreach(Vector2Int dir in ALL_DIRECTIONS){
                    if(dir != -moveDirection){
                        Vector2Int testPos = positionBuffer[headIndex] + dir;
                        if(IsValidMove(testPos)) availableDirectionsCache.Add(dir);
                    }
                }

                if(availableDirectionsCache.Count > 0){
                    moveDirection = availableDirectionsCache[Random.Range(0, availableDirectionsCache.Count)];
                    newHeadPos = positionBuffer[headIndex] + moveDirection;
                    nextHeadPosition = newHeadPos;
                    lastEnqueuedDirection = Vector2Int.zero;
                    inputCount = 0;

                    status.TakeDamage(1);
                }
                else if(enableStuckPrevention){
                    EmergencyUnstuck();
                    return;
                }
                else return;
            }
            else if(enableStuckPrevention){
                EmergencyUnstuck();
                return;
            }
            else return;
        }

        GridGenerator.TileType tileAtNewPos = gridGenerator.GetTileType(newHeadPos);
        if(tileAtNewPos == GridGenerator.TileType.Apple){
            pendingGrowth = true;
            columnCompression?.RemoveEntity(newHeadPos);
            encounterGenerator?.OnItemConsumed(newHeadPos);
            EatApple();
            gridGenerator.SetTileType(newHeadPos, GridGenerator.TileType.SnakeHead);
        }
        else if(tileAtNewPos == GridGenerator.TileType.Poison){
            if(currentLengthCache > 3) pendingShrink = true;
            columnCompression?.RemoveEntity(newHeadPos);
            encounterGenerator?.OnItemConsumed(newHeadPos);
            EatPoison();
            gridGenerator.SetTileType(newHeadPos, GridGenerator.TileType.SnakeHead);
        }
        
        int newHeadIndex = (headIndex + 1) % bufferSize;
        positionBuffer[newHeadIndex] = newHeadPos;
        
        Vector2Int oldHeadPos = positionBuffer[headIndex];
        if(GetSnakeLength() > 1) gridGenerator.SetTileType(oldHeadPos, GridGenerator.TileType.SnakeBody);
        gridGenerator.SetTileType(newHeadPos, GridGenerator.TileType.SnakeHead);
        
        if(pendingShrink){
            Vector2Int oldTailPos = positionBuffer[tailIndex];
            gridGenerator.SetTileType(oldTailPos, GridGenerator.TileType.Empty);
            tailIndex = (tailIndex + 1) % bufferSize;
            currentLengthCache--;
            pendingShrink = false;
        }
        
        if(!pendingGrowth){
            Vector2Int oldTailPos = positionBuffer[tailIndex];
            gridGenerator.SetTileType(oldTailPos, GridGenerator.TileType.Empty);
            tailIndex = (tailIndex + 1) % bufferSize;
            currentLengthCache--;
        }
        else pendingGrowth = false;
        
        headIndex = newHeadIndex;
        currentLengthCache++;
        
        Vector2Int newTailPos = positionBuffer[tailIndex];
        gridGenerator.SetTileType(newTailPos, GridGenerator.TileType.SnakeTail);
    }
    
    public void PushDown(bool isHeadHit = true, GridGenerator.TileType hitType = GridGenerator.TileType.Wall){
        if(!isAlive) return;
        if(isPushingDown) return;

        isPushingDown = true;
        inputCount = 0;
        stuckTickCount = 0;
        lastEnqueuedDirection = Vector2Int.zero;

        int length = currentLengthCache;
        bool isWallHit = hitType == GridGenerator.TileType.Wall;

        if(isHeadHit && isWallHit){
            Vector2Int headPos = GetCurrentGridPosition();
            Vector2Int wallAbove = headPos + Vector2Int.up;

            if(gridGenerator.IsValidPosition(wallAbove) && gridGenerator.GetTileType(wallAbove) == GridGenerator.TileType.Wall){
                columnCompression?.RemoveEntity(wallAbove);
                gridGenerator.SetTileType(wallAbove, GridGenerator.TileType.Empty);
            }
        }

        oldPositionsBuffer.Clear();
        newPositionsBuffer.Clear();
        
        for(int i = 0; i < length; i++) oldPositionsBuffer.Add(GetPositionAtOffset(i));

        for(int i = 0; i < oldPositionsBuffer.Count; i++){
            Vector2Int newPos = new Vector2Int(oldPositionsBuffer[i].x, oldPositionsBuffer[i].y - 1);

            if(newPos.y < 0){
                isPushingDown = false;
                return;
            }

            newPositionsBuffer.Add(newPos);
        }

        if(isHeadHit && isWallHit){
            occupiedBuffer.Clear();
            for(int i = 0; i < newPositionsBuffer.Count; i++){
                Vector2Int pos = newPositionsBuffer[i];

                if(occupiedBuffer.Contains(pos)){
                    isPushingDown = false;
                    return;
                }
                occupiedBuffer.Add(pos);

                GridGenerator.TileType tileType = gridGenerator.GetTileType(pos);
                bool isOwnBody = oldPositionsBuffer.Contains(pos);

                if(tileType == GridGenerator.TileType.Wall){
                    isPushingDown = false;
                    return;
                }

                if((tileType == GridGenerator.TileType.SnakeBody ||
                    tileType == GridGenerator.TileType.SnakeHead ||
                    tileType == GridGenerator.TileType.SnakeTail)
                    && !isOwnBody){
                    isPushingDown = false;
                    return;
                }
            }
        }

        foreach(Vector2Int pos in oldPositionsBuffer) gridGenerator.SetTileType(pos, GridGenerator.TileType.Empty);
        
        bool doReversal = isHeadHit && isWallHit && (oldPositionsBuffer[0].y != oldPositionsBuffer[oldPositionsBuffer.Count - 1].y);
        
        if(doReversal){
            status.TakeDamage(2);
            newPositionsBuffer.Reverse();

            tailIndex = 0;
            headIndex = newPositionsBuffer.Count - 1;

            for(int i = 0; i < newPositionsBuffer.Count; i++) positionBuffer[i] = newPositionsBuffer[newPositionsBuffer.Count - 1 - i];
            if(newPositionsBuffer.Count >= 2){
                Vector2Int newHeadPos = newPositionsBuffer[0];
                Vector2Int newNextPos = newPositionsBuffer[1];
                moveDirection = newHeadPos - newNextPos;
                if(moveDirection == Vector2Int.zero) moveDirection = Vector2Int.down;
            }
            else moveDirection = Vector2Int.down;
        }
        else{
            for(int i = 0; i < newPositionsBuffer.Count; i++){
                int bufferIndex = headIndex - i;
                if(bufferIndex < 0) bufferIndex += bufferSize;
                positionBuffer[bufferIndex] = newPositionsBuffer[i];
            }
            
            if(isHeadHit && !isWallHit){
                if(hitType == GridGenerator.TileType.Apple) EatApple();
                else if(hitType == GridGenerator.TileType.Poison) EatPoison();
            }
        }

        lastEnqueuedDirection = moveDirection;
        pendingGrowth = false;
        pendingShrink = false;

        for(int i = 0; i < newPositionsBuffer.Count; i++){
            GridGenerator.TileType type = GridGenerator.TileType.SnakeBody;
            if(i == 0) type = GridGenerator.TileType.SnakeHead;
            else if(i == newPositionsBuffer.Count - 1) type = GridGenerator.TileType.SnakeTail;
            gridGenerator.SetTileType(newPositionsBuffer[i], type);
        }

        isPushingDown = false;
        
        oldPositionsBuffer.Clear();
        newPositionsBuffer.Clear();
        occupiedBuffer.Clear();
    }
    
    public List<Vector2Int> GetSegmentPositions(){
        List<Vector2Int> result = new List<Vector2Int>();
        for(int i = 0; i < currentLengthCache; i++) result.Add(GetPositionAtOffset(i));
        return result;
    }
    
    public int GetSnakeLength() => currentLengthCache;
    public bool IsAlive() => isAlive;
    public void TogglePause() => ToggleMovementPause();
    public bool IsMovementPaused() => isMovementPaused;
    
    void EatApple(){
        pendingGrowth = true;
        status.RestoreHealth();
        AudioManager.Instance.PlayEatApple();
    }
    
    void EatPoison(){
        if(currentLengthCache > 3) pendingShrink = true;
        AudioManager.Instance.PlayEatPoison();
    }
}