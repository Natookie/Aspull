using UnityEngine;
using Nova;
using System.Collections;

public class SnakeUI : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private SnakeStatus snakeStatus;

    [Header("SHIELD SETTINGS")]
    [SerializeField] private float durationFill = 20f;
    [SerializeField] private int maxShields = 5;

    [Header("UI")]
    [SerializeField] private UIBlock2D[] shieldIcons;
    [SerializeField] private UIBlock2D shieldFill;

    [Header("COLORS")]
    [SerializeField] private Color shieldActiveColor = Color.white;
    [SerializeField] private Color shieldInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private float currentTimer = 0f;
    private int currentShieldCount = 0;
    private Coroutine shieldCoroutine;
    private bool isShieldActive = false;

    void Start(){
        snakeStatus ??= FindFirstObjectByType<SnakeStatus>();
        
        if(snakeStatus == null){
            Debug.LogError("SnakeStatus not found!");
            return;
        }

        for(int i = 0; i < shieldIcons.Length; i++){
            shieldIcons[i].Color = shieldInactiveColor;
        }

        if(shieldFill != null) shieldFill.Size.Y.Percent = 0f;

        if(GameStateManager.Instance != null && GameStateManager.Instance.IsGameActive())
            StartShieldTimer();
    }

    void Update(){
        if(GameStateManager.Instance != null && GameStateManager.Instance.IsGameActive() && !isShieldActive)
            StartShieldTimer();
        
        
        if(GameStateManager.Instance != null && !GameStateManager.Instance.IsGameActive() && isShieldActive)
            PauseShieldTimer();
    }

    void OnDestroy(){
        if(shieldCoroutine != null){
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }
    }

    void StartShieldTimer(){
        if(shieldCoroutine != null){
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }

        isShieldActive = true;
        currentTimer = 0f;
        shieldCoroutine = StartCoroutine(ShieldTimerCoroutine());
    }

    void PauseShieldTimer(){
        isShieldActive = false;
        if(shieldCoroutine != null){
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }
    }

    IEnumerator ShieldTimerCoroutine(){
        while(isShieldActive && currentTimer < durationFill){
            currentTimer += Time.deltaTime;
            
            if(shieldFill != null){
                float Percent = Mathf.Clamp01(currentTimer / durationFill);
                shieldFill.Size.Y.Percent = Percent;
            }

            yield return null;
        }

        if(isShieldActive && currentTimer >= durationFill){
            AddShield();
            StartShieldTimer();
        }
    }

    void AddShield(){
        if(currentShieldCount >= maxShields) return;

        if(currentShieldCount < shieldIcons.Length){
            shieldIcons[currentShieldCount].Color = shieldActiveColor;
            currentShieldCount++;
            snakeStatus.shieldCount = currentShieldCount;
            
            currentTimer = 0f;
            if(shieldFill != null) shieldFill.Size.Y.Percent = 0f;

            AudioManager.Instance.PlayShieldGain();
        }
    }

    public void Interrupt(){
        currentTimer = 0f;
        
        if(shieldFill != null) shieldFill.Size.Y.Percent = 0f;
        if(isShieldActive && GameStateManager.Instance != null && GameStateManager.Instance.IsGameActive()){
            if(shieldCoroutine != null){
                StopCoroutine(shieldCoroutine);
                shieldCoroutine = null;
            }
            shieldCoroutine = StartCoroutine(ShieldTimerCoroutine());
        }
    }

    public void DeductShield(){
        if(currentShieldCount <= 0) return;

        currentShieldCount--;
        snakeStatus.shieldCount = currentShieldCount;
        shieldIcons[currentShieldCount].Color = shieldInactiveColor;

        Interrupt();
    }

    public void ResetAllShields(){
        for(int i = 0; i < shieldIcons.Length; i++){
            shieldIcons[i].Color = shieldInactiveColor;
        }

        currentShieldCount = 0;
        currentTimer = 0f;

        if(shieldFill != null) shieldFill.Size.Y.Percent = 0f;
        if(isShieldActive && GameStateManager.Instance != null && GameStateManager.Instance.IsGameActive()){
            if(shieldCoroutine != null){
                StopCoroutine(shieldCoroutine);
                shieldCoroutine = null;
            }
            shieldCoroutine = StartCoroutine(ShieldTimerCoroutine());
        }
    }

    public void SetShieldActive(bool active){
        isShieldActive = active;
        
        if(!active){
            if(shieldCoroutine != null){
                StopCoroutine(shieldCoroutine);
                shieldCoroutine = null;
            }
            
            if(shieldFill != null) shieldFill.Size.Y.Percent = 0f;
        }
        else if(GameStateManager.Instance != null && GameStateManager.Instance.IsGameActive())
            StartShieldTimer();
    }

    public int GetCurrentShieldCount() => currentShieldCount;
    public int GetMaxShields() => maxShields;
    public float GetCurrentTimer() => currentTimer;
    public float GetDurationFill() => durationFill;
    public bool IsShieldActive() => isShieldActive;
}