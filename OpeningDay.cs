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
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class OpeningDay : Strategy
	{
        // VWAP Values
        double vwap_value;
        double std_deviation_long_value1;
        double std_deviation_long_value2;
        double std_deviation_long_value3;
        double std_deviation_short_value1;
        double std_deviation_short_value2;
        double std_deviation_short_value3;
        double previous_vwap_value;

        //Retest
        bool retest_low_one = false;
        bool retest_low_two = false;
        bool retest_high_one = false;
        bool retest_high_two = false;

        //Highest/Lowest
        double highest;
        double lowest;
        private int lowBreakouts = 0;
        private int highBreakouts = 0;
        bool goinHigh = false;
        bool gettinLow = false;

        // Date
        SessionIterator sessionIterator;
        string currentDate;

        //Bar Counter
        int bar_count = 0;

        bool opening_day = false;
        bool doneForTheDay = false;


        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "OpeningDay";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
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
            if (Bars.IsFirstBarOfSession)
            {
                // use the current bar time to calculate the next session
                sessionIterator.GetNextSession(Time[0], true);
                currentDate = sessionIterator.ActualSessionEnd.ToString("yyyy-MM-dd");
                Print("Current Date: " + currentDate);
                NinjaTrader.Code.Output.Process("--==== Current session start time is " + sessionIterator.ActualSessionEnd.ToString() + " ====--\n\n", PrintTo.OutputTab2);
            }

            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < 1)
                return;

            if (ToTime(Time[0]) > 083000)
                opening_day = true;

            // Get VWAP Values
            VwapCheck();

            // Highs and Lows
            determineHighAndLow();

            // Has retest occurred?
            determineRetest();

            // Are wegettin High or Goin Low?
            getHighGoLow();

            getYourTradeOn();

            if (ToTime(Time[0]) > 100000)
            {
                opening_day = false;
                highBreakouts = 0;
                lowBreakouts = 0;
            }

        }

        public void determineHighAndLow()
        {
            
            double upperWick = High[1];
            double lowerWick = Low[1];
            double open = Open[1];
            double close = Close[1];
            
            // Grab the previous candle's highs and lows 
            if (ToTime(Time[0]) == 084500)
            {
                upperWick = High[1];
                lowerWick = Low[1];
                open = Open[1];
                close = Close[1];

                if (upperWick > close)
                    highest = upperWick;
                else
                    highest = close;

                if (lowerWick < open)
                    lowest = lowerWick;
                else
                    lowest = open;

                NinjaTrader.Code.Output.Process("\n\n-==== HIGHEST/LOWEST ====- \n", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("High: " + upperWick.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Low: " + lowerWick.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Open: " + open.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Close: " + close.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Highest: " + highest.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Lowest: " + lowest.ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("-====  ====- \n\n", PrintTo.OutputTab2);
            }

            
        }

        public void determineRetest()
        {
            if (opening_day)
            {
                bool isBreakoutLow = false;
                bool isBreakoutHigh = false;
                bar_count++;
                
                if (ToTime(Time[0]) >= 084500)
                {
                    double maxx = High[0];//MAX(High, 0)[0];
                    double lowst = Low[0];//MIN(Low, 0)[0];
                    //NinjaTrader.Code.Output.Process("MAX0: " + MAX(High, bar_count)[0].ToString(), PrintTo.OutputTab2);
                    //NinjaTrader.Code.Output.Process("LOW0: " + MIN(Low, bar_count)[0].ToString(), PrintTo.OutputTab2);
                    //NinjaTrader.Code.Output.Process("MAX: " + MAX(High, bar_count)[1].ToString(), PrintTo.OutputTab2);
                    //NinjaTrader.Code.Output.Process("LOW: " + MIN(Low, bar_count)[1].ToString(), PrintTo.OutputTab2);

                    // Identify MAX breakout
                    if (maxx >= highest)
                    {
                        highBreakouts++;
                        isBreakoutHigh = true;
                        NinjaTrader.Code.Output.Process("isBreakoutHigh: " + isBreakoutHigh.ToString(), PrintTo.OutputTab2);
                    }

                    // Identify MIN breakout
                    if (lowst <= lowest)
                    {
                        lowBreakouts++;
                        isBreakoutLow = true;
                        NinjaTrader.Code.Output.Process("isBreakoutLow: " + isBreakoutLow.ToString(), PrintTo.OutputTab2);
                    }

                    

                }
            }
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

            previous_vwap_value = vwiz.PlotVWAP[1];
            vwap_value = vwiz.PlotVWAP[0];
            std_deviation_short_value1 = vwiz.PlotVWAP1L[0];
            std_deviation_long_value1 = vwiz.PlotVWAP1U[0];
            std_deviation_short_value2 = vwiz.PlotVWAP2L[0];
            std_deviation_long_value2 = vwiz.PlotVWAP2U[0];
            std_deviation_short_value3 = vwiz.PlotVWAP3L[0];
            std_deviation_long_value3 = vwiz.PlotVWAP3U[0];

        }

        public void getHighGoLow()
        {
            // High ?
            if (highBreakouts >= 2)
            {
                // Check VWAP
                NinjaTrader.Code.Output.Process("ZLSMA(21)[0]: " + ZLSMA(21)[0].ToString(), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("ZLSMA(21)[1]: " + ZLSMA(21)[1].ToString(), PrintTo.OutputTab2);
                if (vwap_value > previous_vwap_value && ZLSMA(21)[0] > ZLSMA(21)[1])
                {
                    goinHigh = true;
                    NinjaTrader.Code.Output.Process("Gettin High!", PrintTo.OutputTab2);
                }
            }

            // Low ?
            if (lowBreakouts >= 2)
            {
                if (vwap_value < previous_vwap_value)
                {
                    gettinLow = true;
                    NinjaTrader.Code.Output.Process("Goin Low!", PrintTo.OutputTab2);
                }
            }

        }

        public void getYourTradeOn()
        {
            if (Position.MarketPosition != MarketPosition.Flat)
                return;

            if (goinHigh || gettinLow)
            {
                highBreakouts = 0;
                lowBreakouts = 0;
                goinHigh = false;
                gettinLow = false;

                SetStopLoss(CalculationMode.Price, lowest);
                SetProfitTarget(CalculationMode.Price, (Close[0] + ((highest-lowest)*2)));
                EnterLong(4, "Opening Day Long");
                NinjaTrader.Code.Output.Process("Opening Day Long Entered!", PrintTo.OutputTab2);
                opening_day = false;
                doneForTheDay = true;
            }

            if (gettinLow)
            {
                highBreakouts = 0;
                lowBreakouts = 0;
                goinHigh = false;
                gettinLow = false;
                opening_day = false;
                doneForTheDay = true;
            }



            }
    }
}
