using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SnakeHitEffect : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Camera thisCam;
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private GameObject bloodParticlePrefab;
    [SerializeField] private SnakeController snakeController;
    [SerializeField] private GridGenerator gridGenerator;

    [Header("SHAKE SETTINGS")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private int shakeCount = 3;

    [Header("TEXT SETTINGS")]
    [SerializeField] private float textLifetime = 0.8f;
    [SerializeField] private float textFallSpeed = 5f;
    [SerializeField] private float textArcHeight = 2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private Gradient lightGradient;

    [Header("PARTICLE SETTINGS")]
    [SerializeField] private float particleLifetime = 1.5f;
    
    private Vector3 originalPosition;
    private Coroutine activeShake;
    private Queue<GameObject> textPool = new Queue<GameObject>();
    private Queue<GameObject> particlePool = new Queue<GameObject>();
    private List<GameObject> activeTexts = new List<GameObject>();
    private List<GameObject> activeParticles = new List<GameObject>();
    
    void Start(){
        if(thisCam != null) originalPosition = thisCam.transform.position;
        snakeController ??= FindFirstObjectByType<SnakeController>();
        gridGenerator ??= FindFirstObjectByType<GridGenerator>();
    }
    
    public void TriggerHit(int damageAmount){
        TriggerShake();
        SpawnDamageText(damageAmount);
        SpawnParticle();
    }
    
    void TriggerShake(){
        if(activeShake != null) StopCoroutine(activeShake);
        activeShake = StartCoroutine(Shake());
    }
    
    IEnumerator Shake(){
        if(thisCam == null) yield break;
        
        originalPosition = thisCam.transform.position;
        float interval = shakeDuration / (shakeCount * 2);
        
        for(int i = 0; i < shakeCount; i++){
            float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
            float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
            thisCam.transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
            yield return new WaitForSeconds(interval);
            
            thisCam.transform.position = originalPosition;
            yield return new WaitForSeconds(interval);
        }
        
        thisCam.transform.position = originalPosition;
        activeShake = null;
    }
    
    void SpawnDamageText(int damageAmount){
        GameObject textObj = GetFromTextPool();
        textObj.SetActive(true);
        
        Vector2Int headPos = snakeController.GetCurrentGridPosition();
        Vector3 worldPos = gridGenerator.GridToWorldPosition(headPos);
        worldPos.z = -1;
        
        float direction = Random.value > 0.5f ? 1f : -1f;
        
        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        if(tmp != null){
            tmp.text = damageAmount.ToString();
            Color ambientLight = RenderSettings.ambientLight;
            tmp.color = new Color(1f, 0.3f, 0.2f, 1f) * ambientLight;
        }
        
        Light textLight = textObj.GetComponent<Light>();
        if(textLight != null){
            textLight.intensity = 0.5f;
            textLight.color = Color.red;
        }
        
        Transform textTransform = textObj.transform;
        textTransform.position = worldPos;
        textTransform.localScale = Vector3.one * 0.5f;
        textTransform.rotation = Quaternion.identity;
        
        activeTexts.Add(textObj);
        StartCoroutine(AnimateDamageText(textObj, direction, worldPos));
        StartCoroutine(ReturnTextToPoolAfterDelay(textObj, textLifetime));
    }
    
    void SpawnParticle(){
        GameObject particleObj = GetFromParticlePool();
        particleObj.SetActive(true);
        
        Vector2Int headPos = snakeController.GetCurrentGridPosition();
        Vector3 worldPos = gridGenerator.GridToWorldPosition(headPos);
        worldPos.z = -1;
        
        Transform particleTransform = particleObj.transform;
        particleTransform.position = worldPos;
        particleTransform.rotation = Quaternion.identity;
        
        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        if(ps != null){
            ps.Play();
        }
        
        activeParticles.Add(particleObj);
        StartCoroutine(ReturnParticleToPoolAfterDelay(particleObj, particleLifetime));
    }
    
    IEnumerator AnimateDamageText(GameObject textObj, float direction, Vector3 startPos){
        if(textObj == null) yield break;
        
        Transform textTransform = textObj.transform;
        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        Light textLight = textObj.GetComponent<Light>();
        float elapsed = 0f;
        
        while(elapsed < textLifetime && textObj != null && textObj.activeSelf){
            elapsed += Time.deltaTime;
            float t = elapsed / textLifetime;
            
            float popScale = 1f + Mathf.Sin(Mathf.Min(elapsed * 20f, Mathf.PI)) * 0.3f;
            float finalScale = scaleCurve.Evaluate(t) * popScale * 0.5f;
            
            float arcX = startPos.x + (direction * textArcHeight * Mathf.Sin(t * Mathf.PI));
            float y = startPos.y - (textFallSpeed * elapsed);
            
            textTransform.position = new Vector3(arcX, y, startPos.z);
            textTransform.localScale = Vector3.one * finalScale;
            
            if(textLight != null){
                textLight.intensity = 0.5f * (1f - t);
            }
            
            if(tmp != null){
                Color color = tmp.color;
                color.a = 1f - t;
                tmp.color = color;
            }
            
            yield return null;
        }
    }
    
    IEnumerator ReturnTextToPoolAfterDelay(GameObject textObj, float delay){
        yield return new WaitForSeconds(delay);
        if(textObj != null && textObj.activeSelf){
            ReturnTextToPool(textObj);
        }
    }
    
    IEnumerator ReturnParticleToPoolAfterDelay(GameObject particleObj, float delay){
        yield return new WaitForSeconds(delay);
        if(particleObj != null && particleObj.activeSelf){
            ReturnParticleToPool(particleObj);
        }
    }
    
    GameObject GetFromTextPool(){
        while(textPool.Count > 0){
            GameObject obj = textPool.Dequeue();
            if(obj != null) return obj;
        }
        
        GameObject newObj = Instantiate(damageTextPrefab);
        return newObj;
    }
    
    GameObject GetFromParticlePool(){
        while(particlePool.Count > 0){
            GameObject obj = particlePool.Dequeue();
            if(obj != null) return obj;
        }
        
        GameObject newObj = Instantiate(bloodParticlePrefab);
        return newObj;
    }
    
    void ReturnTextToPool(GameObject textObj){
        if(textObj == null) return;
        
        textObj.SetActive(false);
        
        for(int i = 0; i < activeTexts.Count; i++){
            if(activeTexts[i] == textObj){
                activeTexts.RemoveAt(i);
                break;
            }
        }
        
        textPool.Enqueue(textObj);
    }
    
    void ReturnParticleToPool(GameObject particleObj){
        if(particleObj == null) return;
        
        particleObj.SetActive(false);
        
        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        if(ps != null){
            ps.Stop();
            ps.Clear();
        }
        
        for(int i = 0; i < activeParticles.Count; i++){
            if(activeParticles[i] == particleObj){
                activeParticles.RemoveAt(i);
                break;
            }
        }
        
        particlePool.Enqueue(particleObj);
    }
    
    public void SetShakeMagnitude(float magnitude) => shakeMagnitude = magnitude;
    
    public void ClearAllEffects(){
        for(int i = activeTexts.Count - 1; i >= 0; i--){
            if(activeTexts[i] != null){
                ReturnTextToPool(activeTexts[i]);
            }
        }
        for(int i = activeParticles.Count - 1; i >= 0; i--){
            if(activeParticles[i] != null){
                ReturnParticleToPool(activeParticles[i]);
            }
        }
        activeTexts.Clear();
        activeParticles.Clear();
        textPool.Clear();
        particlePool.Clear();
    }
}