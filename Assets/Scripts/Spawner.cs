using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public bool devMode;
    
    public Wave[] waves;
    public Enemy enemy;
    int enemiesRemainingToSpawn;
    float nextSpawnTime;
    int enemiesRemainingAlive;

    LivingEntity playerEntity;
    Transform playerT;

    Wave currentWave;
    int currentWaveNumber;

    MapGenerator map;
    Color originalTileColor;

    float timeBetweenCampingChecks = 2;
    float campThresholdDistance = 1.5f;
    float nextCampCheckTime;
    Vector3 campPositionOld;
    bool isCamping;

    bool isDisabled = false;

    public event System.Action<int> OnNewWave;


    void Start(){
        playerEntity = FindObjectOfType<Player>();
        playerT = playerEntity.transform;
        nextCampCheckTime = timeBetweenCampingChecks + Time.time;
        campPositionOld = playerT.position;
        playerEntity.OnDeath += OnPlayerDeath;
        currentWaveNumber = 0;

        map = FindObjectOfType<MapGenerator>();
        NextWave();
    }

    //Spawn enemies on timer
    void Update(){
        //Regular checking if player is camping
        if(!isDisabled){
            if(Time.time > nextCampCheckTime){
                nextCampCheckTime = Time.time + timeBetweenCampingChecks;

                isCamping = (Vector3.Distance(playerT.position,campPositionOld) < campThresholdDistance);
                campPositionOld = playerT.position;
            }

            if((currentWave.infinite || enemiesRemainingToSpawn > 0) && Time.time >= nextSpawnTime){
                enemiesRemainingToSpawn--;
                nextSpawnTime = Time.time + currentWave.timeBetweenWaves;
            StartCoroutine("SpawnEnemy");
            }
        }
        if(devMode){
            if(Input.GetKeyDown(KeyCode.Return)){
                StopCoroutine("SpawnEnemy");
                foreach(Enemy enemy in FindObjectsOfType<Enemy>()){
                    Destroy(enemy.gameObject);
                }
                NextWave();
            }
        }
    }

    IEnumerator SpawnEnemy(){
        float spawnDelay = 2;
        float tileFlashSpeed = 4;

        Transform spawnTile = map.GetRandomOpenTile();
        if(isCamping){
            spawnTile = map.GetTileFromPosition(playerT.position);
        }
        Material tileMat = spawnTile.GetComponent<Renderer>().material;
        if(originalTileColor == null){
            originalTileColor = tileMat.color;
        }
        Color flashColor = Color.red;
        float spawnTimer = 0;

        while(spawnTimer < spawnDelay){
            tileMat.color = Color.Lerp(originalTileColor,flashColor, Mathf.PingPong(spawnTimer*tileFlashSpeed,1));


            spawnTimer += Time.deltaTime;
            yield return null;
        }

        Enemy spawnedEnemy = Instantiate(enemy, spawnTile.position + Vector3.up,Quaternion.identity) as Enemy;
        spawnedEnemy.OnDeath += OnEnemyDeath;
        spawnedEnemy.SetCharacteristics(currentWave.moveSpeed, currentWave.hitsToKillPlayer,currentWave.enemyHealth, currentWave.skinColor);
    }

    void OnPlayerDeath(){
        isDisabled = true;
    }

    void OnEnemyDeath(){
        enemiesRemainingAlive--;
        if(enemiesRemainingAlive == 0){
            NextWave(); 
        }
    }

    void NextWave(){
        if(currentWaveNumber > 0)
            AudioManager.instance.PlaySound2D("Level Complete");
        currentWaveNumber++;
        if(currentWaveNumber - 1 < waves.Length){
            currentWave = waves[currentWaveNumber-1];
            enemiesRemainingToSpawn = currentWave.enemyCount;
            enemiesRemainingAlive = enemiesRemainingToSpawn;
            if(OnNewWave != null){
                OnNewWave(currentWaveNumber);
                playerT.transform.position = new Vector3(0,1f,0);
            }
        }
    }
    
    [System.Serializable]
    public class Wave{
        public bool infinite;
        public int enemyCount;
        public float timeBetweenWaves;
        
        //Difficulty Options
        public float moveSpeed;
        public int hitsToKillPlayer;
        public float enemyHealth;
        public Color skinColor;
    }

}
