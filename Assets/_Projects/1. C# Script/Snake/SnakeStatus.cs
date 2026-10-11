using UnityEngine;
using NaughtyAttributes;
using System.Collections;

public class SnakeStatus : MonoBehaviour
{
    [Header("HEALTH")]
    [ReadOnly] public int health;
    [ReadOnly] public int shieldCount;
    private const int maxHealth = 10;

    [Header("REFERENCES")]
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private SnakeUI snakeUI;
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private SnakeHitEffect snakeHitEffect;

    [Header("DAMAGE FLICKER")]
    [SerializeField] private float flickerDuration = 0.2f;
    [SerializeField] private int flickerCount = 3;

    public bool isAlive = true;
    [SerializeField] private bool isInvincible = false;
    
    private Coroutine activeFlicker;

    void Start(){
        health = maxHealth;
        isAlive = true;
        
        scoreUI ??= FindFirstObjectByType<ScoreUI>();
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();

        scoreUI?.UpdateHealthDisplay(health);
    }

    public void TakeDamage(int value){
        if(!isAlive) return;
        if(shieldCount > 0){
            snakeUI.DeductShield();
            AudioManager.Instance.PlayShield();
            return;
        }
        snakeUI.Interrupt();
        AudioManager.Instance.PlayDestroyBlock();
        
        health -= value;
        health = Mathf.Clamp(health, 0, maxHealth);
        
        if(scoreUI != null) scoreUI.UpdateHealthDisplay(health);
        
        TriggerDamageFlicker();
        snakeHitEffect?.TriggerHit(value);

        if(health <= 0) Die();
    }

    public void Die(){
        if(isInvincible) return;
        isAlive = false;
        
        scoreUI.HandleFinalScore();
        AudioManager.Instance?.PlayGameOver();
        GameStateManager.Instance.ChangeState(GameStateManager.GameState.Boot);
        gridGenerator?.ClearAllTilesAndGameOver(() => {
            GameStateManager.Instance?.GameOver();
        });
    }

    public void RestoreHealth(){
        if(!isAlive) return;
        
        health++;
        health = Mathf.Clamp(health, 0, maxHealth);
        
        if(scoreUI != null) scoreUI.UpdateHealthDisplay(health);
    }
    
    public void ResetHealth(){
        health = maxHealth;
        isAlive = true;
        
        if(scoreUI != null) scoreUI.UpdateHealthDisplay(health);
    }

    public bool IsInvincible() => isInvincible;
    public void SetInvincible(bool invincible) => isInvincible = invincible;
    public int GetHealth() => health;
    public bool IsAlive() => isAlive;
    
    void TriggerDamageFlicker(){
        if(activeFlicker != null) StopCoroutine(activeFlicker);
        activeFlicker = StartCoroutine(Flicker());
    }
    
    IEnumerator Flicker(){
        float interval = flickerDuration / (flickerCount * 2);
        
        for(int i = 0; i < flickerCount; i++){
            gridGenerator?.SetSnakeHurt(true);
            yield return new WaitForSeconds(interval);
            gridGenerator?.SetSnakeHurt(false);
            yield return new WaitForSeconds(interval);
        }
        
        activeFlicker = null;
    }
}