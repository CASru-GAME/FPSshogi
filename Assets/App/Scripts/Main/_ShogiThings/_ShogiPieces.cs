namespace App.Main.ShogiThings
{
    public class King : IPiece
    {
        public PieceType Type { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public King()
        {
            Type = PieceType.King;
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
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Hisya()
        {
            Type = PieceType.Hisya;
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
        }
    }

    public class Kakugyo : IPiece
    {
        public PieceType Type { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Kakugyo()
        {
            Type = PieceType.Kakugyo;
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
        }
    }

    public class Kin : IPiece
    {
        public PieceType Type { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Kin()
        {
            Type = PieceType.Kin;
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
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Gin()
        {
            Type = PieceType.Gin;
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
        }
    }

    public class Keima : IPiece
    {
        public PieceType Type { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Keima()
        {
            Type = PieceType.Keima;
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
        }
    }

    public class Kyosya : IPiece
    {
        public PieceType Type { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Kyosya()
        {
            Type = PieceType.Kyosya;
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
        }
    }

    public class Fuhyo : IPiece
    {
        public PieceType Type { get; private set; }
        public int[][] Movement { get; private set; }
        public int[][] PromotedMovement { get; private set; }
        public bool IsPromoted { get; private set; }
        public bool IsPromotable { get; private set; }

        public Fuhyo()
        {
            Type = PieceType.fuhyo;
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
        }
    }

    public interface IPiece
    {
        public PieceType Type { get; }
        public int[][] Movement { get; }
        public int[][] PromotedMovement { get; }
        public bool IsPromoted { get; }
        public bool IsPromotable { get; }
        public void Promote();
    }

    public enum PieceType
    {
        King,
        Hisya,
        Kakugyo,
        Kin,
        Gin,
        Keima,
        Kyosya,
        fuhyo
    }
}
