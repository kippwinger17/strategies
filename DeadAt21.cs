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
	public class DeadAt21 : Strategy
	{   
        private double entry_price;

        bool crossTwentyOnesUp = false;
        bool crossTwentyOnesDown= false;
        bool TotemaCrossUp = false;
        bool TotemaCrossDown = false;
        bool smaCrossUp = false;
        bool smaCrossDown = false;
        bool temaCrossUp = false;
        bool temaCrossDown = false;
        bool sixnineCrossUp = false;
        bool sixnineCrossDown = false;

        // Trailing 
        private double CurrentTriggerPrice;
        private double CurrentStopPrice;
        private bool FlipDirection;
        private int ProgressState;
        private Series<double> MathSeries;

        // Support/Resistance
        double supportVal;
        double resistanceVal;

        // HighestHighs/LowestLows
        double highestHigh;
        double LowestLow;


        protected override void OnStateChange()
		{

			if (State == State.SetDefaults)
			{
				Description									= @"Looks for cross of the 21 TEMA and 21 SMA on 1m";
				Name										= "DeadAt21";
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

                // Default Times
                StartTime = 50000;
                EndTime = 150000;
                AllowMarketOpenEntries = false;

                // Set Trialing 
                //SetTrailStop(CalculationMode.Ticks, 4);
                TrailFrequency = 3;
                TrailStopDistance = -8;
                CurrentTriggerPrice = 0;
                CurrentStopPrice = 0;

            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                MathSeries = new Series<double>(this);
            }

        }

		protected override void OnBarUpdate()
		{
            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < 1)
                return;

            CheckLevels();

            // Check for Crosses
            OneMinuteCrosses();
            
            // Enter Longs/Shorts
            PerformEntries();
            
            // Check for Exits
            CheckForExits();

            //doTheVwap();
        }

        public void doTheVwap()
        {
            THE_VWAP_INTRADAY vw = THE_VWAP_INTRADAY(1, 2, 0, 0);
            NinjaTrader.Code.Output.Process("VWAP: " + vw.ToString(), PrintTo.OutputTab2);

        }
        public void CheckLevels()
        {
            // Support/Resistance
            DynamicSRLines d = DynamicSRLines(5, 200, 10, 2, 3, false, Brushes.SteelBlue, Brushes.IndianRed);
            NinjaTrader.Code.Output.Process("CurrentBar: " + CurrentBar.ToString(), PrintTo.OutputTab2);
            if (CurrentBar <= 5323)
                return;

            supportVal = d.SupportPlot[0];
            resistanceVal = d.ResistancePlot[0];

            NinjaTrader.Code.Output.Process("Resistance: " + d.ResistancePlot[0].ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("Support: " + d.SupportPlot[0].ToString(), PrintTo.OutputTab2);


            // Highest Highs / Lowest Lows
            SupportTheResistance s = SupportTheResistance(1, 0, 5, 0);
            NinjaTrader.Code.Output.Process("CurrentBar: " + CurrentBar.ToString(), PrintTo.OutputTab2);
            if (CurrentBar <= 5323)
                return;

            LowestLow = s.SupportPlot[0];
            highestHigh = s.ResistancePlot[0];

            NinjaTrader.Code.Output.Process("Highest: " + s.ResistancePlot[0].ToString(), PrintTo.OutputTab2);
            NinjaTrader.Code.Output.Process("Lowest: " + s.SupportPlot[0].ToString(), PrintTo.OutputTab2);
        }

        

        /// <summary>
        /// Set flags based on crosses
        /// </summary>
        public bool OneMinuteCrosses()
        {
            bool cross_occurred = false;
            //TotemaCrossDown = false;
            //TotemaCrossUp = false;
            smaCrossUp = false;
            smaCrossDown = false;
            temaCrossUp = false;
            temaCrossDown = false;

            // 6 EMA Crossing Above 9 EMA
            if (CrossAbove(EMA(6), EMA(9), 1))
            {
                NinjaTrader.Code.Output.Process("6 Cross UP", PrintTo.OutputTab2);
                //Draw.Text(this, "emaUpText" + Convert.ToString(CurrentBars[0]), "6EMA", 0, Low[0] - 7 * TickSize, Brushes.White);
                //Draw.ArrowUp(this, "emaUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 3 * TickSize, Brushes.Gold);
                sixnineCrossUp = true;
                sixnineCrossDown = false;
            }

            // 6 EMA Crossing Below 9 EMA
            if (CrossBelow(EMA(6), EMA(9), 1))
            {
                NinjaTrader.Code.Output.Process("6 Cross DOWN", PrintTo.OutputTab2);
                //Draw.Text(this, "emaDownText" + Convert.ToString(CurrentBars[0]), "EMA", 0, Low[0] + 7 * TickSize, Brushes.White);
                //Draw.ArrowDown(this, "emaDown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, Brushes.Gold);
                sixnineCrossDown = true;
                sixnineCrossUp = false;
            }

            // TEMA Crossing Above SMA
            if (CrossAbove(TEMA(21), SMA(21), 1))
            {
                TotemaCrossUp = true;
                //NinjaTrader.Code.Output.Process("21TEMA/SMA UP", PrintTo.OutputTab2);
                //Draw.Text(this, "21temaUpText" + Convert.ToString(CurrentBars[0]), "21TEMA/SMA", 0, Low[0] - 15 * TickSize, Brushes.White);
                //Draw.Diamond(this, "21temaUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 12 * TickSize, Brushes.DarkSlateBlue);
                TotemaCrossDown = false;

                cross_occurred = true;
            }

            // TEMA Crossing Below SMA
            if (CrossBelow(TEMA(21), SMA(21), 1))
            {
                TotemaCrossDown = true;

                //NinjaTrader.Code.Output.Process("21TEMA/SMA DOWN", PrintTo.OutputTab2);
                //Draw.Text(this, "21temaDownText" + Convert.ToString(CurrentBars[0]), "21TEMA/SMA", 0, Low[0] + 7 * TickSize, Brushes.White);
                //Draw.Diamond(this, "21temaDown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, Brushes.DarkSlateBlue);
                TotemaCrossUp = false;

                cross_occurred = true;
            }

            // ZLSMA Crossing Above SMA
            if (CrossAbove(ZLSMA(13), SMA(21), 1))
            {
                smaCrossUp = true;
                //NinjaTrader.Code.Output.Process("ZLSMA/SMA UP", PrintTo.OutputTab2);
                //Draw.Text(this, "zlsmaUpText" + Convert.ToString(CurrentBars[0]), "ZLSMA/SMA", 0, Low[0] - 7 * TickSize, Brushes.White);
                //Draw.ArrowUp(this, "zlsmaUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 3 * TickSize, Brushes.SteelBlue);
                smaCrossDown = false;

                cross_occurred = true;
            }

            // ZLSMA Crossing Below SMA
            if (CrossBelow(ZLSMA(13), SMA(21), 1))
            {
                smaCrossDown = true;
                //NinjaTrader.Code.Output.Process("ZLSMA/SMA DOWN", PrintTo.OutputTab2);
                //Draw.Text(this, "zlsmaDownText" + Convert.ToString(CurrentBars[0]), "ZLSMA/SMA", 0, Low[0] + 7 * TickSize, Brushes.White);
                //Draw.ArrowDown(this, "zlsmaDown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, Brushes.SteelBlue);
                smaCrossUp = false;

                cross_occurred = true;
            }

            // ---------------------------

            return cross_occurred;

        }
        public void PerformEntries()
        {
            // Reset stop price 
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                CurrentStopPrice = 0;
            }

            if (Position.MarketPosition == MarketPosition.Long || Position.MarketPosition == MarketPosition.Short)
                return;



            // Check Time 
            if (!(ToTime(Time[0]) >= StartTime && ToTime(Time[0]) <= EndTime))
                return;
            // Check if Market Open entries are allowed
            if (!AllowMarketOpenEntries
                && (ToTime(Time[0]) >= 82500 && ToTime(Time[0]) <= 90000))
                return;
            
            /* 
             * 6/9 looks good & TEMA/SMA cross
             */
            if (CrossAbove(TEMA(21), SMA(21), 1)
                && sixnineCrossUp
                && Close[0] > EMA(100)[0]
                && Close[0] > highestHigh)
            {
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Current Time: " + Times[0][0].ToString(), PrintTo.OutputTab2);
                EnterLong(Convert.ToInt32(DefaultQuantity), @"longEntry");
                // Trail vars
                CurrentTriggerPrice = (Close[0] + (TrailFrequency * TickSize));
                CurrentStopPrice = (Close[0] + (TrailStopDistance * TickSize));

                entry_price = GetCurrentBid();
                NinjaTrader.Code.Output.Process("!!Entered Long: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
            }
            
            if (CrossBelow(TEMA(21), SMA(21), 1)
                && sixnineCrossDown
                && Close[0] < EMA(100)[0])
            {
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Current Time: " + Times[0][0].ToString(), PrintTo.OutputTab2);
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                entry_price = GetCurrentBid();
                NinjaTrader.Code.Output.Process("!!Entered Short: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
            }

            /*
             * 6/9 looks good, TEMA/SMA looks good; Just crossed 100 EMA
             */
            /*if (Position.MarketPosition == MarketPosition.Flat
               && TotemaCrossUp
               && sixnineCrossUp
               && Close[0] > EMA(100)[0])
            {
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Current Time: " + Times[0][0].ToString(), PrintTo.OutputTab2);
                EnterLong(Convert.ToInt32(DefaultQuantity), "");
                entry_price = GetCurrentBid();
                NinjaTrader.Code.Output.Process("!!Entered Long: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
            }
            */
            /*
            if (TotemaCrossDown
                && sixnineCrossDown
                && Close[0] < EMA(100)[0])
            {
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("Current Time: " + Times[0][0].ToString(), PrintTo.OutputTab2);
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                entry_price = GetCurrentBid();
                NinjaTrader.Code.Output.Process("!!Entered Short: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
                NinjaTrader.Code.Output.Process("-------------------------------------", PrintTo.OutputTab2);
            }
            */


        }

        public void CheckForExits()
        {
            // Check if Support/Resistance has been hit
            DynamicSRLines d = DynamicSRLines(5, 200, 10, 2, 3, false, Brushes.SteelBlue, Brushes.IndianRed);
            if (Position.MarketPosition == MarketPosition.Long)
            {
                ExitLong();
                NinjaTrader.Code.Output.Process("!!EXIT Long: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
            }

            if (Position.MarketPosition == MarketPosition.Short
                && Close[0] < d.SupportPlot[0])
            {
                ExitShort();
                NinjaTrader.Code.Output.Process("!!EXIT Short: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
            }

            // Trail has been hit
            if (CurrentStopPrice != 0)
            {
                ExitLongStopMarket(Convert.ToInt32(DefaultQuantity), CurrentStopPrice, @"", "");
            }

            // Has the cross gone against you?
            if (Position.MarketPosition == MarketPosition.Long
            && ((CrossBelow(TEMA(21), SMA(21), 1)))
            )
            {
                ExitLong();
                NinjaTrader.Code.Output.Process("!!EXIT Long: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
            }

            /*if (Position.MarketPosition == MarketPosition.Short
            && ((CrossAbove(TEMA(21), SMA(21), 1)))
            )
            {
                ExitShort();
                NinjaTrader.Code.Output.Process("!!EXIT Short: " + Convert.ToString(entry_price), PrintTo.OutputTab2);
            }*/

            // Have you hit profit target?
            /*if (Position.MarketPosition == MarketPosition.Long
                && (Close[0] - entry_price) >= 2
                   || (High[0] - entry_price) >= 2
                )
            {
                ExitLong();
            }
            */

            if (Position.MarketPosition == MarketPosition.Short
               && ((entry_price - Close[0]) >= .5
                  || (entry_price - Low[0]) >= .5)
               )
            {
                ExitShort();
            }
            /*
            // Has PA gone against your long?
            /*if (Position.MarketPosition == MarketPosition.Long
                && (entry_price - Close[0]) >= -100r5
                  // || (entry_price - Low[0]) >= -100
                )
            {
                ExitLong();
            }
            */
            // Has the long gone over the 100 on a short?

            /*if (Position.MarketPosition == MarketPosition.Short
                && Close[0] > EMA(100)[0])
                ExitShort();*/


            // Make trail adjustments
            if ((Position.MarketPosition == MarketPosition.Long)
                 && (Close[0] > CurrentTriggerPrice))
            {
                CurrentTriggerPrice = (Close[0] + (TrailFrequency * TickSize));
                CurrentStopPrice = (Close[0] + (TrailStopDistance * TickSize));
            }
        }

        public bool IsDoubleTop()
        {
            // Check if there are at least 3 bars in the chart
            if (CurrentBar < 2)
                return false;

            return (((Math.Max(Open[2], Close[2])) == (Math.Max(Open[1], Close[1]))) && ((Math.Max(Open[1], Close[1])) > (Math.Max(Open[0], Close[0]))));
        }

        public bool IsDoubleBottom()
        {
            // Check if there are at least 3 bars in the chart
            if (CurrentBar < 2)
                return false;

            return (((Math.Min(Open[2], Close[2])) == (Math.Min(Open[1], Close[1]))) && ((Math.Min(Open[1], Close[1])) < (Math.Min(Open[0], Close[0]))));
        }

        #region Properties
        [Range(0, 230000)]
        [NinjaScriptProperty]
        [Display(Name = "Start time", Description = "Enter start time, Military time format (50000, 73000)", Order = 1, GroupName = "Parameters")]
        public int StartTime
        { get; set; }

        [Range(0, 230000)]
        [NinjaScriptProperty]
        [Display(Name = "End Time", Description = "Enter start time, Military time format (50000, 73000)", Order = 2, GroupName = "Parameters")]
        public int EndTime
        { get; set; }

        /*
         * Allow entries at market open?
        */
        [NinjaScriptProperty]
        [Display(Name = "Allow entries at market open", Order = 3, GroupName = "Parameters")]
        public bool AllowMarketOpenEntries
        { get; set; }

        /*
         * Trail parameters
         */
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "TrailFrequency", Description = "This will be how often trail action triggers.", Order = 4, GroupName = "Parameters")]
        public int TrailFrequency
        { get; set; }

        [NinjaScriptProperty]
        [Range(-9999, int.MaxValue)]
        [Display(Name = "TrailStopDistance", Description = "Distance stop for exit order will be placed. This needs to be less than the TrailLimitDistance to exit a long position.", Order = 5, GroupName = "Parameters")]
        public int TrailStopDistance
        { get; set; }
        #endregion
    }

}
