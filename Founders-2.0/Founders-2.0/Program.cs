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
using Celebrium;
using ZXing;
using QRCoder;
using System.Drawing;

namespace Founders_2._0
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static KeyboardReader reader = new KeyboardReader();
        public static String rootFolder = Directory.GetCurrentDirectory();
        static FileSystem FS = new FileSystem(rootFolder);
        public static RAIDA raida;
        //static List<RAIDA> networks = new List<RAIDA>();
        public static String prompt = "> ";
        public static Frack_Fixer fixer;
        public static String[] commandsAvailable = new String[] { "Echo raida", "Show CloudCoins in Bank", "Import / Pown & Deposit", "Export / Withdraw", "Fix Fracked", "Show Folders", "Export stack files with one note each", "Help", "Quit" };
        public static int NetworkNumber = 1;
        public static SimpleLogger logger = new SimpleLogger(FS.LogsFolder + "logs" + DateTime.Now.ToString("yyyyMMdd").ToLower() + ".log", true);

        #region Total Variables
        public static int onesCount = 0;
        public static int fivesCount = 0;
        public static int qtrCount = 0;
        public static int hundredsCount = 0;
        public static int twoFiftiesCount = 0;

        public static int onesFrackedCount = 0;
        public static int fivesFrackedCount = 0;
        public static int qtrFrackedCount = 0;
        public static int hundredsFrackedCount = 0;
        public static int twoFrackedFiftiesCount = 0;

        public static int onesTotalCount = 0;
        public static int fivesTotalCount = 0;
        public static int qtrTotalCount = 0;
        public static int hundredsTotalCount = 0;
        public static int twoFiftiesTotalCount = 0;
        #endregion


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
            
            //services.Configure<AppSettings>(appSettings);
        }

        
        public static void SetupRAIDA()
        {
            RAIDA.FileSystem = new FileSystem(rootFolder);
            try
            {
                RAIDA.Instantiate();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
            if (RAIDA.networks.Count == 0)
            {
                updateLog("No Valid Network found.Quitting!!");
                Environment.Exit(1);
            }
            else
            {
                updateLog(RAIDA.networks.Count + " Networks found.");
                raida = (from x in RAIDA.networks
                         where x.NetworkNumber == NetworkNumber
                         select x).FirstOrDefault();
                raida.FS = FS;
                RAIDA.ActiveRAIDA = raida;
                if (raida == null)
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
            raida = (from x in RAIDA.networks
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
                await EchoRaida();
            }
        }

        public static void Main(params string[] args)
        {
            Setup();
            initConfig();
            updateLog("Loading Network Directory");
            SetupRAIDA();
            FS.LoadFileSystem();
            fixer = new Frack_Fixer(FS, Config.milliSecondsToTimeOut);

            Console.Clear();
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

            CommandOption total = commandLineApplication.Option(
            "-$|-b |--bank ",
            "Shows details of your coins in bank.",
            CommandOptionType.NoValue);

            CommandOption backup = commandLineApplication.Option(
            "-$|-ba |--backup ",
            "Backup your coins to specified folder.",
            CommandOptionType.SingleValue);


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

            if (args.Length <1)
            {
                printWelcome();
                while (true)
                {
                    try
                    {
                        int input = DisplayMenu();
                        ProcessInput(input).Wait();
                        if (input == 9)
                            break;
                    }
                    catch(Exception e)
                    {
                        break;
                    }
                }
            }
            else
            {

            commandLineApplication.OnExecute(async () =>
            {
                if (echo.HasValue())
                {
                    //ech();
                    await EchoRaida();
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
                if(total.HasValue())
                {
                    showCoins();
                }
                if (backup.HasValue())
                {
                    Console.WriteLine(backup.Value());
                }
                return 0;
            });
            commandLineApplication.Execute(args);
            }

        }

        private static void showCoins()
        {
            Console.Out.WriteLine("");
            // This is for consol apps.
            Banker bank = new Banker(FS);
            int[] bankTotals = bank.countCoins(FS.BankFolder);
            int[] frackedTotals = bank.countCoins(FS.FrackedFolder);
            // int[] counterfeitTotals = bank.countCoins( counterfeitFolder );

            var bankCoins = FS.LoadFolderCoins(FS.BankFolder);

            
            onesCount = (from x in bankCoins
                         where x.denomination == 1
                         select x).Count();
            fivesCount = (from x in bankCoins
                          where x.denomination == 5
                          select x).Count();
            qtrCount = (from x in bankCoins
                        where x.denomination == 25
                        select x).Count();
            hundredsCount = (from x in bankCoins
                             where x.denomination == 100
                             select x).Count();
            twoFiftiesCount = (from x in bankCoins
                               where x.denomination == 250
                               select x).Count();

            var frackedCoins = FS.LoadFolderCoins(FS.FrackedFolder);
            bankCoins.AddRange(frackedCoins);

            onesFrackedCount = (from x in frackedCoins
                         where x.denomination == 1
                         select x).Count();
            fivesFrackedCount = (from x in frackedCoins
                          where x.denomination == 5
                          select x).Count();
            qtrFrackedCount = (from x in frackedCoins
                        where x.denomination == 25
                        select x).Count();
            hundredsFrackedCount = (from x in frackedCoins
                             where x.denomination == 100
                             select x).Count();
            twoFrackedFiftiesCount = (from x in frackedCoins
                               where x.denomination == 250
                               select x).Count();

            onesTotalCount = onesCount + onesFrackedCount;
            fivesTotalCount = fivesCount + fivesFrackedCount;
            qtrTotalCount = qtrCount + qtrFrackedCount;
            hundredsTotalCount = hundredsCount + hundredsFrackedCount;
            twoFiftiesTotalCount = twoFiftiesCount + twoFrackedFiftiesCount;


            int totalAmount = onesTotalCount + (fivesTotalCount * 5) + (qtrTotalCount * 25) + (hundredsTotalCount * 100) + (twoFiftiesTotalCount * 250);

            //Output  " 12.3"
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.Out.WriteLine("                                                                    ");
            Console.Out.WriteLine("    Total Coins in Bank:    " + string.Format("{0,8:N0}", totalAmount) + "                                ");
            Console.Out.WriteLine("                                                                    ");
            Console.Out.WriteLine("                 1s         5s         25s       100s       250s    ");
            Console.Out.WriteLine("                                                                    ");
            Console.Out.WriteLine("   Perfect:   " + string.Format("{0,7}", onesCount) + "    " + string.Format("{0,7}", fivesCount) + "    " + string.Format("{0,7}", qtrCount) + "    " + string.Format("{0,7}", hundredsCount) + "    " + string.Format("{0,7}", twoFiftiesCount) + "   ");
            Console.Out.WriteLine("                                                                    ");
            Console.Out.WriteLine("   Fracked:   " + string.Format("{0,7}", onesFrackedCount) + "    " + string.Format("{0,7}", fivesFrackedCount) + "    " + string.Format("{0,7}", qtrFrackedCount) + "    " + string.Format("{0,7}", hundredsFrackedCount) + "    " + string.Format("{0,7}", twoFrackedFiftiesCount) + "   ");
            Console.Out.WriteLine("                                                                    ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }
        private async static Task ProcessInput(int input)
        {
            switch (input)
            {
                case 1:
                    await EchoRaida();
                    break;
                case 2:
                    showCoins();
                    break;
                case 3:
                    await detect();
                    break;
                case 4:
                    export();
                    break;
                case 6:
                    try
                    {
                        Process.Start(FS.RootPath);
                    }
                    catch(Exception e)
                    {
                        updateLog(e.Message);
                    }
                    showFolders();
                    break;
                case 5:
                    Fix();
                    //fix(timeout);
                    break;
                case 7:
                    help();
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

        private static int GetNetworkNumber(RAIDADirectory dir)
        {
            return 1;

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
            Console.Out.WriteLine("                            Network Number "+ NetworkNumber +"                      ");
            Console.Out.WriteLine("                                                                  ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;

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

        public async static Task EchoRaida()
        {
            Console.Out.WriteLine(String.Format( "Starting Echo to RAIDA Network {0}\n",NetworkNumber));
            Console.Out.WriteLine("----------------------------------\n");
            var echos = raida.GetEchoTasks();
           

            await Task.WhenAll(echos.AsParallel().Select(async task => await task()));
            Console.Out.WriteLine("Ready Count -" + raida.ReadyCount);
            Console.Out.WriteLine("Not Ready Count -" + raida.NotReadyCount);

            for (int i = 0; i < raida.nodes.Count(); i++)
            {
                Debug.WriteLine("Node" + i + " Status --" + raida.nodes[i].RAIDANodeStatus);
                //updateLog("Node" + i + " Status --" + raida.nodes[i].RAIDANodeStatus);
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
                        coin.pown = "";
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
                    FS.WriteCoin(coins, FS.DetectedFolder);
                    FS.RemoveCoins(coins, FS.PreDetectFolder);


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
            //detectedCoins.ForEach(x => x.pown= "ppppppppppppppppppppppppp");

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

            //Console.Read();
           

        }

        public static void updateLog(string logLine)
        {
            logger.Info(logLine);
            Console.Out.WriteLine(logLine);
        }

        public static void export()
        {
            Console.Out.WriteLine("");
            Banker bank = new Banker(FS);
            int[] bankTotals = bank.countCoins(FS.BankFolder);
            int[] frackedTotals = bank.countCoins(FS.FrackedFolder);
            Console.Out.WriteLine("  Your Bank Inventory:");
            //int grandTotal = (bankTotals[0] + frackedTotals[0]);
            showCoins();
            // state how many 1, 5, 25, 100 and 250
            int exp_1 = 0;
            int exp_5 = 0;
            int exp_25 = 0;
            int exp_100 = 0;
            int exp_250 = 0;
            //Warn if too many coins
            Console.WriteLine(bankTotals[1] + frackedTotals[1] + bankTotals[2] + frackedTotals[2] + bankTotals[3] + frackedTotals[3] + bankTotals[4] + frackedTotals[4] + bankTotals[5] + frackedTotals[5]);
            if (((bankTotals[1] + frackedTotals[1]) + (bankTotals[2] + frackedTotals[2]) + (bankTotals[3] + frackedTotals[3]) + (bankTotals[4] + frackedTotals[4]) + (bankTotals[5] + frackedTotals[5])) > 1000)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine("Warning: You have more than 1000 Notes in your bank. Stack files should not have more than 1000 Notes in them.");
                Console.Out.WriteLine("Do not export stack files with more than 1000 notes. .");
                Console.ForegroundColor = ConsoleColor.White;
            }//end if they have more than 1000 coins

            Console.Out.WriteLine("  Do you want to export your CloudCoin to (1)jpgs , (2) stack (JSON) , (3) QR Code (4) 2D Bar code file?");
            int file_type = reader.readInt(1, 4);
            // 1 jpg 2 stack
            if (onesTotalCount > 0)
            {
                Console.Out.WriteLine("  How many 1s do you want to export?");
                exp_1 = reader.readInt(0, (onesTotalCount));
            }

            // if 1s not zero 
            if (fivesTotalCount > 0)
            {
                Console.Out.WriteLine("  How many 5s do you want to export?");
                exp_5 = reader.readInt(0, (fivesTotalCount));
            }

            // if 1s not zero 
            if ((qtrTotalCount > 0))
            {
                Console.Out.WriteLine("  How many 25s do you want to export?");
                exp_25 = reader.readInt(0, (qtrTotalCount));
            }

            // if 1s not zero 
            if (hundredsTotalCount > 0)
            {
                Console.Out.WriteLine("  How many 100s do you want to export?");
                exp_100 = reader.readInt(0, (hundredsTotalCount));
            }

            // if 1s not zero 
            if (twoFiftiesTotalCount > 0)
            {
                Console.Out.WriteLine("  How many 250s do you want to export?");
                exp_250 = reader.readInt(0, (twoFiftiesTotalCount));
            }

            // if 1s not zero 
            // move to export
            Exporter exporter = new Exporter(FS);
            if (file_type == 1)
            {
                Console.Out.WriteLine("  Tag your jpegs with 'random' to give them a random number.");
            }
            Console.Out.WriteLine("  What tag will you add to the file name?");
            String tag = reader.readString();
            //Console.Out.WriteLine(("Exporting to:" + exportFolder));
            if (file_type == 1)
            {
                exporter.writeJPEGFiles(exp_1, exp_5, exp_25, exp_100, exp_250, tag);
                // stringToFile( json, "test.txt");
            }
            else if(file_type == 2)
            {
                exporter.writeJSONFile(exp_1, exp_5, exp_25, exp_100, exp_250, tag);
            }
            else if (file_type == 3)
            {
                exporter.writeQRCodeFiles(exp_1, exp_5, exp_25, exp_100, exp_250, tag);
                // stringToFile( json, "test.txt");
            }
            else if (file_type == 4)
            {
                exporter.writeBarCode417CodeFiles(exp_1, exp_5, exp_25, exp_100, exp_250, tag);
                // stringToFile( json, "test.txt");
            }


            // end if type jpge or stack
            Console.Out.WriteLine("  Exporting CloudCoins Completed.");
        }// end export One
        /* STATIC METHODS */
        public async static void handleCommand(string[] args)
        {
            string command = args[0];

            switch (command)
            {
                case "echo":
                    await EchoRaida();
                    break;
                case "showcoins":
                    showCoins();
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
                         EchoRaida();
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

        private static void Fix()
        {
            fixer.continueExecution = true;
            fixer.IsFixing = true;
            fixer.FixAll();
            fixer.IsFixing = false;
        }

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

            //QRCodeGenerator qrGenerator = new QRCodeGenerator();
            //QRCodeData qrCodeData = qrGenerator.CreateQrCode("The text which should be encoded.", QRCodeGenerator.ECCLevel.Q);
            //QRCode qrCode = new QRCode(qrCodeData);
            //Bitmap qrCodeImage = qrCode.GetGraphic(20);

            //qrCodeImage.Save("qrcode.jpeg");


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
