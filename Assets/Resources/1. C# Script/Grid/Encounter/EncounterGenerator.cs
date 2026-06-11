using UnityEngine;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using EncounterSystem;

public class EncounterGenerator : MonoBehaviour
{
    [System.Serializable]
    public class ItemDefinition{
        public GridGenerator.TileType tileType;
        public int weight = 10;
    }
    
    [Header("REFERENCES")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private ColumnCompressionMovement wallMovement;
    
    [Header("ENCOUNTER SETTINGS")]
    [SerializeField] private DifficultySettings difficulty;
    [SerializeField] private List<EncounterDefinition> encounterPool;
    
    [Header("SPAWNING")]
    [SerializeField] private int rowsPerChunk = 3;
    
    [Header("ITEM SPAWNING")]
    [SerializeField] private List<ItemDefinition> spawnableItems;
    [SerializeField, Range(0, 1)] private float itemsPerRowChance = 0.3f;
    [SerializeField] private int maxItemsPerRow = 2;
    
    [Header("DEBUG")]
    [SerializeField] private bool disableAutoGeneration = false;
    [SerializeField] private EncounterType debugEncounterType = EncounterType.WaveTunnel;
    [SerializeField] private int debugSpawnRowCount = 5;
    
    private bool isInitialized = false;
    private int rowsUntilNextEncounter = 0;
    private EncounterDefinition currentEncounter;
    private int encounterProgress = 0;
    
    private float[] corridorCenters;
    private float[] targetCorridorCenters;
    private float[] corridorWidths;
    private float[] corridorPhases;
    
    private List<Vector2Int> spawnedWallsCache;
    
    private int cachedGridWidth;
    private int cachedGridHeight;
    private int topRow;
    private int maxX;
    private CorridorSegment[] activeSegments;
    private int activeSegmentCount = 0;
    private CorridorSegment[] mergedSegmentsBuffer;
    private bool[] isOpen;
    private int minCorridorWidthInt;
    private int corridorCount;
    
    private EncounterType? lastNotifiedEncounter = null;
    private int totalItemWeight = 0;
    private bool isDebugEncounterActive = false;
    private bool isManualSpawning = false;
    private int manualSpawnRemaining = 0;
    private EncounterType queuedManualEncounter;
    private float manualSpawnTimer = 0f;
    private float manualSpawnInterval = 0.5f;
    
    private int[] availableColumnsBuffer;
    
    void Start(){
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();
        wallMovement ??= FindFirstObjectByType<ColumnCompressionMovement>();
        StartCoroutine(InitializeCoroutine());
        
        if(TickManager.Instance != null) TickManager.Instance.OnCCMTick += OnCCMTick;
    }
    
    void OnDestroy(){
        if(TickManager.Instance != null) TickManager.Instance.OnCCMTick -= OnCCMTick;
    }
    
    void OnCCMTick(){
        if(!isInitialized) return;
        if(!GameStateManager.Instance.IsGameActive()) return;
        if(disableAutoGeneration) return;
        
        GenerateRows();
    }
    
    void Update(){
        if(!isInitialized) return;
        if(!GameStateManager.Instance.IsGameActive()) return;
        
        UpdateCorridorDrift();
        
        if(isManualSpawning){
            manualSpawnTimer += Time.deltaTime;
            if(manualSpawnTimer >= manualSpawnInterval){
                manualSpawnTimer = 0f;
                if(manualSpawnRemaining > 0){
                    GenerateSingleRowManual(queuedManualEncounter);
                    manualSpawnRemaining--;
                    if(manualSpawnRemaining <= 0){
                        isManualSpawning = false;
                        Debug.Log($"Manual spawn complete. Generated {debugSpawnRowCount} rows.");
                    }
                }
            }
        }
    }
    
    IEnumerator InitializeCoroutine(){
        while(gridGenerator == null){
            gridGenerator = FindFirstObjectByType<GridGenerator>();
            yield return null;
        }
        
        while(!gridGenerator.IsInitialized()) yield return null;
        Initialize();
    }
    
    void Initialize(){
        cachedGridWidth = gridGenerator.GetGridSize().x;
        cachedGridHeight = gridGenerator.GetGridSize().y;
        topRow = cachedGridHeight - 1;
        maxX = cachedGridWidth - 1;
        corridorCount = difficulty.corridorCount;
        minCorridorWidthInt = (int)difficulty.minCorridorWidth;
        
        corridorCenters = new float[corridorCount];
        targetCorridorCenters = new float[corridorCount];
        corridorWidths = new float[corridorCount];
        corridorPhases = new float[corridorCount];
        activeSegments = new CorridorSegment[corridorCount];
        mergedSegmentsBuffer = new CorridorSegment[corridorCount];
        isOpen = new bool[cachedGridWidth];
        spawnedWallsCache = new List<Vector2Int>(cachedGridWidth);
        
        availableColumnsBuffer = new int[cachedGridWidth];
        
        CalculateTotalItemWeight();
        InitializeCorridors();
        
        if(encounterPool == null || encounterPool.Count == 0) InitializeDefaultEncounterPool();
        
        ScheduleNextEncounter();
        isInitialized = true;
    }
    
    void InitializeCorridors(){
        float totalWidth = difficulty.corridorSpacing * (corridorCount - 1);
        float startX = (cachedGridWidth - totalWidth) * 0.5f;
        float baseWidth = difficulty.baseCorridorWidth;
        float maxCenter = cachedGridWidth - baseWidth;
        
        for(int i = 0; i < corridorCount; i++){
            float center = startX + i * difficulty.corridorSpacing;
            center = center < baseWidth ? baseWidth : (center > maxCenter ? maxCenter : center);
            corridorCenters[i] = center;
            targetCorridorCenters[i] = center;
            corridorWidths[i] = baseWidth;
            corridorPhases[i] = Random.Range(0f, 360f);
        }
    }
    
    void InitializeDefaultEncounterPool(){
        encounterPool = new List<EncounterDefinition>{
            new EncounterDefinition{ type = EncounterType.WaveTunnel, duration = 5, intensity = 0.3f },
            new EncounterDefinition{ type = EncounterType.GradualSqueeze, duration = 8, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.ZigZagCorridor, duration = 6, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.SplitPath, duration = 4, intensity = 0.3f },
            new EncounterDefinition{ type = EncounterType.SpiralDrift, duration = 7, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.AlternatingPressure, duration = 6, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.PinchPoint, duration = 4, intensity = 0.6f },
            new EncounterDefinition{ type = EncounterType.Accordion, duration = 6, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.ScissorDoors, duration = 5, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.Heartbeat, duration = 8, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.Corkscrew, duration = 7, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.Mirror, duration = 5, intensity = 0.3f },
            new EncounterDefinition{ type = EncounterType.Tornado, duration = 6, intensity = 0.6f },
            new EncounterDefinition{ type = EncounterType.Pendulum, duration = 7, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.Staggered, duration = 5, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.Funnel, duration = 6, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.Ripple, duration = 7, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.Serpentine, duration = 8, intensity = 0.4f },
            new EncounterDefinition{ type = EncounterType.CrissCross, duration = 6, intensity = 0.5f },
            new EncounterDefinition{ type = EncounterType.Whirlpool, duration = 5, intensity = 0.6f },
            new EncounterDefinition{ type = EncounterType.ShiftWave, duration = 6, intensity = 0.5f }
        };
    }
    
    void CalculateTotalItemWeight(){
        totalItemWeight = 0;
        for(int i = 0; i < spawnableItems.Count; i++) totalItemWeight += spawnableItems[i].weight;
    }
    
    #region ENCOUNTER MANAGEMENT
    void ScheduleNextEncounter(){
        if(isDebugEncounterActive || disableAutoGeneration) return;
        
        float rand = Random.value;
        if(rand < difficulty.clearSectionChance){
            currentEncounter = new EncounterDefinition { type = EncounterType.ClearSection, duration = 3, intensity = 0f };
            NotifyEncounterStart(currentEncounter.type);
        }
        else if(rand < difficulty.encounterFrequency){
            currentEncounter = encounterPool[Random.Range(0, encounterPool.Count)];
            NotifyEncounterStart(currentEncounter.type);
        }
        else if(currentEncounter != null){
            NotifyEncounterEnd();
            currentEncounter = null;
        }
        
        rowsUntilNextEncounter = Random.Range(5, 15);
        encounterProgress = 0;
    }
    
    void NotifyEncounterStart(EncounterType type){
        if(lastNotifiedEncounter == type) return;
        lastNotifiedEncounter = type;
        GameManager.Instance?.NotifyEncounterStarted(type);
    }
    
    void NotifyEncounterEnd(){
        if(lastNotifiedEncounter == null) return;
        lastNotifiedEncounter = null;
        GameManager.Instance?.NotifyEncounterEnded();
    }
    
    [Button("Spawn Debug Encounter", EButtonEnableMode.Playmode)]
    void SpawnDebugEncounter(){
        if(!disableAutoGeneration){
            Debug.LogWarning("Auto generation is enabled. Disable 'disableAutoGeneration' to use manual spawn.");
            return;
        }
        
        if(isManualSpawning){
            Debug.LogWarning("Already manually spawning. Wait for current spawn to complete.");
            return;
        }
        
        queuedManualEncounter = debugEncounterType;
        manualSpawnRemaining = debugSpawnRowCount;
        isManualSpawning = true;
        manualSpawnTimer = 0f;
        Debug.Log($"Starting manual spawn of {debugSpawnRowCount} rows with encounter: {debugEncounterType}");
    }
    #endregion
    
    #region CORRIDOR GENERATION
    void UpdateCorridorDrift(){
        float timeDelta = Time.deltaTime;
        float driftSpeed = difficulty.driftSpeed;
        
        for(int i = 0; i < corridorCount; i++){
            float phaseVal = corridorPhases[i];
            float drift = Mathf.Sin(phaseVal * 0.7f) * 0.8f + Mathf.Sin(phaseVal * 1.3f) * 0.2f;
            targetCorridorCenters[i] += drift * driftSpeed * timeDelta;
            
            float maxCenter = cachedGridWidth - corridorWidths[i];
            float minCenter = corridorWidths[i];
            targetCorridorCenters[i] = targetCorridorCenters[i] < minCenter ? minCenter : (targetCorridorCenters[i] > maxCenter ? maxCenter : targetCorridorCenters[i]);
            
            corridorCenters[i] += (targetCorridorCenters[i] - corridorCenters[i]) * 0.1f;
            corridorPhases[i] += timeDelta * 0.5f;
        }
    }
    
    void GenerateRows(){
        for(int x = 0; x < cachedGridWidth; x++){
            if(gridGenerator.GetTileType(new Vector2Int(x, topRow)) == GridGenerator.TileType.Wall) return;
        }
        
        for(int i = 0; i < rowsPerChunk; i++){
            if(!GenerateSingleRow(false)) break;
        }
        
        if(currentEncounter != null){
            encounterProgress++;
            float progress = (float)encounterProgress / currentEncounter.duration;
            GameManager.Instance?.NotifyEncounterProgress(progress);
            
            if(encounterProgress >= currentEncounter.duration){
                currentEncounter = null;
                NotifyEncounterEnd();
                if(isDebugEncounterActive){
                    isDebugEncounterActive = false;
                    if(!disableAutoGeneration) ScheduleNextEncounter();
                }
            }
        }
        
        rowsUntilNextEncounter--;
        if(rowsUntilNextEncounter <= 0 && currentEncounter == null && !isDebugEncounterActive && !disableAutoGeneration) ScheduleNextEncounter();
    }
    
    bool GenerateSingleRow(bool isManualSpawn){
        float intensity;
        int progress;
        int duration;
        EncounterType? encounterType;
        
        if(isManualSpawn){
            intensity = 0.5f;
            progress = 0;
            duration = 1;
            encounterType = queuedManualEncounter;
        }
        else{
            intensity = currentEncounter?.intensity ?? 0f;
            progress = encounterProgress;
            duration = currentEncounter?.duration ?? 1;
            encounterType = currentEncounter?.type;
        }
        
        float progressNormalized = duration > 0 ? (float)progress / duration : 0f;
        float curveIntensity = (currentEncounter != null && !isManualSpawn) ? currentEncounter.intensityCurve.Evaluate(progressNormalized) : 1f;
        float finalIntensity = intensity * curveIntensity;
        bool hasEncounter = encounterType != null && encounterType != EncounterType.ClearSection;
        
        activeSegmentCount = 0;
        
        for(int i = 0; i < corridorCount; i++){
            float effectiveWidth = corridorWidths[i];
            float effectiveCenter = corridorCenters[i];
            
            if(hasEncounter){
                EncounterContext context = new EncounterContext(progress, duration, finalIntensity, corridorCenters, corridorWidths, difficulty);
                PatternLibrary.ApplyPattern(encounterType.Value, ref effectiveWidth, ref effectiveCenter, i, context);
            }
            
            if(effectiveWidth < difficulty.minCorridorWidth) effectiveWidth = difficulty.minCorridorWidth;
            
            float maxCenter = cachedGridWidth - effectiveWidth;
            effectiveCenter = effectiveCenter < effectiveWidth ? effectiveWidth : (effectiveCenter > maxCenter ? maxCenter : effectiveCenter);
            
            int left = (int)(effectiveCenter - effectiveWidth * 0.5f);
            int right = (int)(effectiveCenter + effectiveWidth * 0.5f);
            left = left < 0 ? 0 : (left > maxX ? maxX : left);
            right = right < 0 ? 0 : (right > maxX ? maxX : right);
            
            int width = right - left;
            if(width < minCorridorWidthInt){
                int mid = (left + right) >> 1;
                int half = (minCorridorWidthInt + 1) >> 1;
                left = mid - half;
                right = mid + half;
                if(left < 0) { left = 0; right = minCorridorWidthInt; }
                if(right >= cachedGridWidth) { right = maxX; left = right - minCorridorWidthInt; }
            }
            
            activeSegments[activeSegmentCount++] = new CorridorSegment { left = left, right = right, intensity = finalIntensity, corridorIndex = i };
        }
        
        activeSegmentCount = MergeOverlappingSegments();
        
        System.Array.Clear(isOpen, 0, cachedGridWidth);
        
        for(int s = 0; s < activeSegmentCount; s++){
            var seg = activeSegments[s];
            for(int x = seg.left; x <= seg.right; x++) isOpen[x] = true;
        }
        
        bool hasValidSegment = false;
        for(int s = 0; s < activeSegmentCount; s++){
            var seg = activeSegments[s];
            int currentRun = 0, bestRun = 0;
            for(int x = seg.left; x <= seg.right; x++){
                if(isOpen[x]){
                    currentRun++;
                    if(currentRun > bestRun) bestRun = currentRun;
                }
                else currentRun = 0;
            }
            if(bestRun >= minCorridorWidthInt) { hasValidSegment = true; break; }
        }
        
        if(!hasValidSegment) return false;
        
        spawnedWallsCache.Clear();
        for(int x = 0; x < cachedGridWidth; x++) if(!isOpen[x]) spawnedWallsCache.Add(new Vector2Int(x, topRow));
        
        for(int s = 0; s < activeSegmentCount; s++){
            var seg = activeSegments[s];
            if(seg.intensity <= 0f) continue;
            
            int corridorWidthInt = seg.right - seg.left + 1;
            int insideWallsCount = (int)(corridorWidthInt * seg.intensity * 0.3f);
            
            for(int i = 0; i < insideWallsCount; i++){
                int randomX = Random.Range(seg.left, seg.right + 1);
                if(randomX == seg.left || randomX == seg.right) continue;
                
                bool exists = false;
                for(int j = 0; j < spawnedWallsCache.Count; j++) if(spawnedWallsCache[j].x == randomX) { exists = true; break; }
                if(!exists) spawnedWallsCache.Add(new Vector2Int(randomX, topRow));
            }
            
            if(seg.intensity > 0.5f){
                bool leftExists = false, rightExists = false;
                for(int j = 0; j < spawnedWallsCache.Count; j++){
                    if(spawnedWallsCache[j].x == seg.left) leftExists = true;
                    if(spawnedWallsCache[j].x == seg.right) rightExists = true;
                }
                if(!leftExists && Random.value < 0.4f) spawnedWallsCache.Add(new Vector2Int(seg.left, topRow));
                if(!rightExists && Random.value < 0.4f) spawnedWallsCache.Add(new Vector2Int(seg.right, topRow));
            }
        }
        
        for(int i = 0; i < spawnedWallsCache.Count; i++) isOpen[spawnedWallsCache[i].x] = false;
        
        hasValidSegment = false;
        for(int s = 0; s < activeSegmentCount; s++){
            var seg = activeSegments[s];
            int currentRun = 0, bestRun = 0;
            for(int x = seg.left; x <= seg.right; x++){
                if(isOpen[x]){
                    currentRun++;
                    if(currentRun > bestRun) bestRun = currentRun;
                }
                else currentRun = 0;
            }
            if(bestRun >= minCorridorWidthInt) { hasValidSegment = true; break; }
        }
        
        if(!hasValidSegment) return false;
        
        for(int i = 0; i < spawnedWallsCache.Count; i++){
            var wallPos = spawnedWallsCache[i];
            gridGenerator.SetTileType(wallPos, GridGenerator.TileType.Wall);
            wallMovement?.AddEntity(wallPos, GridGenerator.TileType.Wall);
        }
        
        SpawnItemsInRow();
        
        return true;
    }
    
    void GenerateSingleRowManual(EncounterType encounterType){
        var previousEncounter = currentEncounter;
        var previousProgress = encounterProgress;
        
        currentEncounter = new EncounterDefinition { type = encounterType, duration = 1, intensity = 0.5f };
        encounterProgress = 0;
        
        GenerateSingleRow(true);
        
        currentEncounter = previousEncounter;
        encounterProgress = previousProgress;
    }
    
    int MergeOverlappingSegments(){
        if(activeSegmentCount <= 1) return activeSegmentCount;
        
        for(int i = 0; i < activeSegmentCount - 1; i++){
            for(int j = i + 1; j < activeSegmentCount; j++){
                if(activeSegments[i].left > activeSegments[j].left){
                    var temp = activeSegments[i];
                    activeSegments[i] = activeSegments[j];
                    activeSegments[j] = temp;
                }
            }
        }
        
        int mergeCount = 0;
        var current = activeSegments[0];
        
        for(int i = 1; i < activeSegmentCount; i++){
            if(activeSegments[i].left <= current.right + 2){
                if(activeSegments[i].right > current.right) current.right = activeSegments[i].right;
                if(activeSegments[i].intensity > current.intensity) current.intensity = activeSegments[i].intensity;
            }
            else{
                mergedSegmentsBuffer[mergeCount++] = current;
                current = activeSegments[i];
            }
        }
        mergedSegmentsBuffer[mergeCount++] = current;
        
        for(int i = 0; i < mergeCount; i++) activeSegments[i] = mergedSegmentsBuffer[i];
        
        return mergeCount;
    }
    #endregion
    
    #region ITEM SPAWNING
    void SpawnItemsInRow(){
        if(spawnableItems == null || spawnableItems.Count == 0) return;
        if(Random.value > itemsPerRowChance) return;
        
        int availableCount = 0;
        for(int x = 0; x < cachedGridWidth; x++){
            if(isOpen[x] && gridGenerator.GetTileType(new Vector2Int(x, topRow)) == GridGenerator.TileType.Empty)
                availableColumnsBuffer[availableCount++] = x;
        }
        
        if(availableCount == 0) return;
        
        int itemsToSpawn = Random.Range(1, Mathf.Min(maxItemsPerRow + 1, availableCount + 1));
        for(int i = 0; i < itemsToSpawn; i++){
            if(availableCount == 0) break;
            
            int idx = Random.Range(0, availableCount);
            int selectedX = availableColumnsBuffer[idx];
            availableColumnsBuffer[idx] = availableColumnsBuffer[--availableCount];
            
            Vector2Int spawnPos = new Vector2Int(selectedX, topRow);
            ItemDefinition selectedItem = GetRandomItem();
            
            if(selectedItem != null){
                gridGenerator.SetTileType(spawnPos, selectedItem.tileType);
                wallMovement?.AddEntity(spawnPos, selectedItem.tileType);
            }
        }
    }
    
    ItemDefinition GetRandomItem(){
        if(spawnableItems.Count == 0) return null;
        
        int randomValue = Random.Range(0, totalItemWeight);
        int cumulative = 0;
        
        for(int i = 0; i < spawnableItems.Count; i++){
            cumulative += spawnableItems[i].weight;
            if(randomValue < cumulative) return spawnableItems[i];
        }
        
        return spawnableItems[0];
    }
    
    public void OnItemConsumed(Vector2Int tile) => EventSystem.OnItemConsumed?.Invoke(tile);
    #endregion
    
    #region PUBLIC METHODS
    public void UpdateDifficulty(float progress){
        difficulty.baseCorridorWidth = Mathf.Lerp(4f, 2.5f, progress);
        difficulty.driftSpeed = Mathf.Lerp(0.3f, 1.2f, progress);
        difficulty.encounterFrequency = Mathf.Lerp(0.2f, 0.6f, progress);
        difficulty.clearSectionChance = Mathf.Lerp(0.3f, 0.1f, progress);
    }
    
    public void ResetGeneration(){
        rowsUntilNextEncounter = 0;
        encounterProgress = 0;
        currentEncounter = null;
        isDebugEncounterActive = false;
        InitializeCorridors();
        ScheduleNextEncounter();
    }
    
    public float GetCorridorCenter() => corridorCenters.Length > 0 ? corridorCenters[0] : 0;
    public float GetCorridorWidth() => corridorWidths.Length > 0 ? corridorWidths[0] : 0;
    public bool IsInitialized() => isInitialized;
    public EncounterType? GetCurrentEncounter() => lastNotifiedEncounter;
    #endregion
}