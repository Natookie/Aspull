using UnityEngine;
using Nova;

public class ScoreUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextBlock elapsedTimeText;
    [SerializeField] private TextBlock snakeLengthText;
    [SerializeField] private TextBlock FPSText;
    [SerializeField] private TextBlock[] healthText;
    [SerializeField] private TextBlock healthValueText;
    [Space(10)]
    [SerializeField] private TextBlock highScoreText;

    [Header("REFERENCES")]
    [SerializeField] private SnakeController snakeController;
    [SerializeField] private SnakeStatus snakeStatus;

    [Header("HEALTH SETTINGS")]
    [SerializeField] private string filledHealthChar = "█";
    [SerializeField] private string emptyHealthChar = "░";
    [SerializeField] private bool useGradient = true;
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private float gradientIntensity = 0.8f;

    private const string HIGH_SCORE_KEY = "HighScore";
    private float elapsedTime;
    private float currentHighScore;
    private int currentSnakeLength;
    private int prevSnakeLength;
    private float deltaTime;
    private float fpsUpdateInterval = 0.5f;
    private float fpsTimer;
    private int frameCount;
    private float currentFPS;
    private bool isGameActive;
    private int maxHealth;
    private int currentHealth;

    void Start(){
        currentSnakeLength = snakeController.GetSnakeLength();
        prevSnakeLength = currentSnakeLength;
        snakeLengthText.Text = currentSnakeLength.ToString();
        elapsedTimeText.Text = "0s";
        FPSText.Text = "0 FPS";
        
        maxHealth = healthText.Length;
        
        if(snakeStatus != null){
            currentHealth = snakeStatus.health;
            UpdateHealthDisplay(currentHealth);
        }

        LoadHighScore();
        UpdateHighScoreDisplay();
    }

    void Update(){
        isGameActive = GameStateManager.Instance.IsGameActive();
        if(!isGameActive) return;

        HandleElapsedTime();
        HandleSnakeLength();
        HandleFPS();
        HandleHighScore();
        
        if(snakeStatus != null && currentHealth != snakeStatus.health){
            currentHealth = snakeStatus.health;
            UpdateHealthDisplay(currentHealth);
        }
    }

    void HandleElapsedTime(){
        elapsedTime += Time.deltaTime;
        UpdateElapsedTimeDisplay();
    }

    void UpdateElapsedTimeDisplay(){
        if(elapsedTime < 60f){
            elapsedTimeText.Text = $"{elapsedTime:F1}s";
        }
        else{
            float minutes = Mathf.FloorToInt(elapsedTime / 60f);
            float seconds = elapsedTime % 60f;
            elapsedTimeText.Text = $"{minutes}:{seconds:F0}s";
        }
    }

    void HandleHighScore(){
        if(elapsedTime > currentHighScore){
            currentHighScore = elapsedTime;
            UpdateHighScoreDisplay();
            SaveHighScore();
        }
    }

    void UpdateHighScoreDisplay(){
        if(highScoreText == null) return;
        
        if(currentHighScore < 60f) highScoreText.Text = $"HIGH SCORE: {currentHighScore:F1}s";
        else{
            float minutes = Mathf.FloorToInt(currentHighScore / 60f);
            float seconds = currentHighScore % 60f;
            highScoreText.Text = $"HIGH SCORE: {minutes}:{seconds:F0}s";
        }
    }

    void HandleSnakeLength(){
        currentSnakeLength = snakeController.GetSnakeLength();
        if(currentSnakeLength == prevSnakeLength) return;
        
        snakeLengthText.Text = currentSnakeLength.ToString();
        prevSnakeLength = currentSnakeLength;
    }

    void HandleFPS(){
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        fpsTimer += Time.unscaledDeltaTime;
        frameCount++;
        
        if(fpsTimer >= fpsUpdateInterval){
            currentFPS = frameCount / fpsTimer;
            FPSText.Text = $"{Mathf.RoundToInt(currentFPS)} FPS";
            
            fpsTimer = 0f;
            frameCount = 0;
        }
    }
    
    public void UpdateHealthDisplay(int health){
        int healthValue = Mathf.Clamp(health, 0, maxHealth);
        healthValueText.Text = $"{healthValue * 10}%";

        for(int i = 0; i < maxHealth; i++){
            if(healthText[i] != null){
                if(i < healthValue) healthText[i].Text = filledHealthChar;
                else healthText[i].Text = emptyHealthChar;
                
                if(useGradient){
                    float healthPercent = (float)healthValue / maxHealth;
                    float positionT = (float)i / (maxHealth - 1);
                    
                    if(i < healthValue){
                        float intensity = 1f - (healthPercent * (1f - gradientIntensity));
                        Color finalColor = Color.Lerp(damagedColor, healthyColor, positionT);
                        finalColor *= intensity;
                        healthText[i].Color = finalColor;
                    }
                    else{
                        Color fadedColor = Color.Lerp(damagedColor, healthyColor, positionT);
                        fadedColor.a = 0.3f;
                        healthText[i].Color = fadedColor;
                    }
                }
                else if(i >= healthValue) healthText[i].Color = damagedColor * 0.5f;
            }
        }
    }
    
    public void ResetTimer(){
        elapsedTime = 0f;
        UpdateElapsedTimeDisplay();
    }

    public void SaveHighScore(){
        PlayerPrefs.SetFloat(HIGH_SCORE_KEY, currentHighScore);
        PlayerPrefs.Save();
    }

    public void LoadHighScore() => currentHighScore = PlayerPrefs.GetFloat(HIGH_SCORE_KEY, 0f);
    public void ResetHighScore(){
        currentHighScore = 0f;
        UpdateHighScoreDisplay();
        SaveHighScore();
    }

    public float GetHighScore() => currentHighScore;
    public float GetCurrentScore() => elapsedTime;
}