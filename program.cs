using System;
using System.Threading.Tasks;
public class Program
{
    static async Task Main(string[] args)
    {
        
        await Testa();
    }

    public static async Task Testa()
    {
        Detour Datest = new Detour();
        Datest.run();
        //Datest.find();
        UDP udp = new UDP();
        await udp.run(Datest);
        
    }
}
