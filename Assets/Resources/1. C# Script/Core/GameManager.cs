using UnityEngine;
using EncounterSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("REFERENCES")]
    [SerializeField] private EncounterGenerator encounterGenerator;
    [SerializeField] private EncounterUI encounterUI;
    
    [Header("DIFFICULTY")]
    [SerializeField] private float currentDifficulty = 0f;
    [SerializeField] private float maxDifficulty = 1f;
    [SerializeField] private float difficultyIncreaseRate = 0.01f;
    
    public System.Action<EncounterType> OnEncounterStarted;
    public System.Action OnEncounterEnded;
    public System.Action<float> OnEncounterProgress;
    
    private EncounterType currentEncounterType;
    private float currentEncounterProgress;
    private bool isGameActive = true;
    
    void Awake(){
        if(Instance == null){
            Instance = this;
            return;
        }
        else Destroy(gameObject);
    }
    
    void Start(){
        encounterGenerator ??= FindFirstObjectByType<EncounterGenerator>();
        encounterUI ??= FindFirstObjectByType<EncounterUI>();
    }
    
    void Update(){
        if(isGameActive){
            currentDifficulty += difficultyIncreaseRate * Time.deltaTime;
            currentDifficulty = Mathf.Min(currentDifficulty, maxDifficulty);
        }
    }
    
    public void NotifyEncounterStarted(EncounterType type){
        currentEncounterType = type;
        currentEncounterProgress = 0f;
        OnEncounterStarted?.Invoke(type);
    }
    
    public void NotifyEncounterEnded(){
        currentEncounterProgress = 0f;
        OnEncounterEnded?.Invoke();
    }
    
    public void NotifyEncounterProgress(float progress){
        currentEncounterProgress = progress;
        OnEncounterProgress?.Invoke(progress);
    }
    
    public EncounterType GetCurrentEncounter() => currentEncounterType;
    public float GetCurrentEncounterProgress() => currentEncounterProgress;
    
    public float GetDifficulty() => currentDifficulty;
    public void SetDifficulty(float difficulty) => currentDifficulty = Mathf.Clamp01(difficulty);
    public void ResetDifficulty() => currentDifficulty = 0f;
    
    public bool IsGameActive() => isGameActive;
    public void SetGameActive(bool active) => isGameActive = active;
}