using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Microsoft.Xna.Framework;
using static System.Runtime.CompilerServices.Unsafe;
using Vec = System.Runtime.Intrinsics.Vector256<float>;

namespace MatrixVisual
{
    internal static class PackB
    {
        const int MaxBlockHeight = 64;
        const int MaxBlockWidth = 64;

        public static unsafe void MatMul(int _m, int _k, int _n, ref float a, ref float b, ref float c, ref float block)
        {
            int aRows = _m;
            int aCols = _k;
            int bCols = _n;
            int bIdx, cIdx;

            for (int k = 0; k < aCols; k += MaxBlockHeight)
            {
                int height = (aCols - k) > MaxBlockHeight ? MaxBlockHeight : (aCols - k);
                cIdx = 0;
                bIdx = k * bCols;
                for (int j = 0; j < bCols; j += MaxBlockWidth, cIdx += MaxBlockWidth, bIdx += MaxBlockWidth)
                {
                    int width = (bCols - j) > MaxBlockWidth ? MaxBlockWidth : (bCols - j);
                    ExecBlock(ref a.Slice(k), ref b.Slice(bIdx), ref c.Slice(cIdx), aRows, height, width, aRows, aCols, bCols,ref block);
                }
            }
        }

        static ref float Slice(this ref float value, int n)
        {
            return ref Add(ref value, n);
        }

        static void CopyTo(this ref Vec value, ref float dest)
        {

        }

