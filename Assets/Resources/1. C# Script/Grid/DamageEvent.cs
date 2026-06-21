using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageEvent : MonoBehaviour
{
    public enum DamageZoneType{
        Segment00, //Bottom-Left
        Segment01, //Bottom-Middle
        Segment02, //Bottom-Right
        Segment10, //Middle-Left
        Segment11, //Middle-Middle
        Segment12, //Middle-Right
        Segment20, //Top-Left
        Segment21, //Top-Middle
        Segment22  //Top-Right
    }

    [Header("REFERENCES")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private SnakeStatus snakeStatus;
    [SerializeField] private SnakeController snakeController;
    [SerializeField] private LightEffect lightEffect;

    [Header("DAMAGE ZONE SETTINGS")]
    [SerializeField] private Transform damageGridParent;
    [SerializeField] private float minTimeBetweenEvents = 3f;
    [SerializeField] private float maxTimeBetweenEvents = 8f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float damageDuration = 1.5f;
    [SerializeField] private float flickerSpeed = 0.1f;
    [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("DIFFICULTY SETTINGS")]
    [SerializeField] private int minSegments = 1;
    [SerializeField] private int maxSegments = 2;
    [SerializeField] [Range(0f, 1f)] private float snakeHeadTargetChance = 0.5f;

    [Header("DEBUG")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private bool forceDamageZone = false;
    [SerializeField] private DamageZoneType forceZoneType = DamageZoneType.Segment00;

    private SpriteRenderer[] allDamageTiles;
    private Vector2Int gridSize;
    private bool isEventActive = false;
    private bool isInitialized = false;
    private Coroutine eventCoroutine;
    
    private SpriteRenderer[] currentDamageZone;
    private int currentDamageZoneCount = 0;
    
    private Dictionary<Vector2Int, SpriteRenderer> tileRendererMap = new Dictionary<Vector2Int, SpriteRenderer>();
    
    private Vector2Int[][] cachedZonePositions;
    private HashSet<Vector2Int>[] cachedZoneHashSets;
    
    private int zoneTypeCount;
    
    private List<int> availableSegments = new List<int>(9);
    private List<int> selectedSegments = new List<int>(2);
    private List<int> zoneIndices = new List<int>(2);
    private List<DamageZoneType> tempZoneList = new List<DamageZoneType>(1);
    
    private int cachedSegmentWidth;
    private int cachedSegmentHeight;
    private int cachedMaxZoneSize;

    void Start(){
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();
        snakeStatus ??= FindFirstObjectByType<SnakeStatus>();
        snakeController ??= FindFirstObjectByType<SnakeController>();
        lightEffect ??= FindFirstObjectByType<LightEffect>();

        if(gridGenerator == null || damageGridParent == null || snakeStatus == null || snakeController == null){
            Debug.LogError("Missing required references!");
            return;
        }

        zoneTypeCount = System.Enum.GetValues(typeof(DamageZoneType)).Length;
        StartCoroutine(Init());
    }

    IEnumerator Init(){
        int maxAttempts = 30;
        int attempts = 0;
        
        while(!gridGenerator.IsInitialized() && attempts < maxAttempts){
            yield return null;
            attempts++;
        }
        
        if(!gridGenerator.IsInitialized()){
            Debug.LogError("GridGenerator failed to initialize!");
            yield break;
        }

        gridSize = gridGenerator.GetGridSize();
        
        if(gridSize == Vector2Int.zero){
            Debug.LogError("Grid size is zero!");
            yield break;
        }

        cachedSegmentWidth = gridSize.x / 3;
        cachedSegmentHeight = gridSize.y / 3;

        yield return null;
        
        CacheAllDamageTiles();
        DisableAllDamageTiles();
        CacheAllZonePositions();
        
        cachedMaxZoneSize = Mathf.Max((gridSize.x / 3 + 1) * (gridSize.y / 3 + 1) * maxSegments, 1);
        currentDamageZone = new SpriteRenderer[cachedMaxZoneSize];
        
        isInitialized = true;
        
        if(eventCoroutine != null) StopCoroutine(eventCoroutine);
        eventCoroutine = StartCoroutine(EventLoop());
    }

    void Update(){
        if(!isInitialized || !GameStateManager.Instance.IsGameActive()) return;
        
        if(enableDebug && forceDamageZone && !isEventActive){
            forceDamageZone = false;
            tempZoneList.Clear();
            tempZoneList.Add(forceZoneType);
            StartDamageZones(tempZoneList);
        }
    }

    void CacheAllDamageTiles(){
        allDamageTiles = damageGridParent.GetComponentsInChildren<SpriteRenderer>();
        tileRendererMap.Clear();

        if(allDamageTiles.Length == 0){
            Debug.LogWarning("No damage tiles found!");
            return;
        }

        tileRendererMap.EnsureCapacity(allDamageTiles.Length);

        for(int i = 0; i < allDamageTiles.Length; i++){
            SpriteRenderer sr = allDamageTiles[i];
            string[] parts = sr.gameObject.name.Split('_');
            if(parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y)){
                tileRendererMap[new Vector2Int(x, y)] = sr;
                sr.color = Color.clear;
            }
        }
    }

    void DisableAllDamageTiles(){
        for(int i = 0; i < allDamageTiles.Length; i++){
            allDamageTiles[i].enabled = false;
            allDamageTiles[i].color = Color.clear;
        }
    }

    void CacheAllZonePositions(){
        cachedZonePositions = new Vector2Int[zoneTypeCount][];
        cachedZoneHashSets = new HashSet<Vector2Int>[zoneTypeCount];

        int remainderX = gridSize.x % 3;
        int remainderY = gridSize.y % 3;

        for(int zoneIndex = 0; zoneIndex < zoneTypeCount; zoneIndex++){
            int row = zoneIndex / 3;
            int col = zoneIndex % 3;
            
            int startX = col * cachedSegmentWidth;
            int endX = (col + 1) * cachedSegmentWidth;
            int startY = row * cachedSegmentHeight;
            int endY = (row + 1) * cachedSegmentHeight;
            
            if(col == 2) endX += remainderX;
            if(row == 2) endY += remainderY;
            
            endX = Mathf.Min(endX, gridSize.x);
            endY = Mathf.Min(endY, gridSize.y);
            
            int positionCount = (endX - startX) * (endY - startY);
            Vector2Int[] positions = new Vector2Int[positionCount];
            HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(positionCount);
            
            int index = 0;
            for(int x = startX; x < endX; x++){
                for(int y = startY; y < endY; y++){
                    Vector2Int pos = new Vector2Int(x, y);
                    positions[index] = pos;
                    hashSet.Add(pos);
                    index++;
                }
            }

            cachedZonePositions[zoneIndex] = positions;
            cachedZoneHashSets[zoneIndex] = hashSet;
        }
    }

    int GetSegmentIndexFromPosition(Vector2Int position){
        int col = position.x / cachedSegmentWidth;
        int row = position.y / cachedSegmentHeight;
        col = col > 2 ? 2 : col;
        row = row > 2 ? 2 : row;
        return row * 3 + col;
    }

    void SelectRandomSegments(List<int> output){
        output.Clear();
        
        int segmentCount = Random.Range(minSegments, maxSegments + 1);
        bool targetHead = Random.value < snakeHeadTargetChance;
        
        if(targetHead && snakeStatus.IsAlive()){
            Vector2Int headPos = snakeController.GetCurrentGridPosition();
            int headSegment = GetSegmentIndexFromPosition(headPos);
            output.Add(headSegment);
            
            if(segmentCount > 1){
                availableSegments.Clear();
                for(int i = 0; i < zoneTypeCount; i++){
                    if(i != headSegment){
                        availableSegments.Add(i);
                    }
                }
                
                int availableCount = availableSegments.Count;
                for(int i = 0; i < availableCount && output.Count < segmentCount; i++){
                    int randomIndex = Random.Range(i, availableCount);
                    int temp = availableSegments[i];
                    availableSegments[i] = availableSegments[randomIndex];
                    availableSegments[randomIndex] = temp;
                    output.Add(availableSegments[i]);
                }
            }
        }
        else{
            availableSegments.Clear();
            for(int i = 0; i < zoneTypeCount; i++){
                availableSegments.Add(i);
            }
            
            int availableCount = availableSegments.Count;
            for(int i = 0; i < availableCount && output.Count < segmentCount; i++){
                int randomIndex = Random.Range(i, availableCount);
                int temp = availableSegments[i];
                availableSegments[i] = availableSegments[randomIndex];
                availableSegments[randomIndex] = temp;
                output.Add(availableSegments[i]);
            }
        }
    }

    bool CanStartEvent(){
        if(!GameStateManager.Instance.IsGameActive()) return false;
        if(!snakeStatus.IsAlive()) return false;
        if(isEventActive) return false;
        if(lightEffect != null && !lightEffect.IsDarkEventOnCooldown()) return false;
        return true;
    }

    IEnumerator EventLoop(){
        while(true){
            float waitTime = Random.Range(minTimeBetweenEvents, maxTimeBetweenEvents);
            yield return new WaitForSeconds(waitTime);

            if(CanStartEvent()){
                SelectRandomSegments(selectedSegments);
                
                zoneIndices.Clear();
                for(int i = 0; i < selectedSegments.Count; i++) zoneIndices.Add(selectedSegments[i]);
                StartDamageZonesInternal(zoneIndices);
            }
        }
    }

    void StartDamageZones(List<DamageZoneType> zoneTypes){
        if(!CanStartEvent()) return;
        
        zoneIndices.Clear();
        for(int i = 0; i < zoneTypes.Count; i++) zoneIndices.Add((int)zoneTypes[i]);
        StartDamageZonesInternal(zoneIndices);
    }

    void StartDamageZonesInternal(List<int> zoneIndices){
        isEventActive = true;
        currentDamageZoneCount = 0;

        for(int z = 0; z < zoneIndices.Count; z++){
            int zoneIndex = zoneIndices[z];
            Vector2Int[] zonePositions = cachedZonePositions[zoneIndex];
            
            if(zonePositions == null || zonePositions.Length == 0){
                Debug.LogError($"Zone positions empty for index {zoneIndex}");
                continue;
            }

            for(int i = 0; i < zonePositions.Length; i++){
                if(tileRendererMap.TryGetValue(zonePositions[i], out SpriteRenderer sr)){
                    sr.enabled = true;
                    sr.color = warningColor;
                    
                    if(currentDamageZoneCount >= currentDamageZone.Length){
                        System.Array.Resize(ref currentDamageZone, currentDamageZone.Length * 2);
                    }
                    currentDamageZone[currentDamageZoneCount] = sr;
                    currentDamageZoneCount++;
                }
            }
        }

        if(currentDamageZoneCount == 0){
            Debug.LogError("No SpriteRenderers found for selected zones!");
            isEventActive = false;
            return;
        }

        StartCoroutine(DamageSequence(zoneIndices));
    }

    IEnumerator DamageSequence(List<int> zoneIndices){
        float elapsed = 0f;
        bool damageDealt = false;
        
        HashSet<Vector2Int> combinedZoneHashSet = new HashSet<Vector2Int>();
        for(int z = 0; z < zoneIndices.Count; z++){
            Vector2Int[] positions = cachedZonePositions[zoneIndices[z]];
            for(int i = 0; i < positions.Length; i++){
                combinedZoneHashSet.Add(positions[i]);
            }
        }
        
        int zoneCount = currentDamageZoneCount;

        while(elapsed < warningDuration){
            if(!GameStateManager.Instance.IsGameActive() || !CanContinueEvent()){
                yield return CleanupZone(zoneCount);
                yield break;
            }

            float flickerPhase = Mathf.Sin(elapsed / flickerSpeed * Mathf.PI * 2);
            float t = (flickerPhase + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.2f, 0.8f, t);
            Color currentColor = warningColor;
            currentColor.a = alpha;

            for(int i = 0; i < zoneCount; i++){
                currentDamageZone[i].color = currentColor;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if(!GameStateManager.Instance.IsGameActive() || !CanContinueEvent()){
            yield return CleanupZone(zoneCount);
            yield break;
        }

        elapsed = 0f;

        for(int i = 0; i < zoneCount; i++){
            currentDamageZone[i].color = damageColor;
        }

        while(elapsed < damageDuration && !damageDealt){
            if(!GameStateManager.Instance.IsGameActive() || !CanContinueEvent()){
                break;
            }

            Vector2Int snakeHeadPos = snakeController.GetCurrentGridPosition();
            if(combinedZoneHashSet.Contains(snakeHeadPos)){
                snakeStatus.TakeDamage(5);
                damageDealt = true;
                
                for(int i = 0; i < zoneCount; i++){
                    currentDamageZone[i].color = Color.red;
                }
                yield return new WaitForSeconds(0.2f);
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if(!GameStateManager.Instance.IsGameActive() || !CanContinueEvent()){
            yield return CleanupZone(zoneCount);
            yield break;
        }

        if(!damageDealt){
            for(int i = 0; i < zoneCount; i++) currentDamageZone[i].color = damageColor;
            yield return new WaitForSeconds(0.3f);
        }

        yield return CleanupZone(zoneCount);
    }

    bool CanContinueEvent(){
        if(lightEffect != null && !lightEffect.IsDarkEventOnCooldown()) return false;
        return true;
    }

    IEnumerator CleanupZone(int zoneCount){
        for(int i = 0; i < zoneCount; i++){
            currentDamageZone[i].enabled = false;
            currentDamageZone[i].color = Color.clear;
        }

        currentDamageZoneCount = 0;
        isEventActive = false;

        yield return new WaitForSeconds(0.5f);
    }

    public void StopCurrentEvent(){
        if(!isEventActive) return;
        
        StopAllCoroutines();
        
        for(int i = 0; i < currentDamageZoneCount; i++){
            currentDamageZone[i].enabled = false;
            currentDamageZone[i].color = Color.clear;
        }
        
        currentDamageZoneCount = 0;
        isEventActive = false;
        
        if(eventCoroutine != null) StopCoroutine(eventCoroutine);
        eventCoroutine = StartCoroutine(EventLoop());
    }

    public bool IsEventActive() => isEventActive;
    public bool IsInitialized() => isInitialized;
    
    public List<Vector2Int> GetCurrentDamageZonePositions(){
        List<Vector2Int> positions = new List<Vector2Int>(currentDamageZoneCount);
        for(int i = 0; i < currentDamageZoneCount; i++){
            string[] parts = currentDamageZone[i].gameObject.name.Split('_');
            if(parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y)){
                positions.Add(new Vector2Int(x, y));
            }
        }
        return positions;
    }

    void OnDestroy(){
        if(eventCoroutine != null) StopCoroutine(eventCoroutine);
        StopAllCoroutines();
    }
}