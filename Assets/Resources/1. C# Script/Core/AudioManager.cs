using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("SOURCES")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("BGM")]
    [SerializeField] private AudioClip[] backgroundMusicList;
    [SerializeField] private int currentMusicIndex = 0;
    [Space(10)]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
    
    [Header("SFX")]
    [SerializeField] private AudioClip playButtonSFX;
    [SerializeField] private AudioClip destroyBlockSFX;
    [SerializeField] private AudioClip clearBlockSFX;
    [SerializeField] private AudioClip eatAppleSFX;
    [SerializeField] private AudioClip eatPoisonSFX;
    [SerializeField] private AudioClip gameOverSFX;
    [SerializeField] private AudioClip countDownSFX;
    [SerializeField] private AudioClip snakeStuckSFX;
    [SerializeField] private AudioClip snakeUnstuckSFX;
    [Space(10)]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;
    
    void Awake(){
        if(Instance == null){
            Instance = this;
            
            if(musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if(sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            
            InitializeAudio();
        }
        else Destroy(gameObject);
    }
    
    void Update(){
        if(Input.GetKeyDown(KeyCode.R)){
            NextBackgroundMusic();
        }
    }
    
    void InitializeAudio(){
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
        
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
        
        if(backgroundMusicList != null && backgroundMusicList.Length > 0){
            PlayBackgroundMusic();
        }
    }
    
    #region BGM
    public void PlayBackgroundMusic(){
        if(backgroundMusicList == null || backgroundMusicList.Length == 0) return;
        if(currentMusicIndex < 0 || currentMusicIndex >= backgroundMusicList.Length) currentMusicIndex = 0;
        
        musicSource.clip = backgroundMusicList[currentMusicIndex];
        musicSource.Play();
    }
    
    public void NextBackgroundMusic(){
        if(backgroundMusicList == null || backgroundMusicList.Length == 0) return;
        
        currentMusicIndex++;
        if(currentMusicIndex >= backgroundMusicList.Length) currentMusicIndex = 0;
        
        musicSource.clip = backgroundMusicList[currentMusicIndex];
        musicSource.Play();
    }
    
    public void PreviousBackgroundMusic(){
        if(backgroundMusicList == null || backgroundMusicList.Length == 0) return;
        
        currentMusicIndex--;
        if(currentMusicIndex < 0) currentMusicIndex = backgroundMusicList.Length - 1;
        
        musicSource.clip = backgroundMusicList[currentMusicIndex];
        musicSource.Play();
    }
    
    public void ChangeBackgroundMusic(int index){
        if(backgroundMusicList == null || backgroundMusicList.Length == 0) return;
        if(index < 0 || index >= backgroundMusicList.Length) return;
        
        currentMusicIndex = index;
        musicSource.clip = backgroundMusicList[currentMusicIndex];
        musicSource.Play();
    }
    
    public void StopBackgroundMusic() => musicSource.Stop();
    #endregion

    #region SFX
    public void PlayEatApple() => PlaySFX(eatAppleSFX);
    public void PlayEatPoison() => PlaySFX(eatPoisonSFX);
    public void PlayDestroyBlock() => PlaySFX(destroyBlockSFX);
    public void PlayClearBlock() => PlaySFX(clearBlockSFX);
    public void PlayGameOver() => PlaySFX(gameOverSFX);
    public void PlayCountDown() => PlaySFX(countDownSFX);
    public void PlaySnakeStuck() => PlaySFX(snakeStuckSFX);
    public void PlaySnakeUnstuck() => PlaySFX(snakeUnstuckSFX);

    void PlaySFX(AudioClip clip){
        if(clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
    #endregion
    
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public void SetMusicVolume(float volume){
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }
    public void SetSFXVolume(float volume) => sfxVolume = Mathf.Clamp01(volume);
    public int GetCurrentMusicIndex() => currentMusicIndex;
    public int GetMusicCount() => backgroundMusicList?.Length ?? 0;
}