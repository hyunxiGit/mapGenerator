using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class MapGenerator : MonoBehaviour {
    public int width=50;
    public int height = 50;
    public int fillPercent = 20;
    public int smooth = 3;
    public int surroundWall = 1;

    //if a rout is found for the room, this value is true;
    int[,] routDraw;
    int[,] map;
    // use for check areaTiles
    int[,] checkedMap;
    //tile 的包括type的房间列表
    CoordTile[,] mapTileArray;
    //list of area
    List<List<CoordTile>> areaTileLists = new List<List<CoordTile>>();
    //房间列表
    List<Room> roomLists = new List<Room>();
    List<Room> linkedRoomLists = new List<Room>();
    List<List<CoordTile>> newRoutList = new List<List<CoordTile>>();

    // Use this for initialization

    //___________________CLASSES_________________________
    

    class Room
    {
        //-3 uninitialize, -2 root, -1 routTile
        // room Code start with 0
        public int areaCode = -3;
        List<CoordTile> roomTilst;
        public List<CoordTile> expandTiles;
        // keep instance for calculation the wall and bla bla
        public CoordTile[,] mapTileArray;

        public Room(int _areaCode, List<CoordTile> list, CoordTile[,] _mapTileArray)
        {
            expandTiles = new List<CoordTile>();
            mapTileArray = _mapTileArray;
            if (_areaCode > -1)
            {
                roomTilst = list;
                setAreaCode(_areaCode);
                setWall();
                //force upadte the mapTileArray
            }
        }
        void setAreaCode(int _areaCode)
        {
            areaCode = _areaCode;
            foreach(CoordTile tile in roomTilst)
            {
                tile.areaCode = _areaCode;
            }
        }
        public void setWall()
        {
            //iterate through room tile list
            foreach(CoordTile tile in roomTilst)
            {
                int x = tile.x;
                int y = tile.y;
                int width = mapTileArray.GetLength(0);
                int height = mapTileArray.GetLength(1);
                bool isWall = false;

                for (int i = x - 1; i <= x + 1; i++)
                {
                    if (isWall)
                        break;
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (!(i < 0 || i >= width || j < 0 || j >= height))
                        {
                            if ((i == x || j == y )&&( !(i==x && j==y)))
                            {
                                //墙点 tile is edge
                                if (mapTileArray[i, j].tileType == 1)
                                {
                                    //遍历wallTiles 看看是不是已经存在
                                    if (!expandTiles.Contains(tile))
                                    {
                                        expandTiles.Add(tile);
                                        isWall = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }

    class CoordTile
    {
        public int x = -1;
        public int y = -1;
        // 和 map[x,y] 一致， 0为room;
        public int tileType = -1;
        public int areaCode = -1;

        //---------------------edge/contact 会有这个属性---------------------

        //store list of tile where this is extented from


        public CoordTile parentTile = null;

        public CoordTile(int myX, int myY)
        {
            this.x = myX;
            this.y = myY;
        }

        public CoordTile(int myX, int myY,int type, int code)
        {
            this.x = myX;
            this.y = myY;
            this.tileType = type;
            this.areaCode = code;
        }

        public Boolean isSame(int myX, int myY)
        {
            return (myX == x && myY == y);
        }

        public void setParent(CoordTile tile)
        {
            this.parentTile = tile;
        }

    }
   
    //___________________FUNCTIONS_________________________
    void Start ()
    {

        routDraw = new int[width, height];
        for(int i = 0; i<width;i++)
        {
            for (int j = 0; j < height; j++)
            {
                routDraw[i, j] = 0;
            }
        }
        generatemap();
    }

    public int[,] generatemap()
    {
        
        checkedMap = new int[width, height];

        //initialize checked map as 0 / unchecked
        //检测过就把对应int[x,y]设为1
        
        

        map = new int[width, height];
        mapTileArray = new CoordTile[width, height];

        int seed = System.DateTime.Now.Second;
        System.Random psudoRandon = new System.Random(seed);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map[i, j] = psudoRandon.Next(0, 99) > fillPercent ? 0 : 1;
            }
        }

        if (smooth < 0)
            smooth = 3;
        for (int i = 0; i < smooth; i++)
        {
            smoothMap();
        }
        addSurroundWall();
        

        int threshold = 4;
        cleanUptiles(threshold);


        //同步 map[] 跟 mapTileList 方便makeRout操作

        makeRout();

        return (map);
    }

    void resetMapArray(int[,] myMap,int value)
    {
        for (int i = 0; i < myMap.GetLength(0); i++)
        {
            for (int j = 0; j < myMap.GetLength(1); j++)
            {
                myMap[i, j] = value;
            }
        }
    }

    void synchMapTileArray()
    {
        if (map == null)
            return;
        if (mapTileArray == null)
            mapTileArray = new CoordTile[map.GetLength(0), map.GetLength(1)];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                CoordTile tile = new CoordTile(i, j);
                tile.tileType = map[i, j];
                mapTileArray[i, j] = tile;
            }
        }
    }

    int checkSurroundOnUnit(int checkX, int checkY)
    {
        int totalOnUnit = 0;
        for (int i = checkX-1; i <=checkX+1; i++)
        {
            for (int j = checkY-1; j <= checkY + 1; j++)
            {
                if ((i >= 0 && i < width) && (j >= 0&&j<height))//inrange
                {
                    if (i!= checkX || j!=checkY)
                    {
                        totalOnUnit+= map[i,j];
                    }
                }
                else
                {
                    totalOnUnit++;
                }
            }
        }
        return (totalOnUnit);
    }

    void smoothMap()
    {
        //smooth map ：按照周围有没有on的unit项来更来本unit的值使得本unit和周围unit相似度更大
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int surroundOnUnit = checkSurroundOnUnit(i, j);
                if (surroundOnUnit > 4)
                {
                    map[i, j] = 1;
                }
                else if (surroundOnUnit < 3)
                {
                    map[i, j] = 0;
                }

            }
        }
    }

    void addSurroundWall()
    {
        if (surroundWall < 1)
            surroundWall = 1;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (!(i >= surroundWall && i < width - surroundWall && j >= surroundWall && j < height - surroundWall))
                    map[i, j] = 1;
            }
        }
    }

    void cleanUptiles(int wallThreshold)
    {
        //clean up tiles if the areea is less than wall ThresholdS
        //临时存储每一个要删除的列表index，删除掉小区域后的列表需要在下一步用到
        List<int> deleteTileLists = new List<int>();
        
        getAreaList(map, areaTileLists);
        //todo :遍历areaTileLists所有的List<CoordTile> 的长度， 若 <50 则

        //todo: 把areaTileLists的列表按照地图进行更新
        for (int i = 0;i< areaTileLists.Count;i++)
        {
            if (areaTileLists[i].Count< wallThreshold)
            {
                int areaType = map[areaTileLists[i][0].x, areaTileLists[i][0].y] == 0 ? 1 : 0 ;
                foreach (CoordTile t in areaTileLists[i])
                {
                    map[t.x, t.y] = areaType;
                }
                deleteTileLists.Add(i);
            }
        }

        //删掉map小区域之后重新索引
        areaTileLists.Clear();
        getAreaList(map, areaTileLists);
  
    }

    void makeRout()
    {
        
        //取得房间列表，并入列
        getRoomInfo();
        
        //store list of routs
        getRoutList2();
        //iterate through list make all list tile walkable
       foreach ( List<CoordTile> routList in newRoutList)
        {
            foreach(CoordTile tile in routList)
            {
                map[tile.x, tile.y] = 0;
                //routDraw[tile.x, tile.y] = 1;
            }
        }

    }

    void getRoomInfo()
    {
        //需要事先有一个完整的Coordtiles列表 因为建立房间的时候需要周边的tiles才能设置wall
        // fill mapTileArray and add tile type info
        if(mapTileArray ==null)
        {
            mapTileArray = new CoordTile[map.GetLength(0), map.GetLength(1)];
        }
        foreach (List<CoordTile> areaList in areaTileLists)
        {
           
            foreach(CoordTile tile in areaList)
            {
                tile.tileType = map[tile.x, tile.y];
                mapTileArray[tile.x, tile.y] = tile;
            }

        }
        foreach (List<CoordTile> areaList in areaTileLists)
        {
            int areaType = -1;
            foreach (CoordTile tile in areaList)
            {
                if (areaType == -1)
                {
                    areaType = map[tile.x, tile.y];
                    break;
                }
                    
            }

            //current list is Room, new Room, and set WallList for room,add to room list
            if (areaType == 0)
            {
                Room myRoom = new Room(roomLists.Count, areaList, mapTileArray);
                roomLists.Add(myRoom);
            }
        }
    }

    void getRoutList2()
    {
        Room startRoom;
        int enlargeCount = linkedRoomLists.Count;

        if (linkedRoomLists.Count == 0)
        {
            startRoom = pickRandomRoom(roomLists);
            linkedRoomLists.Add(startRoom);
        }
            
        while (linkedRoomLists.Count < roomLists.Count)
        {
            //扩展一个房间

            //复制tempTileArray列表
            CoordTile[,] tempTileArray = new CoordTile[mapTileArray.GetLength(0), mapTileArray.GetLength(1)];
            for(int i = 0;i< mapTileArray.GetLength(0);i++)
            {
                for (int j = 0; j < mapTileArray.GetLength(1); j++)
                {
                    CoordTile newTile = new CoordTile(i, j,mapTileArray[i,j].tileType, mapTileArray[i, j].areaCode);
                    tempTileArray[i, j] = newTile;
                }
            }

            Room baseRoom = pickRandomRoom(linkedRoomLists);      
            enlargeRoom(baseRoom, tempTileArray);
        }
    }

    void enlargeRoom(Room baseRoom, CoordTile[,] mapTileArray)//新地图列表)
    {
        // 从 baseRoom 得到一个contact点
        bool foundContact = false;

        //映射room 的expandtile 到新的 mapTileArray 上
        List<CoordTile> toExpand = new List<CoordTile>();
        foreach (CoordTile sourceTile in baseRoom.expandTiles)
        {
            CoordTile targetTile = mapTileArray[sourceTile.x, sourceTile.y];
            toExpand.Add(targetTile);
        }

        

        while (!foundContact/*&& enlarge room 没有撑满*/)
        {
            //放新一轮的expand
            List<CoordTile> expandNewList = new List<CoordTile>();
            foreach (CoordTile expandTile in toExpand)
            {
                List<CoordTile> fourTiles = getPureFourTiles(expandTile, mapTileArray);
                if (fourTiles.Count > 0)
                {
                    foreach (CoordTile tile in fourTiles)
                    {
                        //自己房间内
                        if (tile.areaCode == baseRoom.areaCode)
                            continue;
                        //空tile
                        if (tile.areaCode == -1)
                        {
                            tile.areaCode = baseRoom.areaCode;
                            tile.setParent(expandTile);
                            if (!expandNewList.Contains(tile))
                            {
                                expandNewList.Add(tile);
                            }

                        }
                        //contact point
                        else
                        {
                            // room is not linked room
                            bool isLinked = false;
                            foreach (Room linkedRoom in linkedRoomLists)
                            {
                                if (linkedRoom.areaCode == tile.areaCode)
                                {
                                    isLinked = true;
                                    break;
                                }
                            }
                            if (!isLinked)
                            {
                                foreach (Room room in roomLists)
                                {
                                    if (room.areaCode == tile.areaCode)
                                    {
                                        linkedRoomLists.Add(room);
                                        foundContact = true;
                                        //必须在这个时候得到routList下一轮回被清洗掉
                                        getRout(mapTileArray[expandTile.x, expandTile.y], mapTileArray);
                                        return;
                                    }
                                }
                            }

                        }

                    }
                }
            }
            toExpand = new List<CoordTile>(expandNewList);
        }
    }

    void getRout(CoordTile contactTile, CoordTile[,] mapArray)
    {
        List<CoordTile> routTile = new List<CoordTile>();
        CoordTile findTile = contactTile;
        routTile.Add(contactTile);

        while (findTile.parentTile !=null )
        {
            CoordTile nextTile = findTile.parentTile;
            routTile.Add(mapTileArray[nextTile.x, nextTile.y]);
            findTile = nextTile;
        }

        newRoutList.Add(routTile);
    }

    Room pickRandomRoom(List<Room> roomList)
    {
        Room resultRoom;
        if (roomList.Count ==0)
        {
            //roomlist参数错误
            resultRoom = new Room(-1, new List<CoordTile>(), mapTileArray);
        }
        else
        {
            int index = (int)UnityEngine.Random.Range(0f, roomList.Count);
            resultRoom = roomList[index];
        }

        return resultRoom;
    }
   
    void getAreaList(int[,] myMap, List<List<CoordTile>> targetList )
    {
        resetMapArray(checkedMap,0);
        if (myMap.GetLength(0) == 0 || myMap.GetLength(1) == 0)
            return;

        //iterate map[,], get the unchecked tiles, get the area list that connected to the tile 
        //遍历map[,] 取得on uncked 点， 为它取得连续区域
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (checkedMap[i,j]==0/* && map[i, j]==1需要动态建立type*/) // uncheck and on ,
                {
                    CoordTile refTile = new CoordTile(i, j);
                    // 只要有这个点，必然有至少有一点的连续区域,新建一个新List
                    List<CoordTile> areaTiles = new List<CoordTile>();
                    getAreaTile2(refTile, areaTiles);
                    if (areaTiles.Count>0)
                    {
                        targetList.Add(areaTiles);
                    }
                }
            }
        }
    }

    void getAreaTile2(CoordTile myRefTile, List<CoordTile> resultTileList)
    {
        int areaType = map[myRefTile.x, myRefTile.y];
        Queue<CoordTile> tileQueue = new Queue<CoordTile>();

        tileQueue.Enqueue(myRefTile);
        while (tileQueue.Count > 0)
        {
            CoordTile refTile = tileQueue.Dequeue();
            //put refTile in list, set checkedmap
            resultTileList.Add(refTile);
            checkedMap[refTile.x, refTile.y] = 1;
            //get neighbor tiles ,put into queue
            for (int i = refTile.x - 1; i <= refTile.x + 1; i++)
            {
                for (int j = refTile.y - 1; j <= refTile.y + 1; j++)
                {
                    // in map range
                    if (!(i < 0 || i >= width || j < 0 || j >= height))
                    {
                        //get four neighbor tiles
                        if (i == refTile.x || j == refTile.y)
                        {
                            if (map[i, j] == areaType && checkedMap[i, j] == 0)
                            {
                                //qualified tile into the queue
                                tileQueue.Enqueue(new CoordTile(i, j));
                                checkedMap[i, j] = 1;
                            }
                        }
                    }
                }
            }
        }
    }

    List<CoordTile> getPureFourTiles(CoordTile myRefTile,CoordTile[,] myMapArray = null)
    {
        List<CoordTile> fourTiles = new List<CoordTile>();
        int x = myRefTile.x;
        int y = myRefTile.y;
        if (myMapArray == null)
        {
            myMapArray = mapTileArray;
        }

        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (!(i < 0 || i >= width || j < 0 || j >= height))
                {
                    if ((i== x || j == y) && (!(i == x && j == y)))
                    fourTiles.Add(myMapArray[i,j]);   
                }
            }
        }
        return (fourTiles);
    }
    /*
    void OnDrawGizmos()
    {
        if (map != null)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Gizmos.color = map[i, j] == 1 ? Color.black : Color.white;
                    if (routDraw[i, j] == 1)
                        Gizmos.color = Color.red;
                    Gizmos.DrawCube(new Vector3(i - width / 2, 0, j - height / 2), new Vector3(1, 1, 1));

                }
            }
        }
    }*/
}
