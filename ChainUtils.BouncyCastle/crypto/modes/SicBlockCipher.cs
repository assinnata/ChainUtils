using System;
using ChainUtils.BouncyCastle.Crypto.Parameters;

namespace ChainUtils.BouncyCastle.Crypto.Modes
{
    /**
    * Implements the Segmented Integer Counter (SIC) mode on top of a simple
    * block cipher.
    */
    public class SicBlockCipher
        : IBlockCipher
    {
        private readonly IBlockCipher cipher;
        private readonly int blockSize;
        private readonly byte[] IV;
        private readonly byte[] counter;
        private readonly byte[] counterOut;

        /**
        * Basic constructor.
        *
        * @param c the block cipher to be used.
        */
        public SicBlockCipher(IBlockCipher cipher)
        {
            this.cipher = cipher;
            blockSize = cipher.GetBlockSize();
            IV = new byte[blockSize];
            counter = new byte[blockSize];
            counterOut = new byte[blockSize];
        }

        /**
        * return the underlying block cipher that we are wrapping.
        *
        * @return the underlying block cipher that we are wrapping.
        */
        public IBlockCipher GetUnderlyingCipher()
        {
            return cipher;
        }

        public void Init(
            bool				forEncryption, //ignored by this CTR mode
            ICipherParameters	parameters)
        {
            if (parameters is ParametersWithIV)
            {
                var ivParam = (ParametersWithIV) parameters;
                var iv = ivParam.GetIV();
                Array.Copy(iv, 0, IV, 0, IV.Length);

                Reset();

                // if null it's an IV changed only.
                if (ivParam.Parameters != null)
                {
                    cipher.Init(true, ivParam.Parameters);
                }
            }
            else
            {
                throw new ArgumentException("SIC mode requires ParametersWithIV", "parameters");
            }
        }

        public string AlgorithmName
        {
            get { return cipher.AlgorithmName + "/SIC"; }
        }

        public bool IsPartialBlockOkay
        {
            get { return true; }
        }

        public int GetBlockSize()
        {
            return cipher.GetBlockSize();
        }

        public int ProcessBlock(
            byte[]	input,
            int		inOff,
            byte[]	output,
            int		outOff)
        {
            cipher.ProcessBlock(counter, 0, counterOut, 0);

            //
            // XOR the counterOut with the plaintext producing the cipher text
            //
            for (var i = 0; i < counterOut.Length; i++)
            {
                output[outOff + i] = (byte)(counterOut[i] ^ input[inOff + i]);
            }

            // Increment the counter
            var j = counter.Length;
            while (--j >= 0 && ++counter[j] == 0)
            {
            }

            return counter.Length;
        }

        public void Reset()
        {
            Array.Copy(IV, 0, counter, 0, counter.Length);
            cipher.Reset();
        }
    }
}
