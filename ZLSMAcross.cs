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
	public class ZLSMAcross : Strategy
	{
        private double entry_price;

        // London
        private DateTime londonSessionStart = new DateTime(1, 1, 1, 8, 0, 0); // London session start time (8:00 AM GMT)
        private DateTime londonSessionEnd = new DateTime(1, 1, 1, 16, 0, 0); // London session end time (4:00 PM GMT)
        private int sessionStartIndex = -1; // Index of the first bar in the London session
        public double londonHigh = 0.0;
        public double londonLow = 0.0;

        public double mericaHigh = 0.0;
        public double mericaLow = 0.0;

        public double seriesOneHigh = 0;
        public double seriesOneLow = 0;

        // 5/15 Series
        public int StartHour;
        public int StartMinute;
        public int EndHour;
        public int EndMinute;
        private DateTime startDateTime;
        private DateTime endDateTime;

        public double volume;
        private EMA Ema5m;


        bool crossFifty = false;
        bool crossTwentyOne = false;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "ZLSMAcross";
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

                StartHour = 1;
                StartMinute = 0;
                EndHour = 2;
                EndMinute = 0;
			}
			else if (State == State.Configure)
			{
                // Add a 15 minute Bars object - BarsInProgress index = 2
                AddDataSeries(BarsPeriodType.Minute, 5);
            }
            else if (State == State.DataLoaded)
            {
                Ema5m = EMA(BarsArray[1], 100);  //set EMA here so we make sure it's calculated on the secondary data series
                
            }
        }


        protected override void OnBarUpdate()
        {
           NinjaTrader.Code.Output.Process("BarsInProgress: " + BarsInProgress.ToString(), PrintTo.OutputTab1);
            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < 1 || CurrentBars[1] < 100)
                return;

            NinjaTrader.Code.Output.Process("---------------------------------------------", PrintTo.OutputTab1);

            NinjaTrader.Code.Output.Process(" EMA(100)[0]: " + EMA(100)[0].ToString(), PrintTo.OutputTab1);
            NinjaTrader.Code.Output.Process(" Ema5m[1]: " + Ema5m[0].ToString(), PrintTo.OutputTab1);


            // Check London Highs and Lows
            CheckLondon();

            // Check 'Merica Highs and Lows
            CheckMerica();

            // Check the current volume
            GetVolume();

            // Check for the cross
            if (CheckForEntries())
            {
                NinjaTrader.Code.Output.Process("ZLSMA Cross Happened", PrintTo.OutputTab1);

                // SET TRAILING STOP //
                /*if (High[0] < trailingStopPrice) // If the price is below the trailing stop, adjust the stop
                {
                    trailingStopPrice = Math.Min(tradeParameters.CurrentPrice + trailAmount, trailingStopPrice);
                    SetTrailStop(CalculationMode.Price, trailingStopPrice);
                }*/
            }

            // Check Support/Resistance at m5 and 15m
            //WhereAreTheLimits();

            // In a Position?  Has London Been Hit?
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                if (Position.MarketPosition == MarketPosition.Long)
                {
                    
                    NinjaTrader.Code.Output.Process(" entry_price: " + entry_price.ToString(), PrintTo.OutputTab1);
                    NinjaTrader.Code.Output.Process(" High: " + High[0].ToString(), PrintTo.OutputTab1);
                    NinjaTrader.Code.Output.Process(" Close: " + Close[0].ToString(), PrintTo.OutputTab1);
                    /*if (CrossAbove(Close, londonHigh, 1))
                        ExitLong();*/

                    if (High[0] > londonHigh)
                        ExitLong();
                }

                if (Position.MarketPosition == MarketPosition.Short)
                {
                   /* if (CrossBelow(Close[0], londonLow, 1))
                        ExitShort();*/
                    if (Low[0] < londonLow)
                        ExitShort();
                }

            }


            // In a Short?  Has the 5m 100 been hit?
            if (Position.MarketPosition == MarketPosition.Short)
            {
                
                if (Low[0] <= Ema5m[0])
                    ExitShort();
            }

            

        }

        public bool CheckForEntries()
        {
            bool itIsAlive = false;

            //NinjaTrader.Code.Output.Process("ZLSMA: " + ZLSMA()[0].ToString(), PrintTo.OutputTab1);
            //NinjaTrader.Code.Output.Process(" ZLEMA(50): " + ZLEMA(50)[0].ToString(), PrintTo.OutputTab1);

            // Long Entries
            // - First Cross
            if (CrossAbove(ZLSMA(2), ZLEMA(50), 1))
                crossFifty = true;
            
            // - Second Cross
            if (CrossAbove(ZLSMA(2), SMA(21), 1))
                crossTwentyOne = true;

            // - Have Both Crosses Occurred?
            if (crossFifty && crossTwentyOne)
            {
                Draw.ArrowUp(this, "ZLSMAUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 3 * TickSize, Brushes.White);
                // Check Volume
                if (volume > 1000) 
                {
                    // How does PA look?
                    if (Close[0] > ZLSMA(2)[0] && Close[0] > ZLEMA(50)[0] && Close[0] > EMA(100)[0])
                    {
                        Draw.ArrowUp(this, "ZLSMAUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 3 * TickSize, Brushes.Gold);
                        EnterLong(Convert.ToInt32(DefaultQuantity), "");
                        entry_price = GetCurrentBid();
                    }

                }
                 // Reset Values
                crossFifty = false;
                crossTwentyOne = false;
                itIsAlive = true;
            }

            // Short Entries
            // - First Cross
            if (CrossBelow(ZLSMA(2), ZLEMA(50), 1))
                crossFifty = true;

            // - Second Cross
            if (CrossBelow(ZLSMA(2), SMA(21), 1))
                crossTwentyOne = true;

            // - Have Both Crosses Occurred?
            if (crossFifty && crossTwentyOne)
            {
                Draw.ArrowDown(this, "ZLSMADown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, Brushes.White);
                // Check Volume
                if (volume > 1000)
                {
                    // How does PA look?
                    if (Close[0] < ZLSMA(2)[0] && Close[0] < ZLEMA(50)[0] && Close[0] < EMA(100)[0])
                    { 
                        Draw.ArrowDown(this, "ZLSMADown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, Brushes.Gold);
                        EnterShort(Convert.ToInt32(DefaultQuantity), "");
                        entry_price = GetCurrentBid();
                    }
                }
                // Reset Values
                crossFifty = false;
                crossTwentyOne = false;
                itIsAlive = true;
            }

            return itIsAlive;
        }

        public void WhereAreTheLimits()
        {
            //DateTime today = DateTime.Now.ToString("yyyyMMdd");
            DateTime tDate = DateTime.Now;
            string todaysDate = tDate.ToString("yyyyMMdd");

            NinjaTrader.Code.Output.Process("Today's Date: " + todaysDate, PrintTo.OutputTab1);
            
            NinjaTrader.Code.Output.Process("Time: " + ToTime(Time[0]).ToString(), PrintTo.OutputTab1);

            if (BarsInProgress != 0)
                return;

            if (Bars.BarsType.IsIntraday)
            {
                DateTime currentTime = Times[0][0];
                if (currentTime.Hour >= 1 && currentTime.Hour < 4)
                {
                    if (CurrentDayOHL().CurrentHigh[0] > seriesOneHigh)
                        seriesOneHigh = CurrentDayOHL().CurrentHigh[0];
                }
            }
            NinjaTrader.Code.Output.Process("londonHigh: " + seriesOneHigh.ToString(), PrintTo.OutputTab1);

        }

        /// <summary>
        /// London Trading Session (1AM - 9AM CST)
        /// </summary>
        public void CheckLondon()
        {
            if (BarsInProgress != 0)
                return;

            // Set the start / end datetime 
            //startDateTime = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, 1, 0, 0);
            //endDateTime = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, 7, 0, 0);
            startDateTime = new DateTime(2024, 2, 14, 1, 0, 0);
            endDateTime = new DateTime(2024, 2, 14, 7, 0, 0);

            // Get the bars from the start/end datetime
            int startBarsAgo = Bars.GetBar(startDateTime);
            int endBarsAgo = Bars.GetBar(endDateTime);

            if (CurrentBar > endBarsAgo)
            {
                //Get the MAX/MIN using the barsago
                double highestHigh = MAX(High, endBarsAgo - startBarsAgo + 1)[CurrentBar - endBarsAgo];
                double lowestLow = MIN(Low, endBarsAgo - startBarsAgo + 1)[CurrentBar - endBarsAgo];
                londonHigh = highestHigh;
                londonLow = lowestLow;
            }
        }

        public void CheckMerica()
        {
            if (BarsInProgress != 0)
                return;

            // Set the start / end datetime 
            //startDateTime = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, 1, 0, 0);
            //endDateTime = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, 7, 0, 0);
            startDateTime = new DateTime(2024, 2, 14, 7, 0, 0);
            endDateTime = new DateTime(2024, 2, 14, 16, 0, 0);

            // Get the bars from the start/end datetime
            int startBarsAgo = Bars.GetBar(startDateTime);
            int endBarsAgo = Bars.GetBar(endDateTime);

            if (CurrentBar > endBarsAgo)
            {
                //Get the MAX/MIN using the barsago
                double highestHigh = MAX(High, endBarsAgo - startBarsAgo + 1)[CurrentBar - endBarsAgo];
                double lowestLow = MIN(Low, endBarsAgo - startBarsAgo + 1)[CurrentBar - endBarsAgo];
                mericaHigh = highestHigh;
                mericaLow = lowestLow;

            }
        }

        /// <summary>
        /// See what the volume is on multiple timeframes
        /// ...to be used to know when to enter
        /// </summary>
        public void GetVolume()
        {
            double currentAskVolume = Volumes[0][0];
            volume = currentAskVolume;
            NinjaTrader.Code.Output.Process("currentAskVolume: " + currentAskVolume.ToString(), PrintTo.OutputTab1);
        }

        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> HighestHigh
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LowestLow
        {
            get { return Values[1]; }
        }
        #endregion
    }
}
