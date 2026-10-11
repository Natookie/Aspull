using UnityEngine;
using Nova;
using System.Collections;

public class ScoreUI : MonoBehaviour
{
    [Header("SIDE PANEL UI")]
    [SerializeField] private TextBlock elapsedTimeText;
    [SerializeField] private TextBlock snakeLengthText;
    [SerializeField] private TextBlock FPSText;
    [SerializeField] private TextBlock[] healthText;
    [SerializeField] private TextBlock healthValueText;
    [Space(10)]
    [SerializeField] private TextBlock highScoreText;

    [Header("FINAL SCORE UI")]
    [SerializeField] private UIBlock2D finalScoreBlock;
    [SerializeField] private TextBlock finalWordText;
    [SerializeField] private TextBlock finalScoreText;
    private float finalScoreBlockOriginalY;

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
    private Coroutine finalScoreAnimationCoroutine;

    void Awake(){
        if(finalScoreBlock != null){
            finalScoreBlockOriginalY = finalScoreBlock.Position.Value.y;
            finalScoreBlock.Position.Y.Value = -1000;
        }
    }

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
        if(elapsedTime < 60f) elapsedTimeText.Text = $"{elapsedTime:F1}s";
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

    private static readonly (float MaxTime, string[] Messages)[] finalWords =
    {
        (20f, new[] {
            "What a noob",
            "Did you even try?",
            "Are your eyes closed?",
            "Even a snail lasts longer",
            "That was embarrassing",
            "Game over before it began",
            "Did you blink and miss it?",
            "My pet rock survived longer",
            "That was painful to watch",
            "Is this your first day?"
        }),
        
        (40f, new[] {
            "Barely a warm-up",
            "Is this your first time?",
            "My grandma lasts longer",
            "You lasted 20 seconds... in dog years",
            "That was painful to watch",
            "Room temperature IQ",
            "Better luck next century",
            "That's cute... in a sad way",
            "Almost impressive... NOT",
            "You're getting there... no you're not"
        }),
        
        (60f, new[] {
            "Mediocrity at its finest",
            "Not terrible, not great",
            "You survived... barely",
            "Decent if you're 5 years old",
            "Average at best",
            "The bare minimum",
            "Could be worse... could be better too",
            "I'm not mad, I'm just disappointed",
            "Do you need a participation trophy?",
            "That's one way to play... a bad way"
        }),
        
        (80f, new[] {
            "Not bad. Not good either.",
            "You tried. That's something.",
            "Acceptable, I guess",
            "Could be better",
            "You did the thing. Congratulations.",
            "Barely passed the test",
            "Serviceable",
            "You get a C-",
            "It's a score, I suppose",
            "Below average, but above terrible"
        }),
        
        (100f, new[] {
            "Okay, you're not completely useless",
            "I've seen worse. Much worse.",
            "That wasn't painful... surprisingly",
            "You might have potential",
            "Decent. Just decent.",
            "You're improving... slowly",
            "Not embarrassing",
            "Survivable performance",
            "You're learning",
            "Progress, I guess"
        }),
        
        (120f, new[] {
            "Alright, you can play",
            "You're not terrible anymore",
            "That was competent",
            "You know what you're doing... mostly",
            "Solid, but don't let it go to your head",
            "You survived. Good for you.",
            "Respectable, I guess",
            "You've got hands, I'll give you that",
            "Not bad at all... for a noob",
            "You're getting dangerous... slightly"
        }),
        
        (140f, new[] {
            "You're actually decent",
            "That was clean. I'm surprised.",
            "You might be good at this",
            "Okay, I see you... barely",
            "That was almost impressive",
            "You're not bad. There, I said it.",
            "You've got some skill... unfortunately",
            "I'm not even mad, that was good",
            "You're a threat now",
            "Well, well, well... look who learned"
        }),
        
        (160f, new[] {
            "You're good. Annoyingly good.",
            "That was genuinely impressive",
            "Fine, you've got talent",
            "I hate that you're this good",
            "You're a worthy opponent",
            "That was surgical",
            "You made that look easy... jerk",
            "Okay, you're actually skilled",
            "Respect earned. Barely.",
            "You're not a noob anymore. Congrats."
        }),
        
        (180f, new[] {
            "You're actually cracked",
            "That was elite... I hate to admit",
            "You're a problem now",
            "Who even are you?",
            "You're different from the rest",
            "That was flawless... disgusting",
            "You don't belong here",
            "Too good for this game",
            "You're a menace",
            "I'm scared... slightly"
        }),
        
        (200f, new[] {
            "You're genuinely insane",
            "That was unreal... annoying",
            "You're not human, are you?",
            "Stop being this good",
            "You make it look boring",
            "That was clinical",
            "You've transcended noob status",
            "Who hurt you to make you this good?",
            "You're the final boss now",
            "Unreal. I hate you."
        }),
        
        (220f, new[] {
            "You're a legend. Ugh.",
            "That was legendary... unfortunately",
            "You're too good for this world",
            "Even I couldn't do that",
            "You're one of the best I've seen",
            "That was brutal... for the game",
            "You're the reason games are hard",
            "Save some skill for the rest of us",
            "You're overqualified",
            "This is your game now"
        }),
        
        (240f, new[] {
            "You're untouchable... lucky you",
            "That was flawless. Whatever.",
            "You broke the game. Hope you're happy.",
            "You're too good. It's annoying.",
            "I'm not impressed... okay, I am",
            "That was immaculate... disgustingly so",
            "You could win tournaments",
            "You're the best. Don't let it get to you.",
            "That was perfect. Ugh.",
            "You're a machine. A very annoying one."
        }),
        
        (260f, new[] {
            "You're a god. How boring.",
            "That was celestial. Whatever.",
            "You're unbeatable. How annoying.",
            "Even the game gave up on you",
            "You're a legend. I'm not impressed.",
            "That was superhuman. Yawn.",
            "You've mastered it. Moving on.",
            "There's nothing left for you here",
            "You're the pinnacle. So what?",
            "That was phenomenal. I guess."
        }),
        
        (280f, new[] {
            "You're immortal. How dull.",
            "Five minutes? That's it?",
            "Time means nothing to you now",
            "You've transcended. Cool.",
            "That was eternal. Whatever.",
            "You're beyond gaming. Boring.",
            "You are the game now. So what?",
            "That was transcendent. I suppose.",
            "You're legendary. If that matters.",
            "You broke time itself. Neat."
        }),
        
        (300f, new[] {
            "Five minutes. You're done. Finally.",
            "That took forever. You okay?",
            "You survived. Who cares.",
            "Was that supposed to impress me?",
            "Five minutes. Move on with your life.",
            "You're persistent. Annoyingly so.",
            "Game over. You can leave now.",
            "Five minutes. That's it?",
            "You did it. No one cares.",
            "Finally. What took you so long?"
        }),
        
        (float.MaxValue, new[] {
            "You're still here? Don't you have things to do?",
            "Go touch grass",
            "We get it, you're good. Now go away.",
            "Who hurt you?",
            "You need a hobby... outside",
            "Seek help",
            "That's enough gaming for a lifetime",
            "Get a life",
            "You've peaked. It's all downhill from here.",
            "We're all impressed. Now leave."
        })
    };

