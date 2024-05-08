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

    public class AddUserRequest
    {
        public string GateId { get; set; }
        public string ToAdd { get; set; }

        public AddUserRequest(string gateId, string toAdd)
        {
            GateId = gateId;
            ToAdd = toAdd;
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

    public class Payload
    {
        public string Data { get; set; }
        public string Signature { get; set; }

        public Payload(string data, string signature)
        {
            Data = data;
            Signature = signature;
        }
    }

    public class ExtendedIdentifier
    {
        public string Number { get; set; }
        public string Id { get; set; }
        public Payload Payload { get; set; }

        public ExtendedIdentifier(string number, string id, Payload payload)
        {
            Number = number;
            Id = id;
            Payload = payload;
        }
    }
}
