namespace RingServer.Utils;
public static class CommonClasses
{
    // Classe per la richiesta di signin (registrazione e login gestiti insieme)
    public class SignInRequest
    {
        public string PKey { get; set; }
        public string Number { get; set; }
        public string Id { get; set; }
        public string RememberLogin { get; set; }

        public SignInRequest(string pKey, string number, string id, string rememberLogin)
        {
            PKey = pKey;
            Number = number;
            Id = id;
            RememberLogin = rememberLogin;
        }
    }

    // Classe per l'identificazione dell'utente per altro tipo di richieste
    public class Identifier
    {
        public string Number { get; set; }
        public string Id { get; set; }

        public Identifier(string number, string id)
        {
            Number = number;
            Id = id;
        }
    }


    // Classe richiesta criptata
    public class EncryptedRequest
    {
        public string EncryptedData { get; set; }
        public string EncryptedKey { get; set; }
        public string EncryptedIV { get; set; }

        public EncryptedRequest(string encryptedData, string encryptedKey, string encryptedIV)
        {
            EncryptedData = encryptedData;
            EncryptedKey = encryptedKey;
            EncryptedIV = encryptedIV;
        }
    }

    public class SingleGateRequest
    {
        public string Number { get; set; }
        public string Id { get; set; }
        public string GateId { get; set; }

        public SingleGateRequest(string number, string id, string gateId)
        {
            Number = number;
            Id = id;
            GateId = gateId;
        }
    }
}
