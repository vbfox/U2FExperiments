using BlackFox.U2F.Gnubby.Simulated;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace U2FExperiments
{
    class TestKeyPairGenerator : IKeyPairGenerator
    {
        public ECKeyPair GenerateKeyPair(byte[] applicationSha256, byte[] challengeSha256)
        {
            var conf = new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, new SecureRandom());
            var keyGen = new ECKeyPairGenerator("ECDSA");
            keyGen.Init(conf);
            var pair = keyGen.GenerateKeyPair();
            
            return new ECKeyPair((ECPublicKeyParameters) pair.Public, (ECPrivateKeyParameters) pair.Private);
        }

        public byte[] EncodePublicKey(ECPublicKeyParameters publicKey)
        {
            return publicKey.Q.GetEncoded();
        }
    }
}
