using System;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC;

namespace ChainUtils.BouncyCastle.Crypto.Agreement
{
    public class ECMqvBasicAgreement
        : IBasicAgreement
    {
        protected internal MqvPrivateParameters privParams;

        public virtual void Init(
            ICipherParameters parameters)
        {
            if (parameters is ParametersWithRandom)
            {
                parameters = ((ParametersWithRandom)parameters).Parameters;
            }

            privParams = (MqvPrivateParameters)parameters;
        }

        public virtual int GetFieldSize()
        {
            return (privParams.StaticPrivateKey.Parameters.Curve.FieldSize + 7) / 8;
        }

        public virtual BigInteger CalculateAgreement(
            ICipherParameters pubKey)
        {
            var pubParams = (MqvPublicParameters)pubKey;

            var staticPrivateKey = privParams.StaticPrivateKey;

            var agreement = CalculateMqvAgreement(staticPrivateKey.Parameters, staticPrivateKey,
                privParams.EphemeralPrivateKey, privParams.EphemeralPublicKey,
                pubParams.StaticPublicKey, pubParams.EphemeralPublicKey).Normalize();

            if (agreement.IsInfinity)
                throw new InvalidOperationException("Infinity is not a valid agreement value for MQV");

            return agreement.AffineXCoord.ToBigInteger();
        }

        // The ECMQV Primitive as described in SEC-1, 3.4
        private static ECPoint CalculateMqvAgreement(
            ECDomainParameters		parameters,
            ECPrivateKeyParameters	d1U,
            ECPrivateKeyParameters	d2U,
            ECPublicKeyParameters	Q2U,
            ECPublicKeyParameters	Q1V,
            ECPublicKeyParameters	Q2V)
        {
            var n = parameters.N;
            var e = (n.BitLength + 1) / 2;
            var powE = BigInteger.One.ShiftLeft(e);

            var curve = parameters.Curve;

            var points = new ECPoint[]{
                // The Q2U public key is optional
                ECAlgorithms.ImportPoint(curve, Q2U == null ? parameters.G.Multiply(d2U.D) : Q2U.Q),
                ECAlgorithms.ImportPoint(curve, Q1V.Q),
                ECAlgorithms.ImportPoint(curve, Q2V.Q)
            };

            curve.NormalizeAll(points);

            ECPoint q2u = points[0], q1v = points[1], q2v = points[2];

            var x = q2u.AffineXCoord.ToBigInteger();
            var xBar = x.Mod(powE);
            var Q2UBar = xBar.SetBit(e);
            var s = d1U.D.Multiply(Q2UBar).Add(d2U.D).Mod(n);

            var xPrime = q2v.AffineXCoord.ToBigInteger();
            var xPrimeBar = xPrime.Mod(powE);
            var Q2VBar = xPrimeBar.SetBit(e);

            var hs = parameters.H.Multiply(s).Mod(n);

            return ECAlgorithms.SumOfTwoMultiplies(
                q1v, Q2VBar.Multiply(hs).Mod(n), q2v, hs);
        }
    }
}
