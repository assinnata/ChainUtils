namespace ChainUtils.BouncyCastle.Asn1.X509
{
    /**
     * The TbsCertificate object.
     * <pre>
     * TbsCertificate ::= Sequence {
     *      version          [ 0 ]  Version DEFAULT v1(0),
     *      serialNumber            CertificateSerialNumber,
     *      signature               AlgorithmIdentifier,
     *      issuer                  Name,
     *      validity                Validity,
     *      subject                 Name,
     *      subjectPublicKeyInfo    SubjectPublicKeyInfo,
     *      issuerUniqueID    [ 1 ] IMPLICIT UniqueIdentifier OPTIONAL,
     *      subjectUniqueID   [ 2 ] IMPLICIT UniqueIdentifier OPTIONAL,
     *      extensions        [ 3 ] Extensions OPTIONAL
     *      }
     * </pre>
     * <p>
     * Note: issuerUniqueID and subjectUniqueID are both deprecated by the IETF. This class
     * will parse them, but you really shouldn't be creating new ones.</p>
     */
	public class TbsCertificateStructure
		: Asn1Encodable
	{
		internal Asn1Sequence            Seq;
		internal DerInteger              version;
		internal DerInteger              serialNumber;
		internal AlgorithmIdentifier     signature;
		internal X509Name                issuer;
		internal Time                    startDate, endDate;
		internal X509Name                subject;
		internal SubjectPublicKeyInfo    subjectPublicKeyInfo;
		internal DerBitString            IssuerUniqueId;
		internal DerBitString            SubjectUniqueId;
		internal X509Extensions          extensions;

		public static TbsCertificateStructure GetInstance(
			Asn1TaggedObject	obj,
			bool				explicitly)
		{
			return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
		}

		public static TbsCertificateStructure GetInstance(
			object obj)
		{
		    var instance = obj as TbsCertificateStructure;
		    if (instance != null)
				return instance;

			return obj != null ? new TbsCertificateStructure(Asn1Sequence.GetInstance(obj)) : null;
		}

		internal TbsCertificateStructure(
			Asn1Sequence seq)
		{
			var seqStart = 0;

			Seq = seq;

			//
			// some certficates don't include a version number - we assume v1
			//
		    var o = seq[0] as DerTaggedObject;
		    if (o != null)
			{
				version = DerInteger.GetInstance(o, true);
			}
			else
			{
				seqStart = -1;          // field 0 is missing!
				version = new DerInteger(0);
			}

			serialNumber = DerInteger.GetInstance(seq[seqStart + 1]);

			signature = AlgorithmIdentifier.GetInstance(seq[seqStart + 2]);
			issuer = X509Name.GetInstance(seq[seqStart + 3]);

			//
			// before and after dates
			//
			var  dates = (Asn1Sequence)seq[seqStart + 4];

			startDate = Time.GetInstance(dates[0]);
			endDate = Time.GetInstance(dates[1]);

			subject = X509Name.GetInstance(seq[seqStart + 5]);

			//
			// public key info.
			//
			subjectPublicKeyInfo = SubjectPublicKeyInfo.GetInstance(seq[seqStart + 6]);

			for (var extras = seq.Count - (seqStart + 6) - 1; extras > 0; extras--)
			{
				var extra = (DerTaggedObject) seq[seqStart + 6 + extras];

				switch (extra.TagNo)
				{
					case 1:
						IssuerUniqueId = DerBitString.GetInstance(extra, false);
						break;
					case 2:
						SubjectUniqueId = DerBitString.GetInstance(extra, false);
						break;
					case 3:
						extensions = X509Extensions.GetInstance(extra);
						break;
				}
			}
		}

		public int Version
		{
			get { return version.Value.IntValue + 1; }
		}

		public DerInteger VersionNumber
		{
			get { return version; }
		}

		public DerInteger SerialNumber
		{
			get { return serialNumber; }
		}

		public AlgorithmIdentifier Signature
		{
			get { return signature; }
		}

		public X509Name Issuer
		{
			get { return issuer; }
		}

		public Time StartDate
		{
			get { return startDate; }
		}

		public Time EndDate
		{
			get { return endDate; }
		}

		public X509Name Subject
		{
			get { return subject; }
		}

		public SubjectPublicKeyInfo SubjectPublicKeyInfo
		{
			get { return subjectPublicKeyInfo; }
		}

		public DerBitString IssuerUniqueID
		{
			get { return IssuerUniqueId; }
        }

		public DerBitString SubjectUniqueID
        {
			get { return SubjectUniqueId; }
        }

		public X509Extensions Extensions
        {
			get { return extensions; }
        }

		public override Asn1Object ToAsn1Object()
        {
            return Seq;
        }
    }
}
