namespace App.Main.ShogiThings
{
    public class King : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public King(PlayerType player)
        {
            Type = PieceType.King;
            Player = player;
            Movement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 },
                new int[] { -1, -1 },
                new int[] { -1, 1 },
                new int[] { 1, -1 }
            };
            IsPromoted = false;
            IsPromotable = false;
        }

        public void Promote()
        {
            IsPromoted = true;
        }
    }

    public class Hisya : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Hisya(PlayerType player)
        {
            Type = PieceType.Hisya;
            Player = player;
            Movement = new int[][] {
                new int[] { 1, 0 },
                new int[] { 2, 0 },
                new int[] { 3, 0 },
                new int[] { 4, 0 },
                new int[] { 5, 0 },
                new int[] { 6, 0 },
                new int[] { 7, 0 },
                new int[] { 8, 0 },
                new int[] { 0, 1 },
                new int[] { 0, 2 },
                new int[] { 0, 3 },
                new int[] { 0, 4 },
                new int[] { 0, 5 },
                new int[] { 0, 6 },
                new int[] { 0, 7 },
                new int[] { 0, 8 },
                new int[] { -1, 0 },
                new int[] { -2, 0 },
                new int[] { -3, 0 },
                new int[] { -4, 0 },
                new int[] { -5, 0 },
                new int[] { -6, 0 },
                new int[] { -7, 0 },
                new int[] { -8, 0 },
                new int[] { 0, -1 },
                new int[] { 0, -2 },
                new int[] { 0, -3 },
                new int[] { 0, -4 },
                new int[] { 0, -5 },
                new int[] { 0, -6 },
                new int[] { 0, -7 },
                new int[] { 0, -8 }
            };
            PromotedMovement = new int[][] {
                new int[] { 1, 0 },
                new int[] { 2, 0 },
                new int[] { 3, 0 },
                new int[] { 4, 0 },
                new int[] { 5, 0 },
                new int[] { 6, 0 },
                new int[] { 7, 0 },
                new int[] { 8, 0 },
                new int[] { 0, 1 },
                new int[] { 0, 2 },
                new int[] { 0, 3 },
                new int[] { 0, 4 },
                new int[] { 0, 5 },
                new int[] { 0, 6 },
                new int[] { 0, 7 },
                new int[] { 0, 8 },
                new int[] { -1, 0 },
                new int[] { -2, 0 },
                new int[] { -3, 0 },
                new int[] { -4, 0 },
                new int[] { -5, 0 },
                new int[] { -6, 0 },
                new int[] { -7, 0 },
                new int[] { -8, 0 },
                new int[] { 0, -1 },
                new int[] { 0, -2 },
                new int[] { 0, -3 },
                new int[] { 0, -4 },
                new int[] { 0, -5 },
                new int[] { 0, -6 },
                new int[] { 0, -7 },
                new int[] { 0, -8 },
                new int[] { 1, 1 },
                new int[] { 1, -1 },
                new int[] { -1, 1 },
                new int[] { -1, -1 }
            };
            IsPromoted = false;
            IsPromotable = true;
        }

        public void Promote()
        {
            IsPromoted = true;
            Movement = PromotedMovement;
        }
    }

    public class Kakugyo : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Kakugyo(PlayerType player)
        {
            Type = PieceType.Kakugyo;
            Player = player;
            Movement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 2, 2 },
                new int[] { 3, 3 },
                new int[] { 4, 4 },
                new int[] { 5, 5 },
                new int[] { 6, 6 },
                new int[] { 7, 7 },
                new int[] { 8, 8 },
                new int[] { -1, -1 },
                new int[] { -2, -2 },
                new int[] { -3, -3 },
                new int[] { -4, -4 },
                new int[] { -5, -5 },
                new int[] { -6, -6 },
                new int[] { -7, -7 },
                new int[] { -8, -8 },
                new int[] { 1, -1 },
                new int[] { 2, -2 },
                new int[] { 3, -3 },
                new int[] { 4, -4 },
                new int[] { 5, -5 },
                new int[] { 6, -6 },
                new int[] { 7, -7 },
                new int[] { 8, -8 },
                new int[] { -1, 1 },
                new int[] { -2, 2 },
                new int[] { -3, 3 },
                new int[] { -4, 4 },
                new int[] { -5, 5 },
                new int[] { -6, 6 },
                new int[] { -7, 7 },
                new int[] { -8, 8 }
            };
            PromotedMovement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 2, 2 },
                new int[] { 3, 3 },
                new int[] { 4, 4 },
                new int[] { 5, 5 },
                new int[] { 6, 6 },
                new int[] { 7, 7 },
                new int[] { 8, 8 },
                new int[] { -1, -1 },
                new int[] { -2, -2 },
                new int[] { -3, -3 },
                new int[] { -4, -4 },
                new int[] { -5, -5 },
                new int[] { -6, -6 },
                new int[] { -7, -7 },
                new int[] { -8, -8 },
                new int[] { 1, -1 },
                new int[] { 2, -2 },
                new int[] { 3, -3 },
                new int[] { 4, -4 },
                new int[] { 5, -5 },
                new int[] { 6, -6 },
                new int[] { 7, -7 },
                new int[] { 8, -8 },
                new int[] { -1, 1 },
                new int[] { -2, 2 },
                new int[] { -3, 3 },
                new int[] { -4, 4 },
                new int[] { -5, 5 },
                new int[] { -6, 6 },
                new int[] { -7, 7 },
                new int[] { -8, 8 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 }
            };
            IsPromoted = false;
            IsPromotable = true;
        }
        public void Promote()
        {
            IsPromoted = true;
            Movement = PromotedMovement;
        }
    }

    public class Kin : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Kin(PlayerType player)
        {
            Type = PieceType.Kin;
            Player = player;
            Movement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 },
                new int[] { -1, 1 }
            };
            PromotedMovement = Movement; // 金将は成っても動きは変わらない
            IsPromoted = false;
            IsPromotable = false;
        }

        public void Promote()
        {
            IsPromoted = true;
        }
    }

    public class Gin : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Gin(PlayerType player)
        {
            Type = PieceType.Gin;
            Player = player;
            Movement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, -1 },
                new int[] { -1, 1 },
                new int[] { -1, -1 },
                new int[] { 0, 1 }
            };
            PromotedMovement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 },
                new int[] { -1, 1 }
            };
            IsPromoted = false;
            IsPromotable = true;
        }

        public void Promote()
        {
            IsPromoted = true;
            Movement = PromotedMovement;
        }
    }

    public class Keima : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Keima(PlayerType player)
        {
            Type = PieceType.Keima;
            Player = player;
            Movement = new int[][] {
                new int[] { 1, 2 },
                new int[] { -1, 2 }
            };
            PromotedMovement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 },
                new int[] { -1, 1 }
            };
            IsPromoted = false;
            IsPromotable = true;
        }

        public void Promote()
        {
            IsPromoted = true;
            Movement = PromotedMovement;
        }
    }

    public class Kyosya : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Kyosya(PlayerType player)
        {
            Type = PieceType.Kyosya;
            Player = player;
            Movement = new int[][] {
                new int[] { 0, 1 },
                new int[] { 0, 2 },
                new int[] { 0, 3 },
                new int[] { 0, 4 },
                new int[] { 0, 5 },
                new int[] { 0, 6 },
                new int[] { 0, 7 },
                new int[] { 0, 8 }
            };
            PromotedMovement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 },
                new int[] { -1, 1 }
            };
            IsPromoted = false;
            IsPromotable = true;
        }

        public void Promote()
        {
            IsPromoted = true;
            Movement = PromotedMovement;
        }
    }

    public class Fuhyo : IPiece
    {
        public PieceType Type { get; private set; }
        public PlayerType Player { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Fuhyo(PlayerType player)
        {
            Type = PieceType.Fuhyo;
            Player = player;
            Movement = new int[][] {
                new int[] { 0, 1 }
            };
            PromotedMovement = new int[][] {
                new int[] { 1, 1 },
                new int[] { 1, 0 },
                new int[] { 0, 1 },
                new int[] { -1, 0 },
                new int[] { 0, -1 },
                new int[] { -1, 1 }
            };
            IsPromoted = false;
            IsPromotable = true;
        }

        public void Promote()
        {
            IsPromoted = true;
            Movement = PromotedMovement;
        }
    }

    public interface IPiece
    {
        public PieceType Type { get; }
        public PlayerType Player { get; }
        public int[][] Movement { get; }
        public int[][] PromotedMovement { get; }
        public bool IsPromoted { get; }
        public bool IsPromotable { get; }
        public void Promote();
    }

    public enum PieceType
    {
        King = 0,
        Hisya = 1,
        Kakugyo = 2,
        Kin = 3,
        Gin = 4,
        Keima = 5,
        Kyosya = 6,
        Fuhyo = 7
    }
    public enum PlayerType
    {
        PlayerOne,
        PlayerTwo
    }
}
