using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrabFormat
{
    class Program
    {
        static void Main(string[] args)
        {


            Execution ex = new Execution();


            //http://adf.ly/9692067/www.areanerdx.blogspot.com.br/p/listas-para-iptv.html",
                
            IList<Execution.Entry> list = ex.Execute(@"C:\Thoris\Playlist\GrabFormat\GrabFormat\bin\debug", "*.m3u", ".\\Temp",
                //"http://bit.ly/husham2015", //5903 canais, 5 grupos
                //"http://iptvglobal.ml/lista", //38 canais, 1
                ////--"http://bit.ly/1GNP932",
                "http://bit.ly/CanaisIPTVbr", //1577 , 16 SERIADOS
                "http://zip.net/bqqSqK", //1577, 16
                //"http://pastebin.com/raw.php?i=vHQ5EBLq" ,//416, 2
                ////--"http://bit.ly/1GNP932",
                "http://lista.iptvglobal.com.br", //793, 23
                ////--"http://bit.do/igor_lista",
                ////--"http://bit.ly/BRASILIPTV",
                //"https://lista.iptvbr.org", //38, 1
                //"http://bit.ly/hushamallsports", //164,2
                //"http://zip.net/bqqSqK" ,//1597, 16
                //"http://lista.iptvglobal.com.br/", //793, 27
                "http://j.mp/Listaiptvbrasil",
                "http://bit.ly/CanaisIPTVbr"
                );

            ex.Write(list, @"C:\Thoris\Playlist\GrabFormat\GrabFormat\bin\Debug\thoris.txt");




        }


    }
}
