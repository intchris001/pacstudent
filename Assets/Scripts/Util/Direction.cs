using UnityEngine;

namespace PacmanGame.Util
{
    public enum Dir { None, Left, Up, Right, Down }

    public static class DirectionUtil
    {
        public static Vector2Int ToVec(Dir d)
        {
            return d switch
            {
                Dir.Left => new Vector2Int(-1, 0),
                Dir.Up => new Vector2Int(0, -1), // grid y increases downwards; up is -1
                Dir.Right => new Vector2Int(1, 0),
                Dir.Down => new Vector2Int(0, 1),
                _ => Vector2Int.zero,
            };
        }

        public static Dir FromVec(Vector2Int v)
        {
            if (v == new Vector2Int(-1, 0)) return Dir.Left;
            if (v == new Vector2Int(1, 0)) return Dir.Right;
            if (v == new Vector2Int(0, -1)) return Dir.Up;
            if (v == new Vector2Int(0, 1)) return Dir.Down;
            return Dir.None;
        }

        public static Dir Opposite(Dir d)
        {
            return d switch
            {
                Dir.Left => Dir.Right,
                Dir.Right => Dir.Left,
                Dir.Up => Dir.Down,
                Dir.Down => Dir.Up,
                _ => Dir.None,
            };
        }
    }
}

