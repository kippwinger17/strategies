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
	public class SixtyNiner : Strategy
	{
        bool crossUp = false;
        bool crossDown = false;
        bool overNine = false;
        bool overSix = false;
        bool underSix = false;
        bool underNine = false;
        public int TickOffsetLong;
        public int TickOffsetShort;
        private double MyStopLow;
        private DateTime entryTime;
        private double entry_price;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"v1.0";
				Name										= "SixtyNiner";
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


                TickOffsetLong = -20;
                TickOffsetShort = 20;
                MyStopLow = 1;
            }
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
            if (BarsInProgress != 0)
                return;

            bool marketOpen = ToTime(Time[0]) >= 083500 && ToTime(Time[0]) <= 140000;

            if (marketOpen)
            {
                WeakCross();

                ThirtyCrossUpAfterSixtyNine();

                ThirtyCrossDownAfterSixtyNine();

                ExitLongOnPoints();

                ExitOnCross();
            }
        }

        private void WeakCross()
        {
            if (CrossAbove(EMA(6), EMA(9), 1))
            {
                //Draw.ArrowUp(this, "WeakCrossUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 3 * TickSize, WeakBrush);
                crossUp = true;

                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    EnterLong(Convert.ToInt32(DefaultQuantity), "");
                    MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                    entry_price = GetCurrentBid();
                    string message = ("\n///  6 EMA & 9 EMA Cross \n" +
                        "///\n");
                    entryTime = Times[0][0];
                    message += "// Entry Time: " + entryTime.ToString() + "\n";
                    message += "// Entry Price: " + entry_price.ToString() + "\n";
                    //Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);

                    overNine = false;
                    overSix = false;
                    crossDown = false;
                    crossUp = false;
                }
            }

            if (CrossAbove(EMA(9), EMA(6), 1))
            {
                //Draw.ArrowDown(this, "WeakCrossDown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, WeakBrush);
                crossDown = true;
            }
        }

        private void CrossRespectingThirty()
        {
            if (CrossAbove(EMA(6), EMA(9), 1))
            {
                if (EMA(6)[0] > EMA(30)[0])
                {
                    if (Position.MarketPosition == MarketPosition.Flat)
                    {
                        EnterLong(Convert.ToInt32(DefaultQuantity), "");
                        MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                        entry_price = GetCurrentBid();
                        string message = ("\n///  6 EMA & 9 EMA Cross and above 30 \n" +
                            "///\n");
                        entryTime = Times[0][0];
                        message += "// Entry Time: " + entryTime.ToString() + "\n";
                        message += "// Entry Price: " + entry_price.ToString() + "\n";
                        //Print(message);
                        NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);

                        overNine = false;
                        overSix = false;
                        crossDown = false;
                    }
                }
                    //Draw.ArrowUp(this, "CrossUp" + Convert.ToString(CurrentBars[0]), true, 0, Low[0] - 3 * TickSize, TBBrush);
            }
            if (CrossAbove(EMA(9), EMA(6), 1))
            {
                if (EMA(6)[0] < EMA(30)[0])
                    Draw.ArrowDown(this, "CrossDown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 3 * TickSize, Brushes.DeepSkyBlue);
            }

        }


        private void ThirtyCrossUpAfterSixtyNine()
        {
            double ema30 = EMA(30)[0];
            double ema9 = EMA(9)[0];
            double ema6 = EMA(6)[0];
            //check to see when the 6 crosses above the 30 and then when the 9 crosses above the 30

            // 6 & 9 cross happened; looking for 6 crossing 30
            if (crossUp && (CrossAbove(EMA(6), EMA(30), 1)))
            {
                overSix = true;
            }


            // 6 & 9 cross happened; looking for 9 crossing 30
            if (crossUp && (CrossAbove(EMA(9), EMA(30), 1)))
            {
                overNine = true;
            }


             if (overSix && overNine)
            {
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    EnterLong(Convert.ToInt32(DefaultQuantity), "");
                    MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                    entry_price = GetCurrentBid();
                    string message = ("\n///  6 EMA & 9 EMA Cross / Above 30 EMA \n" +
                        "///\n");
                    entryTime = Times[0][0];
                    message += "// Entry Time: " + entryTime.ToString() + "\n";
                    message += "// Entry Price: " + entry_price.ToString() + "\n";
                    //Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);

                    overNine = false;
                    overSix = false;
                    crossDown = false;
                    crossUp = false;
                }
            }

        }

        private void ThirtyCrossDownAfterSixtyNine()
        {
            double ema30 = EMA(30)[0];
            double ema9 = EMA(9)[0];
            double ema6 = EMA(6)[0];
            //check to see when the 6 crosses above the 30 and then when the 9 crosses above the 30

            // 6 & 9 cross happened; looking for 6 crossing 30
            if (crossDown && (CrossBelow(EMA(6), EMA(30), 1)))
            {
                underSix = true;
            }


            // 6 & 9 cross happened; looking for 9 crossing 30
            if (crossDown && (CrossBelow(EMA(9), EMA(30), 1)))
            {
                underNine = true;
            }


            if (underNine && underSix)
            {
               // Draw.ArrowDown(this, "30AfterCrossDown" + Convert.ToString(CurrentBars[0]), true, 0, High[0] + 2 * TickSize, StrongBrush);
                underNine = false;
                underSix = false;
            }

        }

        private void ExitLongOnPoints()
        {
            if (Position.MarketPosition == MarketPosition.Long)
            {
                double diff;
                diff = entry_price - Close[0];
                if (diff <= 5)//-5
                {
                    ExitLong();
                    string message = ("\n///  Exit When -5 Points ///\n");
                    entryTime = Times[0][0];
                    message += entryTime.ToString() + "\n";
                    Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
            }
        }

        private void ExitOnCross()
        {
            if (Position.MarketPosition == MarketPosition.Long
                && crossDown)
            {
                ExitLong();
                string message = ("\n///  Exit When Opposite Cross Occurs ///\n");
                entryTime = Times[0][0];
                message += entryTime.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }

        }
    }
}
