using UnityEngine.Networking;

namespace Virtuademy.SDK.Http
{
    public class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] _) => true;
    }
}
