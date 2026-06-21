using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightEffect : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Transform headLight;
    [SerializeField] private Transform shineLight;
    [SerializeField] private Light2D globalLight;
    [SerializeField] private SnakeController snakeController;
    [SerializeField] private GridGenerator gridGenerator;
    
    [Header("SHINE SETTINGS")]
    [SerializeField] private float topPosition = 10f;
    [SerializeField] private float bottomPosition = -5f;
    [SerializeField] private float shineMoveDuration = 1.5f;
    [SerializeField] private float shineCooldownMin = 3f;
    [SerializeField] private float shineCooldownMax = 8f;
    [SerializeField] private bool enableShine = true;
    
    [Header("GLOBAL LIGHT SETTINGS")]
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 0.5f;
    [SerializeField] private float fadeToDarkDuration = 0.5f;
    [SerializeField] private float stayDarkDuration = 1f;
    [SerializeField] private float fadeToLightDuration = 0.5f;
    [SerializeField] private float darkEventCooldownMin = 8f;
    [SerializeField] private float darkEventCooldownMax = 20f;
    [SerializeField] private bool enableGlobalLightEffect = true;
    
    private float shineTimer = 0f;
    private float shineCooldownTimer = 0f;
    private bool isShining = false;
    
    private enum DarkEventState { Cooldown, FadingToDark, StayDark, FadingToLight }
    private DarkEventState darkState = DarkEventState.Cooldown;
    private float darkStateTimer = 0f;
    private float originalIntensity;
    
    void Start(){
        snakeController ??= FindFirstObjectByType<SnakeController>();
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();
        
        if(globalLight != null){
            originalIntensity = globalLight.intensity;
            maxIntensity = originalIntensity;
        }
        
        if(shineLight != null){
            Vector3 startPos = shineLight.position;
            startPos.y = topPosition;
            shineLight.position = startPos;
        }
        
        ResetShineCooldown();
        ResetDarkEventCooldown();
        
        if(TickManager.Instance != null) TickManager.Instance.OnSnakeTick += OnSnakeTick;
    }
    
    void OnDestroy(){
        if(TickManager.Instance != null) TickManager.Instance.OnSnakeTick -= OnSnakeTick;
    }
    
    void OnSnakeTick(){
        if(!enableShine) return;
        UpdateHeadLight();
    }
    
    void Update(){
        if(enableShine) UpdateShineLight();
        if(enableGlobalLightEffect) UpdateGlobalLight();
    }
    
    void UpdateHeadLight(){
        if(headLight == null) return;
        if(snakeController == null) return;
        if(!snakeController.IsAlive()) return;
        
        Vector2Int headGridPos = snakeController.GetCurrentGridPosition();
        if(headGridPos == Vector2Int.zero) return;
        
        if(gridGenerator == null) return;
        Vector3 worldPos = gridGenerator.GridToWorldPosition(headGridPos);
        
        if(headLight.position != worldPos) headLight.position = worldPos;
    }
    
    void UpdateShineLight(){
        if(shineLight == null) return;
        
        if(isShining){
            shineTimer += Time.deltaTime;
            float t = shineTimer / shineMoveDuration;
            
            if(t >= 1f){
                isShining = false;
                shineTimer = 0f;
                ResetShineCooldown();
                
                Vector3 resetPos = shineLight.position;
                resetPos.y = topPosition;
                shineLight.position = resetPos;
                return;
            }
            
            Vector3 newPos = shineLight.position;
            newPos.y = Mathf.Lerp(topPosition, bottomPosition, t);
            shineLight.position = newPos;
        }
        else{
            shineCooldownTimer -= Time.deltaTime;
            if(shineCooldownTimer <= 0f){
                isShining = true;
                shineTimer = 0f;
            }
        }
    }
    
    void UpdateGlobalLight(){
        if(globalLight == null) return;
        
        switch(darkState){
            case DarkEventState.Cooldown:
                darkStateTimer -= Time.deltaTime;
                if(darkStateTimer <= 0f){
                    darkState = DarkEventState.FadingToDark;
                    darkStateTimer = 0f;
                }
                break;
                
            case DarkEventState.FadingToDark:
                darkStateTimer += Time.deltaTime;
                float t1 = darkStateTimer / fadeToDarkDuration;
                if(t1 >= 1f){
                    globalLight.intensity = minIntensity;
                    darkState = DarkEventState.StayDark;
                    darkStateTimer = 0f;
                }
                else{
                    float intensity = Mathf.Lerp(originalIntensity, minIntensity, t1);
                    globalLight.intensity = intensity;
                }
                break;
                
            case DarkEventState.StayDark:
                darkStateTimer += Time.deltaTime;
                if(darkStateTimer >= stayDarkDuration){
                    darkState = DarkEventState.FadingToLight;
                    darkStateTimer = 0f;
                }
                break;
                
            case DarkEventState.FadingToLight:
                darkStateTimer += Time.deltaTime;
                float t2 = darkStateTimer / fadeToLightDuration;
                if(t2 >= 1f){
                    globalLight.intensity = originalIntensity;
                    darkState = DarkEventState.Cooldown;
                    ResetDarkEventCooldown();
                }
                else{
                    float intensity = Mathf.Lerp(minIntensity, originalIntensity, t2);
                    globalLight.intensity = intensity;
                }
                break;
        }
    }
    
    void ResetShineCooldown() => shineCooldownTimer = Random.Range(shineCooldownMin, shineCooldownMax);
    void ResetDarkEventCooldown() => darkStateTimer = Random.Range(darkEventCooldownMin, darkEventCooldownMax);
    
    public void SetShineEnabled(bool enabled){
        enableShine = enabled;
        if(!enabled && shineLight != null){
            shineLight.gameObject.SetActive(false);
            isShining = false;
        }
        else if(enabled && shineLight != null) shineLight.gameObject.SetActive(true);
    }
    
    public void SetHeadLightEnabled(bool enabled){
        if(headLight != null) headLight.gameObject.SetActive(enabled);
    }
    
    public void SetGlobalLightEffectEnabled(bool enabled){
        enableGlobalLightEffect = enabled;
        if(!enabled && globalLight != null){
            globalLight.intensity = originalIntensity;
            darkState = DarkEventState.Cooldown;
        }
    }
    
    public void ResetShine(){
        isShining = false;
        shineTimer = 0f;
        ResetShineCooldown();
        if(shineLight != null){
            Vector3 resetPos = shineLight.position;
            resetPos.y = topPosition;
            shineLight.position = resetPos;
        }
    }
    
    public void TriggerDarkEvent(){
        if(!enableGlobalLightEffect) return;
        if(darkState != DarkEventState.Cooldown) return;
        darkState = DarkEventState.FadingToDark;
        darkStateTimer = 0f;
    }
    
    public void TriggerShine(){
        if(!enableShine) return;
        isShining = true;
        shineTimer = 0f;
    }

    public bool IsDarkEventOnCooldown() => darkState == DarkEventState.Cooldown;
}