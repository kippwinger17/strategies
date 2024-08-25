# TempleOfDoomLong Strategy - README

## Overview
`TempleOfDoomLong` is a custom trading strategy for the NinjaTrader platform. It is designed to enter long positions between 5:00 AM and just before 11:00 AM, taking into account various market indicators such as VWAP, support and resistance levels, and news events that could impact trading.

## Features
- **Time-Based Entries**: Trades are only entered between specified times (default: 5:00 AM to 10:50 AM).
- **News Integration**: The strategy checks for significant economic news events, particularly those related to the USD, and avoids trading on days with high-impact events.
- **VWAP and Standard Deviation Levels**: The strategy utilizes the VWAP and multiple standard deviation levels to identify potential entry points for trades.
- **Support/Resistance Levels**: Dynamic support and resistance levels are calculated and monitored for additional trade validation.
- **Exit Criteria**: The strategy employs several safeguards to exit trades, including profit targets, stop losses, and conditions based on price movement relative to standard deviation and support/resistance levels.

## Code Structure
### 1. **Variables**
   - **Support/Resistance Levels**: Tracks various support and resistance levels for dynamic decision-making.
   - **VWAP Values**: Stores the VWAP and its standard deviation levels for use in entry and exit decisions.
   - **Times**: Holds the start and end times for when the strategy is active.
   - **News**: Handles news data and checks if trading should be avoided on specific days due to high-impact news.
   - **Date Tracking**: Manages date-related operations to ensure news is checked once per day and to prevent trading on pre-specified dates.

### 2. **State Management**
   - **OnStateChange()**: Initializes and configures the strategy settings.
   - **OnBarUpdate()**: The main function that executes on each bar update, managing trade entries, exits, and news checks.

### 3. **Entry and Exit Logic**
   - **getYourTradeOn()**: Determines whether to enter a long or short position based on VWAP, EMA, TEMA, and other conditions.
   - **getYourExitOn()**: Manages exits for open positions, including profit-taking and stop-loss conditions.

### 4. **News Handling**
   - **GetEventsForWeek()**: Checks for high-impact news events for the current week from a local XML file.
   - **GetEventsForToday()**: Fetches and processes the latest news data from an online source to determine if trading should be avoided on the current day.

### 5. **Support and Resistance Calculation**
   - **CheckLevels()**: Calculates dynamic support and resistance levels using custom indicators.
   - **VwapCheck()**: Updates the VWAP and standard deviation values to assist in trade decision-making.

## Usage
### 1. **Configuration**
   - Ensure that the start and end times are configured correctly for the desired trading session.
   - Customize the list of dates in the `dates` variable to specify days when trading should be avoided.
   - Place the XML file containing the news data for the current week in the specified directory.

### 2. **Running the Strategy**
   - Load the strategy into NinjaTrader and apply it to the desired chart.
   - The strategy will automatically handle entry and exit decisions based on the configured criteria.

### 3. **Monitoring**
   - The strategy will output relevant information, such as current date, support/resistance levels, and news checks, to NinjaTrader's output window.
   - It is recommended to monitor this output to understand the strategy's decision-making process.

## Customization
- **Debugging**: Set the `Debug` variable to `true` to enable detailed logging for troubleshooting.
- **Time Settings**: Modify the `StartTime` and `EndTime` variables to change the active trading window.
- **News Impact**: Update the logic in `GetEventsForWeek` and `GetEventsForToday` to add or remove conditions for avoiding trading on news days.

## Requirements
- **NinjaTrader**: This strategy is built for NinjaTrader 8.
- **News XML File**: A local XML file containing the week's economic events is required for the news-checking functionality.

## Notes
- This strategy is intended for educational purposes and should be thoroughly tested in a simulated environment before live trading.
- Users are responsible for ensuring the strategy's parameters and conditions align with their trading goals and risk tolerance.
