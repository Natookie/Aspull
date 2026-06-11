using UnityEngine;
using Nova;
using EncounterSystem;
using System.Collections;

public class EncounterUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextBlock encounterNameText;
    [SerializeField] private float displayDuration = 2f;
    
    private Coroutine hideCoroutine;
    
    void Start(){
        if(GameManager.Instance != null){
            GameManager.Instance.OnEncounterStarted += OnEncounterStarted;
            GameManager.Instance.OnEncounterEnded += OnEncounterEnded;
        }
        
        if(encounterNameText != null) encounterNameText.Text = "";
    }
    
    void OnDestroy(){
        if(GameManager.Instance != null){
            GameManager.Instance.OnEncounterStarted -= OnEncounterStarted;
            GameManager.Instance.OnEncounterEnded -= OnEncounterEnded;
        }
    }
    
    void OnEncounterStarted(EncounterType type){
        if(hideCoroutine != null) StopCoroutine(hideCoroutine);
        
        if(encounterNameText != null){
            encounterNameText.Text = GetEncounterName(type);
            hideCoroutine = StartCoroutine(HideAfterDelay());
        }
    }
    
    void OnEncounterEnded(){
        if(hideCoroutine != null) StopCoroutine(hideCoroutine);
        if(encounterNameText != null) encounterNameText.Text = "";
    }
    
    IEnumerator HideAfterDelay(){
        yield return new WaitForSeconds(displayDuration);
        if(encounterNameText != null) encounterNameText.Text = "";
        hideCoroutine = null;
    }
    
    string GetEncounterName(EncounterType type){
        switch(type){
            case EncounterType.WaveTunnel: return "WAVE TUNNEL";
            case EncounterType.GradualSqueeze: return "GRADUAL SQUEEZE";
            case EncounterType.ZigZagCorridor: return "ZIG ZAG";
            case EncounterType.SplitPath: return "SPLIT PATH";
            case EncounterType.SpiralDrift: return "SPIRAL DRIFT";
            case EncounterType.AlternatingPressure: return "ALTERNATING PRESSURE";
            case EncounterType.ClearSection: return "CLEAR SECTION";
            case EncounterType.PinchPoint: return "PINCH POINT";
            case EncounterType.Accordion: return "ACCORDION";
            case EncounterType.ScissorDoors: return "SCISSOR DOORS";
            case EncounterType.Heartbeat: return "HEARTBEAT";
            case EncounterType.Corkscrew: return "CORKSCREW";
            case EncounterType.Mirror: return "MIRROR";
            case EncounterType.Tornado: return "TORNADO";
            case EncounterType.Pendulum: return "PENDULUM";
            case EncounterType.Staggered: return "STAGGERED";
            case EncounterType.Funnel: return "FUNNEL";
            case EncounterType.Ripple: return "RIPPLE";
            case EncounterType.Serpentine: return "SERPENTINE";
            case EncounterType.CrissCross: return "CRISS CROSS";
            case EncounterType.Whirlpool: return "WHIRLPOOL";
            case EncounterType.ShiftWave: return "SHIFT WAVE";
            default: return type.ToString();
        }
    }
}