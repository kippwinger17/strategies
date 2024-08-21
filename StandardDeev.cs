#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

using System.Globalization;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public class StandardDeev : Strategy
    {
        public bool show_output = false;

        private double entry_price;
        private double stopLossPrice = 0.0;
        private double sl_price = 0.0;

        // Support/Resistance
        double supportVal;
        double resistanceVal;

        double prevSuppBottom1;
        double prevSuppBottom2;
        double prevSuppBottom3;
        double supTop1;
        double supTop2;
        double supTop3;
        double supBottom1;
        double supBottom2;
        double supBottom3;

        double prevResTop1;
        double prevResTop2;
        double prevResTop3;
        double resTop1;
        double resTop2;
        double resTop3;
        double resBotton1;
        double resBotton2;
        double resBotton3;

        // HighestHighs/LowestLows
        double highestHigh;
        double LowestLow;

        // VWAP Values
        double vwap_value;
        double std_deviation_long_value1;
        double std_deviation_long_value2;
        double std_deviation_long_value3;
        double std_deviation_short_value1;
        double std_deviation_short_value2;
        double std_deviation_short_value3;

        // Times
        int StartTime;
        int EndTime;
        int KillingTime;

        // Entries
        bool vwap_long = false;
        bool std_dev_long_1 = false;
        bool std_dev_long_2 = false;
        bool vwap_short = false;
        bool std_dev_short_1 = false;
        bool std_dev_short_2 = false;

        // Date
        SessionIterator sessionIterator;
        string currentDate;

        // News
        public bool has_news_been_checked_today = false;
        public DateTime date_news_was_last_checked = new DateTime();
        private DateTime lastNewsUpdate = DateTime.MinValue;
        private string lastLoadError;
        public bool Debug = false;
        private CultureInfo ffDateTimeCulture = CultureInfo.CreateSpecificCulture("en-US");

        public bool bad_news_day = false;

        HashSet<string> dates = new HashSet<string>
        {
            "2024-01-11", // January dates
            "2024-01-17",
            "2024-01-19",
            "2024-01-24",
            "2024-01-25",
            "2024-01-30",
            "2024-01-31",

            "2024-02-01", // February dates
            "2024-02-02",
            "2024-02-04",
            "2024-02-05",
            "2024-02-08",
            "2024-02-13",
            "2024-02-15",
            "2024-02-21",
            "2024-02-22",
            "2024-02-27",
            "2024-02-28",

            "2024-03-01", // March dates
            "2024-03-05",
            "2024-03-06",
            "2024-03-07",
            "2024-03-08",
            "2024-03-12",
            "2024-03-13",
            "2024-03-15",
            "2024-03-20",
            "2024-03-21",
            "2024-03-22",
            "2024-03-26",
            "2024-03-27",

            "2024-04-01", // April dates
            "2024-04-02",
            "2024-04-03",
            "2024-04-04",
            "2024-04-05",
            "2024-04-10",
            "2024-04-12",
            "2024-04-15",
            "2024-04-16",
            "2024-04-18",
            "2024-04-23",
            "2024-04-25",
            "2024-04-30",

            "2024-05-01", // May dates
            "2024-05-02",
            "2024-05-03",
            "2024-05-09",
            "2024-05-10",
            "2024-05-15",
            "2024-05-16",
            "2024-05-22",
            "2024-05-23",
            "2024-05-24",
            "2024-05-28",
            "2024-05-30",

            "2024-06-03", // June dates
            "2024-06-04",
            "2024-06-05",
            "2024-06-06",
            "2024-06-07",
            "2024-06-12",
            "2024-06-14",
            "2024-06-17",
            "2024-06-18",
            "2024-06-20",
            "2024-06-21",
            "2024-06-25",
            "2024-06-26",

            "2024-07-02", // July dates
            "2024-07-11",
            "2024-07-25",

            "2024-08-02",
            "2024-08-05", // August
            "2024-08-14"

        };

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Strategy here.";
                Name = "StandardDeev";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;

                // Default Times
                StartTime = 50000;
                EndTime = 105000;

                Debug = false;

            }
            else if (State == State.Configure)
            {
            }
            if (State == State.Historical)
            {
                // Instantiate mySessionIterator once in State.Configure
                sessionIterator = new SessionIterator(Bars);
            }
        }

        protected override void OnBarUpdate()
        {
            // Get the date
            DateTime todays_date = Time[0].Date;
            //NinjaTrader.Code.Output.Process("Today " + todays_date.ToString(), PrintTo.OutputTab2);
            
            //if (date_news_was_last_checked == todays_date)
            //    bad_news_day = false;


            if (Bars.IsFirstBarOfSession)
            {
                // use the current bar time to calculate the next session
                sessionIterator.GetNextSession(Time[0], true);
                currentDate = sessionIterator.ActualSessionEnd.ToString("yyyy-MM-dd");
                Print("Current Date: " + currentDate);
                NinjaTrader.Code.Output.Process("--==== Current session start time is " + sessionIterator.ActualSessionEnd.ToString() + " ====--", PrintTo.OutputTab2);
            }

            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < 1)
                return;

            //bool isBadNews = await CheckNews();
            if (date_news_was_last_checked != todays_date)
            {
                bad_news_day = GetEventsForWeek(todays_date);
                NinjaTrader.Code.Output.Process("\tNews Checked on " + date_news_was_last_checked.ToString(), PrintTo.OutputTab2);
                if (bad_news_day)
                    NinjaTrader.Code.Output.Process("\tDON'T TRADE TODAY!!", PrintTo.OutputTab2);
                else
                    NinjaTrader.Code.Output.Process("\tWE TRADE TODAY!!", PrintTo.OutputTab2);

                NinjaTrader.Code.Output.Process("\n--====  ====--\n", PrintTo.OutputTab2);
            }

                CheckLevels();
            if (show_output)
            {
                NinjaTrader.Code.Output.Process("------------------------CHECKS------------------------", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Prev. Close: " + Close[1].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Prev. Open: " + Open[1].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Open: " + Open[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("High: " + High[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("\n", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop1: " + resTop1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop2: " + resTop2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop3: " + resTop3.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton1: " + resBotton1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton2: " + resBotton2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton3: " + resBotton3.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("\n", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Close - entry_price: " + (Close[0] - entry_price).ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("\n", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("------------------------END CHECKS------------------------", PrintTo.OutputTab2);
            }
            VwapCheck();

            getYourTradeOn();

            getYourExitOn();

        }

        public bool GetEventsForWeek(DateTime today)
        {
            string filePath = @"C:\Users\Jones\Documents\Stocks\News\news.xml";
            string formattedDate = today.ToString("MM-dd-yyyy");
            bool bday = false;

            try
            {
                // Load the XML from a local file
                XDocument doc = XDocument.Load(filePath);

                // Parse the XML and pull out the events where the date matches today's date
                var eventsToday = doc.Descendants("event")
                                     .Where(e => e.Element("date")?.Value.Trim() == formattedDate &&  // Date condition
                                         e.Element("country")?.Value == "USD" &&  // Country condition
                                         e.Element("impact")?.Value.Trim() == "High")  // Impact condition
                                     .Select(e => new
                                     {
                                         Title = e.Element("title")?.Value,
                                         Country = e.Element("country")?.Value,
                                         Date = e.Element("date")?.Value,
                                         Impact = e.Element("impact")?.Value
                                     });

                // Check if any events match today's date and print them out
                foreach (var ev in eventsToday)
                {
                    NinjaTrader.Code.Output.Process($"Title: {ev.Title}, Country: {ev.Country}, Date: {ev.Date}", PrintTo.OutputTab2);
                    if (ev.Title.Contains("CPI ") ||
                        ev.Title.Contains("Core Retail") ||
                        ev.Title.Contains("Fed Chair ") ||
                        ev.Title.Contains("Prelim UoM Consumer Sentiment") ||
                        ev.Title.Contains("ISM Services PMI") ||
                        ev.Title.Contains("Unemployment Claims"))
                    {
                        NinjaTrader.Code.Output.Process("Don't Trade Today! (" + formattedDate + ")", PrintTo.OutputTab2);
                        date_news_was_last_checked = today;
                        return true;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching or processing the news: {ex.Message}");
            }

            date_news_was_last_checked = today;
            return false;
        }

        public bool GetEventsForToday(DateTime today)
        {
            string url = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml";
            //DateTime today = Time[0].Date;
            string formattedDate = today.ToString("MM-dd-yyyy");

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Download the XML content synchronously
                    string xmlContent = client.GetStringAsync(url).Result;

                    // Load the XML into an XDocument
                    XDocument doc = XDocument.Parse(xmlContent);

                    // Parse the XML and pull out the events where the date matches today's date
                    var eventsToday = doc.Descendants("event")
                                         .Where(e => e.Element("date")?.Value == formattedDate)
                                         .Select(e => new
                                         {
                                             Title = e.Element("title")?.Value,
                                             Country = e.Element("country")?.Value,
                                             Date = e.Element("date")?.Value,
                                             Impact = e.Element("impact")?.Value
                                         });

                    // Output the results
                    foreach (var ev in eventsToday)
                    {
                        if (ev.Country == "USD" && ev.Date == formattedDate && ev.Impact == "High")
                        {
                            NinjaTrader.Code.Output.Process($"Title: {ev.Title}, Country: {ev.Country}, Date: {ev.Date}", PrintTo.OutputTab2);
                            if (ev.Title.Contains("CPI ") ||
                                ev.Title.Contains("core retail") ||
                                ev.Title.Contains("Fed Chair ") ||
                                ev.Title.Contains("Prelim UoM Consumer Sentiment") ||
                                ev.Title.Contains("ISM Services PMI") ||
                                ev.Title.Contains("Unemployment Claims"))
                            {
                                NinjaTrader.Code.Output.Process("Don't Trade Today! (" + formattedDate + ")", PrintTo.OutputTab2);
                                date_news_was_last_checked = today;
                                return true;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching or processing the news: {ex.Message}");
                }

            }
            date_news_was_last_checked = today;
            return false;
        }

        
        public void CheckLevels()
        {
            // Support/Resistance
            DynamicSRLines d = DynamicSRLines(5, 200, 10, 2, 3, false, Brushes.SteelBlue, Brushes.IndianRed);
            //NinjaTrader.Code.Output.Process("CurrentBar: " + CurrentBar.ToString(), PrintTo.OutputTab2);
            if (CurrentBar <= 300)
                return;

            supportVal = d.SupportPlot[0];
            resistanceVal = d.ResistancePlot[0];

            prevSuppBottom1 = d.SupBottom1[1];
            prevSuppBottom2 = d.SupBottom2[1];
            prevSuppBottom3 = d.SupBottom3[1];
            supTop1 = d.SupTop1[0];
            supTop2 = d.SupTop2[0];
            supTop3 = d.SupTop3[0];
            supBottom1 = d.SupBottom1[0];
            supBottom2 = d.SupBottom2[0];
            supBottom3 = d.SupBottom3[0];

            resTop1 = d.ResTop1[0];
            resTop2 = d.ResTop2[0];
            resTop3 = d.ResTop3[0];
            prevResTop1 = d.ResTop1[1];
            prevResTop2 = d.ResTop2[1];
            prevResTop3 = d.ResTop3[1];
            resBotton1 = d.ResBottom1[0];
            resBotton2 = d.ResBottom2[0];
            resBotton3 = d.ResBottom3[0];

            //NinjaTrader.Code.Output.Process("Resistance: " + d.ResistancePlot[0].ToString(), PrintTo.OutputTab2);
            //NinjaTrader.Code.Output.Process("Support: " + d.SupportPlot[0].ToString(), PrintTo.OutputTab2);


            // Highest Highs / Lowest Lows
            SupportTheResistance s = SupportTheResistance(1, 0, 5, 0);
            //NinjaTrader.Code.Output.Process("CurrentBar: " + CurrentBar.ToString(), PrintTo.OutputTab2);
            if (CurrentBar <= 5323)
                return;

            LowestLow = s.SupportPlot[0];
            highestHigh = s.ResistancePlot[0];

            //NinjaTrader.Code.Output.Process("Highest: " + s.ResistancePlot[0].ToString(), PrintTo.OutputTab2);
            //NinjaTrader.Code.Output.Process("Lowest: " + s.SupportPlot[0].ToString(), PrintTo.OutputTab2);
        }

        public void VwapCheck()
        {
            VWAPx vwiz = VWAPx();
            vwiz.NumDeviations = 3;
            vwiz.SD1 = 1;
            vwiz.SD2 = 2.01;
            vwiz.SD3 = 2.51;
            vwiz.SD4 = 3.1;
            vwiz.SD5 = 4;

            vwap_value = vwiz.PlotVWAP[0];
            std_deviation_short_value1 = vwiz.PlotVWAP1L[0];
            std_deviation_long_value1 = vwiz.PlotVWAP1U[0];
            std_deviation_short_value2 = vwiz.PlotVWAP2L[0];
            std_deviation_long_value2 = vwiz.PlotVWAP2U[0];
            std_deviation_short_value3 = vwiz.PlotVWAP3L[0];
            std_deviation_long_value3 = vwiz.PlotVWAP3U[0];

            /*
            NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("Prev. Close: " + Close[1].ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("Prev. Open: " + Open[1].ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("Open: " + Open[0].ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("std_deviation_short_value1: " + std_deviation_short_value1.ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("std_deviation_long_value2: " + std_deviation_long_value2.ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("std_deviation_short_value2: " + std_deviation_short_value2.ToString(), PrintTo.OutputTab2);
            */
        }

        public void getYourTradeOn()
        {

            if (Position.MarketPosition != MarketPosition.Flat)
                return;

            // Check Time 
            if (!(ToTime(Time[0]) >= StartTime && ToTime(Time[0]) <= EndTime))
                return;

            // *** AVOID CPI DAYS & core retail sales & Prelim UoM Consumer Sentiment & Unemployment Claims ***
            if (dates.Contains(currentDate) || bad_news_day)
            {
                return;
            }

            // Longs
            // VWAP
            if (show_output)
            {
                NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Prev. Close: " + Close[1].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Prev. Open: " + Open[1].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Open: " + Open[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_short_value1: " + std_deviation_short_value1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_long_value2: " + std_deviation_long_value2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_short_value2: " + std_deviation_short_value2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("\n", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop1: " + resTop1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop2: " + resTop2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop3: " + resTop3.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton1: " + resBotton1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton2: " + resBotton2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton3: " + resBotton3.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("\n", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("(Close[0] > EMA(55)[0]): " + (Close[0] > EMA(55)[0]).ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("TEMA(21)[0] > SMA(21)[0]: " + (TEMA(21)[0] > SMA(21)[0]).ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("(Close[1] < vwap_value): " + (Close[1] < vwap_value).ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("(Open[1] < vwap_value): " + (Open[1] < vwap_value).ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Close[0] > vwap_value: " + (Close[0] > vwap_value).ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process(" Open[0] > vwap_value: " + (Open[0] > vwap_value).ToString(), PrintTo.OutputTab2);
            }
            if (Close[0] > EMA(55)[0]
                && TEMA(21)[0] > SMA(21)[0]
                && vwap_value > EMA(55)[0]
                && ((Close[1] < vwap_value || Open[1] < vwap_value)
                    && (Close[0] > vwap_value && Open[0] > vwap_value))
                )
            {
                if (show_output)
                {
                    NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Prev. Close: " + Close[1].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Prev. Open: " + Open[1].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Open: " + Open[0].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_short_value1: " + std_deviation_short_value1.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_long_value2: " + std_deviation_long_value2.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_short_value2: " + std_deviation_short_value2.ToString(), PrintTo.OutputTab2);
                }

                EnterLong(4, "VWAP Cross Long");
                vwap_long = true;

                //EnterLongStopLimit(std_deviation_long_value1, std_deviation_short_value2);
                //stopLossPrice = Position.AveragePrice - 10 * TickSize;
                //sl_price = std_deviation_short_value2 - 20;
                //SetStopLoss(CalculationMode.Price, sl_price);
                double profit_target = std_deviation_long_value1;
                //ExitLongLimit(resTop1, "VWAP Cross Long");

                /*if (resTop1 < std_deviation_long_value1)
                    SetProfitTarget(CalculationMode.Price, resTop1);
                else
                    SetProfitTarget(CalculationMode.Price, std_deviation_long_value1);*/

                entry_price = GetCurrentBid();
                //SetStopLoss(CalculationMode.Price, Close[0] + 20);
                //SetProfitTarget(CalculationMode.Price, std_deviation_long_value2);

                NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
            }

            // Std. Dev1 to std. dev2
            if (Close[0] > EMA(55)[0]
                && TEMA(21)[0] > SMA(21)[0]
                && std_deviation_long_value1 > EMA(55)[0]
                && ((Close[1] < std_deviation_long_value1 || Open[1] < std_deviation_long_value1)
                    && (Close[0] > std_deviation_long_value1 && Open[0] > std_deviation_long_value1))
                )
            {
                if (show_output)
                {
                    NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Prev. Close: " + Close[1].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Prev. Open: " + Open[1].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Open: " + Open[0].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_short_value1: " + std_deviation_short_value1.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_long_value2: " + std_deviation_long_value2.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_short_value2: " + std_deviation_short_value2.ToString(), PrintTo.OutputTab2);
                }

                //EnterLong(4, "Std1 Cross Long");
                //std_dev_long_1 = true;
                //EnterLongStopLimit(std_deviation_long_value1, std_deviation_short_value2);
                //stopLossPrice = Position.AveragePrice - 10 * TickSize;
                //sl_price = std_deviation_short_value2 - 20;
                //SetStopLoss(CalculationMode.Price, sl_price);
                double profit_target = std_deviation_long_value1;
                //ExitLongLimit(resTop1, "VWAP Cross Long");

                /*if (resTop1 < std_deviation_long_value1)
                    SetProfitTarget(CalculationMode.Price, resTop1);
                else
                    SetProfitTarget(CalculationMode.Price, std_deviation_long_value1);*/

                entry_price = GetCurrentBid();
                //SetStopLoss(CalculationMode.Price, Close[0] + 20);
                //SetProfitTarget(CalculationMode.Price, std_deviation_long_value2);

                NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
            }


            // Shorts 
            if (Close[0] < EMA(55)[0]
                && TEMA(21)[0] < SMA(21)[0]
                && ((Close[1] > std_deviation_short_value1 || Open[1] > std_deviation_short_value1)
                    && (Close[0] < std_deviation_short_value1 && Open[0] < std_deviation_short_value1))
                )
            {
                vwap_short = true;
                if (show_output)
                {
                    NinjaTrader.Code.Output.Process("", PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Prev. Close: " + Close[1].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Prev. Open: " + Open[1].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("Open: " + Open[0].ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_short_value1: " + std_deviation_short_value1.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_long_value2: " + std_deviation_long_value2.ToString(), PrintTo.OutputTab2);
                    NinjaTrader.Code.Output.Process("std_deviation_short_value2: " + std_deviation_short_value2.ToString(), PrintTo.OutputTab2);
                }
                //EnterShort(1, "VWAP Cross Short");
                //EnterLongStopLimit(std_deviation_long_value1, std_deviation_short_value2);
                //stopLossPrice = Position.AveragePrice - 10 * TickSize;
                //sl_price = std_deviation_short_value2 - 20;
                //SetStopLoss(CalculationMode.Price, sl_price);
                double profit_target = std_deviation_long_value1;
                entry_price = GetCurrentBid();

                //ExitLongLimit(resTop1, "VWAP Cross Long");

                /*if (resTop1 < std_deviation_long_value1)
                    SetProfitTarget(CalculationMode.Price, resTop1);
                else
                    SetProfitTarget(CalculationMode.Price, std_deviation_long_value1);*/

                //entry_price = GetCurrentBid();
                //SetStopLoss(CalculationMode.Price, Close[0] + 20);
                //SetProfitTarget(CalculationMode.Price, std_deviation_long_value2);

            }

        }



        public void getYourExitOn()
        {
            if (Position.MarketPosition == MarketPosition.Long)
            {
                /*NinjaTrader.Code.Output.Process("SELL SELL SELL", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Close: " + Close[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("vwap_value: " + vwap_value.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_long_value1: " + std_deviation_long_value1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_short_value1: " + std_deviation_short_value1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_long_value2: " + std_deviation_long_value2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("std_deviation_short_value2: " + std_deviation_short_value2.ToString(), PrintTo.OutputTab2);

                NinjaTrader.Code.Output.Process("prevResTop1: " + prevResTop1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("prevResTop2: " + prevResTop2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("prevResTop3: " + prevResTop3.ToString(), PrintTo.OutputTab2);

                NinjaTrader.Code.Output.Process("resTop1: " + resTop1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop2: " + resTop2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resTop3: " + resTop3.ToString(), PrintTo.OutputTab2);

                NinjaTrader.Code.Output.Process("resBotton1: " + resBotton1.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton2: " + resBotton2.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("resBotton3: " + resBotton3.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("END SELL\n\n", PrintTo.OutputTab2);
                */


                if (vwap_long)
                {
                    // Safe guards longs
                    // If we made 8 ticks 
                    if (Close[0] > entry_price + 5)
                    {
                        ExitLong("VWAP Cross Long");
                        vwap_long = false;
                        std_dev_long_1 = false;
                        vwap_short = false;
                    }

                    // If we went under 28 ticks
                    if (Close[0] < entry_price - 28)
                    {
                        ExitLong("VWAP Cross Long");
                        vwap_long = false;
                        std_dev_long_1 = false;
                        vwap_short = false;
                    }

                    // If PA is below the 1st std dev. and PA hits a combined resistance
                    if (Close[0] < std_deviation_long_value1
                        && (resTop3 == resTop2)
                        && (Close[0] >= resBotton1)
                        && ((resBotton1 - entry_price) > 4)
                        )
                    {
                        ExitLong("VWAP Cross Long");
                        vwap_long = false;
                        std_dev_long_1 = false;
                        vwap_short = false;
                    }

                    // If we close over the 1st deviation
                    if (Close[0] >= std_deviation_long_value1
                        && ((std_deviation_long_value1 - entry_price) > 2)
                        )
                    {
                        ExitLong("VWAP Cross Long");
                        vwap_long = false;
                        std_dev_long_1 = false;
                        vwap_short = false;
                    }
                }

                if (std_dev_long_1)
                {
                    if (Close[0] > entry_price + 1)
                    {
                        ExitLong("Std1 Cross Long");
                        vwap_long = false;
                        std_dev_long_1 = false;
                        vwap_short = false;
                    }

                    // If we went under X# ticks
                    /*if (Close[0] < entry_price - 30)
                    {
                        ExitLong("Std1 Cross Long");
                        vwap_long = false;
                        std_dev_long_1 = false;
                        vwap_short = false;
                    }*/
                }


                //if (Close[0] < entry_price - 20)
                //    ExitLong("VWAP Cross Long");

                // Too short
                //if (Close[0] > entry_price - 32)
                //    ExitLong("VWAP Cross Long");

                // Exit Long
                /*if (vwap_long 
                    && Close[0] >= prevResTop1
                    && resTop1 > entry_price + 4)
                {
                    ExitLong("Exit Cross", "");
                    vwap_long = false;
                    vwap_short = false;
                    std_dev_long_1 = false;
                    std_dev_long_2 = false;
                    std_dev_short_1 = false;
                    std_dev_short_2 = false;
                }*/



                // Catch low dips 
                /*if (vwap_long && (Close[0] <= std_deviation_short_value1)
                   )
                {
                    ExitLong("Exit Cross", "");
                    vwap_long = false;
                    vwap_short = false;
                    std_dev_long_1 = false;
                    std_dev_long_2 = false;
                    std_dev_short_1 = false;
                    std_dev_short_2 = false;
                }*/


                // In a VWAP Cross; Close Above 1st Resistance, 
                /*if (vwap_long && (Close[0] <= resTop1))                    
                {
                    ExitLong("Exit Cross", "");
                    vwap_long = false;
                    vwap_short = false;
                    std_dev_long_1 = false;
                    std_dev_long_2 = false;
                    std_dev_short_1 = false;
                    std_dev_short_2 = false;
                }

                if (vwap_long && (Close[0] <= std_deviation_short_value2) 
                    || (std_dev_long_1 && (Close[0] <= std_deviation_short_value1))
                    || (std_dev_long_2 && (Close[0] <= vwap_value))
                    )
                {
                    ExitLong("Exit Cross", "");
                    vwap_long = false;
                    vwap_short = false;
                    std_dev_long_1 = false;
                    std_dev_long_2 = false;
                    std_dev_short_1 = false;
                    std_dev_short_2 = false;
                }*/


            }

            if (Position.MarketPosition == MarketPosition.Short)
            {
                //if (Close[0] == entry_price - (10/4))
                //    ExitShort();

                // Safe guards longs
                // If we made 8 ticks 
                if (Close[0] < entry_price - 2)
                {
                    ExitShort("VWAP Cross Short");
                    vwap_long = false;
                    std_dev_long_1 = false;
                    vwap_short = false;
                }

                if (Close[0] > entry_price + 11)
                {
                    ExitShort("VWAP Cross Short");
                    vwap_long = false;
                    std_dev_long_1 = false;
                    vwap_short = false;
                }


                // If we went under 29 ticks
                //if (Close[0] > entry_price + 28)
                //    ExitLong("VWAP Cross Short");

                // Exit Short
                /*if (vwap_short
                    && Close[0] <= prevSuppBottom1
                    && supBottom1 > entry_price - 4)
                {
                    ExitShort("Exit Cross", "");
                    vwap_long = false;
                    vwap_short = false;
                    std_dev_long_1 = false;
                    std_dev_long_2 = false;
                    std_dev_short_1 = false;
                    std_dev_short_2 = false;
                }*/
            }
        }
    }
}
