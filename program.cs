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
        List<Detour> Maps =  new List<Detour>();
        Detour Datest = new Detour();
        // Y = hauteur
        Datest.run("maplevel2",0,0,0);
     
       
        //Datest.find();

        //Datest.findTest();

        UDP udp = new UDP();
        await udp.run(Datest);
        
    }
}
