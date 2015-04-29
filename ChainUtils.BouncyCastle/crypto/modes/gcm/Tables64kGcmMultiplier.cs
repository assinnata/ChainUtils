using ChainUtils.BouncyCastle.Crypto.Utilities;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Modes.Gcm
{
    public class Tables64kGcmMultiplier
        : IGcmMultiplier
    {
        private byte[] H;
        private uint[][][] M;

        public void Init(byte[] H)
        {
            if (M == null)
            {
                M = new uint[16][][];
            }
            else if (Arrays.AreEqual(this.H, H))
            {
                return;
            }

            this.H = Arrays.Clone(H);

            M[0] = new uint[256][];
            M[0][0] = new uint[4];
            M[0][128] = GcmUtilities.AsUints(H);
            for (var j = 64; j >= 1; j >>= 1)
            {
                var tmp = (uint[])M[0][j + j].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[0][j] = tmp;
            }
            for (var i = 0; ; )
            {
                for (var j = 2; j < 256; j += j)
                {
                    for (var k = 1; k < j; ++k)
                    {
                        var tmp = (uint[])M[i][j].Clone();
                        GcmUtilities.Xor(tmp, M[i][k]);
                        M[i][j + k] = tmp;
                    }
                }

                if (++i == 16) return;

                M[i] = new uint[256][];
                M[i][0] = new uint[4];
                for (var j = 128; j > 0; j >>= 1)
                {
                    var tmp = (uint[])M[i - 1][j].Clone();
                    GcmUtilities.MultiplyP8(tmp);
                    M[i][j] = tmp;
                }
            }
        }

        public void MultiplyH(byte[] x)
        {
            var z = new uint[4];
            for (var i = 0; i != 16; ++i)
            {
                //GcmUtilities.Xor(z, M[i][x[i]]);
                var m = M[i][x[i]];
                z[0] ^= m[0];
                z[1] ^= m[1];
                z[2] ^= m[2];
                z[3] ^= m[3];
            }

            Pack.UInt32_To_BE(z, x, 0);
        }
    }
}
