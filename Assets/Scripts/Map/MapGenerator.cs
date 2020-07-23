using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public Map[] maps;
    public int mapIndex;

    public Transform tilePrefab;
    public Transform obstaclePrefab;
    public Transform mapFloor;
    public Transform navmeshFloor;
    public Transform navMeshMaskPrefab;
    public Vector2Int maxMapSize;

    public float tileScale;
    
    Transform mapHolder;
    List<Coord> allTileCoords;
    Queue<Coord> shuffledTileCoords;
    Queue<Coord> shuffledOpenTileCoords;
    System.Random prng;
    Transform[,] tileMap;

    Map currentMap;
    

    void Start(){
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
    }

    void OnNewWave(int waveNumber){
        mapIndex = waveNumber - 1;
        GenerateMap();
    }

    public void GenerateMap(){
        currentMap = maps[mapIndex];
        tileMap = new Transform[currentMap.mapSize.x,currentMap.mapSize.y];
        prng = new System.Random(currentMap.seed);
        

        string holderName = "Generated Map";
        if(transform.Find (holderName)){
            DestroyImmediate(transform.Find(holderName).gameObject);
        }

        
        mapHolder = new GameObject(holderName).transform;
        mapHolder.parent = transform;
        //*Generate coords
        allTileCoords = new List<Coord>();
         for(int x = 0; x < currentMap.mapSize.x; x++){
            for(int y = 0; y < currentMap.mapSize.y; y++){
                allTileCoords.Add(new Coord(x,y));
            }
         }
        shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(),currentMap.seed));

        //*Spawn tiles
        for(int x = 0; x < currentMap.mapSize.x; x++){
            for(int y = 0; y < currentMap.mapSize.y; y++){
                Vector3 tilePosition = CoordToPos(x,y);
                Transform newTile = Instantiate(tilePrefab,tilePosition,Quaternion.Euler(Vector3.right*90)) as Transform;
                newTile.localScale = Vector3.one * (1-currentMap.outlinePercent)*tileScale;
                newTile.parent = mapHolder;
                tileMap[x,y] = newTile; 
            }
        }
        GenerateObstacles();
    }

    //*Loop through random coordinates and generate obstacles at those coords
    void GenerateObstacles(){
        List<Coord> allOpenCoords = new List<Coord>(allTileCoords);
        bool[,] obstacleMap = new bool[currentMap.mapSize.x,currentMap.mapSize.y];
        int obstacleCount = (int)(Mathf.Floor(currentMap.mapSize.x * currentMap.mapSize.y * currentMap.obstaclePercent));
        int currentObstacleCount = 0;

        for(int i = 0; i < obstacleCount; i++){
            Coord randomCoord = GetRandomCoord();
			obstacleMap[randomCoord.x,randomCoord.y] = true;
			currentObstacleCount ++;

            if (randomCoord != currentMap.mapCentre && MapIsFullyAccessible(obstacleMap, currentObstacleCount)) {
                Vector3 obstaclePos = CoordToPos(randomCoord.x,randomCoord.y);
                float obstacleHeight = Mathf.Lerp(currentMap.minObstacleHeight,currentMap.maxObstacleHeight,(float)prng.NextDouble());
                Transform newObstacle = Instantiate(obstaclePrefab, obstaclePos + Vector3.up * obstacleHeight/2, Quaternion.identity) as Transform;
                //Scale obstacle and set parent
                
                newObstacle.localScale = new Vector3((1-currentMap.outlinePercent)*tileScale,obstacleHeight,(1-currentMap.outlinePercent)*tileScale);
                newObstacle.parent = mapHolder;

                Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
                float colourPercent = randomCoord.y / (float)currentMap.mapSize.y;
                obstacleMaterial.color = Color.Lerp(currentMap.foregroundColor,currentMap.backgroundColor,colourPercent);
                obstacleRenderer.sharedMaterial = obstacleMaterial;

                //Remove from list of all open coords, as tile now has obstacle
                allOpenCoords.Remove(randomCoord);

            }
            else{
                obstacleMap[randomCoord.x,randomCoord.y] = false;
				currentObstacleCount --;
            }
        }
                shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(),currentMap.seed));

        generateNavMeshMask();
        
    }

    //Generate navmesh masks to define boundary of playable area
    void generateNavMeshMask(){
        Transform maskLeft = Instantiate(navMeshMaskPrefab, Vector3.left * (currentMap.mapSize.x + maxMapSize.x)/4f*tileScale,Quaternion.identity) as Transform;
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x)/2f,1,currentMap.mapSize.y)*tileScale;

        Transform maskRight = Instantiate(navMeshMaskPrefab, Vector3.right * (currentMap.mapSize.x + maxMapSize.x)/4f*tileScale,Quaternion.identity) as Transform;
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x)/2f,1,currentMap.mapSize.y)*tileScale;

        Transform maskTop = Instantiate(navMeshMaskPrefab,Vector3.forward * (currentMap.mapSize.y+maxMapSize.y)/4f*tileScale,Quaternion.identity) as Transform;
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(currentMap.mapSize.x,1,(maxMapSize.y - currentMap.mapSize.y)/2f)*tileScale;
        
        Transform maskBot = Instantiate(navMeshMaskPrefab,Vector3.back * (currentMap.mapSize.y+maxMapSize.y)/4f*tileScale,Quaternion.identity) as Transform;
        maskBot.parent = mapHolder;
        maskBot.localScale = new Vector3(currentMap.mapSize.x,1,(maxMapSize.y - currentMap.mapSize.y)/2f)*tileScale;

        navmeshFloor.localScale = new Vector3(maxMapSize.x,1,maxMapSize.y)*tileScale;
        mapFloor.localScale = new Vector3(currentMap.mapSize.x*tileScale,currentMap.mapSize.y*tileScale,.05f);

    }

    //Find all tiles accessible from center, return if this is equal the number of non-obstacle tiles
    bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount){
       bool[,] mapFlags = new bool[obstacleMap.GetLength(0),obstacleMap.GetLength(1)];
		Queue<Coord> queue = new Queue<Coord> ();
		queue.Enqueue (currentMap.mapCentre);
		mapFlags [currentMap.mapCentre.x, currentMap.mapCentre.y] = true;

		int accessibleTileCount = 1;

		while (queue.Count > 0) {
			Coord tile = queue.Dequeue();

			for (int x = -1; x <= 1; x ++) {
				for (int y = -1; y <= 1; y ++) {
					int neighbourX = tile.x + x;
					int neighbourY = tile.y + y;
					if (x == 0 || y == 0) {
						if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1)) {
							if (!mapFlags[neighbourX,neighbourY] && !obstacleMap[neighbourX,neighbourY]) {
								mapFlags[neighbourX,neighbourY] = true;
								queue.Enqueue(new Coord(neighbourX,neighbourY));
								accessibleTileCount ++;
							}
						}
					}
				}
			}
		}
        
		int targetAccessibleTileCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount);
		return targetAccessibleTileCount == accessibleTileCount;
    }

    //Convert tile coordinate to Vector3 pos
    Vector3 CoordToPos(int x, int y){
        return new Vector3(-currentMap.mapSize.x/2f +0.5f+x,0,-currentMap.mapSize.y/2f+0.5f+y)*tileScale;
    }

    //Conver global position to x,y on gameboard, and return tile at that position
    public Transform GetTileFromPosition(Vector3 position){
        int x = Mathf.RoundToInt(position.x / tileScale + (currentMap.mapSize.x-1)/2f);
        int y = Mathf.RoundToInt(position.z / tileScale + (currentMap.mapSize.y-1)/2f);
        x = Mathf.Clamp(x,0,tileMap.GetLength(0)-1);
        y = Mathf.Clamp(y,0,tileMap.GetLength(1)-1);
        return tileMap[x,y];
    }


    //Return next item in shuffleTileCoord queue
    public Coord GetRandomCoord(){
        Coord randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }

    //Get random tile that does not have obstacle
    public Transform GetRandomOpenTile(){
        Coord randomCoord = shuffledOpenTileCoords.Dequeue();
        shuffledOpenTileCoords.Enqueue(randomCoord);
        return tileMap[randomCoord.x,randomCoord.y];
    }

    public struct Coord{
        public int x;
        public int y;

        public Coord(int x,int y){
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(Coord c1, Coord c2) => c1.x == c2.x && c1.y == c2.y;
        public static bool operator !=(Coord c1, Coord c2) => c1.x != c2.x || c1.y != c2.y;
        public bool Equals(Coord c1){
            return x == c1.x && y == c1.y;
        }
    }

    [System.Serializable]
    public class Map{

        
        [Range(0,1)]
        public float obstaclePercent;
        public int seed;
        public float minObstacleHeight;
        public float maxObstacleHeight;
        public float outlinePercent;
        public Color foregroundColor;
        public Color backgroundColor;
        public Vector2Int mapSize;

        public Coord mapCentre{
            get{
                return new Coord(mapSize.x/2,mapSize.y/2);
            }
        }


    }
}
