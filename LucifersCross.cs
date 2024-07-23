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
	
	public class TradeParameters
    {
        public bool MarketOpen { get; set; }
        public double CurrentPrice { get; set; }
        public double PreviousPrice { get; set; }
        public double TopBollinger { get; set; }
        public double LowerBollinger { get; set; }
        public double PreviousTopbollinger { get; set; }
        public double PreviousLowerbollinger { get; set; }
        public double UpperWick { get; set; }
        public double LowerWick { get; set; }
        public double PreviousUpperWick { get; set; }
        public double PreviousLowerWick { get; set; }
        public double HundredEMA { get; set; }
        public double TwoHundredEMA { get; set; }
        public double EmaSix { get; set; }
        public double SmaThirteen { get; set; }
        public double ProfitTargetLong { get; set; }
        public double ProfitTargetShort { get; set; }
        public double PrevRsi { get; set; }
        public string MPos { get; set; }
        public double SmaSix { get; set; }
        public double Rsi {get; set; }
        public double RsiEma { get; set; }
        public double EmaNine { get; set; }


        // Constructor for required parameters
        public TradeParameters(bool marketOpen, double currentPrice, double previousPrice, double topBollinger, double lowerBollinger, double previousTopbollinger,
            double previousLowerbollinger, double upperWick, double lowerWick, double previousUpperWick, double previousLowerWick, double hundredEMA, double twoHundredEMA,
            double emaSix, double smaThirteen, double profitTargetLong, double profitTargetShort, double prevRsi, string mPos, double smaSix, double rsi, double rsiEma, double emaNine)
        {
            MarketOpen = marketOpen;
            CurrentPrice = currentPrice;
            PreviousPrice = previousPrice;
            TopBollinger = topBollinger;
            LowerBollinger = lowerBollinger;
            PreviousTopbollinger = previousTopbollinger;
            PreviousLowerbollinger = previousLowerbollinger;
            UpperWick = upperWick;
            LowerWick = lowerWick;
            PreviousUpperWick = previousUpperWick;
            PreviousLowerWick = previousLowerWick;
            HundredEMA = hundredEMA;
            TwoHundredEMA = twoHundredEMA;
            EmaSix = emaSix;
            SmaThirteen = smaThirteen;
            ProfitTargetLong = profitTargetLong;
            ProfitTargetShort = profitTargetShort;
            PrevRsi = prevRsi;
            MPos = mPos;
            SmaSix = smaSix;
            Rsi = rsi;
            RsiEma = rsiEma;
            EmaNine = emaNine;
        }
	}
	
	public class LucifersCross : Strategy
	{
		// Globals 
		private SMA sma;
        private StdDev stdDev;
        private Bollinger bollinger;
        private RSI rsi14;
        private EMA rsiEMA;
        private SMA sma2;
        private int previousBar = -1;
        private Series<double> primarySeries;
        private Series<double> secondarySeries;
        bool upperBollingerHit = false;
        bool rsiLookingGood = false;
        private DateTime entryTime;
        private double entry_price;
        private double trailAmount = 10; // You can adjust the trail amount as needed
        private double trailingStopPrice = 0;
        bool inACross = false;
        bool sixAndNine = false;
        private double CurrentTriggerPrice;
        private double CurrentStopPrice;
        public int TrailFrequency;
        public int TrailStopDistance;
        
		// Define the threshold for the upper wick that triggers the exit
        private double upperWickThreshold = 0.1; // Adjust upper wick threshold as needed
        private double lowerWickThreshold = 0.1; // Adjust lower wick threshold as needed

        private double MyStopLow;

        // SR Level Variables
        public int i = 0, j = 0, x = 0;
        int LastBar = 0;
        int pxAbove = 0, countAbove = 0, maxCountAbove = 0;
        int pxBelow = 0, countBelow = 0, maxCountBelow = 0;
        double p = 0, p1 = 0, level = 0;
        string str = "";
        public struct PRICE_SWING
        {
            public int Type;
            public int Bar;
            public double Price;
        }
        PRICE_SWING listEntry;
        List<PRICE_SWING> pivot = new List<PRICE_SWING>();
        NinjaTrader.Gui.Tools.SimpleFont boldFont = new NinjaTrader.Gui.Tools.SimpleFont("Arial", 10) { Size = 25, Bold = true };
        NinjaTrader.Gui.Tools.SimpleFont textFont = new NinjaTrader.Gui.Tools.SimpleFont("Times", 8) { Size = 25, Bold = false };

        public int MaxLookBackBars = 200;
        public int PivotStrength = 5;
        public int PivotTickDiff = 10;
        public int ZoneTickSize = 2;
        public int MaxLevels = 3;
        public bool ShowPivots = true;
        public Brush ColorBelow = Brushes.DeepSkyBlue;
        public Brush ColorAbove = Brushes.Magenta;
        double upperLevel;
        double lowerLevel;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name										= "LucifersCross";
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
				
				EMA6 = 6;
                SMA13 = 13;
                EMA100 = 100;
				TrailFrequency = 5;
                TrailStopDistance = -5;
                CurrentTriggerPrice = 0;
                CurrentStopPrice = 0;
                WarningsOnly = false;
                LongEntries = true;
                ShortEntries = true;

                TickOffsetLong = -20;
                TickOffsetShort = 20;
                MyStopLow = 1;

            }
			else if (State == State.Configure)
			{
                AddDataSeries(BarsPeriodType.Tick, 1000);

                // Bollinger crap
                bollinger = Bollinger(2, 14);

                // RSI indicator
                rsi14 = RSI(14, 1);

                // EMA for RSI
                rsiEMA = EMA(RSI(14, 1), 6);

                // Trailing Stop
                
            }
		}

		protected override void OnBarUpdate()
		{
            double supportLevel = 0.0;
            double resistanceLevel = 0.0;

            bool marketOpen = ToTime(Time[0]) >= 083500 && ToTime(Time[0]) <= 140000;
            //bool marketOpen = true;

            //NinjaTrader.Code.Output.Process("CurrentBar: " + CurrentBar.ToString(), PrintTo.OutputTab1);

            if (CurrentBar < 256)
                return;
            // Start Strategy
            if (BarsInProgress != 0)
                return;

            supportResistanceUpdate();

            double low = Low[LowestBar(Low, 256)];
            double high = High[HighestBar(High, 256)];

            double mlow = Low[LowestBar(Low, 88)];
            double mhigh = High[HighestBar(High, 88)];

            // Support and Resistance levels
            supportLevel = low;
            resistanceLevel = high;

            /*NinjaTrader.Code.Output.Process("S1: " + supportLevel.ToString(), PrintTo.OutputTab1);
            NinjaTrader.Code.Output.Process("R1: " + resistanceLevel.ToString(), PrintTo.OutputTab1);

            NinjaTrader.Code.Output.Process("S2: " + mlow.ToString(), PrintTo.OutputTab1);
            NinjaTrader.Code.Output.Process("R2: " + mhigh.ToString(), PrintTo.OutputTab1);*/

            priceEvaluation(marketOpen);
		}
        
        public void supportResistanceUpdate()
        {
            if (CurrentBar <= MaxLookBackBars)
                return;

            if (LastBar != CurrentBar)
            {
                x = HighestBar(High, (PivotStrength * 2) + 1);
                if (x == PivotStrength)
                {
                    listEntry.Type = +1;
                    listEntry.Price = High[x];
                    listEntry.Bar = CurrentBar - x;
                    pivot.Add(listEntry);

                    if (pivot.Count > MaxLookBackBars)
                    {
                        pivot.RemoveAt(0);
                    }
                }
                x = LowestBar(Low, (PivotStrength * 2) + 1);
                if (x == PivotStrength)
                {
                    listEntry.Type = -1;
                    listEntry.Price = Low[x];
                    listEntry.Bar = CurrentBar - x;
                    pivot.Add(listEntry);

                    if (pivot.Count > MaxLookBackBars)
                    {
                        pivot.RemoveAt(0);
                    }
                }
                
                if (CurrentBar >= Bars.Count - 2 && CurrentBar > MaxLookBackBars)
                {
                    p = Close[0]; p1 = 0; str = "";
                    
                    for (level = 1; level <= MaxLevels; level++)
                    {
                        p = get_sr_level(p, +1);
                        var _resZone = p - ((ZoneTickSize * TickSize) / 2);
                        var idk = p + ((ZoneTickSize * TickSize) / 2);
                        Draw.Rectangle(this, "resZone" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p + ((ZoneTickSize * TickSize) / 2), ColorAbove, ColorAbove, 30);
                        Draw.Line(this, "resLine" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p - ((ZoneTickSize * TickSize) / 2), ColorAbove, DashStyleHelper.Solid, 1);
                        str = (p == p1) ? str + " L" + level : "L" + level;
                        Draw.Text(this, "resTag" + level, false, "\t    " + str, -5, p, 0, ColorAbove, boldFont, TextAlignment.Right, Brushes.Transparent, Brushes.Transparent, 0);
                        p1 = p; p += (PivotTickDiff * TickSize);
                        upperLevel = idk;
                        if (level == 1)
                            NinjaTrader.Code.Output.Process("Res Level 1: " + upperLevel.ToString(), PrintTo.OutputTab1);
                        if (level == 2)
                            NinjaTrader.Code.Output.Process("Res Level 2: " + upperLevel.ToString(), PrintTo.OutputTab1);
                        if (level == 3)
                            NinjaTrader.Code.Output.Process("Res Level 3: " + upperLevel.ToString(), PrintTo.OutputTab1);

                    }
                    
                    p = Close[0]; p1 = 0; str = "";
                    for (level = 1; level <= MaxLevels; level++)
                    {
                        p = get_sr_level(p, -1);
                        var lrec = p - ((ZoneTickSize * TickSize) / 2);
                        var urec = p + ((ZoneTickSize * TickSize) / 2);
                        Draw.Rectangle(this, "supZone" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p + ((ZoneTickSize * TickSize) / 2), ColorBelow, ColorBelow, 30);
                        Draw.Line(this, "supLine" + level, false, 0, p - ((ZoneTickSize * TickSize) / 2), MaxLookBackBars, p - ((ZoneTickSize * TickSize) / 2), ColorBelow, DashStyleHelper.Solid, 1);
                        str = (p == p1) ? str + " L" + level : "L" + level;
                        Draw.Text(this, "supTag" + level, false, "\t    " + str, -5, p, 0, ColorBelow, boldFont, TextAlignment.Right, Brushes.Transparent, Brushes.Transparent, 0);
                        p1 = p; p -= (PivotTickDiff * TickSize);
                        //lowerLevel = p - ((ZoneTickSize * TickSize) / 2);
                        lowerLevel = urec;
                        if (level == 1)
                            NinjaTrader.Code.Output.Process("Supp Level 1: " + lowerLevel.ToString(), PrintTo.OutputTab1);
                        if (level == 2)
                            NinjaTrader.Code.Output.Process("Supp Level 2: " + lowerLevel.ToString(), PrintTo.OutputTab1);
                        if (level == 3)
                            NinjaTrader.Code.Output.Process("Supp Level 3: " + lowerLevel.ToString(), PrintTo.OutputTab1);
                        //NinjaTrader.Code.Output.Process("Support1: " + lowerLevel.ToString(), PrintTo.OutputTab1);
                        //NinjaTrader.Code.Output.Process("suppP: " + p.ToString(), PrintTo.OutputTab1);
                    }
                }
            }

            LastBar = CurrentBar;
            var test = "";
        }

        double get_sr_level(double refPrice, int pos)
        {
            int i = 0, j = 0;
            double levelPrice = 0;

            if (CurrentBar <= MaxLookBackBars)
                return 0.0;

            if (CurrentBar >= Bars.Count - 2 && CurrentBar > MaxLookBackBars)
            {
                maxCountAbove = 0; maxCountBelow = 0; pxAbove = -1; pxBelow = -1;
                for (i = pivot.Count - 1; i >= 0; i--)
                {
                    countAbove = 0; countBelow = 0;
                    if (pivot[i].Bar < CurrentBar - MaxLookBackBars) break;
                    for (j = 0; j < pivot.Count - 1; j++)
                    {
                        if (pivot[j].Bar > CurrentBar - MaxLookBackBars)
                        {
                            if (pos > 0 && pivot[i].Price >= refPrice)
                            {
                                if (Math.Abs(pivot[i].Price - pivot[j].Price) / TickSize <= PivotTickDiff)
                                {
                                    countAbove++;
                                }
                                if (countAbove > maxCountAbove)
                                {
                                    maxCountAbove = countAbove;
                                    levelPrice = pivot[i].Price;
                                    pxAbove = i;
                                }
                            }
                            else
                                if (pos < 0 && pivot[i].Price <= refPrice)
                            {
                                if (Math.Abs(pivot[i].Price - pivot[j].Price) / TickSize <= PivotTickDiff)
                                {
                                    countBelow++;
                                }
                                if (countBelow > maxCountBelow)
                                {
                                    maxCountBelow = countBelow;
                                    levelPrice = pivot[i].Price;
                                    pxBelow = i;
                                }
                            }
                        }
                    }
                }

                if (pos > 0)
                {
                    levelPrice = (pxAbove >= 0) ? pivot[pxAbove].Price : High[HighestBar(High, MaxLookBackBars)];
                }
                if (pos < 0)
                {
                    levelPrice = (pxBelow >= 0) ? pivot[pxBelow].Price : Low[LowestBar(Low, MaxLookBackBars)];
                }

            }
            return Instrument.MasterInstrument.RoundToTickSize(levelPrice);

        }

        public void priceEvaluation(bool marketOpen)
        {
            secondarySeries = new Series<double>(EMA(BarsArray[1], 100));


            // Pull chart variables
            double currentPrice = Close[0];
            double previousPrice = Close[1];
            double topBollinger = bollinger.Upper[0];
            double lowerBollinger = bollinger.Lower[0];
            double previousTopbollinger = bollinger.Upper[1];
            double previousLowerbollinger = bollinger.Lower[1];
            double upperWick = High[0];
            double lowerWick = Low[0];
            double previousUpperWick = High[1];
            double previousLowerWick = Low[1];
            double hundredEMA = EMA(100)[0];
            //double hundredEMAOnFifteen = EMA(100)[1];
            double twoHundredEMA = EMA(200)[0];
            double emaSix = EMA(6)[0];
            double emaNine = EMA(9)[0];
            double smaThirteen = SMA(13)[0];
            double profitTargetLong = Close[0] + 5 * TickSize;
            double profitTargetShort = Close[0] - 10 * TickSize;
            double rsi = rsi14[0];
            double rsiEma = rsiEMA[0];
            double prevRsi = rsi14[1];
            string mPos = Position.MarketPosition.ToString();
            double smaSix = SMA(6)[0];

            entryTime = Times[0][0];

            TradeParameters tradeParameters = new TradeParameters(marketOpen, currentPrice, previousPrice, topBollinger, lowerBollinger, previousTopbollinger,
            previousLowerbollinger, upperWick, lowerWick, previousUpperWick, previousLowerWick, hundredEMA, twoHundredEMA,
            emaSix, smaThirteen, profitTargetLong, profitTargetShort, prevRsi, mPos, smaSix, rsi, rsiEma, emaNine);
            
            if (marketOpen)
            {
                double profit = (Close[0] - Position.AveragePrice) / TickSize;
                double highProfit = (High[0] - Position.AveragePrice) / TickSize;
                double lowProfit = (Low[0] - Position.AveragePrice) / TickSize;

                // Where are we with RSI?
                if ((rsi14[0] > rsiEMA[0])
                    && rsi14[0] > rsi14[1]
                    )
                    rsiLookingGood = true;
                else
                    rsiLookingGood = false;

                
                /********************* 
                * Long Entries TODO: Set TP @ 5 pts
                ********************/
                if (LongEntries)
                    enterLongs(marketOpen, tradeParameters);

                /********************* 
                * Short Entries TODO: Set TP @ 10
                ********************/
                if (ShortEntries)
                    enterShorts(marketOpen, tradeParameters);

                /*********
                 * Trailing crap
                 *********/
                // set low with new value
                if ((Position.MarketPosition == MarketPosition.Long)
                     && ((Low[1] + (TickOffsetLong * TickSize)) >= MyStopLow))
                {
                    MyStopLow = (Low[1] + (TickOffsetLong * TickSize));
                }

                //NinjaTrader.Code.Output.Process("TS MyStopLow: " + MyStopLow.ToString(), PrintTo.OutputTab2);
                //NinjaTrader.Code.Output.Process("TS Lows: " + Lows[0][0].ToString(), PrintTo.OutputTab2);
                //NinjaTrader.Code.Output.Process("TS Diff: " + (Lows[0][0] - MyStopLow).ToString(), PrintTo.OutputTab2);
                

                // apply new low value to stop
                if (Position.MarketPosition == MarketPosition.Long
                    && MyStopLow < Lows[0][0]
                    && (Lows[0][0] - MyStopLow) > 4
                    )
                {
                    ExitLongStopMarket(Convert.ToInt32(DefaultQuantity), MyStopLow, "", "");
                }

                // Set stop for shorts
                if ((Position.MarketPosition == MarketPosition.Short)
                     && ((High[1] + (TickOffsetShort * TickSize)) <= MyStopLow))
                {
                    MyStopLow = (High[1] + (TickOffsetShort * TickSize));
                }
                
                // Exit Short using stop price
                if (Position.MarketPosition == MarketPosition.Short
                    && MyStopLow > Highs[0][0])
                {
                    ExitShortStopMarket(Convert.ToInt32(DefaultQuantity), MyStopLow, "", "");
                }
                

                // Set Upper Bollinger Hit
                if (Position.MarketPosition == MarketPosition.Long && !CrossBelow(EMA(EMA6), SMA(SMA13), 1)
                && upperWick > bollinger.Upper[0] // Upper wick goes over top bollinger band
                )
                upperBollingerHit = true;

                /********************* 
                * Long Exits
                ********************/
                //Print("RSI Not Under EMA(RSI) " + rsiLookingGood.ToString());

                //exitLongs(marketOpen, tradeParameters);

                /********************* 
                * Short Exits
                ********************/
                exitShorts(marketOpen, tradeParameters);

                /*********************
                 * Exit All
                 ********************/
                 exitAll(marketOpen, tradeParameters);

            }

            if (!marketOpen && (Position.MarketPosition == MarketPosition.Short || Position.MarketPosition == MarketPosition.Long))
            {
                if (Position.MarketPosition == MarketPosition.Short)
                    ExitShort();
                if (Position.MarketPosition == MarketPosition.Long)
                    ExitLong();
            }
		}
		
		/// <summary>
        /// Enter Longs w/ Trailing Stop
        /// </summary>
        /// <param name="marketOpen"></param>
        /// <param name="tradeParameters"></param>
        public void enterLongs(bool marketOpen, TradeParameters tradeParameters)
        {
            ///   * Tim's Strategy *
            ///   6 and 9 cross above 30 and 100
            LongEntryStrategyThree(tradeParameters);

            ///   * Tim's Strategy 2 *
            ///   6 above 9 and PA above 30 and 100
            //LongEntryStrategyFour(tradeParameters);

            ///  *Strategy* 
            ///  6 EMA & 13 SMA Cross
            ///
            ////LongEntryStrategyOne(tradeParameters);

            ///  *Strategy* 
            ///  6 EMA & 9 EMA Cross
            ///
            ////LongEntryStrategyTwo(tradeParameters);

            /*
            // SET TRAILING STOP //
            if (Position.MarketPosition != MarketPosition.Flat
                && Low[0] > trailingStopPrice) // If the price is above the trailing stop, adjust the stop
            {
                trailingStopPrice = Math.Max(Close[0] - trailAmount, trailingStopPrice);
                SetTrailStop(CalculationMode.Price, trailingStopPrice);
                string message = "\nIncreasing trailing stop\n";
                message += "TrailingPrice: " + trailingStopPrice.ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab1);
            }*/
        }

        /// <summary>
        /// Exit Longs
        /// </summary>
        /// <param name="tradeParameters"></param>
        public void exitLongs(bool marketOpen, TradeParameters tradeParameters)
        {
            ///  *Exit Strategy* 
            ///  Hit x Points
            ///
            int points = 5;
            //LongExitStrategyOne(points, tradeParameters);

            ///  *Exit Strategy* 
            ///  6 EMA & 13 SMA Cross
            ///
            LongExitStrategyTwo(tradeParameters);

            ///  *Exit Strategy* 
            ///  6 EMA & 13 SMA Cross
            ///
            //LongExitStrategyThree(tradeParameters);

            ///  *Exit Strategy* 
            ///  6 EMA Under 13 SMA 
            ///
            LongExitStrategySix(tradeParameters);

            ///  Dodgy Exit
            //LongExitStrategyFive();

            // WARNINGS!!
            //LongExitWarning(tradeParameters);


        }

        /// <summary>
        /// Enter Short w/ Trailing Stop
        /// </summary>
        /// <param name="tradeParameters"></param>
        public void enterShorts(bool marketOpen, TradeParameters tradeParameters)
        {
           
            ///   * Tim's Strategy *
            ///   6 EMA and 9 EMA Cross & Below 30
            ShortEntryStrategyThree(tradeParameters);

            //ShortEntryStrategyFour(tradeParameters);

            ///  *Strategy* 
            ///  6 EMA & 13 SMA Cross
            ///
            ShortEntryStrategyOne(tradeParameters);

            ///  *Strategy* 
            ///  6 EMA & 9 EMA Cross
            ///
            ShortEntryStrategyTwo(tradeParameters);
            
            /*else {
                ///   * Tim's Strategy *
                ///   6 EMA and 9 EMA Cross & Below 30
                ShortEntryWarningThree(tradeParameters);

                ShortEntryWarningFour(tradeParameters);

                ///  *Strategy* 
                ///  6 EMA & 13 SMA Cross
                ///
                ShortEntryWarningOne(tradeParameters);

                ///  *Strategy* 
                ///  6 EMA & 9 EMA Cross
                ///
                ShortEntryWarningTwo(tradeParameters);
            }*/


            // SET TRAILING STOP //
            if (High[0] < trailingStopPrice) // If the price is below the trailing stop, adjust the stop
            {
                trailingStopPrice = Math.Min(tradeParameters.CurrentPrice + trailAmount, trailingStopPrice);
                SetTrailStop(CalculationMode.Price, trailingStopPrice);
            }

        }

        public void exitShorts(bool marketOpen, TradeParameters tradeParameters)
        {
            ShortExitStrategyOne(tradeParameters);

            //ShortExitStrategyTwo(tradeParameters);

            ShortExitStrategyThree(tradeParameters);

            //ShortExitStrategyFour(tradeParameters);

            //if (sixAndNine)
            //    ShortExitStrategyFive(tradeParameters);


        }

        public void exitAll(bool marketOpen, TradeParameters tradeParameters)
        {
            ExitAllStrategyOne(tradeParameters);
        }

        #region EntryStrategies
        public void LongEntryStrategyOne(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (tradeParameters.Rsi > tradeParameters.RsiEma // Grey below yellow
                    && CrossAbove(EMA(EMA6), SMA(SMA13), 1) // Yellow above Orange
                    && tradeParameters.CurrentPrice > tradeParameters.HundredEMA // Close can't be below EMA 100
                    && (int)tradeParameters.LowerWick != (int)tradeParameters.HundredEMA // Lower wick can't touch EMA 100
                                                                                         //&& (!(upperWick >= emaSix && lowerWick <= smaThirteen)) // Not when the 6 and 13 are in the entire candle 
                                                                                         //&& upperWick < bollinger.Upper[0] // Upper wick can't be above upper bollinger band 
                    && (tradeParameters.LowerWick > tradeParameters.HundredEMA && tradeParameters.LowerWick > tradeParameters.TwoHundredEMA)
                    && (int)tradeParameters.HundredEMA != (int)tradeParameters.EmaSix
                    )
                {
                    //EnterLongLimit(GetCurrentBid());
                    EnterLong(Convert.ToInt32(DefaultQuantity), "");
                    MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                    entry_price = GetCurrentBid();
                    inACross = true;
                    SetProfitTarget("Long", CalculationMode.Ticks, tradeParameters.ProfitTargetLong);
                    entry_price = GetCurrentBid();
                    string message = ("\n///  * Long Entry Strategy One * \n" +
                        "///  6 EMA & 13 SMA Cross \n" +
                        "///\n");
                    entryTime = Times[0][0];
                    message += "// Entry Time: " + entryTime.ToString() + "\n";
                    message += "// Entry Price: " + entry_price.ToString() + "\n";
                    Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
            }
        }

        public void LongEntryStrategyTwo(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
               && !CrossAbove(tradeParameters.EmaSix,SMA(SMA13) , 1)
               && CrossAbove(EMA(6), EMA(9), 1)
               && tradeParameters.CurrentPrice > tradeParameters.HundredEMA
                && tradeParameters.Rsi > tradeParameters.RsiEma
               && tradeParameters.LowerWick > tradeParameters.HundredEMA
               )
            {
                //EnterLong();
                EnterLong(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                entry_price = GetCurrentBid();
                string message = ("\n///  * Long Entry Strategy Two * \n" +
                    "///  6 EMA & 9 EMA Cross \n" +
                    "///\n");
                entryTime = Times[0][0];
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }

        public void LongEntryStrategyThree(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
               && CrossAbove(EMA(6), EMA(9), 1)
               //&& tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               //&& tradeParameters.LowerWick > tradeParameters.HundredEMA
               && tradeParameters.CurrentPrice > EMA(30)[0]
               )
            {
                //EnterLong();
                EnterLong(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                entry_price = GetCurrentBid();
                string message = ("\n///  * Long Entry Strategy Three * \n" +
                    "///  6 EMA & 9 EMA Cross / Above 30 EMA \n" +
                    "///\n");
                entryTime = Times[0][0];
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                //Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }

        
        public void LongEntryStrategyFour(TradeParameters tradeParameters)
        {
            
            if (Position.MarketPosition == MarketPosition.Flat
               && EMA(6)[0] > EMA(9)[0]
               //&& tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               //&& tradeParameters.LowerWick > tradeParameters.HundredEMA
               && tradeParameters.CurrentPrice > EMA(30)[0]
               && tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               && tradeParameters.Rsi > tradeParameters.RsiEma
               && tradeParameters.UpperWick < tradeParameters.TopBollinger
               && EMA(6)[1] < tradeParameters.EmaSix // EMA6 is increasing
               //&& (Math.Abs(tradeParameters.LowerWick - EMA(6)[0]) > 4 * TickSize)
               //&& (Math.Abs(Close[0] - EMA(6)[0]) > 4 * TickSize)
               )
            {
                //EnterLong();
                EnterLong(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (Low[1] + (TickOffsetLong * TickSize));

                entry_price = GetCurrentBid();
                string message = ("\n///  * Long Entry Strategy Four * \n" +
                    "///  6 EMA Above 9 EMA / PA Above 30 & 100 EMA \n" +
                    "///\n");
                entryTime = Times[0][0];
                //Print(message);
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }

        public void ShortEntryStrategyOne(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
                && CrossBelow(EMA(6), SMA(13), 1) // Yellow under Orange
                //&& tradeParameters.Rsi < tradeParameters.RsiEma // Grey below yellow
                && (Math.Abs(Close[0] - EMA(100)[0]) > 8 * TickSize) // Must be more than 8 ticks away from EMA(100)
                && tradeParameters.CurrentPrice > tradeParameters.LowerBollinger
                && tradeParameters.LowerWick > tradeParameters.LowerBollinger
                && (Close[0] < tradeParameters.SmaThirteen)
                && (Close[0] < tradeParameters.EmaSix)
                && (Open[0] < tradeParameters.SmaThirteen)
                && (Open[0] < tradeParameters.EmaSix)
                && (EMA(100)[0] > Close[0])
                
                )
            {
                double askPrice = GetCurrentAsk();

                //EnterShortLimit(askPrice);
                //EnterShort();
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (High[1] + (TickOffsetShort * TickSize));

                entry_price = GetCurrentBid();
                double profitTargetShort = Close[0] - 10 * TickSize;
                SetProfitTarget("Short", CalculationMode.Ticks, profitTargetShort);
                string message = ("\n///  * Short Entry Strategy One * \n" +
                    "///  6 EMA & 13 SMA Cross \n" +
                    "///\n");
                entryTime = Times[0][0];
                double previousLow;
                previousLow = Lows[1].GetValueAt(0);
                message += "Entry Time: " + entryTime.ToString() + "\n";
                message += "askPrice: " + askPrice.ToString() + "\n";
                message += "Entry Price: " + entry_price.ToString() + "\n";
                message += "Previous Low: " + previousLow.ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }

            
        }

        public void ShortEntryStrategyTwo(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
                && !CrossBelow(EMA(6), SMA(13), 1)
                && CrossBelow(EMA(6), EMA(9), 0)
                && EMA(13)[1] < tradeParameters.SmaThirteen // SMA13 is decreasing
                 //&& tradeParameters.Rsi < tradeParameters.RsiEma
                )
            {
                //EnterShort();
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (High[1] + (TickOffsetShort * TickSize));

                entry_price = GetCurrentBid();
                string message = ("\n///  * Short Entry Strategy Two * \n" +
                    "///  6 EMA & 9 EMA Cross \n" +
                    "///\n");
                entryTime = Times[0][0];
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }

        }

        public void ShortEntryStrategyThree(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
               && CrossBelow(EMA(6), EMA(9), 1)
               //&& tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               //&& tradeParameters.LowerWick > tradeParameters.HundredEMA
               && tradeParameters.CurrentPrice < EMA(30)[0]
               && EMA(13)[1] < tradeParameters.SmaThirteen // SMA13 is decreasing
               )
            {

                //EnterShort();
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (High[1] + (TickOffsetShort * TickSize));

                entry_price = GetCurrentBid();
                string message = ("\n///  * Short Entry Strategy Three * \n" +
                    "///  6 EMA & 9 EMA Cross / Below 30 EMA \n" +
                    "///\n");
                entryTime = Times[0][0];
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                //Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }

        public void ShortEntryStrategyFour(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
               && EMA(6)[0] < EMA(9)[0]
               //&& tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               //&& tradeParameters.LowerWick > tradeParameters.HundredEMA
               && tradeParameters.CurrentPrice < EMA(30)[0]
               && tradeParameters.CurrentPrice < tradeParameters.HundredEMA
               && tradeParameters.Rsi < tradeParameters.RsiEma
               && tradeParameters.LowerWick > tradeParameters.LowerBollinger
               && EMA(13)[1] < tradeParameters.SmaThirteen // SMA13 is decreasing
               )
            {
                //EnterShort();
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                MyStopLow = (High[1] + (TickOffsetShort * TickSize));

                entry_price = GetCurrentBid();
                string message = ("\n///  * Short Entry Strategy Four * \n" +
                    "///  6 EMA Below 9 EMA / PA Below 30 & 100 EMA \n" +
                    "///\n");
                entryTime = Times[0][0];
                //Print(message);
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }
        #endregion

        #region LongWarnings
        public void ShortEntryWarningOne(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
                && CrossBelow(EMA(6), SMA(13), 1) // Yellow under Orange
                && tradeParameters.Rsi < tradeParameters.RsiEma // Grey below yellow
                                                                //&& (Math.Abs(Close[0] - EMA(100)[0]) > 8 * TickSize) // Must be more than 8 ticks away from EMA(100)
                && tradeParameters.CurrentPrice > tradeParameters.LowerBollinger
                && tradeParameters.LowerWick > tradeParameters.LowerBollinger

                )
            {
                double askPrice = GetCurrentAsk();

                //EnterShortLimit(askPrice);

                entry_price = Close[0];
                double profitTargetShort = Close[0] - 10 * TickSize;
                SetProfitTarget("Short", CalculationMode.Ticks, profitTargetShort);
                string message = ("\n///  * Short Entry Strategy One * \n" +
                    "///  6 EMA & 13 SMA Cross \n" +
                    "///  ENTER NOW!! \n");
                entryTime = Times[0][0];
                double previousLow;
                previousLow = Lows[1].GetValueAt(0);
                message += "Entry Time: " + entryTime.ToString() + "\n";
                message += "askPrice: " + askPrice.ToString() + "\n";
                message += "Entry Price: " + entry_price.ToString() + "\n";
                message += "Previous Low: " + previousLow.ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }


        }

        public void ShortEntryWarningTwo(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
                && !CrossBelow(EMA(6), SMA(13), 1)
                && CrossBelow(EMA(6), EMA(9), 0)
                 && tradeParameters.Rsi < tradeParameters.RsiEma
                )
            {
                //EnterShort();
                entry_price = Close[0];
                string message = ("\n///  * Short Entry Strategy Two * \n" +
                    "///  6 EMA & 9 EMA Cross \n" +
                    "///  ENTER NOW!! \n");
                entryTime = Times[0][0];
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }

        }

        public void ShortEntryWarningThree(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
               && CrossBelow(EMA(6), EMA(9), 1)
               //&& tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               //&& tradeParameters.LowerWick > tradeParameters.HundredEMA
               && tradeParameters.CurrentPrice < EMA(30)[0]
               )
            {
                //EnterLong();
                entry_price = Close[0];
                string message = ("\n///  * Short Entry Strategy Three * \n" +
                    "///  6 EMA & 9 EMA Cross / Below 30 EMA \n" +
                    "///  ENTER NOW!! \n");
                entryTime = Times[0][0];
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                //Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }

        public void ShortEntryWarningFour(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Flat
               && EMA(6)[0] < EMA(9)[0]
               //&& tradeParameters.CurrentPrice > tradeParameters.HundredEMA
               //&& tradeParameters.LowerWick > tradeParameters.HundredEMA
               && tradeParameters.CurrentPrice < EMA(30)[0]
               && tradeParameters.CurrentPrice < tradeParameters.HundredEMA
               && tradeParameters.Rsi < tradeParameters.RsiEma
               && tradeParameters.LowerWick > tradeParameters.LowerBollinger
               )
            {
                EnterLong();
                entry_price = GetCurrentBid();
                string message = ("\n///  * Short Entry Strategy Four * \n" +
                    "///  6 EMA Below 9 EMA / PA Below 30 & 100 EMA \n" +
                    "///\n");
                entryTime = Times[0][0];
                //Print(message);
                message += "// Entry Time: " + entryTime.ToString() + "\n";
                message += "// Entry Price: " + entry_price.ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = true;
            }
        }
        #endregion


        #region ExitStrategies
        public void LongExitStrategyOne(int points, TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Long
                && ((Close[0] - entry_price) >= points
                   || (tradeParameters.UpperWick - entry_price) >= points)
                )
            {
                ExitLong();
                string message = ("\n///  * Long Exit Strategy One * \n" +
                    "///  Exit When x Points \n" +
                    "///\n");
                message = message.Replace(" x ", points.ToString());
                message += "// Exit Time: " + entryTime.ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = false;
            }
        }

        public void LongExitStrategyTwo(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Long 
                && CrossBelow(EMA(6), SMA(13), 1))
            {
                ExitLong();
                upperBollingerHit = false;
                string message = ("\n///  * Long Exit Strategy Two * \n" +
                    "///  6 EMA & 13 SMA Cross \n" +
                    "///\n");
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                inACross = false;
                sixAndNine = false;
            }
        }

      

        public void LongExitWarning(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Long && CrossBelow(EMA(6), SMA(13), 1))
            {
                upperBollingerHit = false;
                string message = ("\n///  6 EMA & 13 SMA Crossed \n" +
                    "///  GET OUT NOW!!  \n");
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }

            if (Position.MarketPosition == MarketPosition.Long && CrossBelow(EMA(6), EMA(9), 1))
            {
                upperBollingerHit = false;
                string message = ("\n///  6 EMA & 9 EMA Crossed \n" +
                    "///  GET OUT NOW!!  \n");
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }
        }



        public void LongExitStrategyThree(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Long && !CrossBelow(EMA(EMA6), SMA(SMA13), 1)
               && upperBollingerHit
               && !rsiLookingGood
               && !inACross
               )
            {
                //ExitLongLimit(upperWick);
                ExitLong();

                upperBollingerHit = false;
                string message = "\n\nExit Long when PA Hit Upper Bollinger OR RSI Started Dipping Downward!!\n\n";
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                inACross = false;
                sixAndNine = false;
            }

        }

        



        public void LongExitStrategyFour(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Long && !CrossBelow(EMA(6), SMA(13), 1)
                && tradeParameters.UpperWick > tradeParameters.TopBollinger // Upper wick goes over top bollinger band
                && tradeParameters.Rsi <= tradeParameters.RsiEma
                && tradeParameters.PreviousUpperWick > tradeParameters.PreviousTopbollinger
                && !inACross
                )
            {
                ExitLongLimit(tradeParameters.TopBollinger); // Need to exit on that upper bollinger
                
                upperBollingerHit = false;
                string message = "\n\nExit Long whenPrevious Upper Wick > Upper Bollinger!!\n\n";
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                inACross = false;
                sixAndNine = false;
            }
        }

        public void LongExitStrategyFive()
        {
            //// Calculate the upper wick of the current bar
            double upperWick = High[0] - Math.Max(Open[0], Close[0]);
            double lowerWick = Math.Min(Open[0], Close[0]) - Low[0];

            if (Position.MarketPosition == MarketPosition.Long
                && upperWick > upperWickThreshold
                && lowerWick > lowerWickThreshold)
            {
                //ExitLongLimit(upperWick);
                ExitLong();

                upperBollingerHit = false;
                string message = "\n\nExit Long when dodgy candle is created\n\n";
                message += "Exit Time: " + Times[0][0].ToString() + "\n";
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                inACross = false;
                sixAndNine = false;
            }
        }

        public void LongExitStrategySix(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Long
                && EMA(6)[0] < SMA(13)[0])
            {
                ExitLong();
                upperBollingerHit = false;
                string message = ("\n///  * Long Exit Strategy Three * \n" +
                    "///  6 EMA Under 13 SMA  \n" +
                    "///\n");
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                inACross = false;
                sixAndNine = false;
            }
        }

        public void ShortExitStrategyOne(TradeParameters tradeParameters)
        {
            if (Position.MarketPosition == MarketPosition.Short
                && CrossAbove(EMA(6), SMA(13), 1)
                )
            {
                ExitShort();
                string message = ("\n///  * Short Exit Strategy Two * \n" +
                   "///  6 EMA & 13 SMA Cross \n" +
                   "///\n");
                entryTime = Times[0][0];
                message += entryTime.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }

        }

        public void ShortExitStrategyTwo(TradeParameters tradeParameters)
        {
            // Exit when it's noon - 1 PM and 20 points have been met
            if (Position.MarketPosition == MarketPosition.Short
                && (ToTime(Time[0]) >= 120000 && ToTime(Time[0]) <= 130000)
                && ((entry_price - Close[0]) >= 20
                   || (entry_price - tradeParameters.LowerWick) >= 20)
                && !sixAndNine
                )
            {
                ExitShort();
                string message = ("\n///  * Short Exit Strategy Two * \n" +
                   "///  Exit When x Points \n" +
                   "///\n");
                entryTime = Times[0][0];
                message += entryTime.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }
        }

        public void ShortExitStrategyThree(TradeParameters tradeParameters)
        {
            // Exit when in a EMA 6 and EMA 9 short and 2 points have been made 
            if (Position.MarketPosition == MarketPosition.Short
                && sixAndNine
                && ((entry_price - tradeParameters.CurrentPrice) >= 2
                   || (entry_price - tradeParameters.LowerWick) >= 2)
                )
            {
                ExitShort();
                string message = ("\n///  * Short Exit Strategy Three * \n" +
                   "///  Exit When 2 Points \n" +
                   "///\n");
                entryTime = Times[0][0];
                message += entryTime.ToString() + "\n";
                Print(message);
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }
        }

        public void ShortExitStrategyFour(TradeParameters tradeParameters)
        {
            // When x Points have been reached
            if (Position.MarketPosition == MarketPosition.Short
                && ((entry_price - tradeParameters.CurrentPrice) >= 10
                   || (entry_price - tradeParameters.LowerWick) >= 10)
                )
            {
                ExitShort();
                string message = ("\n///  * Short Exit Strategy Four * \n" +
                   "///  Exit When 10 Points \n" +
                   "///\n");
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
            }
        }

        public void ShortExitStrategyFive(TradeParameters tradeParameters)
        {
            // When x Points have been reached
            if (Position.MarketPosition == MarketPosition.Short
                && ((entry_price - tradeParameters.CurrentPrice) >= 3
                   || (entry_price - tradeParameters.LowerWick) >= 3)
                )
            {
                ExitShort();
                string message = ("\n///  * Short Exit Strategy Five * \n" +
                   "///  Exit When 3 Points \n" +
                   "///\n");
                message += Times[0][0].ToString();
                NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                sixAndNine = false;
            }
        }

        /// <summary>
        /// Exit if trade is going against you!!
        /// </summary>
        /// <param name="tradeParameters"></param>
        public void ExitAllStrategyOne(TradeParameters tradeParameters)
        {
            double diff = tradeParameters.CurrentPrice - entry_price;
            //NinjaTrader.Code.Output.Process("Diff:" + diff.ToString() + "\n", PrintTo.OutputTab2);
            //if (Position.MarketPosition == MarketPosition.Short || Position.MarketPosition == MarketPosition.Long)
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                diff = entry_price - tradeParameters.CurrentPrice;
                if (diff <= -5) //-5 
                {
                    ExitShort();
                    string message = ("\n///  * Exit All Strategy One * \n" +
               "///  Exit When -5 Points \n" +
               "///\n");
                    entryTime = Times[0][0];
                    message += entryTime.ToString() + "\n";
                    Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
                    
            }
            if (Position.MarketPosition == MarketPosition.Long)
            {
                //diff = tradeParameters.CurrentPrice - entry_price;
                diff = entry_price - tradeParameters.CurrentPrice;
                //NinjaTrader.Code.Output.Process("Entry Diff: " + entry_price.ToString(), PrintTo.OutputTab2);
                //NinjaTrader.Code.Output.Process("Current Diff: " + tradeParameters.CurrentPrice.ToString(), PrintTo.OutputTab2);
                //NinjaTrader.Code.Output.Process("Current - Entry: " + diff.ToString(), PrintTo.OutputTab2);
                if (diff <= -5)//-5
                {
                    ExitLong();
                    string message = ("\n///  * Exit All Strategy One * \n" +
               "///  Exit When -5 Points \n" +
               "///\n");
                    entryTime = Times[0][0];
                    message += entryTime.ToString() + "\n";
                    Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
            }

            /*if (Position.MarketPosition == MarketPosition.Long)
            {
                diff = entry_price - tradeParameters.CurrentPrice;
                if (tradeParameters.TopBollinger < tradeParameters.PreviousTopbollinger)
                {
                    //ExitLong();
                    string message = ("\n///  * Exit All Strategy One * \n" +
               "///  Top Bollinger Going Down \n" +
               "///\n");
                    entryTime = Times[0][0];
                    message += entryTime.ToString() + "\n";
                    Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
            }*/
        }

        /// <summary>
        /// Exit when SMA 6 hits PA 
        /// </summary>
        /// <param name="tradeParameters"></param>
        public void ExitAllStrategyTwo(TradeParameters tradeParameters)
        {
            
            if (Position.MarketPosition == MarketPosition.Short || Position.MarketPosition == MarketPosition.Long)
            {
                if (tradeParameters.UpperWick > tradeParameters.SmaSix
                    && tradeParameters.LowerWick < tradeParameters.SmaSix
                    && Highs[0][0] > tradeParameters.SmaSix
                    && Lows[0][0] < tradeParameters.SmaSix
                    )
                {
                    ExitShort();
                    string message = ("\n///  * Exit All Strategy Two * \n" +
               "///  Exit When SMA 6 Hits Price Action \n" +
               "///\n");
                    entryTime = Times[0][0];
                    message += entryTime.ToString() + "\n";
                    Print(message);
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }

            }
        }


        #endregion

        public void showPriceDiff(double enteredPrice, double currentPrice)
        {
            double diffPrice;
            if (enteredPrice != 0 && (enteredPrice != currentPrice))
            {

                string message = "";

                if (Position.MarketPosition == MarketPosition.Long)
                {
                    message += "\n+\n";
                    message += "Price Movement: " + (enteredPrice - currentPrice).ToString() + "\n";
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
                if (Position.MarketPosition == MarketPosition.Short)
                {
                    message += "\n-\n";
                    message += "Price Movement: " + (enteredPrice - currentPrice).ToString() + "\n";
                    NinjaTrader.Code.Output.Process(message, PrintTo.OutputTab2);
                }
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "LongEntries", Order = 1, GroupName = "Parameters")]
        public bool LongEntries
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ShortEntries", Order = 1, GroupName = "Parameters")]
        public bool ShortEntries
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TickOffsetLong", Description = "Negative Ticks to trail below", Order = 1, GroupName = "Parameters")]
        public int TickOffsetLong
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TickOffsetShort", Description = "Positive Ticks to trail above", Order = 2, GroupName = "Parameters")]
        public int TickOffsetShort
        { get; set; }

        [NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA6", Description="Yellow", Order=1, GroupName="Parameters")]
		public int EMA6
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SMA13", Description="Orange", Order=2, GroupName="Parameters")]
		public int SMA13
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA100", Description="Blue", Order=1, GroupName="Parameters")]
		public int EMA100
		{ get; set; }

        [NinjaScriptProperty]
        [Display(Name = "WarningsOnly", Order = 1, GroupName = "Parameters")]
        public bool WarningsOnly
        { get; set; }
        
        [Browsable(false)]
		[XmlIgnore()]
		public Series<double> Lower
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Middle
		{
			get { return Values[1]; }
		}

		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumStdDev", GroupName = "NinjaScriptParameters", Order = 0)]
		public double NumStdDev
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Upper
		{
			get { return Values[0]; }
		}
		#endregion
	}
}
