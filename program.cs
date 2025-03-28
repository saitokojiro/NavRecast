using System;
public class Program
{
    public static void Main(string[] args)
    {
        
        Testa();
    }

    public static void Testa()
    {
        //Detour Datest = new Detour();
        //Datest.run();
        UDP udp = new UDP();
        udp.run();
        
    }
}
