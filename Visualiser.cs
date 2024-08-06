using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance;
using System.Runtime.Intrinsics;

namespace MatrixVisual
{
    internal record struct MatmulItem(Color Color, MatrixTile Tile);
    internal class Visualiser
    {
        public static Visualiser Instance { get; set; } = null!;
        
        
        public SpriteBatch SpriteBatch { get; set; }


        private Stack<MatmulItem[]> _items = new();
        private List<MatmulItem> _buffer = new();

        public static readonly MatmulItem DefaultMat = new MatmulItem(Color.White, new MatrixTile(0, default, default));

        private float[] _a;
        private float[] _b;
        private float[] _c;
        private float[] _block;

        private MatrixTile[,] _dispA;
        private MatrixTile[,] _dispB;
        private MatrixTile[,] _dispC;
        private MatrixTile[,] _dispBlock;

        private int _m;
        private int _k;
        private int _n;

        public Visualiser(int m, int k, int n, Vector2 a, Vector2 b, Vector2 c, Vector2 bloc, int width)
        {
            _m = m;
            _k = k;
            _n = n;
            _a = new float[m * k];
            _b = new float[k * n];
            _c = new float[m * n];
            _block = new float[64 * 64];

            _dispA = new MatrixTile[m, k];
            _dispB = new MatrixTile[k, n];
            _dispC = new MatrixTile[m, n];
            _dispBlock = new MatrixTile[64, 64];
            GenerateMatrix(ref _dispA, m, k, a, Color.Red);
            GenerateMatrix(ref _dispB, k, n, b, Color.Green);
            GenerateMatrix(ref _dispC, m, n, c, Color.Blue);
            GenerateMatrix(ref _dispBlock, 64, 64, bloc, Color.OrangeRed);

            void GenerateMatrix(ref MatrixTile[,] tile, int x, int y, Vector2 poss, Color c)
            {
                tile = new MatrixTile[y, x];
                for (int i = 0; i < y; i++)
                {
                    for (int j = 0; j < x; j++)
                    {
                        Rectangle r = new Rectangle(
                            (new Vector2(j, i) * (width) + poss).ToPoint(), new Point(width - 1));

                        tile[i, j] = new MatrixTile(0, r
                            , c);
                    }
                }
            }
        }

        public ref float AddItem(ref float location, Color? color = null)
        {
            if (GetFromArr(_dispA, _a, ref location, color ?? Color.White))
                return ref location;
            if (GetFromArr(_dispB, _b, ref location, color ?? Color.White))
                return ref location;
            if (GetFromArr(_dispC, _c, ref location, color ?? Color.White))
                return ref location;
            if (GetFromArr(_dispBlock, _block, ref location, color ?? Color.White))
                return ref location;

            System.Diagnostics.Debug.WriteLine("you fucked up");
            return ref location;
            throw new InvalidOperationException();

            bool GetFromArr(MatrixTile[,] tiles, float[] arr, ref float location, Color color)
            {
                Span<MatrixTile> mtile = tiles.AsSpan();
                ref float arrStart = ref arr[0];
                ref float arrEnd = ref arr[arr.Length - 1];

                if (Unsafe.IsAddressGreaterThan(ref location, ref arrEnd) || Unsafe.IsAddressLessThan(ref location, ref arrStart))
                {
                    return false;
                }

                int index = Unsafe.ByteOffset(ref arrStart, ref location).ToInt32() / Unsafe.SizeOf<float>();

                _buffer.Add(new MatmulItem(color, mtile[index]));
                return true;
            }
        }

        public void NextTurn()
        {
            _items.Push(_buffer.ToArray());
            _buffer.Clear();
        }

        public IEnumerable<MatmulItem[]> GetItems() => _items;

        public void Update()
        {
            foreach (var t in _dispA)
                t.Update();
            foreach (var t in _dispB)
                t.Update();
            foreach (var t in _dispC)
                t.Update();
            foreach (var t in _dispBlock)
                t.Update();
        }

        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            foreach (var t in _dispA.AsSpan())
                sb.Draw(pixel, t.Bounds, t.DrawColor);
            foreach (var t in _dispB.AsSpan())
                sb.Draw(pixel, t.Bounds, t.DrawColor);
            foreach (var t in _dispC.AsSpan())
                sb.Draw(pixel, t.Bounds, t.DrawColor);
            foreach (var t in _dispBlock.AsSpan())
                sb.Draw(pixel, t.Bounds, t.DrawColor);
        }

        public void Init()
        {
            PackB.MatMulPackB(_m, _k, _n, ref _a[0], ref _b[0], ref _c[0], ref _block[0]);
            return;
            PackB.MatMul(_m, _k, _n, ref _a[0], ref _b[0], ref _c[0], ref _block[0]);
        }
    }

    internal static class Ext
    {
        public static ref float Record(this ref float value, Color? color = null)
        {
            return ref Visualiser.Instance.AddItem(ref value, color);
        }

        public static ref Vector256<float> Record(this ref Vector256<float> value, Color? color = null)
        {
            ref float asf = ref Unsafe.As<Vector256<float>, float>(ref value);
            for(int i = 0; i < 8; i++)
            {
                Visualiser.Instance.AddItem(ref Unsafe.Add(ref asf, i), color);
            }
            return ref value;
        }

        public static ref float RecordAsVec(this ref float value, Color? color = null)
        {
            for (int i = 0; i < 8; i++)
            {
                Visualiser.Instance.AddItem(ref Unsafe.Add(ref value, i), color);
            }
            return ref value;
        }

        public static ref T EndOp<T>(this ref T value)
            where T : struct
        {
            Visualiser.Instance.NextTurn();
            return ref value;
        }

    }
}
