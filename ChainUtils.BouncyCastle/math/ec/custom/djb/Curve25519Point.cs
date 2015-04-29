using System;
using ChainUtils.BouncyCastle.Math.EC.Custom.Sec;

namespace ChainUtils.BouncyCastle.Math.EC.Custom.Djb
{
    internal class Curve25519Point
        : AbstractFpPoint
    {
        /**
         * Create a point which encodes with point compression.
         * 
         * @param curve the curve to use
         * @param x affine x co-ordinate
         * @param y affine y co-ordinate
         * 
         * @deprecated Use ECCurve.CreatePoint to construct points
         */
        public Curve25519Point(ECCurve curve, ECFieldElement x, ECFieldElement y)
            : this(curve, x, y, false)
        {
        }

        /**
         * Create a point that encodes with or without point compresion.
         * 
         * @param curve the curve to use
         * @param x affine x co-ordinate
         * @param y affine y co-ordinate
         * @param withCompression if true encode with point compression
         * 
         * @deprecated per-point compression property will be removed, refer {@link #getEncoded(bool)}
         */
        public Curve25519Point(ECCurve curve, ECFieldElement x, ECFieldElement y, bool withCompression)
            : base(curve, x, y, withCompression)
        {
            if ((x == null) != (y == null))
                throw new ArgumentException("Exactly one of the field elements is null");
        }

        internal Curve25519Point(ECCurve curve, ECFieldElement x, ECFieldElement y, ECFieldElement[] zs, bool withCompression)
            : base(curve, x, y, zs, withCompression)
        {
        }

        protected override ECPoint Detach()
        {
            return new Curve25519Point(null, AffineXCoord, AffineYCoord);
        }

        public override ECFieldElement GetZCoord(int index)
        {
            if (index == 1)
            {
                return GetJacobianModifiedW();
            }

            return base.GetZCoord(index);
        }

        public override ECPoint Add(ECPoint b)
        {
            if (IsInfinity)
                return b;
            if (b.IsInfinity)
                return this;
            if (this == b)
                return Twice();

            var curve = Curve;

            Curve25519FieldElement X1 = (Curve25519FieldElement)RawXCoord, Y1 = (Curve25519FieldElement)RawYCoord,
                Z1 = (Curve25519FieldElement)RawZCoords[0];
            Curve25519FieldElement X2 = (Curve25519FieldElement)b.RawXCoord, Y2 = (Curve25519FieldElement)b.RawYCoord,
                Z2 = (Curve25519FieldElement)b.RawZCoords[0];

            uint c;
            var tt1 = Nat256.CreateExt();
            var t2 = Nat256.Create();
            var t3 = Nat256.Create();
            var t4 = Nat256.Create();

            var Z1IsOne = Z1.IsOne;
            uint[] U2, S2;
            if (Z1IsOne)
            {
                U2 = X2.x;
                S2 = Y2.x;
            }
            else
            {
                S2 = t3;
                Curve25519Field.Square(Z1.x, S2);

                U2 = t2;
                Curve25519Field.Multiply(S2, X2.x, U2);

                Curve25519Field.Multiply(S2, Z1.x, S2);
                Curve25519Field.Multiply(S2, Y2.x, S2);
            }

            var Z2IsOne = Z2.IsOne;
            uint[] U1, S1;
            if (Z2IsOne)
            {
                U1 = X1.x;
                S1 = Y1.x;
            }
            else
            {
                S1 = t4;
                Curve25519Field.Square(Z2.x, S1);

                U1 = tt1;
                Curve25519Field.Multiply(S1, X1.x, U1);

                Curve25519Field.Multiply(S1, Z2.x, S1);
                Curve25519Field.Multiply(S1, Y1.x, S1);
            }

            var H = Nat256.Create();
            Curve25519Field.Subtract(U1, U2, H);

            var R = t2;
            Curve25519Field.Subtract(S1, S2, R);

            // Check if b == this or b == -this
            if (Nat256.IsZero(H))
            {
                if (Nat256.IsZero(R))
                {
                    // this == b, i.e. this must be doubled
                    return Twice();
                }

                // this == -b, i.e. the result is the point at infinity
                return curve.Infinity;
            }

            var HSquared = Nat256.Create();
            Curve25519Field.Square(H, HSquared);

            var G = Nat256.Create();
            Curve25519Field.Multiply(HSquared, H, G);

            var V = t3;
            Curve25519Field.Multiply(HSquared, U1, V);

            Curve25519Field.Negate(G, G);
            Nat256.Mul(S1, G, tt1);

            c = Nat256.AddBothTo(V, V, G);
            Curve25519Field.Reduce27(c, G);

            var X3 = new Curve25519FieldElement(t4);
            Curve25519Field.Square(R, X3.x);
            Curve25519Field.Subtract(X3.x, G, X3.x);

            var Y3 = new Curve25519FieldElement(G);
            Curve25519Field.Subtract(V, X3.x, Y3.x);
            Curve25519Field.MultiplyAddToExt(Y3.x, R, tt1);
            Curve25519Field.Reduce(tt1, Y3.x);

            var Z3 = new Curve25519FieldElement(H);
            if (!Z1IsOne)
            {
                Curve25519Field.Multiply(Z3.x, Z1.x, Z3.x);
            }
            if (!Z2IsOne)
            {
                Curve25519Field.Multiply(Z3.x, Z2.x, Z3.x);
            }

            var Z3Squared = (Z1IsOne && Z2IsOne) ? HSquared : null;

            // TODO If the result will only be used in a subsequent addition, we don't need W3
            var W3 = CalculateJacobianModifiedW((Curve25519FieldElement)Z3, Z3Squared);

            var zs = new ECFieldElement[] { Z3, W3 };

            return new Curve25519Point(curve, X3, Y3, zs, IsCompressed);
        }

        public override ECPoint Twice()
        {
            if (IsInfinity)
                return this;

            var curve = Curve;

            var Y1 = RawYCoord;
            if (Y1.IsZero)
                return curve.Infinity;

            return TwiceJacobianModified(true);
        }

        public override ECPoint TwicePlus(ECPoint b)
        {
            if (this == b)
                return ThreeTimes();
            if (IsInfinity)
                return b;
            if (b.IsInfinity)
                return Twice();

            var Y1 = RawYCoord;
            if (Y1.IsZero)
                return b;

            return TwiceJacobianModified(false).Add(b);
        }

        public override ECPoint ThreeTimes()
        {
            if (IsInfinity || RawYCoord.IsZero)
                return this;

            return TwiceJacobianModified(false).Add(this);
        }

        public override ECPoint Negate()
        {
            if (IsInfinity)
                return this;

            return new Curve25519Point(Curve, RawXCoord, RawYCoord.Negate(), RawZCoords, IsCompressed);
        }

        protected virtual Curve25519FieldElement CalculateJacobianModifiedW(Curve25519FieldElement Z, uint[] ZSquared)
        {
            var a4 = (Curve25519FieldElement)Curve.A;
            if (Z.IsOne)
                return a4;

            var W = new Curve25519FieldElement();
            if (ZSquared == null)
            {
                ZSquared = W.x;
                Curve25519Field.Square(Z.x, ZSquared);
            }
            Curve25519Field.Square(ZSquared, W.x);
            Curve25519Field.Multiply(W.x, a4.x, W.x);
            return W;
        }

        protected virtual Curve25519FieldElement GetJacobianModifiedW()
        {
            var ZZ = RawZCoords;
            var W = (Curve25519FieldElement)ZZ[1];
            if (W == null)
            {
                // NOTE: Rarely, TwicePlus will result in the need for a lazy W1 calculation here
                ZZ[1] = W = CalculateJacobianModifiedW((Curve25519FieldElement)ZZ[0], null);
            }
            return W;
        }

        protected virtual Curve25519Point TwiceJacobianModified(bool calculateW)
        {
            Curve25519FieldElement X1 = (Curve25519FieldElement)RawXCoord, Y1 = (Curve25519FieldElement)RawYCoord,
                Z1 = (Curve25519FieldElement)RawZCoords[0], W1 = GetJacobianModifiedW();

            uint c;

            var M = Nat256.Create();
            Curve25519Field.Square(X1.x, M);
            c = Nat256.AddBothTo(M, M, M);
            c += Nat256.AddTo(W1.x, M);
            Curve25519Field.Reduce27(c, M);

            var _2Y1 = Nat256.Create();
            Curve25519Field.Twice(Y1.x, _2Y1);

            var _2Y1Squared = Nat256.Create();
            Curve25519Field.Multiply(_2Y1, Y1.x, _2Y1Squared);

            var S = Nat256.Create();
            Curve25519Field.Multiply(_2Y1Squared, X1.x, S);
            Curve25519Field.Twice(S, S);

            var _8T = Nat256.Create();
            Curve25519Field.Square(_2Y1Squared, _8T);
            Curve25519Field.Twice(_8T, _8T);

            var X3 = new Curve25519FieldElement(_2Y1Squared);
            Curve25519Field.Square(M, X3.x);
            Curve25519Field.Subtract(X3.x, S, X3.x);
            Curve25519Field.Subtract(X3.x, S, X3.x);

            var Y3 = new Curve25519FieldElement(S);
            Curve25519Field.Subtract(S, X3.x, Y3.x);
            Curve25519Field.Multiply(Y3.x, M, Y3.x);
            Curve25519Field.Subtract(Y3.x, _8T, Y3.x);

            var Z3 = new Curve25519FieldElement(_2Y1);
            if (!Nat256.IsOne(Z1.x))
            {
                Curve25519Field.Multiply(Z3.x, Z1.x, Z3.x);
            }

            Curve25519FieldElement W3 = null;
            if (calculateW)
            {
                W3 = new Curve25519FieldElement(_8T);
                Curve25519Field.Multiply(W3.x, W1.x, W3.x);
                Curve25519Field.Twice(W3.x, W3.x);
            }

            return new Curve25519Point(Curve, X3, Y3, new ECFieldElement[] { Z3, W3 }, IsCompressed);
        }
    }
}
