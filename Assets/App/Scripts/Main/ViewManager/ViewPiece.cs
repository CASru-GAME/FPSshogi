using UnityEngine;
using App.Common.Initialize;
using App.Main.GameMaster;
using App.Main.ShogiThings;

namespace App.Main.ViewManager
{

    public class ViewPiece : MonoBehaviour, IInitializable
    {
        public GameObject fuhyo;
        public GameObject kyosya;
        public GameObject keima;
        public GameObject gin;
        public GameObject kin;
        public GameObject kakugyo;
        public GameObject hisya;
        public GameObject ou;
        public GameObject gyoku;

        [SerializeField] private GameObject boardCellPositionMarkers;
        public Vector3[,] boardCellPositions = new Vector3[9, 9];

        public int InitializationPriority => 80; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard) }; // 依存関係
        private ShogiBoard shogiBoard = null;
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // ShogiBoardの参照を取得
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();

            CreateBoardCellPositions();
            CalculateCommonScale();
            // 初期化処理
            SetPieces();
        }

        private GameObject[,] pieceObjects = new GameObject[9, 9];
        private IPiece[,] previousBoardState = new IPiece[9, 9];
        private bool[,] previousPromotion = new bool[9, 9];
        private float boardSurface = 29.845f;
        private float cellSize = 5.2222f;
        private float commonPieceScale=1f;
        private void CalculateCommonScale()
        {
            float largest=0;
            foreach(var prefab in new[]{fuhyo,kyosya,keima,gin,kin,kakugyo,hisya,ou,gyoku})
            {
                if(prefab==null)continue;
                // Asset Renderer.bounds can be stale and includes the source root rotation.
                // Measure mesh corners in the same pose used when instantiating a board piece.
                var pose=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(-90,0,0),prefab.transform.localScale);
                Bounds bounds=default;bool first=true;
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.sharedMesh==null)continue;
                    var matrix=pose*prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    var b=filter.sharedMesh.bounds;
                    for(int corner=0;corner<8;corner++)
                    {
                        var point=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                        if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                    }
                }
                largest=Mathf.Max(largest,bounds.size.x,bounds.size.z);
            }
            commonPieceScale=largest>0?Mathf.Min(1f,cellSize*.78f/largest):1f;
        }

        private Vector3 GetBoardCellPosition(int x, int y)
        {
            return boardCellPositions[x, y];
        }

        public void CreateBoardCellPositions()
        {
            // Centres of the model's nine equal cells (the old hand-placed markers drifted).
            // These are measured on the upper grid, including the Field2 scene transform.
            const float minX=-3.670f, maxX=43.340f, minZ=-22.675f, maxZ=24.325f;
            cellSize=(maxX-minX)/9f;
            for(int x=0;x<9;x++)for(int y=0;y<9;y++)
                boardCellPositions[x,y]=new Vector3(maxX-(x+.5f)*cellSize,boardSurface+.025f,maxZ-(y+.5f)*(maxZ-minZ)/9f);
        }

        private void FitPiece(GameObject piece,int x,int y,bool promoted,PlayerType player)
        {
            piece.transform.rotation=promoted?Quaternion.Euler(90,-90,player==PlayerType.PlayerOne?90:-90):Quaternion.Euler(-90,0,player==PlayerType.PlayerOne?0:-180);
            var renderers=piece.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            piece.transform.localScale*=commonPieceScale;
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Vector3 centre=boardCellPositions[x,y];piece.transform.position+=new Vector3(centre.x-bounds.center.x,centre.y-bounds.min.y,centre.z-bounds.center.z);
        }

        private void SetPieces()
        {
            IPiece[,] board = shogiBoard.GetBoard();
            // 盤面の初期化処理

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    IPiece piece = board[x, y];
                    if (piece == null) continue;

                    GameObject prefab = null;
                    if (piece is Kyosya)
                        prefab = kyosya;
                    else if (piece is Keima)
                        prefab = keima;
                    else if (piece is Gin)
                        prefab = gin;
                    else if (piece is Kin)
                        prefab = kin;
                    else if (piece is Kakugyo)
                        prefab = kakugyo;
                    else if (piece is Hisya)
                        prefab = hisya;
                    else if (piece is Fuhyo)
                        prefab = fuhyo;
                    else if (piece is King)
                        prefab = (piece.Player == PlayerType.PlayerOne) ? ou : gyoku;

                    if (prefab == null) continue;

                    Vector3 position = GetBoardCellPosition(x, y);
                    float rotationZ = (piece.Player == PlayerType.PlayerOne) ? 0f : -180f;

                    GameObject pieceObject = Instantiate(prefab, position, Quaternion.Euler(-90, 0, rotationZ));
                    pieceObjects[x, y] = pieceObject;
                    FitPiece(pieceObject,x,y,piece.IsPromoted,piece.Player);
                }
            }

            // 現在の盤面状態を保存
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    previousBoardState[x, y] = board[x, y];
        }


        private void changeUI()
        {
            IPiece[,] currentBoardState = shogiBoard.GetBoard();

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    IPiece current = currentBoardState[x, y];
                    IPiece previous = previousBoardState[x, y];

                    if (current != previous || (current!=null && previousPromotion[x,y]!=current.IsPromoted))
                    {
                        // 以前の位置にオブジェクトがあれば削除
                        if (pieceObjects[x, y] != null)
                        {
                            Destroy(pieceObjects[x, y]);
                            pieceObjects[x, y] = null;
                        }

                        if (current != null)
                        {
                            // 新しい位置に駒を生成
                            GameObject prefab = null;
                            if (current is Kyosya)
                                prefab = kyosya;
                            else if (current is Keima)
                                prefab = keima;
                            else if (current is Gin)
                                prefab = gin;
                            else if (current is Kin)
                                prefab = kin;
                            else if (current is Kakugyo)
                                prefab = kakugyo;
                            else if (current is Hisya)
                                prefab = hisya;
                            else if (current is Fuhyo)
                                prefab = fuhyo;
                            else if (current is King)
                                prefab = (current.Player == PlayerType.PlayerOne) ? ou : gyoku;

                            Vector3 position = GetBoardCellPosition(x, y);
                            float rotationZ = (current.Player == PlayerType.PlayerOne) ? 0f : -180f;

                            GameObject pieceObject = Instantiate(prefab, position, Quaternion.Euler(-90, 0, rotationZ));
                            pieceObjects[x, y] = pieceObject;
                            FitPiece(pieceObject,x,y,current.IsPromoted,current.Player);
                        }
                    }
                    if (current != null && current.IsPromoted && pieceObjects[x, y] != null)
                    {
                        if (current.Player == PlayerType.PlayerOne)
                            pieceObjects[x, y].transform.rotation = Quaternion.Euler(90, -90, 90);
                        else
                            pieceObjects[x, y].transform.rotation = Quaternion.Euler(90, -90, -90);
                    }
                }
            }

            // 現在の盤面状態を保存
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    previousBoardState[x, y] = currentBoardState[x, y];
                    previousPromotion[x,y] = currentBoardState[x,y]?.IsPromoted ?? false;
                }
        }

        void Update()
        {
            changeUI();
        }

    }
}
