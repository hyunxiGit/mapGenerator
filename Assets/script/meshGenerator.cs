using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meshGenerator : MonoBehaviour {

    public GameObject wallObject;

    List<int> drawVerticsIndex;//all the vertics in this list will have gizmo drawn cube
    List<Vector3> drawVerticsPosition;

    SqureGrid myGrid; // map data info

    //create wall vertics info
    Dictionary<int, List<Triangles>> vertTriDictionary;
    HashSet<int> checkedVertics;
    List<List<int>> wallList;

    List<Vector3> verticsList;
    List<int> triangleList;
    List<Vector3> wallVerticsList;
    List<int> wallTriangleList;
    //Dictionary<int verticsIndex, List<Triangles> triList> vertTriDictionary;

    MeshFilter myMeshFilter;
    MeshFilter wallMeshFilter;

    // Use this for initialization
    void Start() {

        this.wallVerticsList = new List<Vector3>();
        this.wallTriangleList = new List<int>();

        drawVerticsIndex = new List<int>();
        drawVerticsPosition = new List<Vector3>();
        wallList = new List<List<int>>();
        checkedVertics = new HashSet<int>();
        //int verticsIndex, List<Triangles> triList
        vertTriDictionary = new Dictionary<int, List<Triangles>>();

        verticsList = new List<Vector3>();
        triangleList = new List<int>();

        myMeshFilter = GetComponent<MeshFilter>();
        wallMeshFilter = wallObject.GetComponent<MeshFilter>();

        MapGenerator mapData = GetComponent<MapGenerator>();
        mapData.generatemap();
        myGrid = new SqureGrid(mapData.generatemap(), 1f);

        createMesh(myGrid);

    }

    void createMesh(SqureGrid grid)
    {
        for (int i = 0; i < grid.squareArray.GetLength(0); i++)
        {
            for (int j = 0; j < grid.squareArray.GetLength(1); j++)
            {
                //遍历所有的Square
                //get all vertix 还有 Triangle
                triangulateSquare(grid.squareArray[i, j]);
            }
        }
        createWall();

        Mesh myMesh = new Mesh();
        myMesh.vertices = verticsList.ToArray();
        myMesh.triangles = triangleList.ToArray();

        myMeshFilter.mesh = myMesh;
        myMeshFilter.mesh.RecalculateNormals();

        Mesh myWallMesh = new Mesh();
        myWallMesh.vertices = wallVerticsList.ToArray();
        myWallMesh.triangles = wallTriangleList.ToArray();
        wallMeshFilter.mesh = myWallMesh;
        wallMeshFilter.mesh.RecalculateNormals();

    }

    void createWall()
    {
        //遍历所有的vertics,已经check
        foreach (int verticsIndex in this.vertTriDictionary.Keys)
        {
            if (!checkedVertics.Contains(verticsIndex))
            {
                checkedVertics.Add(verticsIndex);

                List<int> wallVertics = new List<int>();
                getWallVerticsList(verticsIndex, wallVertics);
                if (wallVertics.Count > 0)
                {
                    //是 wall vertics
                    wallVertics.Add(verticsIndex);
                    wallList.Add(wallVertics);
                }
            }
        }

       
        foreach(List<int> wallVList in wallList)
        {
            
            List<Vector3> segWallVerList = new List<Vector3>();
            List<int> segWallTriList = new List<int>();
            for (int i =0; i< wallVList.Count; i++)
            {
                //每一个vertics拷贝到list里面, v0,v0',v1,v1'....
                segWallVerList.Add(this.verticsList[wallVList[i]]);
                segWallVerList.Add(this.verticsList[wallVList[i]] - transform.up);
            }
            foreach (Vector3 ver in segWallVerList)
            {
                this.drawVerticsPosition.Add(ver);
            }
            //三角形的逻辑
            int latTriIndex = this.wallVerticsList.Count;

            for (int i=0; i< segWallVerList.Count-3; i=i+2)
            {
                segWallTriList.Add(i+ latTriIndex);
                segWallTriList.Add(i+1+ latTriIndex);
                segWallTriList.Add(i+3+ latTriIndex);

                segWallTriList.Add(i+ latTriIndex);
                segWallTriList.Add(i+3+ latTriIndex);
                segWallTriList.Add(i+2+ latTriIndex);

            }


            //todo : 闭口
            //this.wallVerticsList.AddRange(segWallVerList);

            segWallTriList.Add(segWallVerList.Count - 2 + latTriIndex);
            segWallTriList.Add(segWallVerList.Count - 1 + latTriIndex);
            segWallTriList.Add(1 + latTriIndex);


            segWallTriList.Add(segWallVerList.Count - 2 + latTriIndex);
            segWallTriList.Add(1 + latTriIndex);
            segWallTriList.Add(latTriIndex);

            this.wallTriangleList.AddRange(segWallTriList);

            this.wallVerticsList.AddRange(segWallVerList);



        }

    }
    void getWallVerticsList(int refVerticsIndex, List<int>wallVerticsList)
    {
        int nextIndex = nextVertix(refVerticsIndex);
        if (nextIndex>-1)
        {
            //found next vertics
            wallVerticsList.Add(nextIndex);
            getWallVerticsList(nextIndex, wallVerticsList);
        }

    }

    int nextVertix(int myIndex)
    {
        int nextIndex = -1;
        //get next vertics
        //遍历所有三角表中的顶点
        foreach(Triangles tri in vertTriDictionary[myIndex])
        {
            foreach (int triIndex in tri.vIndexArray)
            {
                if (triIndex != myIndex && !checkedVertics.Contains(triIndex))
                {                    
                    if(isWallEdge(myIndex, triIndex))
                    {
                        checkedVertics.Add(triIndex);
                        return (triIndex);
                    }
                }
            }
        }
        return (nextIndex);
    }

    bool isWallEdge(int refIndex, int checkIndex)
    {
        int containCount = 0;
        foreach (Triangles tri in vertTriDictionary[refIndex])
        {
            if (tri.vIndexArray.Contains(checkIndex))
            {
                containCount += 1;
                if (containCount>1)
                {
                    break;
                }               
            }
        }
        return (containCount==1);
    }

    void triangulateSquare(Square mySquare)
    {
        //理出有顺序的一个vertix列表
        switch (mySquare.conFigCode)
        {
            //one vertix :
            case 8:
                addList(mySquare.bottomLeft, mySquare.left, mySquare.bottom);
                break;
            case 4:
                addList(mySquare.bottom, mySquare.right, mySquare.bottomRight);
                break;
            case 2:
                addList(mySquare.right, mySquare.top, mySquare.topRight);
                break;
            case 1:
                addList(mySquare.top, mySquare.left, mySquare.topLeft);
                break;

            //two vertics:
            case 12:
                addList(mySquare.bottomLeft, mySquare.left, mySquare.right, mySquare.bottomRight);
                break;
            case 6:
                addList(mySquare.bottom, mySquare.top, mySquare.topRight, mySquare.bottomRight);
                break;
            case 3:
                addList(mySquare.right, mySquare.left, mySquare.topLeft, mySquare.topRight);
                break;
            case 9:
                addList(mySquare.bottomLeft, mySquare.topLeft, mySquare.top, mySquare.bottom);
                break;

            //diagnose
            case 10:
                addList(mySquare.bottomLeft, mySquare.left, mySquare.top, mySquare.topRight, mySquare.right, mySquare.bottom);
                break;
            case 5:
                addList(mySquare.bottom, mySquare.left, mySquare.topLeft, mySquare.top, mySquare.right, mySquare.bottomRight);
                break;

            //three vertix
            case 14:
                addList(mySquare.bottomLeft, mySquare.left, mySquare.top, mySquare.topRight, mySquare.bottomRight);
                break;
            case 7:
                addList(mySquare.bottom, mySquare.left, mySquare.topLeft, mySquare.topRight, mySquare.bottomRight);
                break;
            case 11:
                addList(mySquare.bottomLeft, mySquare.topLeft, mySquare.topRight, mySquare.right, mySquare.bottom);
                break;
            case 13:
                addList(mySquare.bottomLeft, mySquare.topLeft, mySquare.top, mySquare.right, mySquare.bottomRight);
                break;

            case 15:
                addList(mySquare.bottomLeft, mySquare.topLeft, mySquare.topRight, mySquare.bottomRight);
                break;
        }
    }

    void addList(params Node[] myNodes)
    {
        //把所有的myNode签到
        //放入Vertix List 里面
        //并在便利过程中把三角形的Array也做出来
        for (int i = 0; i < myNodes.Length; i++)
        {
            if (myNodes[i].vertexIndex == -1)
            {
                myNodes[i].vertexIndex = this.verticsList.Count;
                this.verticsList.Add(myNodes[i].position);
            }
        }
        //按照个数加三角形
        if (myNodes.Length >= 3)
        {
            makeTriangle(myNodes[0].vertexIndex, myNodes[1].vertexIndex, myNodes[2].vertexIndex);
        }
        if (myNodes.Length >= 4)
        {
            makeTriangle(myNodes[0].vertexIndex, myNodes[2].vertexIndex, myNodes[3].vertexIndex);
        }
        if (myNodes.Length >= 5)
        {
            makeTriangle(myNodes[0].vertexIndex, myNodes[3].vertexIndex, myNodes[4].vertexIndex);
        }
        if (myNodes.Length >= 6)
        {
            makeTriangle(myNodes[0].vertexIndex, myNodes[4].vertexIndex, myNodes[5].vertexIndex);
        }
        if (myNodes.Length >= 7)
        {
            makeTriangle(myNodes[0].vertexIndex, myNodes[5].vertexIndex, myNodes[6].vertexIndex);
        }


    }

    void makeTriangle(int myNodeIndex1, int myNodeIndex2, int myNodeIndex3)
    {
        Triangles myTri = new Triangles(myNodeIndex1, myNodeIndex2, myNodeIndex3);
        this.triangleList.Add(myNodeIndex1);
        this.triangleList.Add(myNodeIndex2);
        this.triangleList.Add(myNodeIndex3);
        foreach(int verIndex in myTri.vIndexArray)
        {
            if (!this.vertTriDictionary.ContainsKey(verIndex))
            {
                List<Triangles> myTriList = new List<Triangles>();
                myTriList.Add(myTri);
                this.vertTriDictionary.Add(verIndex, myTriList);
            }
            else
            {
                this.vertTriDictionary[verIndex].Add(myTri);
             }
         }
    }

    class Triangles
    {
        public int vIndex1;
        public int vIndex2;
        public int vIndex3;

        public HashSet<int> vIndexArray;
        public Triangles(int myIndex1, int myIndex2, int myIndex3)
        {
            this.vIndex1 = myIndex1;
            this.vIndex2 = myIndex2;
            this.vIndex3 = myIndex3;

            vIndexArray = new HashSet<int>();
            vIndexArray.Add(this.vIndex1);
            vIndexArray.Add(this.vIndex2);
            vIndexArray.Add(this.vIndex3);
        }
    }

    private void OnDrawGizmos()
    {
        //squareGridGizmo();
        vertixGizmo();
    }

    void vertixGizmo()
    {
        Gizmos.color = Color.red;
        //if (drawVerticsIndex!=null)
        //{
        //    foreach (int index in drawVerticsIndex)
        //    {
        //        Gizmos.DrawCube(this.verticsList[index], new Vector3(0.2f, 0.2f, 0.2f));

        //    }
        //}
        if (drawVerticsPosition != null)
        {
            foreach (Vector3 position in drawVerticsPosition)
            {
                Gizmos.DrawCube(position, new Vector3(0.1f, 0.1f, 0.1f));

            }
        }
        
    }

    void squareGridGizmo()
    {
        if (myGrid != null)
        {

            for (int i = 0; i < myGrid.squareArray.GetLength(0); i++)
            {
                for (int j = 0; j < myGrid.squareArray.GetLength(1); j++)
                {
                    if (i == 0 && j == 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(myGrid.squareArray[i, j].bottomLeft.position, new Vector3(0.2f, 0.2f, 0.2f));

                        Gizmos.DrawCube(myGrid.squareArray[i, j].bottomRight.position, new Vector3(0.2f, 0.2f, 0.2f));
                    }

                    else
                    {
                        Gizmos.color = myGrid.squareArray[i, j].bottomLeft.isOn ? Color.black : Color.white;
                        Gizmos.DrawCube(myGrid.squareArray[i, j].bottomLeft.position, new Vector3(0.2f, 0.2f, 0.2f));

                        Gizmos.color = myGrid.squareArray[i, j].bottomRight.isOn ? Color.black : Color.white;
                        Gizmos.DrawCube(myGrid.squareArray[i, j].bottomRight.position, new Vector3(0.2f, 0.2f, 0.2f));

                    }
                    Gizmos.color = myGrid.squareArray[i, j].topLeft.isOn ? Color.black : Color.white;
                    Gizmos.DrawCube(myGrid.squareArray[i, j].topLeft.position, new Vector3(0.2f, 0.2f, 0.2f));

                    Gizmos.color = myGrid.squareArray[i, j].topRight.isOn ? Color.black : Color.white;
                    Gizmos.DrawCube(myGrid.squareArray[i, j].topRight.position, new Vector3(0.2f, 0.2f, 0.2f));

                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(myGrid.squareArray[i, j].bottomLeft.top.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].bottomLeft.right.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].bottomRight.top.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].bottomRight.right.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].topLeft.top.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].topLeft.right.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].topRight.top.position, new Vector3(0.1f, 0.1f, 0.1f));
                    Gizmos.DrawCube(myGrid.squareArray[i, j].topRight.right.position, new Vector3(0.1f, 0.1f, 0.1f));

                }
            }
        }
    }

    class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;
        public Node(Vector3 _pos)
        {
            this.position = _pos;
        }

    }

    class ControlNode:Node
    {
        public bool isOn;
        public Node top;
        public Node right;
        private float squareSize;
        public ControlNode(Vector3 _pos, bool _on,float _squareSize) : base(_pos)
        {
            this.isOn = _on;
            this.squareSize = _squareSize;
            this.top = new Node(new Vector3(this.position.x, this.position.y, this.position.z+ _squareSize/2));
            this.right = new Node(new Vector3(this.position.x + _squareSize / 2, this.position.y, this.position.z ));
        }
    }

    class Square
    {
        public Node top;
        public Node bottom;
        public Node left;
        public Node right;

        public ControlNode bottomLeft;
        public ControlNode bottomRight;
        public ControlNode topRight;
        public ControlNode topLeft;

        public int conFigCode =0;

        Vector3 position;
        float squareSize;

        public Square( float _size, ControlNode _bl, ControlNode _br, ControlNode _tr, ControlNode _tl)
        {
            this.bottomLeft = _bl;
            this.bottomRight = _br;
            this.topRight = _tr;
            this.topLeft = _tl;

            this.conFigCode += this.bottomLeft.isOn ? 8 : 0;
            this.conFigCode += this.bottomRight.isOn ? 4 : 0;
            this.conFigCode += this.topRight.isOn ? 2 : 0;
            this.conFigCode += this.topLeft.isOn ? 1 : 0;

            this.position = new Vector3(_bl.position.x+squareSize/2,0, _bl.position.y + squareSize / 2);
            this.squareSize = _size;

            this.top = this.topLeft.right;
            this.bottom = this.bottomLeft.right;
            this.left = this.bottomLeft.top;
            this.right = this.bottomRight.top;
        }
    }
    class SqureGrid
    {
        float squareSize;
        public Square[,] squareArray;
        public ControlNode[,] ctrlArray;

        int squareLengthX; //number of squres
        int squareLengthY; 

        float mapWidth;
        float mapHeight;

        public SqureGrid(int[,] _data, float _size)
        {
            int xSize = _data.GetLength(0);
            int ySize = _data.GetLength(1);

            this.squareSize = _size;

            squareLengthX = xSize - 1;
            squareLengthY = ySize - 1;

            this.mapWidth = squareLengthX * squareSize;
            this.mapHeight = squareLengthY * squareSize;

            this.squareSize = _size;
            ctrlArray = new ControlNode[xSize, ySize];
            squareArray = new Square[squareLengthX, squareLengthY];

            // new control point
            for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    ControlNode myNode = new ControlNode(new Vector3(i * squareSize - mapWidth / 2, 0, j * squareSize - mapHeight / 2), _data[i, j] == 1, squareSize);
                    ctrlArray[i, j] = myNode;
                }
            }

            for (int i = 0; i < xSize-1; i++)
            {
                for (int j = 0; j < ySize-1; j++)
                {
                    Square mySquare = new Square(squareSize, ctrlArray[i, j], ctrlArray[i+1, j], ctrlArray[i + 1, j+1], ctrlArray[i, j + 1]);
                    squareArray[i, j] = mySquare;
                }
            }
            

        }
    }

}
