using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Google.FlatBuffers;
using navMesh.Sample;
using Silk.NET.Vulkan.Video;

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
    private static readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(8);
    private static int _packetsProcessed = 0;

    // 🔹 Chaque thread garde son propre builder + buffer + liste pour éviter toute alloc
    [ThreadStatic] private static FlatBufferBuilder? _threadBuilder;
    //[ThreadStatic] private static Offset<navMesh.Sample.Vec3f>[]? _threadOffsets;
    [ThreadStatic] private static Offset<Vec3f>[]? _threadOffsets;
    
    //[ThreadStatic] private static ByteBuffer? _threadByteBuffer;

    private readonly Channel<UdpReceiveResult> _queue = Channel.CreateBounded<UdpReceiveResult>(
        new BoundedChannelOptions(50000)
        {
            SingleWriter = true,
            SingleReader = false
        }
    );
    public async Task run(Detour map)
    {
        // Create a new UDP client
        // Create a new UDP client
        int port = 11000;

        //using (UdpClient udpServer = new UdpClient(port))
        //{

        //var udpServer = new UdpClient(port);

        Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, port));

        Console.WriteLine($"Serveur UDP démarré sur le port {port}...");
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //Detour detour = new Detour();

        _socket.ReceiveBufferSize = 2 * 1024 * 1024;
        _socket.SendBufferSize = 2 * 1024 * 1024;
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        /*udpServer.Client.ReceiveBufferSize = 2 * 1024 * 1024;
        udpServer.Client.SendBufferSize = 2 * 1024 * 1024;*/
        for (int i = 0; i < 16; i++)
        {
            //_ = Task.Run(() => WorkerLoop(udpServer, map));
            _ = Task.Run(() => WorkerLoop(_socket, map));
            Console.WriteLine($"worker run: {i}");
        }
        _ = Task.Run(async () =>
        {
            while (true)
            {
                int start = _packetsProcessed;
                await Task.Delay(1000);
                int end = _packetsProcessed;
                int perSec = end - start;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"TPS: {perSec} req/s");
                Console.WriteLine($"TPS: {perSec} req/s | Queue count: {_queue.Reader.Count}");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Black;
            }
        });
        while (true)
        {
            var buffer = new byte[65535];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                //_socket.ReceiveFromAsync();
                //var result = await udpServer.ReceiveAsync();
                SocketReceiveFromResult resultR = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
                int bytesReceived = resultR.ReceivedBytes;
                var result = new UdpReceiveResult(buffer[..bytesReceived], (IPEndPoint)resultR.RemoteEndPoint);

                if (!_queue.Writer.TryWrite(result))
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("full, paquet ignored");
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                // UDP client distant fermé brutalement → on ignore
                Console.WriteLine("⚠️ Client UDP a fermé brutalement (10054) — ignoré");
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur ReceiveAsync: {ex.Message}");
            }

        }


        /*while (true)
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
        /*  }
          catch (Exception ex)
          {
              Console.WriteLine($"Erreur: {ex.Message}");
          }
      }*/
        //}
    }

    //private async Task WorkerLoop(UdpClient client, Detour map)
    private async Task WorkerLoop(Socket client, Detour map)
    {
        var builder = _threadBuilder ??= new FlatBufferBuilder(2048);
        int loopcounter = 0;
        const int yieldEvery = 64;
        //var offsets = _threadOffsets ??= new List<Offset<Vec3f>>(256);
        //var bb = _threadByteBuffer ??= new ByteBuffer(new byte[65535]); // buffer max UDP
        
        await foreach (var packet in _queue.Reader.ReadAllAsync())
        {
            try
            {

                handelPacket(packet, client, map, builder);
                //Console.WriteLine($"check worker: {a}");
                Interlocked.Increment(ref _packetsProcessed);

            }
            catch (Exception ex)
            {


                Console.WriteLine($"Erreur: {ex.Message}");

            }
        }
    }

    //public void handelPacket(UdpReceiveResult result, UdpClient udpClient, Detour map, FlatBufferBuilder builder)
    public async Task handelPacket(UdpReceiveResult result, Socket udpClient, Detour map, FlatBufferBuilder builder)
    {
        //FlatBufferBuilder builder = new FlatBufferBuilder(2048);

        byte[] receivedBytes = result.Buffer;
        //ByteBuffer bb = new ByteBuffer(receivedBytes);

        builder.Clear();
        //pointsOffsets.Clear();
        var bb = new ByteBuffer(receivedBytes);
        //bb.Reset(result.Buffer);
        NavMesh ftnavMesh = NavMesh.GetRootAsNavMesh(bb);

        RcVec3f start = new RcVec3f(ftnavMesh.Start.Value.X, ftnavMesh.Start.Value.Y, ftnavMesh.Start.Value.Z);
        RcVec3f end = new RcVec3f(ftnavMesh.End.Value.X, ftnavMesh.End.Value.Y, ftnavMesh.End.Value.Z);

        List<RcVec3f> path = map.find(start, end);



        //Console.WriteLine($"2");
        //var pointsOffsets = new Offset<navMesh.Sample.Vec3f>[path.Count];
        if (_threadOffsets == null || _threadOffsets.Length < path.Count)
            _threadOffsets = new Offset<Vec3f>[Math.Max(256, path.Count)];
        for (int i = 0; i < path.Count; i++)
        {

            var p = path[i];
            _threadOffsets[i] = navMesh.Sample.Vec3f.CreateVec3f(builder, p.X, p.Y, p.Z);
        }


        var pointsVector = navMesh.Sample.NavMesh.CreatePointsVector(builder, _threadOffsets.AsSpan(0, path.Count).ToArray());
        var startV = navMesh.Sample.Vec3f.CreateVec3f(builder, ftnavMesh.Start.Value.X, ftnavMesh.Start.Value.Y, ftnavMesh.Start.Value.Z);
        var endV = navMesh.Sample.Vec3f.CreateVec3f(builder, ftnavMesh.End.Value.X, ftnavMesh.End.Value.Y, ftnavMesh.End.Value.Z);
        var typeString = builder.CreateString("navmesh");

        //Console.WriteLine($"3");
        navMesh.Sample.NavMesh.StartNavMesh(builder);
        navMesh.Sample.NavMesh.AddType(builder, typeString);
        navMesh.Sample.NavMesh.AddStart(builder, startV);
        navMesh.Sample.NavMesh.AddEnd(builder, endV);
        navMesh.Sample.NavMesh.AddPoints(builder, pointsVector);
        var navmeshoff = navMesh.Sample.NavMesh.EndNavMesh(builder);
        builder.Finish(navmeshoff.Value);
        byte[] buff = builder.SizedByteArray();
        //Console.WriteLine($"4");
        //byte[] buff = Encoding.UTF8.GetBytes("message recu");
        //lock (sendLock)
        //{
        //udpClient.SendAsync(buff, buff.Length, result.RemoteEndPoint);
        //IPEndPoint destinationEP = new IPEndPoint(IPAddress.Parse("192.168.1.61"), 3302);
        //udpClient.SendAsync(buff, SocketFlags.None, destinationEP);
        //Console.WriteLine($"addr = {result.RemoteEndPoint.Port}");
        //_ = udpClient.SendToAsync(buff, SocketFlags.None, result.RemoteEndPoint);
        //}
        await SendBufferedAsync(udpClient, buff, result.RemoteEndPoint);

    }
    
    private async Task SendBufferedAsync(Socket udpClient, byte[] buffer, EndPoint destination)
{
    await _sendSemaphore.WaitAsync();
    try
    {
        // SendToAsync renvoie un ValueTask/Task selon la signature ; on attend pour gérer erreurs
        await udpClient.SendToAsync(buffer, SocketFlags.None, destination);
    }
    catch (SocketException sx)
    {
        Console.WriteLine($"Socket send error: {sx.SocketErrorCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Send error: {ex.Message}");
    }
    finally
    {
        _sendSemaphore.Release();
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


