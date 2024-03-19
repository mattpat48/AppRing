using System.Net.Sockets;
using System.Text;

namespace RingServer.Controllers;

public class SMSutils
{

    private readonly string fromNumber;
    private readonly string ip;
    private readonly int port;

    public SMSutils(string fromNumber, string ip, int port)
    {
        this.fromNumber = fromNumber;
        this.ip = ip;
        this.port = port;
    }

    public string SendSms(string toNumber, string content)
    {
        try
        {
            bool connected;
            TcpClient smsServer = OpenConnection(ip, port, out connected);

            if (connected)
            {
                string sms = content;
                string response = SendSmsToClient(smsServer, fromNumber, toNumber, sms);
                return response;
            }
            else
            {
                return "Failed to connect to server";
            }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    protected static TcpClient OpenConnection(string ip, int port, out bool connected)
    {
        TcpClient tcpClient = new TcpClient();
        try
        {
            tcpClient.Connect(ip, port);
            connected = true;
            return tcpClient;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            connected = false;
            return tcpClient;
        }
    }

    protected static void CloseConnection(TcpClient client)
    {
        client.Close();
        Console.WriteLine("Connection Closed process terminated...");
    }


    protected static string SendSmsToClient(TcpClient client, string fromNumber, string toNumber, string smsBody)
    {
        string response = string.Empty;
        string message = string.Empty;
        string eventMsg = string.Empty;

        ASCIIEncoding asen = new ASCIIEncoding();

        try
        {
            Stream stm = client.GetStream();

            string smsSend = string.Format("action: smscommand\r\ncommand: gsm send sms {0} {1} \r\n\r\n", fromNumber, toNumber);

            byte[] smsCmd = asen.GetBytes(smsSend);

            stm.Write(smsCmd, 0, smsCmd.Length);
            stm.Flush();

            byte[] smsResp = new byte[1000];
            stm.Read(smsResp, 0, 1000);
            response = asen.GetString(smsResp);

            if (!string.IsNullOrEmpty(response))
            {
                stm.Read(smsResp, 0, 1000);
                message = asen.GetString(smsResp);

                if (!string.IsNullOrEmpty(message))
                {
                    stm.Read(smsResp, 0, 1000);

                    eventMsg = asen.GetString(smsResp);

                    if (!string.IsNullOrEmpty(eventMsg))
                    {
                        string[] list = eventMsg.Split('\n');

                        foreach (string value in list)
                        {
                            if (value.StartsWith("--END"))
                            {
                                stm.Flush();
                            }
                        }
                    }
                }
                CloseConnection(client);
                return response;
            }
            else
            {
                CloseConnection(client);
                return "No response from server";
            }
        }
        catch (Exception ex)
        {
            CloseConnection(client);
            return ex.Message;
        }
    }
}
