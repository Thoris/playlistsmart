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



            IList<Execution.Entry> list = ex.Execute(@"C:\Thoris\Playlist\GrabFormat\GrabFormat\bin\debug", "*.m3u", ".\\Temp",
                "http://bit.ly/husham2015",
                "http://iptvglobal.ml/lista",
                "http://bit.ly/1GNP932",
                "http://bit.ly/CanaisIPTVbr",
                "http://zip.net/bqqSqK",
                "http://pastebin.com/raw.php?i=vHQ5EBLq",
                "http://bit.ly/1GNP932",
                "http://lista.iptvglobal.com.br",
                "http://bit.do/igor_lista",
                "http://bit.ly/BRASILIPTV",
                "https://lista.iptvbr.org",
                "http://bit.ly/hushamallsports",
                "http://zip.net/bqqSqK",
                "http://lista.iptvglobal.com.br/"              
                );

            ex.Write(list, @"C:\Thoris\Playlist\GrabFormat\GrabFormat\bin\Debug\thoris.txt");




        }


    }
}
