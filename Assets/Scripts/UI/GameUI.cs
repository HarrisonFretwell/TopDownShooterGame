using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public Image fadePlane;
    public GameObject gameOverUI;
    
    public Text scoreUI;
    public RectTransform healthBar;
    [Header("New Wave Banner")]
    public RectTransform newWaveBanner;
    public Text newWaveTitle;
    public Text newWaveEnemyCount;
   
    
    Spawner spawner;
    Player player;
    float animateBannerTime = 1f;
    
    void Start()
    {
        player = FindObjectOfType<Player>();
        player.OnDeath += OnGameOver;
        spawner = FindObjectOfType<Spawner>();
        spawner.OnNewWave += OnNewWave;
    }
    
    void Update(){
        scoreUI.text = ScoreKeeper.score.ToString("D6");
        float healthPercent = 0;
        if(player != null){
            healthPercent = player.health / player.startingHealth;
            
        }
        healthBar.localScale = new Vector3(healthPercent,1,1);
        
    }
    
    //Display new wave banner on new wave
    void OnNewWave(int waveNumber){
        string [] numbers = {"One", "Two", "Three", "Four", "Five"};
        newWaveTitle.text ="- Wave " + numbers[waveNumber-1] + " -";
        string enemyCountString = spawner.waves[waveNumber-1].infinite?"Infinite":spawner.waves[waveNumber-1].enemyCount+"";
        newWaveEnemyCount.text = "Enemies: " + enemyCountString;
        
        StartCoroutine(AnimateNewWaveBanner());
    }
    
    IEnumerator AnimateNewWaveBanner(){
        float animatePercent = 0;
        float animateSpeed = 1/animateBannerTime;
        float delayTime = 3f;
        float endDelayTime = Time.time + animateBannerTime + delayTime;
        int dir = 1;
        
        while(animatePercent >= 0){
            animatePercent += animateSpeed * Time.deltaTime * dir;
            if(animatePercent >=1){
                animatePercent = 1;
                if(Time.time > endDelayTime){
                    dir = -1;
                }
            }
            newWaveBanner.anchoredPosition = Vector2.up * Mathf.Lerp(-170,45,animatePercent);
            yield return null;
        }
         
    }

    void OnGameOver(){
        Cursor.visible = true;
        StartCoroutine(Fade (Color.clear,Color.black,1));
        gameOverUI.SetActive(true);
    }

    
    IEnumerator Fade(Color from, Color to, float time){
        float speed = 1/time;
        float percent = 0;

        while(percent < 1){
            percent += Time.deltaTime * speed;
            fadePlane.color = Color.Lerp(from,to,percent);
            yield return null;
        }
    }
    
    // UI Input
    public void StartNewGame() {
        Cursor.visible = false;
        SceneManager.LoadScene("Game");
    }
}
