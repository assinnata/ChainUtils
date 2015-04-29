using System;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Digests
{
    /// <summary>
    /// Implementation of SHA-3 based on following KeccakNISTInterface.c from http://keccak.noekeon.org/
    /// </summary>
    /// <remarks>
    /// Following the naming conventions used in the C source code to enable easy review of the implementation.
    /// </remarks>
    public class Sha3Digest
        : IDigest, IMemoable
    {
        private static readonly ulong[] KeccakRoundConstants = KeccakInitializeRoundConstants();

        private static readonly int[] KeccakRhoOffsets = KeccakInitializeRhoOffsets();

        private static ulong[] KeccakInitializeRoundConstants()
        {
            var keccakRoundConstants = new ulong[24];
            byte LFSRState = 0x01;

            for (var i = 0; i < 24; i++)
            {
                keccakRoundConstants[i] = 0;
                for (var j = 0; j < 7; j++)
                {
                    var bitPosition = (1 << j) - 1;

                    // LFSR86540

                    var loBit = (LFSRState & 0x01) != 0;
                    if (loBit)
                    {
                        keccakRoundConstants[i] ^= 1UL << bitPosition;
                    }

                    var hiBit = (LFSRState & 0x80) != 0;
                    LFSRState <<= 1;
                    if (hiBit)
                    {
                        LFSRState ^= 0x71;
                    }

                }
            }

            return keccakRoundConstants;
        }

        private static int[] KeccakInitializeRhoOffsets()
        {
            var keccakRhoOffsets = new int[25];
            int x, y, t, newX, newY;

            var rhoOffset = 0;
            keccakRhoOffsets[(((0) % 5) + 5 * ((0) % 5))] = rhoOffset;
            x = 1;
            y = 0;
            for (t = 1; t < 25; t++)
            {
                //rhoOffset = ((t + 1) * (t + 2) / 2) % 64;
                rhoOffset = (rhoOffset + t) & 63;
                keccakRhoOffsets[(((x) % 5) + 5 * ((y) % 5))] = rhoOffset;
                newX = (0 * x + 1 * y) % 5;
                newY = (2 * x + 3 * y) % 5;
                x = newX;
                y = newY;
            }

            return keccakRhoOffsets;
        }

        private byte[] state = new byte[(1600 / 8)];
        private byte[] dataQueue = new byte[(1536 / 8)];
        private int rate;
        private int bitsInQueue;
        private int fixedOutputLength;
        private bool squeezing;
        private int bitsAvailableForSqueezing;
        private byte[] chunk;
        private byte[] oneByte;

        private void ClearDataQueueSection(int off, int len)
        {
            for (var i = off; i != off + len; i++)
            {
                dataQueue[i] = 0;
            }
        }

        public Sha3Digest()
        {
            Init(0);
        }

        public Sha3Digest(int bitLength)
        {
            Init(bitLength);
        }

        public Sha3Digest(Sha3Digest source)
        {
			CopyIn(source);
		}

		private void CopyIn(Sha3Digest source)
		{
            Array.Copy(source.state, 0, state, 0, source.state.Length);
            Array.Copy(source.dataQueue, 0, dataQueue, 0, source.dataQueue.Length);
            rate = source.rate;
            bitsInQueue = source.bitsInQueue;
            fixedOutputLength = source.fixedOutputLength;
            squeezing = source.squeezing;
            bitsAvailableForSqueezing = source.bitsAvailableForSqueezing;
            chunk = Arrays.Clone(source.chunk);
            oneByte = Arrays.Clone(source.oneByte);
        }

        public virtual string AlgorithmName
        {
            get { return "SHA3-" + fixedOutputLength; }
        }

        public virtual int GetDigestSize()
        {
            return fixedOutputLength / 8;
        }

        public virtual void Update(byte input)
        {
            oneByte[0] = input;

            DoUpdate(oneByte, 0, 8L);
        }

        public virtual void BlockUpdate(byte[] input, int inOff, int len)
        {
            DoUpdate(input, inOff, len * 8L);
        }

        public virtual int DoFinal(byte[] output, int outOff)
        {
            Squeeze(output, outOff, fixedOutputLength);

            Reset();

            return GetDigestSize();
        }

        public virtual void Reset()
        {
            Init(fixedOutputLength);
        }

        /**
         * Return the size of block that the compression function is applied to in bytes.
         *
         * @return internal byte length of a block.
         */
        public virtual int GetByteLength()
        {
            return rate / 8;
        }

        private void Init(int bitLength)
        {
            switch (bitLength)
            {
            case 0:
            case 288:
                InitSponge(1024, 576);
                break;
            case 224:
                InitSponge(1152, 448);
                break;
            case 256:
                InitSponge(1088, 512);
                break;
            case 384:
                InitSponge(832, 768);
                break;
            case 512:
                InitSponge(576, 1024);
                break;
            default:
                throw new ArgumentException("must be one of 224, 256, 384, or 512.", "bitLength");
            }
        }

        private void DoUpdate(byte[] data, int off, long databitlen)
        {
            if ((databitlen % 8) == 0)
            {
                Absorb(data, off, databitlen);
            }
            else
            {
                Absorb(data, off, databitlen - (databitlen % 8));

                var lastByte = new byte[1];

                lastByte[0] = (byte)(data[off + (int)(databitlen / 8)] >> (int)(8 - (databitlen % 8)));
                Absorb(lastByte, off, databitlen % 8);
            }
        }

        private void InitSponge(int rate, int capacity)
        {
            if (rate + capacity != 1600)
            {
                throw new InvalidOperationException("rate + capacity != 1600");
            }
            if ((rate <= 0) || (rate >= 1600) || ((rate % 64) != 0))
            {
                throw new InvalidOperationException("invalid rate value");
            }

            this.rate = rate;
            // this is never read, need to check to see why we want to save it
            //  this.capacity = capacity;
            fixedOutputLength = 0;
            Arrays.Fill(state, (byte)0);
            Arrays.Fill(dataQueue, (byte)0);
            bitsInQueue = 0;
            squeezing = false;
            bitsAvailableForSqueezing = 0;
            fixedOutputLength = capacity / 2;
            chunk = new byte[rate / 8];
            oneByte = new byte[1];
        }

        private void AbsorbQueue()
        {
            KeccakAbsorb(state, dataQueue, rate / 8);

            bitsInQueue = 0;
        }

        private void Absorb(byte[] data, int off, long databitlen)
        {
            long i, j, wholeBlocks;

            if ((bitsInQueue % 8) != 0)
            {
                throw new InvalidOperationException("attempt to absorb with odd length queue.");
            }
            if (squeezing)
            {
                throw new InvalidOperationException("attempt to absorb while squeezing.");
            }

            i = 0;
            while (i < databitlen)
            {
                if ((bitsInQueue == 0) && (databitlen >= rate) && (i <= (databitlen - rate)))
                {
                    wholeBlocks = (databitlen - i) / rate;

                    for (j = 0; j < wholeBlocks; j++)
                    {
                        Array.Copy(data, (int)(off + (i / 8) + (j * chunk.Length)), chunk, 0, chunk.Length);

                        //displayIntermediateValues.displayBytes(1, "Block to be absorbed", curData, rate / 8);

                        KeccakAbsorb(state, chunk, chunk.Length);
                    }

                    i += wholeBlocks * rate;
                }
                else
                {
                    var partialBlock = (int)(databitlen - i);
                    if (partialBlock + bitsInQueue > rate)
                    {
                        partialBlock = rate - bitsInQueue;
                    }
                    var partialByte = partialBlock % 8;
                    partialBlock -= partialByte;
                    Array.Copy(data, off + (int)(i / 8), dataQueue, bitsInQueue / 8, partialBlock / 8);

                    bitsInQueue += partialBlock;
                    i += partialBlock;
                    if (bitsInQueue == rate)
                    {
                        AbsorbQueue();
                    }
                    if (partialByte > 0)
                    {
                        var mask = (1 << partialByte) - 1;
                        dataQueue[bitsInQueue / 8] = (byte)(data[off + ((int)(i / 8))] & mask);
                        bitsInQueue += partialByte;
                        i += partialByte;
                    }
                }
            }
        }

        private void PadAndSwitchToSqueezingPhase()
        {
            if (bitsInQueue + 1 == rate)
            {
                dataQueue[bitsInQueue / 8] |= (byte)(1U << (bitsInQueue % 8));
                AbsorbQueue();
                ClearDataQueueSection(0, rate / 8);
            }
            else
            {
                ClearDataQueueSection((bitsInQueue + 7) / 8, rate / 8 - (bitsInQueue + 7) / 8);
                dataQueue[bitsInQueue / 8] |= (byte)(1U << (bitsInQueue % 8));
            }
            dataQueue[(rate - 1) / 8] |= (byte)(1U << ((rate - 1) % 8));
            AbsorbQueue();

            //displayIntermediateValues.displayText(1, "--- Switching to squeezing phase ---");

            if (rate == 1024)
            {
                KeccakExtract1024bits(state, dataQueue);
                bitsAvailableForSqueezing = 1024;
            }
            else
            {
                KeccakExtract(state, dataQueue, rate / 64);
                bitsAvailableForSqueezing = rate;
            }

            //displayIntermediateValues.displayBytes(1, "Block available for squeezing", dataQueue, bitsAvailableForSqueezing / 8);

            squeezing = true;
        }

        private void Squeeze(byte[] output, int offset, long outputLength)
        {
            long i;
            int partialBlock;

            if (!squeezing)
            {
                PadAndSwitchToSqueezingPhase();
            }
            if ((outputLength % 8) != 0)
            {
                throw new InvalidOperationException("outputLength not a multiple of 8");
            }

            i = 0;
            while (i < outputLength)
            {
                if (bitsAvailableForSqueezing == 0)
                {
                    KeccakPermutation(state);

                    if (rate == 1024)
                    {
                        KeccakExtract1024bits(state, dataQueue);
                        bitsAvailableForSqueezing = 1024;
                    }
                    else

                    {
                        KeccakExtract(state, dataQueue, rate / 64);
                        bitsAvailableForSqueezing = rate;
                    }

                    //displayIntermediateValues.displayBytes(1, "Block available for squeezing", dataQueue, bitsAvailableForSqueezing / 8);

                }
                partialBlock = bitsAvailableForSqueezing;
                if ((long)partialBlock > outputLength - i)
                {
                    partialBlock = (int)(outputLength - i);
                }

                Array.Copy(dataQueue, (rate - bitsAvailableForSqueezing) / 8, output, offset + (int)(i / 8), partialBlock / 8);
                bitsAvailableForSqueezing -= partialBlock;
                i += partialBlock;
            }
        }

        private static void FromBytesToWords(ulong[] stateAsWords, byte[] state)
        {
            for (var i = 0; i < (1600 / 64); i++)
            {
                stateAsWords[i] = 0;
                var index = i * (64 / 8);
                for (var j = 0; j < (64 / 8); j++)
                {
                    stateAsWords[i] |= ((ulong)state[index + j] & 0xff) << ((8 * j));
                }
            }
        }

        private static void FromWordsToBytes(byte[] state, ulong[] stateAsWords)
        {
            for (var i = 0; i < (1600 / 64); i++)
            {
                var index = i * (64 / 8);
                for (var j = 0; j < (64 / 8); j++)
                {
                    state[index + j] = (byte)(stateAsWords[i] >> (8 * j));
                }
            }
        }

        private void KeccakPermutation(byte[] state)
        {
            var longState = new ulong[state.Length / 8];

            FromBytesToWords(longState, state);

            //displayIntermediateValues.displayStateAsBytes(1, "Input of permutation", longState);

            KeccakPermutationOnWords(longState);

            //displayIntermediateValues.displayStateAsBytes(1, "State after permutation", longState);

            FromWordsToBytes(state, longState);
        }

        private void KeccakPermutationAfterXor(byte[] state, byte[] data, int dataLengthInBytes)
        {
            for (var i = 0; i < dataLengthInBytes; i++)
            {
                state[i] ^= data[i];
            }

            KeccakPermutation(state);
        }

        private void KeccakPermutationOnWords(ulong[] state)
        {
            int i;

            //displayIntermediateValues.displayStateAs64bitWords(3, "Same, with lanes as 64-bit words", state);

            for (i = 0; i < 24; i++)
            {
                //displayIntermediateValues.displayRoundNumber(3, i);

                Theta(state);
                //displayIntermediateValues.displayStateAs64bitWords(3, "After theta", state);

                Rho(state);
                //displayIntermediateValues.displayStateAs64bitWords(3, "After rho", state);

                Pi(state);
                //displayIntermediateValues.displayStateAs64bitWords(3, "After pi", state);

                Chi(state);
                //displayIntermediateValues.displayStateAs64bitWords(3, "After chi", state);

                Iota(state, i);
                //displayIntermediateValues.displayStateAs64bitWords(3, "After iota", state);
            }
        }

        ulong[] C = new ulong[5];

        private void Theta(ulong[] A)
        {
            for (var x = 0; x < 5; x++)
            {
                C[x] = 0;
                for (var y = 0; y < 5; y++)
                {
                    C[x] ^= A[x + 5 * y];
                }
            }
            for (var x = 0; x < 5; x++)
            {
                var dX = ((((C[(x + 1) % 5]) << 1) ^ ((C[(x + 1) % 5]) >> (64 - 1)))) ^ C[(x + 4) % 5];
                for (var y = 0; y < 5; y++)
                {
                    A[x + 5 * y] ^= dX;
                }
            }
        }

        private void Rho(ulong[] A)
        {
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    var index = x + 5 * y;
                    A[index] = ((KeccakRhoOffsets[index] != 0) ? (((A[index]) << KeccakRhoOffsets[index]) ^ ((A[index]) >> (64 - KeccakRhoOffsets[index]))) : A[index]);
                }
            }
        }

        ulong[] tempA = new ulong[25];

        private void Pi(ulong[] A)
        {
            Array.Copy(A, 0, tempA, 0, tempA.Length);

            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    A[y + 5 * ((2 * x + 3 * y) % 5)] = tempA[x + 5 * y];
                }
            }
        }

        ulong[] chiC = new ulong[5];

        private void Chi(ulong[] A)
        {
            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    chiC[x] = A[x + 5 * y] ^ ((~A[(((x + 1) % 5) + 5 * y)]) & A[(((x + 2) % 5) + 5 * y)]);
                }
                for (var x = 0; x < 5; x++)
                {
                    A[x + 5 * y] = chiC[x];
                }
            }
        }

        private static void Iota(ulong[] A, int indexRound)
        {
            A[(((0) % 5) + 5 * ((0) % 5))] ^= KeccakRoundConstants[indexRound];
        }

        private void KeccakAbsorb(byte[] byteState, byte[] data, int dataInBytes)
        {
            KeccakPermutationAfterXor(byteState, data, dataInBytes);
        }

        private void KeccakExtract1024bits(byte[] byteState, byte[] data)
        {
            Array.Copy(byteState, 0, data, 0, 128);
        }

        private void KeccakExtract(byte[] byteState, byte[] data, int laneCount)
        {
            Array.Copy(byteState, 0, data, 0, laneCount * 8);
        }

		public IMemoable Copy()
		{
			return new Sha3Digest(this);
		}

		public void Reset(IMemoable other)
		{
			var d = (Sha3Digest)other;

			CopyIn(d);
		}


    }
}
