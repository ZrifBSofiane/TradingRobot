using System;
using Color = System.Drawing.Color;
using NQuotes;

namespace MetaQuotesSample
{
   
    public class MovingAverage : MqlApi
    {

        static bool currentlyTrading = false;



        const int MAGICMA = 20050610;

        double MaximumRisk = 0.02;
        double DecreaseFactor = 3;
        int MovingPeriodHigh = 30;
        int MovingPeriodLow = 15;
        int MovingShift = 6;

        //+------------------------------------------------------------------+
        //| Calculate open positions                                         |
        //+------------------------------------------------------------------+
        int CalculateCurrentOrders()
        {
            int buys = 0, sells = 0;

            for (int i = 0; i < OrdersTotal(); i++)
            {
                if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) break;
                if (OrderSymbol() == Symbol() && OrderMagicNumber() == MAGICMA)
                {
                    if (OrderType() == OP_BUY) buys++;
                    if (OrderType() == OP_SELL) sells++;
                }
            }
            
            //---- return orders volume
            if (buys > 0) return (buys);
            return (-sells);
        }

        //+------------------------------------------------------------------+
        //| Calculate optimal lot size                                       |
        //+------------------------------------------------------------------+
        double LotsOptimized()
        {
            // history orders total
            int orders = OrdersHistoryTotal();
            // number of losses orders without a break
            int losses = 0;                  
            //---- select lot size
            double lot = NormalizeDouble(AccountFreeMargin() * MaximumRisk / 1000.0, 1);
            //---- calcuulate number of losses orders without a break
            if (DecreaseFactor > 0)
            {
                for (int i = orders - 1; i >= 0; i--)
                {
                    if (!OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)) { Print("Error in history!"); break; }
                    if (OrderSymbol() != Symbol() || OrderType() > OP_SELL) continue;
                    
                    if (OrderProfit() > 0) break;
                    if (OrderProfit() < 0) losses++;
                }
                if (losses > 1) lot = NormalizeDouble(lot - lot * losses / DecreaseFactor, 1);
            }
            //---- return lot size
            if (lot < 0.1) lot = 0.1;
            return (lot);
        }

        //+------------------------------------------------------------------+
        //| Check for open order conditions                                  |
        //+------------------------------------------------------------------+
        void CheckForOpen(string symbol)
        {
            //---- go trading only for first tiks of new bar
            if (Volume[0] > 1) return;
            
            //---- get Moving Average 
            double maH = iMA(symbol, 0, MovingPeriodHigh, MovingShift, MODE_SMA, PRICE_CLOSE, 0);
            double maL = iMA(symbol, 0, MovingPeriodLow, MovingShift, MODE_SMA, PRICE_CLOSE, 0);


            double minstoplevel = MarketInfo(Symbol(), MODE_STOPLEVEL);
            Console.WriteLine("Min Stop level : " + minstoplevel);
            //---- sell conditions
            if (maL < maH)
            {
                double tp = NormalizeDouble(Ask - 0.06, Digits);
                {
                    Console.WriteLine("Order commission : " + OrderCommission());
                }
                return;
            }
            //---- buy conditions
            if (maL > maH )
            {
                
                double tp = NormalizeDouble(Bid + 0.06, Digits);
                currentlyTrading = true;
                int ticket = OrderSend(Symbol(), OP_BUY, LotsOptimized(), Ask, 3, 0, tp, "", MAGICMA, DateTime.MinValue, Color.Blue);
                if (OrderSelect(ticket, SELECT_BY_TICKET))
                {
                    Console.WriteLine("Order commission : " + OrderCommission());
                }
            }
        }

        //+------------------------------------------------------------------+
        //| Check for close order conditions                                 |
        //+------------------------------------------------------------------+
      /*  void CheckForClose(string symbol)
        {
            if (currentlyTrading)
            {
                currentlyTrading = false;



                //---- go trading only for first tiks of new bar
                if (Volume[0] > 1) return;
                //---- get Moving Average 
                double maH = iMA(symbol, 0, MovingPeriodHigh, MovingShift, MODE_SMA, PRICE_CLOSE, 0);
                double maL = iMA(symbol, 0, MovingPeriodLow, MovingShift, MODE_SMA, PRICE_CLOSE, 0);

                for (int i = 0; i < OrdersTotal(); i++)
                {
                    if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) break;
                    if (OrderMagicNumber() != MAGICMA || OrderSymbol() != Symbol()) continue;
                    //---- check order type 
                    if (OrderType() == OP_SELL)
                    {
                        if (maL < maH) OrderClose(OrderTicket(), OrderLots(), Bid, 3, Color.White);
                        break;
                    }
                    if (OrderType() == OP_BUY)
                    {
                        if (maL > maH) OrderClose(OrderTicket(), OrderLots(), Ask, 3, Color.White);
                        break;
                    }
                }
            }
        }
        
    */
        //+------------------------------------------------------------------+
        //| Start function                                                   |
        //+------------------------------------------------------------------+
        public override int start()
        {
            //---- check for history and trading
            if (Bars < 100 || !IsTradeAllowed()) return 0;


          //  Console.WriteLine("NEW CANDLE");


            //---- calculate open orders by current symbol
            string symbol = Symbol();
            if (CalculateCurrentOrders() == 0) CheckForOpen(symbol);
           // else CheckForClose(symbol);

            return 0;
        }

    }
}
