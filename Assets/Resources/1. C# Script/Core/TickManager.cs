using UnityEngine;
using System;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }
    
    [Header("SETTINGS")]
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private int snakeMovesPerCCMShift = 2;
    
    public event Action OnSnakeTick;
    public event Action OnCCMTick;
    
    private float timer = 0f;
    private int snakeMoveCount = 0;
    private bool isActive = true;
    
    void Awake(){
        if(Instance == null){
            Instance = this;
            return;
        }
        Destroy(gameObject);
    }
    
    void Update(){
        if(!isActive) return;
        if(GameStateManager.Instance == null) return;
        if(!GameStateManager.Instance.IsGameActive()) return;
        
        timer += Time.deltaTime;
        if(timer >= tickInterval){
            timer = 0f;
            ProcessTick();
        }
    }
    
    void ProcessTick(){
        OnSnakeTick?.Invoke();
        snakeMoveCount++;
        
        if(snakeMoveCount >= snakeMovesPerCCMShift){
            snakeMoveCount = 0;
            OnCCMTick?.Invoke();
        }
    }
    
    public void SetActive(bool active) => isActive = active;
    public void ResetTickCount() => snakeMoveCount = 0;
    public float GetTickInterval() => tickInterval;
    public void SetTickInterval(float interval) => tickInterval = Mathf.Max(0.05f, interval);
}