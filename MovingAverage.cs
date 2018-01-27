using System;
using Color = System.Drawing.Color;
using NQuotes;
using System.Collections.Generic;

namespace MetaQuotesSample
{

    public class MovingAverage : MqlApi
    {

       

         List<String> smallPip = new List<String>()
        {
            "USDJPY"
        };


        const int MAGICMA = 20050610;
        const double maxAbsoluteMACD = 0.000004;
        double MaximumRisk = 0.02;
        double DecreaseFactor = 3;
        int MovingPeriodHigh = 20;
        int MovingPeriodLow = 5;
        double smaValueCheck = 0.1;
        const double risk = 0.5 / 100;

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
            
            return (sells + buys); 
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
              double macdMain = iMACD(symbol, 0, MovingPeriodHigh, MovingPeriodLow, 9, PRICE_CLOSE,MODE_MAIN,0);
              double macdSignal = iMACD(symbol, 0, MovingPeriodHigh, MovingPeriodLow, 9, PRICE_CLOSE, MODE_SIGNAL, 0);
            double maH = iMA(symbol, 0, 22, 0, MODE_SMA, PRICE_CLOSE, 0);
            double maL = iMA(symbol, 0,12, 0, MODE_SMA, PRICE_CLOSE, 0);
            int sellOrder = 0, buyOrder = 0;

          //  Console.WriteLine(maH.ToString());
            for (int i = 0; i < OrdersTotal(); i++)
            {
                if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) break;
                if (OrderMagicNumber() != MAGICMA || OrderSymbol() != Symbol()) continue;
                if (OrderType() == OP_SELL)
                {
                    sellOrder++;
                    
                }
                if (OrderType() == OP_BUY)
                {
                    buyOrder++;
                }


            }

            //---- sell conditions
            double spread = Ask - Bid;
            Console.WriteLine(" " + spread*1000);
            if (macdMain > macdSignal && maL < maH )
            {
                double lot = LotsOptimized();
                int ticket = OrderSend(Symbol(), OP_SELL, 1, Bid, 3, Ask + 2*spread, Ask - 1.5*spread , "", MAGICMA, DateTime.MinValue, Color.Green);

                if(ticket == -1)
                {
                    Console.WriteLine(GetLastError());
                }
                if (OrderSelect(ticket, SELECT_BY_TICKET))
                {
                    // Console.WriteLine("SELL " + Symbol() + " Lot : " + lot);
                }
                   
            }
            //---- buy conditions
            if (macdMain < macdSignal && maL > maH)
            {
                double lot = LotsOptimized();
                int ticket =  OrderSend(Symbol(), OP_BUY, 1, Ask, 3, Bid - 2*spread, Bid + 1.5*spread  , "", MAGICMA, DateTime.MinValue, Color.Red);
                if (ticket == -1)
                {
                    Console.WriteLine(GetLastError());
                }
                if (OrderSelect(ticket, SELECT_BY_TICKET))
                 {
                     //Console.WriteLine("BUY " + Symbol() + " Lot : " + lot + " Price : " +   OrderOpenPrice()  +" TP : " + tp) ;
                 }
                 
            }
            
        }

      
      
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
            double cal = CalculateCurrentOrders();
            //Console.WriteLine("ORDER = " + cal);
           
                CheckForOpen(symbol);
           
              //  Console.WriteLine("end");
               // CheckForClose(symbol);



            return 0;
        }

    }
}