        // (m, p) * (p, n)
        private static unsafe void ExecBlock(ref float a, ref float b, ref float c, int m, int p, int n, int aRows, int aCols, int bCols, ref float packedB)
        {
            int aIdx, bIdx, cIdx;

            for (int i = 0; i <= m - 8; i += 8)
            {
                cIdx = i * bCols;
                aIdx = i * aCols;
                for (int j = 0; j <= n - 8; j += 8, cIdx += 8)
                {
                    bIdx = j * p;
                    if (i == 0) PackMatrixBWith8xP(ref b.Slice(j), ref packedB.Slice(bIdx), p, bCols);
                    Kernel32b8x8(ref a.Slice(aIdx), ref packedB.Slice(bIdx), ref c.Slice(cIdx), p, 8, aRows, aCols, bCols);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                a.EndOp();
            }
        }

        private static void PackMatrixBWith8xP(ref float src, ref float dst, int p, int bCols)
        {
            int dstIdx = 0;
            Vec data;
            for (int k = 0; k < p; k++, dstIdx += 8)
            {
                data = As<float, Vec>(ref src.Slice(k * bCols)).Record(Color.Yellow);
                data.CopyTo(ref dst.Slice(dstIdx).RecordAsVec(Color.Yellow));
            }
            src.EndOp();
        }

        private static unsafe void Kernel32b8x8(ref float a, ref float b, ref float c, int p, int n, int aRows, int aCols, int bCols)
        {
            int offset = 0;
            int aIdx;
            int cIdx = bCols;

            Vec c_0_v = As<float, Vec>(ref c);
            Vec c_1_v = As<float, Vec>(ref c.Slice(cIdx));
            cIdx += bCols;
            Vec c_2_v = As<float, Vec>(ref c.Slice(cIdx));
            cIdx += bCols;
            Vec c_3_v = As<float, Vec>(ref c.Slice(cIdx));
            cIdx += bCols;
            Vec c_4_v = As<float, Vec>(ref c.Slice(cIdx));
            cIdx += bCols;
            Vec c_5_v = As<float, Vec>(ref c.Slice(cIdx));
            cIdx += bCols;
            Vec c_6_v = As<float, Vec>(ref c.Slice(cIdx));
            cIdx += bCols;
            Vec c_7_v = As<float, Vec>(ref c.Slice(cIdx));

            Vec b_v;

            for (int k = 0; k < p; k++)
            {
                b_v = As<float, Vec>(ref b.Slice(offset)).Record();
                offset += n;
                aIdx = k;

                c_0_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_1_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_2_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_3_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_4_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_5_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_6_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
                aIdx += aCols;
                c_7_v += Vector256.Multiply(b_v, Add(ref a, aIdx).Record());
            }
            cIdx = bCols;
            c_0_v.CopyTo(ref c.RecordAsVec());
            c_1_v.CopyTo(ref c.Slice(cIdx).RecordAsVec());
            cIdx += bCols;
            c_2_v.CopyTo(ref c.Slice(cIdx).RecordAsVec());
            cIdx += bCols;
            c_3_v.CopyTo(ref c.Slice(cIdx).RecordAsVec());
            cIdx += bCols;
            c_4_v.CopyTo(ref c.Slice(cIdx).RecordAsVec());
            cIdx += bCols;
            c_5_v.CopyTo(ref c.Slice(cIdx).RecordAsVec());
            cIdx += bCols;
            c_6_v.CopyTo(ref c.Slice(cIdx).RecordAsVec());
            cIdx += bCols;
            c_7_v.CopyTo(ref c.Slice(cIdx).RecordAsVec().EndOp());
        }

        [SkipLocalsInit]
        public static void MatMulPackB(int m, int k, int n, ref float a, ref float b, ref float c, ref float packedB)
        {
            //ref float packedB = ref (stackalloc float[MaxBlockHeight * MaxBlockWidth])[0];
            //uninitalized max block size data

            for (int p = 0; p < k; p += MaxBlockHeight)
            {
                int blockHeight = Math.Min(MaxBlockHeight, m - p);
                for (int j = 0; j < n; j += MaxBlockWidth)
                {
                    int blockWidth = Math.Min(MaxBlockWidth, n - j);

                    for (int i1 = 0; i1 < blockHeight; i1 += 8)
                    {
                        for (int j1 = 0; j1 < blockWidth; j1 += 8)
                        {
                            if(i1 == 0)
                                Pack(ref Add(ref b, p * n + j + j1), ref Add(ref packedB, blockWidth * j1), blockHeight, n);

                            AddDot8x8_Packed(blockHeight, ref Add(ref a, i1 * k), n, ref Add(ref packedB, blockWidth * j1), ref Add(ref c, i1 * n + j + j1), k);
                            k.EndOp();
                        }
                    }
                }
            }
        }

        private static void Pack(ref float source, ref float dest, int blockSize, int n)
        {
            for(int i = 0; i < blockSize; i++)
            {
                Add(ref As<float, Vec>(ref dest), i).Record() = As<float, Vec>(ref Add(ref source, n * i)).Record(Color.Yellow);
            }

            source.EndOp();
        }

        static void AddDot8x8_Packed(int block_k, ref float a, int incx, ref float bpacked, ref float gamma, int k)
        {
            Vec c_n_0 = Vec.Zero;
            Vec c_n_1 = Vec.Zero;
            Vec c_n_2 = Vec.Zero;
            Vec c_n_3 = Vec.Zero;
            Vec c_n_4 = Vec.Zero;
            Vec c_n_5 = Vec.Zero;
            Vec c_n_6 = Vec.Zero;
            Vec c_n_7 = Vec.Zero;

            ref float aptr_0 = ref Add(ref a, 0 * k);
            ref float aptr_1 = ref Add(ref a, 1 * k);
            ref float aptr_2 = ref Add(ref a, 2 * k);
            ref float aptr_3 = ref Add(ref a, 3 * k);
            ref float aptr_4 = ref Add(ref a, 4 * k);
            ref float aptr_5 = ref Add(ref a, 5 * k);
            ref float aptr_6 = ref Add(ref a, 6 * k);
            ref float aptr_7 = ref Add(ref a, 7 * k);

            ref Vec bptr_n = ref As<float, Vec>(ref bpacked);

            for (int p = 0; p < block_k; p++)
            {
                c_n_0 = Fma.MultiplyAdd(Vector256.Create(aptr_0.Record()), bptr_n, c_n_0);
                c_n_1 = Fma.MultiplyAdd(Vector256.Create(aptr_1.Record()), bptr_n, c_n_1);
                c_n_2 = Fma.MultiplyAdd(Vector256.Create(aptr_2.Record()), bptr_n, c_n_2);
                c_n_3 = Fma.MultiplyAdd(Vector256.Create(aptr_3.Record()), bptr_n, c_n_3);
                c_n_4 = Fma.MultiplyAdd(Vector256.Create(aptr_4.Record()), bptr_n, c_n_4);
                c_n_5 = Fma.MultiplyAdd(Vector256.Create(aptr_5.Record()), bptr_n, c_n_5);
                c_n_6 = Fma.MultiplyAdd(Vector256.Create(aptr_6.Record()), bptr_n, c_n_6);
                c_n_7 = Fma.MultiplyAdd(Vector256.Create(aptr_7.Record()), bptr_n, c_n_7);

                aptr_0 = ref Add(ref aptr_0, 1);
                aptr_1 = ref Add(ref aptr_1, 1);
                aptr_2 = ref Add(ref aptr_2, 1);
                aptr_3 = ref Add(ref aptr_3, 1);
                aptr_4 = ref Add(ref aptr_4, 1);
                aptr_5 = ref Add(ref aptr_5, 1);
                aptr_6 = ref Add(ref aptr_6, 1);
                aptr_7 = ref Add(ref aptr_7, 1);

                bptr_n = ref Add(ref bptr_n, 1);
            }

            As<float, Vec>(ref Add(ref gamma, 0 * incx)).Record() = c_n_0;
            As<float, Vec>(ref Add(ref gamma, 1 * incx)).Record() = c_n_1;
            As<float, Vec>(ref Add(ref gamma, 2 * incx)).Record() = c_n_2;
            As<float, Vec>(ref Add(ref gamma, 3 * incx)).Record() = c_n_3;
            As<float, Vec>(ref Add(ref gamma, 4 * incx)).Record() = c_n_4;
            As<float, Vec>(ref Add(ref gamma, 5 * incx)).Record() = c_n_5;
            As<float, Vec>(ref Add(ref gamma, 6 * incx)).Record() = c_n_6;
            As<float, Vec>(ref Add(ref gamma, 7 * incx)).Record() = c_n_7;
        }


        #region 8x8
        public static void MatMul8x8(int m, int k, int n, ref float a, ref float b, ref float c)
        {
            //mat A is mxk
            //mat B is kxn
            for (int j = 0; j < n; j += 8)
            {
                for (int i = 0; i < m; i += 8)
                {
                    AddDot8x8(k, ref Add(ref a, i * k), n, ref Add(ref b, j), ref Add(ref c, i * n + j));
                }
            }
        }

        static void AddDot8x8(int k, ref float a, int incx, ref float b, ref float gamma)
        {
            Vec c_n_0 = Vec.Zero;
            Vec c_n_1 = Vec.Zero;
            Vec c_n_2 = Vec.Zero;
            Vec c_n_3 = Vec.Zero;
            Vec c_n_4 = Vec.Zero;
            Vec c_n_5 = Vec.Zero;
            Vec c_n_6 = Vec.Zero;
            Vec c_n_7 = Vec.Zero;

            ref float aptr_0 = ref Add(ref a, 0 * k);
            ref float aptr_1 = ref Add(ref a, 1 * k);
            ref float aptr_2 = ref Add(ref a, 2 * k);
            ref float aptr_3 = ref Add(ref a, 3 * k);
            ref float aptr_4 = ref Add(ref a, 4 * k);
            ref float aptr_5 = ref Add(ref a, 5 * k);
            ref float aptr_6 = ref Add(ref a, 6 * k);
            ref float aptr_7 = ref Add(ref a, 7 * k);

            ref Vec bptr_n = ref As<float, Vec>(ref b);

            for (int p = 0; p < k; p++)
            {
                c_n_0 = Fma.MultiplyAdd(Vector256.Create(aptr_0.Record()), bptr_n.Record(), c_n_0);
                c_n_1 = Fma.MultiplyAdd(Vector256.Create(aptr_1.Record()), bptr_n, c_n_1);
                c_n_2 = Fma.MultiplyAdd(Vector256.Create(aptr_2.Record()), bptr_n, c_n_2);
                c_n_3 = Fma.MultiplyAdd(Vector256.Create(aptr_3.Record()), bptr_n, c_n_3);
                c_n_4 = Fma.MultiplyAdd(Vector256.Create(aptr_4.Record()), bptr_n, c_n_4);
                c_n_5 = Fma.MultiplyAdd(Vector256.Create(aptr_5.Record()), bptr_n, c_n_5);
                c_n_6 = Fma.MultiplyAdd(Vector256.Create(aptr_6.Record()), bptr_n, c_n_6);
                c_n_7 = Fma.MultiplyAdd(Vector256.Create(aptr_7.Record()), bptr_n, c_n_7);

                aptr_0 = ref Add(ref aptr_0, 1);
                aptr_1 = ref Add(ref aptr_1, 1);
                aptr_2 = ref Add(ref aptr_2, 1);
                aptr_3 = ref Add(ref aptr_3, 1);
                aptr_4 = ref Add(ref aptr_4, 1);
                aptr_5 = ref Add(ref aptr_5, 1);
                aptr_6 = ref Add(ref aptr_6, 1);
                aptr_7 = ref Add(ref aptr_7, 1);

                bptr_n = ref Add(ref bptr_n, incx / 8);
                a.EndOp();
            }

            As<float, Vec>(ref Add(ref gamma, 0 * incx)).Record() = c_n_0;
            As<float, Vec>(ref Add(ref gamma, 1 * incx)).Record() = c_n_1;
            As<float, Vec>(ref Add(ref gamma, 2 * incx)).Record() = c_n_2;
            As<float, Vec>(ref Add(ref gamma, 3 * incx)).Record() = c_n_3;
            As<float, Vec>(ref Add(ref gamma, 4 * incx)).Record() = c_n_4;
            As<float, Vec>(ref Add(ref gamma, 5 * incx)).Record() = c_n_5;
            As<float, Vec>(ref Add(ref gamma, 6 * incx)).Record() = c_n_6;
            As<float, Vec>(ref Add(ref gamma, 7 * incx)).Record() = c_n_7;

            a.EndOp();
        }
        #endregion

        #region 4x4
        //[SkipLocalsInit]
        public static void MatMul4x4(int m, int k, int n, ref float a, ref float b, ref float c)
        {
            //mat A is mxk
            //mat B is kxn
            for (int j = 0; j < n; j += 4)
            {
                for (int i = 0; i < m; i += 4)
                {
                    AddDot4x4(k, ref Add(ref a, (i + 0) * k), n, ref Add(ref b, j), ref Add(ref c, (i + 0) * n + j));
                }
            }
        }

        static void AddDot4x4(int k, ref float a, int incx, ref float b, ref float gamma)
        {
            float c_0_0 = 0; float c_0_1 = 0; float c_0_2 = 0; float c_0_3 = 0;
            float c_1_0 = 0; float c_1_1 = 0; float c_1_2 = 0; float c_1_3 = 0;
            float c_2_0 = 0; float c_2_1 = 0; float c_2_2 = 0; float c_2_3 = 0;
            float c_3_0 = 0; float c_3_1 = 0; float c_3_2 = 0; float c_3_3 = 0;

            SkipInit(out float a_0);
            SkipInit(out float a_1);
            SkipInit(out float a_2);
            SkipInit(out float a_3);

            SkipInit(out float b_0);
            SkipInit(out float b_1);
            SkipInit(out float b_2);
            SkipInit(out float b_3);

            ref float bptr_0 = ref Add(ref b, 0 - incx);
            ref float bptr_1 = ref Add(ref b, 1 - incx);
            ref float bptr_2 = ref Add(ref b, 2 - incx);
            ref float bptr_3 = ref Add(ref b, 3 - incx);

            for (int p = 0; p < k; p++)
            {
                a_0 = Add(ref a, p + 0 * k).Record();
                a_1 = Add(ref a, p + 1 * k).Record();
                a_2 = Add(ref a, p + 2 * k).Record();
                a_3 = Add(ref a, p + 3 * k).Record();

                b_0 = bptr_0 = ref Add(ref bptr_0, incx).Record();
                b_1 = bptr_1 = ref Add(ref bptr_1, incx).Record();
                b_2 = bptr_2 = ref Add(ref bptr_2, incx).Record();
                b_3 = bptr_3 = ref Add(ref bptr_3, incx).Record();

                c_0_0 += a_0 * b_0;
                c_1_0 += a_0 * b_1;
                c_2_0 += a_0 * b_2;
                c_3_0 += a_0 * b_3;

                c_0_1 += a_1 * b_0;
                c_1_1 += a_1 * b_1;
                c_2_1 += a_1 * b_2;
                c_3_1 += a_1 * b_3;

                c_0_2 += a_2 * b_0;
                c_1_2 += a_2 * b_1;
                c_2_2 += a_2 * b_2;
                c_3_2 += a_2 * b_3;

                c_0_3 += a_3 * b_0;
                c_1_3 += a_3 * b_1;
                c_2_3 += a_3 * b_2;
                c_3_3 += a_3 * b_3;
            }

            Add(ref gamma, 0 + 0 * incx).Record() = c_0_0; Add(ref gamma, 0 + 1 * incx).Record() = c_0_1; Add(ref gamma, 0 + 2 * incx).Record() = c_0_2; Add(ref gamma, 0 + 3 * incx).Record() = c_0_3;
            Add(ref gamma, 1 + 0 * incx).Record() = c_1_0; Add(ref gamma, 1 + 1 * incx).Record() = c_1_1; Add(ref gamma, 1 + 2 * incx).Record() = c_1_2; Add(ref gamma, 1 + 3 * incx).Record() = c_1_3;
            Add(ref gamma, 2 + 0 * incx).Record() = c_2_0; Add(ref gamma, 2 + 1 * incx).Record() = c_2_1; Add(ref gamma, 2 + 2 * incx).Record() = c_2_2; Add(ref gamma, 2 + 3 * incx).Record() = c_2_3;
            Add(ref gamma, 3 + 0 * incx).Record() = c_3_0; Add(ref gamma, 3 + 1 * incx).Record() = c_3_1; Add(ref gamma, 3 + 2 * incx).Record() = c_3_2; Add(ref gamma, 3 + 3 * incx).Record() = c_3_3;

            gamma.EndOp();
        }
        #endregion
    }
}
