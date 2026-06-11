using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    
    [SerializeField] private bool immediateRun = false;
    
    public enum GameState{
        Boot,
        Menu,
        Running,
        GameOver,
    }

    [SerializeField] private GameState currentState = GameState.Boot;    
    public System.Action<GameState, GameState> OnStateChanged;
    
    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    void Start(){
        if(currentState != GameState.Boot) return;
        
        if(immediateRun) ChangeState(GameState.Running);
        else ChangeState(GameState.Menu);
    }
    
    public void ChangeState(GameState newState){
        if(currentState == newState) return;
        GameState oldState = currentState;
        
        ExitState(currentState);
        currentState = newState;
        
        EnterState(newState);
        OnStateChanged?.Invoke(oldState, newState);
    }
    
    void ExitState(GameState state){
        switch(state){
            case GameState.Running:
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
                break;
        }
    }
    
    void EnterState(GameState state){
        switch(state){
            case GameState.Menu:
                Time.timeScale = 1f;
                break;
            case GameState.Running:
                Time.timeScale = 1f;
                AudioManager.Instance?.PlayCountDown();
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                RestartGame();
                break;
        }
    }
    
    public void RestartGame(){
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public bool IsGameActive() => currentState == GameState.Running;
    
    public void StartGame() => ChangeState(GameState.Running);
    public void GameOver() => ChangeState(GameState.GameOver);
    public void ReturnToMenu() => ChangeState(GameState.Menu);
    public GameState CurrentState() => currentState;
}