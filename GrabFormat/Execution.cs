using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GrabFormat
{
    public class Execution
    {
        #region Variables
        #endregion

        #region Constructors/Destructors

        public Execution()
        {

        }

        #endregion

        #region Methods

        private int IsGroupExists(IList<Group> list, string entry)
        {
            for (int c = 0; c < list.Count; c++)
            {
                if (string.Compare(list[c].Name, entry, true) == 0)
                    return c;
            }
            return -1;
        }
        private int IsExists(IList<Entry> list, Entry entry)
        {
            for (int c = 0; c < list.Count; c++)
            {
                if (string.Compare(entry.Link, list[c].Link, true) == 0)
                    return c;
            }
            return -1;
        }
        
        public IList<Entry> Execute(string folder, string extension, string tempFolder, params string [] urls)
        {
            IList<Entry> entries = new List<Entry>();
            IList<Entry> duplicated = new List<Entry>();
            IList<Entry> invalids = new List<Entry>();

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " - Iniciando o processo...");

            IList<Group> groups = new List<Group>();


            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);


            for (int c = 0; c < urls.Length; c++)
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " - (" + (c + 1) + ")Requisitando..." + urls[c]);


                string result = RequestUrl(urls[c], null, null, null, 0);

                if (string.IsNullOrEmpty(result))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " XXXXXXX ERRO XXXXXXX");
                }
                else
                {

                    string file = System.IO.Path.Combine(tempFolder, "file_" + c + "_temp.m3u");

                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);

                    StreamWriter writer = new StreamWriter(file);
                    writer.Write(result);
                    writer.Close();

                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " - Feito");
                }
            }


            string[] files = System.IO.Directory.GetFiles(folder, extension);

            string[] urlsFiles = System.IO.Directory.GetFiles(tempFolder, extension);



            for (int c = 0; c < files.Length + urlsFiles.Length; c++)
            {

                string currentFile = "";

                if (c > files.Length - 1)
                    currentFile = urlsFiles[c - files.Length];
                else
                    currentFile = files[c];

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " - Lendo arquivo: " + currentFile);

                IList<Entry> newEntries = ReadFile(currentFile);
                
                for (int l = newEntries.Count - 1; l >= 0; l--)
                {
                    if (newEntries[l].IsLinkValid() && newEntries[l].IsGroupValid())



                    if (newEntries[l].IsValid() && newEntries[l].IsGroupValid() && newEntries[l].IsAliasValid() 
                        && newEntries[l].IsAliasStartWithValid() && newEntries[l].IsLinkValid())
                    {
                        int posEntry = IsExists(entries, newEntries[l]);


                        if (posEntry == -1)
                        {
                            entries.Add(newEntries[l]);


                            string groupName = newEntries[l].Group;
                            if (string.IsNullOrEmpty(groupName))
                                groupName = "GRUPO";

                            int pos = IsGroupExists(groups, groupName);

                            if (pos == -1)
                            {
                                groups.Add(new Group() { Count = 1, Name = groupName });
                                groups[groups.Count - 1].List = new List<Entry>();
                                groups[groups.Count - 1].List.Add(newEntries[l]);
                            }
                            else
                            {
                                groups[pos].Count++;
                                groups[pos].List.Add(newEntries[l]);
                            }

                        }
                        else
                        {
                            if (string.IsNullOrEmpty(entries[posEntry].Group) && !string.IsNullOrEmpty(newEntries[l].Group))
                            {
                                entries[posEntry] = newEntries[l];
                            }

                            duplicated.Add(newEntries[l]);
                        }
                    }
                    else
                    {
                        invalids.Add(newEntries[l]);
                    }
                }
            }

            entries = OrderByGroup(groups);


            AddFavorites(entries);


            return entries;
        
        }
        public IList<Entry> ReadFile(string file)
        {
            IList<Entry> entries = new List<Entry>();

            System.IO.StreamReader reader = new System.IO.StreamReader(file, Encoding.UTF8);

            string line = reader.ReadLine();

            //if (string.Compare(line.ToLower(), Entry.EntryFile.ToLower()) != 0)
            //    return entries;

            bool found = false;
            string entry = "";

            while (reader.Peek() >= 0)
            {
                line = reader.ReadLine();

                if (line.ToLower().StartsWith(Entry.EntryPrefix.ToLower()))
                {
                    if (found)
                    {
                        Entry data = new Entry();
                        data.Read(entry);

                        if (data != null)
                            entries.Add(data);
                    }


                    entry = line;
                    found = true;
                }
                else if (line.Length == 0 && found)
                {
                    Entry data = new Entry();
                    data.Read(entry);

                    if (data != null)
                        entries.Add(data);


                    entry = "";
                    found = false;
                }
                else if (found)
                {
                    entry += "\n" + line;
                }


            }

            if (found)
            {
                Entry data = new Entry();
                data.Read(entry);

                if (data != null)
                    entries.Add(data);
            }


            reader.Close();

            return entries;
        }
        public void Write(IList<Entry> list, string file)
        {
            if (System.IO.File.Exists(file))
                System.IO.File.Delete(file);

            System.IO.StreamWriter writer = new System.IO.StreamWriter(file, true, Encoding.UTF8);

            writer.WriteLine(Entry.EntryFile);

            for (int c = 0; c < list.Count; c++)
            {
                writer.Write(list[c].Write());
                writer.WriteLine("");
            }

            writer.Close();

        }
        public string RequestUrl(string url, string login, string password, string domain, int port)
        {
            string html = "";

            HttpWebRequest wRequest = (HttpWebRequest)WebRequest.Create(url);
            wRequest.Method = WebRequestMethods.Http.Get;

            if (!string.IsNullOrEmpty(login))
                wRequest.Proxy = GetProxyConfiguration(login, password, domain, port);

            if (wRequest != null)
            {
                HttpWebResponse wResponse = null;

                try
                {
                    wResponse = (HttpWebResponse)wRequest.GetResponse();
                    StreamReader sReader = new StreamReader(wResponse.GetResponseStream());
                    html = sReader.ReadToEnd().ToString();
                    sReader.Close();
                    wResponse.Close();
                }
                catch (Exception ex)
                {                   
                    return null;
                }
            }

            return html;

        }
        protected IWebProxy GetProxyConfiguration(string login, string password, string domain, int port)
        {
            if (!string.IsNullOrEmpty(login) || !string.IsNullOrEmpty(password))
            {

                IWebProxy proxy = new WebProxy(domain, port);
                proxy.Credentials = new NetworkCredential(login, password);

                return proxy;

            }

            return null;

        }
        public IList<Entry> OrderByGroup(IList<Group> groups)
        {
            IList<Entry> res = new List<Entry>();
            for (int c = 0; c < groups.Count; c++)
            {
                Entry[] list = groups[c].List.ToArray();

                Array.Sort(list);

                groups[c].List = list.ToArray<Entry>();

                for (int i = 0; i < groups[c].List.Count; i++)
                {
                    res.Add(groups[c].List[i]);
                }
            }

            return res;
        
        }

        public void AddFavorites(IList<Entry> list)
        {
            IList<Entry> fav = ReadFile(".\\favorites.txt");


            for (int c = fav.Count - 1; c >= 0; c--)
            {
                list.Insert(0, fav[c]);
            }


        }

        #endregion

        public class Entry : IComparable, IComparer
        {
            #region Constants

            public const string EntryFile = "#EXTM3U";
            public const string EntryPrefix = "#EXTINF";
            public const string KeyShifting = "tvg-shift";
            public const string KeyName = "tvg-name";
            public const string KeyLogo = "tvg-logo";
            public const string KeyGroup = "group-title";
            public const string KeyAudio = "audio-track";

            #endregion

            #region Properties

            public string Shifting { get; set; }
            public string Name { get; set; }
            public string Logo { get; set; }
            public string Group { get; set; }
            public string Audio { get; set; }
            public int Duration { get; set; }
            public string Link { get; set; }
            public string Alias { get; set; }

            #endregion

            #region Constructors/Destructors

            public Entry()
            {

            }

            #endregion

            #region Methods

            public static string GetStringNoAccents(string str)
            {
                /** Troca os caracteres acentuados por não acentuados **/
                string[] acentos = new string[] { "ç", "Ç", "á", "é", "í", "ó", "ú", "ý", "Á", "É", "Í", "Ó", "Ú", "Ý", "à", "è", "ì", "ò", "ù", "À", "È", "Ì", "Ò", "Ù", "ã", "õ", "ñ", "ä", "ë", "ï", "ö", "ü", "ÿ", "Ä", "Ë", "Ï", "Ö", "Ü", "Ã", "Õ", "Ñ", "â", "ê", "î", "ô", "û", "Â", "Ê", "Î", "Ô", "Û" };
                string[] semAcento = new string[] { "c", "C", "a", "e", "i", "o", "u", "y", "A", "E", "I", "O", "U", "Y", "a", "e", "i", "o", "u", "A", "E", "I", "O", "U", "a", "o", "n", "a", "e", "i", "o", "u", "y", "A", "E", "I", "O", "U", "A", "O", "N", "a", "e", "i", "o", "u", "A", "E", "I", "O", "U" };
                for (int i = 0; i < acentos.Length; i++)
                {
                    str = str.Replace(acentos[i], semAcento[i]);
                }
                /** Troca os caracteres especiais da string por "" **/
                string[] caracteresEspeciais = { "\\.", ",", "-", ":", "\\(", "\\)", "ª", "\\|", "\\\\", "°" };
                for (int i = 0; i < caracteresEspeciais.Length; i++)
                {
                    str = str.Replace(caracteresEspeciais[i], "");
                }
                /** Troca os espaços no início por "" **/
                str = str.Replace("^\\s+", "");
                /** Troca os espaços no início por "" **/
                str = str.Replace("\\s+$", "");
                /** Troca os espaços duplicados, tabulações e etc por  " " **/
                str = str.Replace("\\s+", " ");
                return str;
            }
            private string GetValue(string input, string key)
            {
                string result = "";

                int pos = input.IndexOf(key);
                int posInitString = 0, posEndString = 0;
                bool found = false;


                if (pos == -1)
                    return "";


                for (int c = pos + key.Length; c < input.Length; c++)
                {
                    if (input[c] == ' ' || input[c] == '=')
                        continue;
                    else if (input[c] != '\"' && !found)
                        return "";

                    if (input[c] == '\"')
                    {
                        found = true;
                        if (posInitString == 0)
                            posInitString = c;
                        else
                        {
                            posEndString = c;
                            result = input.Substring(posInitString + 1, posEndString - posInitString - 1);
                            break;
                        }
                    }
                }


                return result;
            }
            private string RemoveComments(string data)
            {
                string result = data;

                int pos = result.IndexOf("[");

                while (pos != -1)
                {
                    int end = result.IndexOf("]", pos);

                    if (end == -1)
                        break;

                    result = result.Substring(0, pos) + result.Substring(end + 1);

                    pos = result.IndexOf("[");
                }


                return result;
            }
            private int GetDuration(string input)
            {
                int result = 0;
                int posStart = 0, posEnd = 0;
                bool found = false;

                for (int c = EntryPrefix.Length; c < input.Length; c++)
                {
                    if (input[c] == ':')
                    {
                        posStart = c;
                        found = true;
                    }
                    else if ((input[c] == ' ' || input[c] == ',') && found)
                    {
                        posEnd = c;
                        break;
                    }
                }
                string temp = input.Substring(posStart + 1, posEnd - posStart - 1);

                if (temp.Length > 0)
                {
                    try
                    {
                        result = int.Parse(temp);
                    }
                    catch
                    {
                        result = 0;
                    }
                }
                return result;
            }
            private string GetLink(string input)
            {
                string result = "";
                string[] split = input.Split(new char[] { '\n' });

                if (split.Length != 2)
                    return "";

                result = split[1];
                for (int c = 0; c < result.Length; c++)
                {
                    if (result[c] == ' ')
                    {
                        result = result.Substring(0, c).Trim();
                        break;
                    }
                }

                return result;

            }
            private string GetAlias(string input)
            {
                string result = "";
                string[] split = input.Split(new char[] { '\n' });

                if (split.Length != 2)
                    return "";

                int posStart = 0;
                for (int c = split[0].Length - 1; c > 0; c--)
                {
                    if (split[0][c] == ',')
                    {
                        posStart = c;
                        break;
                    }
                }

                if (posStart == 0)
                    return "";

                result = split[0].Substring(posStart + 1).Trim();

                return result;

            }
            public void Read(string data)
            {
                //#EXTINF:-1 tvg-logo="game.png" tvg-logo="http://anderson.clubeon.in/png/game.png" group-title="Séries e Novelas", Game of Thrones 5º Temporada Legendada
                //http://s001.video.pw/

                this.Duration = GetDuration(data);
                //this.Name = GetStringNoAccents ( GetValue(data, KeyName) );
                this.Name = GetValue(data, KeyName).Trim();
                this.Logo = GetValue(data, KeyLogo).Trim();
                //this.Group = GetStringNoAccents ( GetValue(data, KeyGroup) );
                this.Group = GetValue(data, KeyGroup).Trim();
                this.Audio = GetValue(data, KeyAudio);
                this.Shifting = GetValue(data, KeyShifting);
                //this.Alias = GetStringNoAccents ( this.GetAlias(data) );
                this.Alias = RemoveComments(this.GetAlias(data).Trim()).Trim();
                this.Link = GetLink(data);

            }
            public string Write()
            {
                string result = "";

                result += EntryPrefix + ":" + this.Duration;

                if (!string.IsNullOrEmpty(this.Name))
                    result += " " + KeyName + "=\"" + this.Name + "\"";
                else
                    result += " " + KeyName + "=\"" + "Name" + "\"";

                if (!string.IsNullOrEmpty(this.Audio))
                    result += " " + KeyAudio + "=\"" + this.Audio + "\"";


                if (!string.IsNullOrEmpty(this.Logo))
                    result += " " + KeyLogo + "=\"" + this.Logo + "\"";
                else
                    result += " " + KeyLogo + "=\"" + "logo.png" + "\"";

                string grupo = "";
                if (!string.IsNullOrEmpty(this.Group))
                {
                    result += " " + KeyGroup + "=\"" + this.Group + "\"";
                    grupo = this.Group + "-";
                }
                else
                    result += " " + KeyGroup + "=\"" + "GRUPO" + "\"";

                


                if (!string.IsNullOrEmpty(this.Alias))
                    result += ", " +  grupo + this.Alias + "";
                else
                    result += ", " + grupo + "Alias" + "";
                result += "\r\n";

                result += this.Link;



                return result;
            }
            public override string ToString()
            {
                return this.Write();
            }
            public bool IsValid()
            {
                if (string.IsNullOrEmpty(this.Alias))
                    return false;

                if (string.IsNullOrEmpty(this.Link))
                    return false;

                if (!IsPrefixValid())
                    return false;


                return true;
            }
            public bool IsGroupValid()
            {
                string[] groupsNotValid = new[]{
                    "ARROW", "DEMOLIDOR", "KINGDOM", "PERSON OF INTEREST", "SMALLVILLE",
                    "SUPERNATURAL", "THE FLASH", "THE WALKING DEAD", "BLEACH",
                    "IPTV WOLRD", "IPTV SPAIN", "IPTV PORTUGAL", "IPTV MEXICO", "IPTV ARGENTINA", 
                    "IPTV R�SSIA", "IPTV MÉXICO", "IPTV CHILE", "IPTV EQUADOR", "IPTV COLOMBIA", 
                    "IPTV CHINA", "IPTV TURQUIA", "IPTV USA", "IPTV ESPANHA", "UK and USA",
                    "FRANÇA","ESPANHA", "DESPORTO", "RÁDIO", "adultos", "CANAIS ESPANHA", "IPTV Mц┴XICO",
                    "CANAIS PORTUGUESES", "RADIOS", "Informa��es","ESPAÑOL", "MEXICO", "PORTUGAL",
                    "R�dios","Creditos","DEMOLIDOR HD","Rádios", "Beast Wars", "Gotham", "Godzilla",                   
                    "IPTV RUSSIA","IPTV M�XICO", "Canais Religiosos", "As Aventuras de Jackie Chan",
                    "Informações","S�ries e Novelas", "NOTÍCIAS INTERNACIONAIS","VARIOS INTERNACIONAIS",
                    "CANAIS VIASAT","Noticias","RELIGIOSOS","JORNALISMO","Rdios Online",
                    "ARGENTINA","CANAIS INTERNACIONAIS","USA Radio","Local Radio","israel Radio",
                    "BBTS Germany","TOP-Radio","CLIPES E MUSICAS","italiano","Beast Wars/Machines",
                    "American Horror Story","CDZ THE LOST CANVAS","CDZ ALMA DE OURO","OS CAVALEIROS DO ZODÍACO",
                    "CANAIS DE TV DO MÉXICO","Canais Portugal","Filmes e S�ries","Documentries",
                    "MAIORES","UK Radio","TV REGIONAL","TODOS CANAIS TV","Novotelecom",
                   
                    "Karate Kid", "Astroboy", "Os Novos Cacafantasmas", "A Feiticeira","CRASH",
                    "VIP", "Leverage","Missing Persons Unit", "A Ilha da Fantasia", 
                  


                    //"ESPORTES INTERNACIONAS","BOXE INTERNACIONAL", "INTERNATIONAL SPORTS","ESPORTES ESTRANGEIROS",
                    
                    //"FILMES RECEM ADICIONADOS",
                    //"Mélo Filmes", "Filmes HD", "FILMES ON DEMAND","Esportes internacionais",
                    //"SHOWS/DVD/CLIPES","FILMES ONLINE", "Filmes"
                    
                };


                //string[] groupsNotValid2 = new[]{
                //    "ARROW", "DEMOLIDOR", "KINGDOM", "PERSON OF INTEREST", "SMALLVILLE",
                //    "SUPERNATURAL", "THE FLASH", "THE WALKING DEAD", "BLEACH",
                //    "IPTV PORTUGAL", "IPTV MEXICO", "IPTV ARGENTINA", "IPTV R�SSIA", "IPTV MÉXICO",
                //    "IPTV SPAIN", "IPTV CHILE", "IPTV EQUADOR", "IPTV COLOMBIA", "IPTV RUSSIA",
                //    "Gotham", "ESPANOL", "MEXICO", "PORTUGAL", "CANAIS ESPANHA","CANAIS PORTUGUESES",
                //    "Rádios", "RADIOS", "IPTV CHINA", "IPTV TURQUIA", "INFORMACoES",
                //    "IPTV USA", "IPTV ESPANHA", "RELIGIOSOS","Rdios Online", "UK and USA",
                //    "INFORMACÕES", "INFORMACõES", "Canais Religiosos", "FRANCA", "ESPANHA",
                //    "IPTV Mц┴XICO", "Gotham", "Canais Religiosos", "Mélo Filmes", "RÁDIO",
                //    "Beast Wars", "FRANÇA", "Rdios Online", "ESPAÑOL", "S�ries e Novelas",
                //    "Sц╘ries 24 horas","Rdios Online","NOTÍCIAS INTERNACIONAIS","USA Live Tv",
                //    "IPTV Mц┴XICO", "Informa��es", "INFORMAÇÕES", "Informa��es", "Jornalismo",
                //    "Beast Wars", "LOCAIS", "IPTV M�XICO","Filmes e Sц╘ries","DESPORTO","OTHER Live TV",
                //    "TV REGIONAL", "NOTÍCIAS","IPTV WOLRD", "R�dios","Noticias","Filmes e S�ries",
                //    "Creditos","MUSIC","CDs Completos","Adultos",


                //};

                string group = this.Group.ToUpper();

                for (int c = 0; c < groupsNotValid.Length; c++)
                {
                    if (string.Compare(group, groupsNotValid[c], true) == 0)
                        return false;
                }

                return true;
            }
            public bool IsAliasValid()
            {
                string[] aliasNotValid = new[]{
                    "(RUS)","Australia","(KAZ)","(HON)","(ESP)","(UK)",
                    "(TUR)","(AZERB)","(AUS)","(UCR)","(MEX)","(ARG)",
                    "(SUE)","(GEO)","(Arabia)","(French)","(PORTUGAL)",
                    "(FRA)","(UAE)", "S1-", "S2-", "S3-", "S4-", "S5-", "S6-", "S7-","S8-",
                    "Antenne Bayern","Aljazeera","ARABIC","Balkanika","BBC ","bruaysis fm",
                    "BURSA RADYO","CCTV","Chile -","Chérie ","China","Ddisclaimer",
                    "Delta FM","Deutsche Welle","DI.FM","Diema","DRadio Wissen",
                    "ELAZIĞ","France","Frequence","Fréquence", "Generation FM",
                    "Goom Radio","Impact FM","INDONESIA","Iraq", "ALJAZERRA",
                    "AZTECA","DASDING","Deutschland","EL TRECE","Fun Radio ",
                    "Génération ","Generation ","Happy Hour Radio","HRVATSKI",
                    "Hubei ","Jazz Radio","Joint Radio","Jurnal ","KUWAIT ",
                    "La Radio ","Mediaset Italia ","METROPOL FM","Nan-ching ",
                    "Nostalgie ","OUI FM ","Peking ","Perviy Kana","Pink Music",
                    "qtvchv ","RADYO","RADIO","RÁDIO", "Россия", "Traxx ",
                    "Tring ","Taiwan","Syria","Spain","Russian","RUS-NTV",
                    "Polska","AlHayat","Alwatan","Argentina","Baghdad",
                    "Beur FM","Dominik-",".flv","(Israel)","**CANAIS DO BRASIL**",
                    "_فيلم_HIV_اتش_اي_في_","_القشاش_مصري_اكشن_2014","فيلم_عمر_وسلوى_",
                    "_واحد_صعيدي_كوميدي_اج","عمتي_فيلم_مصري_ك","Dhabi","ACZEN",
                    "AKSARAY","AKŞEHİR","Albanian","Albadawiyahtv","Alouette","ANNAHAR",
                    "(Afghanistan)","(Korea)"," FM", "FM ", "Russia", "Rusya"
                    

                };

                string alias = this.Alias;

                for (int c = 0; c < aliasNotValid.Length; c++)
                {
                    if (alias.ToUpper ().IndexOf (aliasNotValid[c].ToUpper ()) != -1)
                        return false;
                }

                return true;
            }
            public bool IsLinkValid()
            {
                string[] linkNotValid = new[]{
                    ".flv",

                };

                string alias = this.Link;

                for (int c = 0; c < linkNotValid.Length; c++)
                {
                    if (alias.ToUpper().IndexOf (linkNotValid[c].ToUpper()) != -1)
                        return false;
                }

                return true;
            }
            public bool IsAliasStartWithValid()
            {
                string[] aliasNotValid = new[]{
                    "AL_", "AL "

                };

                string alias = this.Alias;

                for (int c = 0; c < aliasNotValid.Length; c++)
                {
                    if (alias.ToUpper().StartsWith(aliasNotValid[c].ToUpper()))
                        return false;
                }

                return true;
            } 
            private bool IsPrefixValid()
            {
                string[] formats = new[] { "http", "https" };

                string prefix = GetPrefix().ToLower();

                for (int c = 0; c < formats.Length; c++)
                {
                    if (string.Compare(prefix, formats[c]) == 0)
                        return true;
                }

                return false;
            }
            private string GetPrefix()
            {
                int pos = this.Link.IndexOf(":");

                if (pos == -1)
                    return "";

                string result = this.Link.Substring(0, pos);

                return result;
            }

            #endregion

            #region IComparable members

            public int CompareTo(object obj)
            {

                Entry c = (Entry)obj;
                return String.Compare(this.Alias, c.Alias);
            }

            #endregion

            #region IComparer members

            public int Compare(object x, object y)
            {
                return string.Compare(((Entry)x).Alias, ((Entry)y).Alias);
            }

            #endregion
        }

        public class Group : IComparable, IComparer
        {
            #region Properties

            public string Name { get; set; }
            public int Count { get; set; }
            public IList<Entry> List { get; set; }

            #endregion

            #region Constructors/Destructors

            public Group()
            {

            }

            #endregion

            #region Methods

            public override string ToString()
            {
                return this.Name + " - " + this.Count;
            }

            #endregion

            #region IComparable members

            public int CompareTo(object obj)
            {

                Group c = (Group)obj;
                return String.Compare(this.Name, c.Name);
            }

            #endregion

            #region IComparer members

            public int Compare(object x, object y)
            {
                return string.Compare(((Group)x).Name, ((Group)y).Name);
            }

            #endregion
        }

    }
}