    public void HandleFinalScore(){
        finalWordText.Text = GetFinalWord(elapsedTime);
        finalScoreText.Text = $"Your Time: {elapsedTimeText.Text}";
        if(finalScoreAnimationCoroutine != null) StopCoroutine(finalScoreAnimationCoroutine);
        finalScoreAnimationCoroutine = StartCoroutine(AnimateFinalScoreIn());
    }

    public void HideFinalScore(){
        if(finalScoreAnimationCoroutine != null) StopCoroutine(finalScoreAnimationCoroutine);
        finalScoreAnimationCoroutine = StartCoroutine(AnimateFinalScoreOut());
    }

    private IEnumerator AnimateFinalScoreIn(){
        float duration = 0.5f;
        float elapsed = 0f;
        float startY = -1000f;
        float targetY = finalScoreBlockOriginalY;
        
        while(elapsed < duration){
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            Length3 newPos = finalScoreBlock.Position;
            newPos.Y = Mathf.Lerp(startY, targetY, t);
            finalScoreBlock.Position = newPos;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Length3 finalPos = finalScoreBlock.Position;
        finalPos.Y = targetY;
        finalScoreBlock.Position = finalPos;
    }

    private IEnumerator AnimateFinalScoreOut(){
        float duration = 0.3f;
        float elapsed = 0f;
        float startY = finalScoreBlock.Position.Value.y;
        float targetY = -1000f;
        
        while(elapsed < duration){
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            Length3 newPos = finalScoreBlock.Position;
            newPos.Y = Mathf.Lerp(startY, targetY, t);
            finalScoreBlock.Position = newPos;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Length3 finalPos = finalScoreBlock.Position;
        finalPos.Y = targetY;
        finalScoreBlock.Position = finalPos;
    }

    public static string GetFinalWord(float survivalTimeSeconds){
        foreach(var category in finalWords){
            if(survivalTimeSeconds <= category.MaxTime){
                int randomIndex = Random.Range(0, category.Messages.Length);
                return category.Messages[randomIndex];
            }
        }
        
        var lastCategory = finalWords[finalWords.Length - 1];
        return lastCategory.Messages[Random.Range(0, lastCategory.Messages.Length)];
    }
}