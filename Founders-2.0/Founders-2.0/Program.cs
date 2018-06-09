using System;
using CloudCoinCore;
using CloudCoinClient.CoreClasses;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Founders;
using Microsoft.Extensions.CommandLineUtils;
using System.Collections.Generic;
using CloudCoinCoreDirectory;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions;
using Microsoft.Extensions.Configuration;

namespace Founders_2._0
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static KeyboardReader reader = new KeyboardReader();
        public static String rootFolder = Directory.GetCurrentDirectory();
        static FileSystem FS = new FileSystem(rootFolder);
        static RAIDA raida;
        static List<RAIDA> networks = new List<RAIDA>();
        public static String prompt = "> ";
        public static String[] commandsAvailable = new String[] { "Echo raida", "Show CloudCoins in Bank", "Import / Pown & Deposit", "Export / Withdraw", "Fix Fracked", "Show Folders", "Export stack files with one note each", "Help", "Quit" };
        public static int NetworkNumber = 1;
        static public int DisplayMenu()
        {
            Console.WriteLine("Founders Actions");
            Console.WriteLine();
            Console.WriteLine("1. Echo RAIDA");
            Console.WriteLine("2. Show CloudCoins");
            Console.WriteLine("3. Import CloudCoins");
            Console.WriteLine("4. Export CloudCoins");
            Console.WriteLine("5. Fix Fracked Coins");
            Console.WriteLine("6. Show Folders");
            Console.WriteLine("7. Help");
            Console.WriteLine("8. Switch Network");

            Console.WriteLine("9. Exit");
            var result = Console.ReadLine();
            return Convert.ToInt32(result);
        }
        public class AppSettings
        {
            public string Hello { get; set; }
        }
        public static void initConfig()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
            string nn = Configuration["NetworkNumber"];

            try
            {
                NetworkNumber = Convert.ToInt16(nn);
            }
            catch(Exception e)
            {
                NetworkNumber = 1;
                updateLog("Reading Network Number Config failed. Setting default to 1.");
            }
            Configuration["NetworkNumber"] = "2";

            //services.Configure<AppSettings>(appSettings);
        }

        
        public static void SetupRAIDA()
        {
            string json = loadDirectory();
            if (json == "")
            {
                updateLog("Directory could not be loaded.Trying to load backup!!");
                try
                {
                    parseDirectoryJSON();
                }
                catch (Exception exe)
                {
                    updateLog("Directory loading from backup failed.No RAIDA networks found.Quitting!!");
                    Environment.Exit(1);
                }

            }
            else
            {
                FS.WriteTextFile("directory.json", json);
            }
            parseDirectoryJSON(json);
            if (networks.Count == 0)
            {
                updateLog("No Valid Network found.Quitting!!");
                System.Environment.Exit(1);
            }
            else
            {
                updateLog(networks.Count + " Networks found.");
                raida = (from x in networks
                         where x.NetworkNumber == NetworkNumber
                         select x).FirstOrDefault();
                if(raida == null)
                {
                    updateLog("Selected Network Number not found. Quitting.");
                    Environment.Exit(0);
                }
                else
                {
                    updateLog("Network Number set to " + NetworkNumber);
                }
            }
            //networks[0]
        }

        public async static Task SwitchNetwork(int NewNetworkNumber){
            int oldRAIDANumber = NetworkNumber;
            RAIDA oldRAIDA = raida;
            NetworkNumber = NewNetworkNumber;
            raida = (from x in networks
                     where x.NetworkNumber == NetworkNumber
                     select x).FirstOrDefault();
            if (raida == null)
            {
                updateLog("Selected Network Number not found. Reverting to  previous network.");
                raida = oldRAIDA;
            }
            else
            {
                updateLog("Network Number set to " + NetworkNumber);
                await echoRaida();
            }
        }

        public static void Main(params string[] args)
        {
            Setup();
            initConfig();
            updateLog("Loading Network Directory");
            SetupRAIDA();
          
            // Program.exe <-g|--greeting|-$ <greeting>> [name <fullname>]
            // [-?|-h|--help] [-u|--uppercase]
            #region CommandLineArguments
            CommandLineApplication commandLineApplication =
              new CommandLineApplication(throwOnUnexpectedArg: false);
            CommandArgument names = null;
            commandLineApplication.Command("name",
              (target) =>
                names = target.Argument(
                  "fullname",
                  "Enter the full name of the person to be greeted.",
                  multipleValues: true));
            CommandOption greeting = commandLineApplication.Option(
              "-$|-g |--greeting <greeting>",
              "The greeting to display. The greeting supports"
              + " a format string where {fullname} will be "
              + "substituted with the full name.",
              CommandOptionType.NoValue);
            CommandOption uppercase = commandLineApplication.Option(
              "-u | --uppercase", "Display the greeting in uppercase.",
              CommandOptionType.NoValue);
            commandLineApplication.HelpOption("-? | -h | --help");

            CommandOption echo = commandLineApplication.Option(
              "-$|-e |--echo ",
              "The greeting to display. The greeting supports"
              + " a format string where {fullname} will be "
              + "substituted with the full name.",
                CommandOptionType.NoValue);

            CommandOption folders = commandLineApplication.Option(
              "-$|-f |--folders ",
              "The command to display CloudCoin Working Folder Structure",
                CommandOptionType.NoValue);

            CommandOption pown = commandLineApplication.Option(
              "-$|-p |--pown ",
              "The command to pown/detect/import your CloudCoins.",
                CommandOptionType.NoValue);

            CommandOption detection = commandLineApplication.Option(
              "-$|-d |--detect ",
              "The command to pown/detect/import your CloudCoins.",
                CommandOptionType.NoValue);

            CommandOption import = commandLineApplication.Option(
              "-$|-i |--import ",
              "The command to pown/detect/import your CloudCoins.",
                CommandOptionType.NoValue);

            #endregion

            if (args.Length <= 1)
            {
                printWelcome();
                while (true)
                {
                    int input = DisplayMenu();
                    ProcessInput(input).Wait();
                    if (input == 9)
                        break;
                }
            }
            else
            {

            commandLineApplication.OnExecute(async () =>
            {
                if (echo.HasValue())
                {
                    //ech();
                    await echoRaida();
                }
                if(folders.HasValue())
                {
                    showFolders();
                }

                if (pown.HasValue() || detection.HasValue() || import.HasValue())
                {
                    await detect();
                }
                if (greeting.HasValue())
                {
                    printWelcome();
                }
                return 0;
            });
            commandLineApplication.Execute(args);
            }

        }

        private static void showCoins()
        {

        }
        private async static Task ProcessInput(int input)
        {
            switch (input)
            {
                case 1:
                    await echoRaida();
                    break;
                case 2:
                    showCoins();
                    break;
                case 3:
                    await detect();
                    break;
                case 4:
                    //export();
                    break;
                case 5:
                    Process.Start(FS.RootPath);
                    //showFolders();
                    break;
                case 6:
                    //fix(timeout);
                    break;
                case 7:
                    //dump();
                    break;
                case 8:
                    Console.Write("Enter New Network Number - ");
                    int nn= Convert.ToInt16( Console.ReadLine());
                    await SwitchNetwork(nn);
                    break;
                default:
                    break;
            }
        }
        private static void ech(){
            Console.WriteLine("Echo...");
        }
        private static void Greet(
          string greeting, IEnumerable<string> values, bool useUppercase)
        {
            Console.WriteLine(greeting);
        }

        /*  static void Main(string[] args)
          {


              var app = new CommandLineApplication();
              app.Name = "ninja";
              app.HelpOption("-?|-h|--help");


              /* Console.Out.WriteLine("Loading File system...");
              Setup();
              Console.Out.WriteLine("File system loading Completed.");
              int argLength = args.Length;
              if (argLength > 0)
              {
                  handleCommand(args);
              }
              else
              {
                  printWelcome();
                  run();
              }

          }
      */
        private static int GetNetworkNumber(RAIDADirectory dir)
        {
            return 1;

        }
        public static void parseDirectoryJSON()
        {
            try
            {
                string json = File.ReadAllText(Environment.CurrentDirectory + @"\directory.json");

                //JavaScriptSerializer ser = new JavaScriptSerializer();
               // var dict = ser.Deserialize<Dictionary<string, object>>(json);


                //RAIDADirectory dir = ser.Deserialize<RAIDADirectory>(json);

                RAIDADirectory dir = JsonConvert.DeserializeObject<RAIDADirectory>(json);
                raida = RAIDA.GetInstance(dir.networks[GetNetworkNumber(dir)]);
                foreach(var network in dir.networks)
                {
                    networks.Add(RAIDA.GetInstance(network));

                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static void parseDirectoryJSON(string json)
        {
            RAIDADirectory dir = JsonConvert.DeserializeObject<RAIDADirectory>(json);
            raida = RAIDA.GetInstance(dir.networks[GetNetworkNumber(dir)]);
            foreach (var network in dir.networks)
            {
                networks.Add(RAIDA.GetInstance(network));
            }
        }

        public static void InitiateRAIDA()
        {
            string json = loadDirectory();
            if (json == "")
            {
                //MessageBox.Show("Directory could not be loaded.Trying to load backup!!");
                try
                {
                    parseDirectoryJSON();
                }
                catch (Exception exe)
                {
                    //MessageBox.Show("Directory loading from backup failed.Quitting!!");
                    Environment.Exit(1);

                }

            }
            parseDirectoryJSON(json);
        }
        public static string loadDirectory()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string s = client.DownloadString(Config.URL_DIRECTORY);
                    return s;
                }
                catch (Exception e)
                {

                }
            }
            return "";
        }
        public static void printWelcome()
        {
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Out.WriteLine("                                                                  ");
            Console.Out.WriteLine("                   CloudCoin Founders Edition                     ");
            Console.Out.WriteLine("                      Version: October.10.2017                    ");
            Console.Out.WriteLine("          Used to Authenticate, Store and Payout CloudCoins       ");
            Console.Out.WriteLine("      This Software is provided as is with all faults, defects    ");
            Console.Out.WriteLine("          and errors, and without warranty of any kind.           ");
            Console.Out.WriteLine("                Free from the CloudCoin Consortium.               ");
            Console.Out.WriteLine("                                                                  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            //Console.Out.Write("  Checking RAIDA");
            //await echoRaida();
            //RAIDA_Status.showMs();
            //Check to see if suspect files need to be imported because they failed to finish last time. 
            //String[] suspectFileNames = new DirectoryInfo(suspectFolder).GetFiles().Select(o => o.Name).ToArray();//Get all files in suspect folder
            //if (suspectFileNames.Length > 0)
            //{
            //    Console.ForegroundColor = ConsoleColor.Green;
            //    Console.Out.WriteLine("  Finishing importing coins from last time...");//
            //    Console.ForegroundColor = ConsoleColor.White;

            //    //import();//temp stop while testing, change this in production
            //             //grade();

            //} //end if there are files in the suspect folder that need to be imported

        } // End print welcome
        public static void help()
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("Customer Service:");
            Console.Out.WriteLine("Open 9am to 11pm California Time(PST).");
            Console.Out.WriteLine("1 (530) 500 - 2646");
            Console.Out.WriteLine("CloudCoin.HelpDesk@gmail.com(unsecure)");
            Console.Out.WriteLine("CloudCoin.HelpDesk@Protonmail.com(secure if you get a free encrypted email account at ProtonMail.com)");

        }//End Help

        public async static Task echoRaida()
        {
            Console.Out.WriteLine(String.Format( "Starting Echo to RAIDA Network {0}\n",NetworkNumber));
            Console.Out.WriteLine("----------------------------------\n");
            var echos = raida.GetEchoTasks();
           

            await Task.WhenAll(echos.AsParallel().Select(async task => await task()));
            //MessageBox.Show("Finished Echo");
            Console.Out.WriteLine("Ready Count -" + raida.ReadyCount);
            Console.Out.WriteLine("Not Ready Count -" + raida.NotReadyCount);

            for (int i = 0; i < raida.nodes.Count(); i++)
            {
                // Console.Out.WriteLine("Node " + i + " Status --" + raida.nodes[i].RAIDANodeStatus + "\n");
                Debug.WriteLine("Node" + i + " Status --" + raida.nodes[i].RAIDANodeStatus);
            }
            Console.Out.WriteLine("-----------------------------------\n");

        }

        public static async Task detect()
        {
            Console.Out.WriteLine(FS.ImportFolder);
            updateLog("Starting Multi Detect..");
            TimeSpan ts = new TimeSpan();
            DateTime before = DateTime.Now;
            DateTime after;
            FS.LoadFileSystem();

            // Prepare Coins for Import
            FS.DetectPreProcessing();

            var predetectCoins = FS.LoadFolderCoins(FS.PreDetectFolder);
            predetectCoins = (from x in predetectCoins
                              where x.nn == NetworkNumber
                              select x).ToList();
            
            FileSystem.predetectCoins = predetectCoins;

            // Process Coins in Lots of 200. Can be changed from Config File
            int LotCount = predetectCoins.Count() / Config.MultiDetectLoad;
            if (predetectCoins.Count() % Config.MultiDetectLoad > 0) LotCount++;
            ProgressChangedEventArgs pge = new ProgressChangedEventArgs();

            int CoinCount = 0;
            int totalCoinCount = predetectCoins.Count();
            for (int i = 0; i < LotCount; i++)
            {
                //Pick up 200 Coins and send them to RAIDA
                var coins = predetectCoins.Skip(i * Config.MultiDetectLoad).Take(200);
                raida.coins = coins;

                var tasks = raida.GetMultiDetectTasks(coins.ToArray(), Config.milliSecondsToTimeOut);
                try
                {
                    string requestFileName = Utils.RandomString(16).ToLower() + DateTime.Now.ToString("yyyyMMddHHmmss") + ".stack";
                    // Write Request To file before detect
                    FS.WriteCoinsToFile(coins, FS.RequestsFolder + requestFileName);
                    await Task.WhenAll(tasks.AsParallel().Select(async task => await task()));
                    int j = 0;
                    foreach (var coin in coins)
                    {
                        //coin.pown = "";
                        for (int k = 0; k < CloudCoinCore.Config.NodeCount; k++)
                        {
                            coin.response[k] = raida.nodes[k].MultiResponse.responses[j];
                            coin.pown += coin.response[k].outcome.Substring(0, 1);
                        }
                        int countp = coin.response.Where(x => x.outcome == "pass").Count();
                        int countf = coin.response.Where(x => x.outcome == "fail").Count();
                        coin.PassCount = countp;
                        coin.FailCount = countf;
                        CoinCount++;


                        updateLog("No. " + CoinCount + ". Coin Deteced. S. No. - " + coin.sn + ". Pass Count - " + coin.PassCount + ". Fail Count  - " + coin.FailCount + ". Result - " + coin.DetectionResult + "." + coin.pown);
                        Debug.WriteLine("Coin Deteced. S. No. - " + coin.sn + ". Pass Count - " + coin.PassCount + ". Fail Count  - " + coin.FailCount + ". Result - " + coin.DetectionResult);
                        //coin.sortToFolder();
                        pge.MinorProgress = (CoinCount) * 100 / totalCoinCount;
                        Debug.WriteLine("Minor Progress- " + pge.MinorProgress);
                        raida.OnProgressChanged(pge);
                        j++;
                    }
                    pge.MinorProgress = (CoinCount - 1) * 100 / totalCoinCount;
                    Debug.WriteLine("Minor Progress- " + pge.MinorProgress);
                    raida.OnProgressChanged(pge);


                    //FS.WriteCoin(coins, FS.DetectedFolder);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }


            }
            pge.MinorProgress = 100;
            Debug.WriteLine("Minor Progress- " + pge.MinorProgress);
            raida.OnProgressChanged(pge);
            var detectedCoins = FS.LoadFolderCoins(FS.DetectedFolder);

            // Apply Sort to Folder to all detected coins at once.
            updateLog("Starting Sort.....");
            detectedCoins.ForEach(x => x.SortToFolder());
            updateLog("Ended Sort........");

            var passedCoins = (from x in detectedCoins
                               where x.folder == FS.BankFolder
                               select x).ToList();

            var failedCoins = (from x in detectedCoins
                               where x.folder == FS.CounterfeitFolder
                               select x).ToList();
            var lostCoins = (from x in detectedCoins
                             where x.folder == FS.LostFolder
                             select x).ToList();
            var suspectCoins = (from x in detectedCoins
                                where x.folder == FS.SuspectFolder
                                select x).ToList();

            Debug.WriteLine("Total Passed Coins - " + passedCoins.Count());
            Debug.WriteLine("Total Failed Coins - " + failedCoins.Count());
            updateLog("Coin Detection finished.");
            updateLog("Total Passed Coins - " + passedCoins.Count() + "");
            updateLog("Total Failed Coins - " + failedCoins.Count() + "");
            updateLog("Total Lost Coins - " + lostCoins.Count() + "");
            updateLog("Total Suspect Coins - " + suspectCoins.Count() + "");

            // Move Coins to their respective folders after sort
            FS.MoveCoins(passedCoins, FS.DetectedFolder, FS.BankFolder);

            //FS.WriteCoin(failedCoins, FS.CounterfeitFolder, true);
            FS.MoveCoins(lostCoins, FS.DetectedFolder, FS.LostFolder);
            FS.MoveCoins(suspectCoins, FS.DetectedFolder, FS.SuspectFolder);

            // Clean up Detected Folder
            FS.RemoveCoins(failedCoins, FS.DetectedFolder);
            FS.RemoveCoins(lostCoins, FS.DetectedFolder);
            FS.RemoveCoins(suspectCoins, FS.DetectedFolder);

            FS.MoveImportedFiles();
            //FileSystem.detectedCoins = FS.LoadFolderCoins(FS.RootPath + System.IO.Path.DirectorySeparatorChar + FS.DetectedFolder);
            after = DateTime.Now;
            ts = after.Subtract(before);

            Debug.WriteLine("Detection Completed in - " + ts.TotalMilliseconds / 1000);
            updateLog("Detection Completed in - " + ts.TotalMilliseconds / 1000);

            Console.Read();
           

        }

        public static void updateLog(string logLine)
        {
            Console.Out.WriteLine(logLine);
        }
        /* STATIC METHODS */
        public async static void handleCommand(string[] args)
        {
            string command = args[0];

            switch (command)
            {
                case "echo":
                    await echoRaida();
                    break;
                case "showcoins":
                    //showCoins();
                    break;
                case "import":
                    //import();
                    break;
                case "export":
                    //export();
                    break;
                case "showfolders":
                    Process.Start(FS.RootPath);
                    //showFolders();
                    break;
                case "fix":
                    //fix(timeout);
                    break;
                case "dump":
                    //dump();
                    break;
                case "help":
                    help();
                    break;
                default:
                    break;
            }
        }
        public static void run()
        {
            bool restart = false;
            while (!restart)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Out.WriteLine("");
                //  Console.Out.WriteLine("========================================");
                Console.Out.WriteLine("");
                Console.Out.WriteLine("  Commands Available:");//"Commands Available:";
                Console.ForegroundColor = ConsoleColor.White;
                int commandCounter = 1;
                foreach (String command in commandsAvailable)
                {
                    Console.Out.WriteLine("  " + commandCounter + (". " + command));
                    commandCounter++;
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Out.Write(prompt);
                Console.ForegroundColor = ConsoleColor.White;
                int commandRecieved = reader.readInt(1, 9);
                switch (commandRecieved)
                {
                    case 1:
                         echoRaida();
                        break;
                    case 2:
                        //showCoins();
                        break;
                    case 3:
                        detect();
                        //import();
                        break;
                    case 4:
                        // export();
                        break;
                    case 5:
                        //fix(timeout);
                        break;
                    case 6:
                        showFolders();
                        break;
                    case 7:
                        //dump();
                        break;
                    case 8:
                        Environment.Exit(0);
                        break;
                    case 9:
                        //testMind();
                        //partialImport();
                        break;
                    case 10:
                        //toMind();
                        break;
                    case 11:
                        //fromMind();
                        break;
                    default:
                        Console.Out.WriteLine("Command failed. Try again.");//"Command failed. Try again.";
                        break;
                }// end switch
            }// end while
        }// end run method

        public static void showFolders()
        {
            Console.Out.WriteLine(" Root:        " + rootFolder);
            Console.Out.WriteLine(" Import:      " + FS.ImportFolder);
            Console.Out.WriteLine(" Imported:    " + FS.ImportedFolder);
            Console.Out.WriteLine(" Suspect:     " + FS.SuspectFolder);
            Console.Out.WriteLine(" Trash:       " + FS.TrashFolder);
            Console.Out.WriteLine(" Bank:        " + FS.BankFolder);
            Console.Out.WriteLine(" Fracked:     " + FS.FrackedFolder);
            Console.Out.WriteLine(" Templates:   " + FS.TemplateFolder);
            //Console.Out.WriteLine(" Directory:   " + FS.di);
            Console.Out.WriteLine(" Counterfeits:" + FS.CounterfeitFolder);
            Console.Out.WriteLine(" Export:      " + FS.ExportFolder);
            Console.Out.WriteLine(" Lost:        " + FS.LostFolder);
        } // end show folders

        public static void Setup()
        {
            // Create the Folder Structure
            FS.CreateFolderStructure();
            // Populate RAIDA Nodes
            raida = RAIDA.GetInstance();
            //raida.Echo();
            FS.LoadFileSystem();

            //Load Local Coins

          //  Console.Read();
        }

    }
}
