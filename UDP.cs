using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using DotRecast.Core.Numerics;
using DotRecast.Detour;


class UDP
{
    public void run()
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
                    byte[] receivedBytes = udpServer.Receive(ref remoteEndPoint);
                    string receivedText = Encoding.ASCII.GetString(receivedBytes);
                    Console.WriteLine($"Reçu de {remoteEndPoint}: {receivedText}");



                    var query = new DtNavMeshQuery(detour.mesh);
                    var startPos = new RcVec3f(76.2f, -0.1f, 78.9f);
                    var endPos = new RcVec3f(-72.3f, -0.1f, -72.7f);
                    var halfExtents = new RcVec3f(0.1f, 0.1f, 0.1f);
                    var test = detour.CNaveMesh.PathFinder(query, startPos, endPos, halfExtents);
                    Console.WriteLine($"pathfinder {test[0]} done");

                    // Répondre au client
                    List<RcVec3f> responseText = test;
                    //byte[] responseBytes = Encoding.ASCII.GetBytes(responseText);
                    byte[] responseBytes = SerializeVectorList(responseText);

                    remoteEndPoint.Port = 8068;
                    udpServer.Send(responseBytes, responseBytes.Length, remoteEndPoint);
                    Console.WriteLine($"Envoyé à {remoteEndPoint}: {responseText}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur: {ex.Message}");
                }
            }
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


