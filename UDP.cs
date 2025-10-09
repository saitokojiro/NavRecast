using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using DotRecast.Core.Numerics;
using DotRecast.Detour;

public class Vector3DTO
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3 ToVector3() => new Vector3(X, Y, Z);
}
public class JsVec
{
        public Vector3DTO start { get; set; }
        public Vector3DTO end { get; set; }

    }

class UDP
{
    private readonly object sendLock = new object();
    public async Task run(Detour map)
    {
        // Create a new UDP client
        // Create a new UDP client
        int port = 11000;
        
        using (UdpClient udpServer = new UdpClient(port))
        {
            Console.WriteLine($"Serveur UDP démarré sur le port {port}...");
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Detour detour = new Detour();


            while (true)
            {
                try
                {
                    // Recevoir les données du client
                    // byte[] receivedBytes = udpServer.Receive(ref remoteEndPoint);
                    //string receivedText = Encoding.ASCII.GetString(receivedBytes);
                    //Console.WriteLine($"Reçu de {remoteEndPoint}: {receivedText}");
                    var result = await udpServer.ReceiveAsync();
                    _ = Task.Run(() => handelPacket(result, udpServer, map));

                    // Répondre au client
                    /*List<RcVec3f> responseText = test;
                    //byte[] responseBytes = Encoding.ASCII.GetBytes(responseText);
                    byte[] responseBytes = SerializeVectorList(responseText);

                    remoteEndPoint.Port = 8068;
                    udpServer.Send(responseBytes, responseBytes.Length, remoteEndPoint);
                    Console.WriteLine($"Envoyé à {remoteEndPoint}: {responseText}");*/
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur: {ex.Message}");
                }
            }
        }
    }

    public void handelPacket(UdpReceiveResult result, UdpClient udpClient, Detour map)
    {
        byte[] receivedBytes = result.Buffer;
        string receivedText = System.Text.Encoding.UTF8.GetString(receivedBytes);
        byte[] responseBytes = Encoding.UTF8.GetBytes("message recu");

        JsVec? jsvec = JsonSerializer.Deserialize<JsVec>(receivedText);
                
        RcVec3f start = new RcVec3f(jsvec.start.X, jsvec.start.Y, jsvec.start.Z);
        RcVec3f end   = new RcVec3f(jsvec.end.X, jsvec.end.Y, jsvec.end.Z);
        Console.WriteLine($"Envoyé à {result.RemoteEndPoint}: {receivedText}");
        Console.WriteLine($"================================");
        Console.WriteLine($"start: {jsvec.start.X}, {jsvec.start.Y}, {jsvec.start.Z}");
        Console.WriteLine($"end:   {jsvec.end.X}, {jsvec.end.Y}, {jsvec.end.Z}");
        Console.WriteLine($"================================");
        var path = map.find(start, end);
        Console.WriteLine($"path count : {path.Count}");
        if (path != null)
        {
            Console.WriteLine($"path : {path[0]}");
        }

        Console.WriteLine($"================================");
        lock(sendLock)
        {
            udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
        }
    }
    private byte[] SerializeVectorList(List<RcVec3f> vectors)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            // Écriture du nombre de vecteurs
            writer.Write(vectors.Count);

            // Écriture des données de chaque vecteur
            foreach (var vec in vectors)
            {
                writer.Write(vec.X);
                writer.Write(vec.Y);
                writer.Write(vec.Z);
            }
            return ms.ToArray();
        }
    }
}


