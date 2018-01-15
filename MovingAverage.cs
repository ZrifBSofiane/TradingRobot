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

        double MaximumRisk = 0.02;
        double DecreaseFactor = 3;
        int MovingPeriodHigh = 20;
        int MovingPeriodLow = 5;
        double smaValueCheck = 0.1;
       

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
            double maH = iMA(symbol, 0, MovingPeriodHigh, 0, MODE_SMA, PRICE_CLOSE, 0);
            double maL = iMA(symbol, 0, MovingPeriodLow, 0, MODE_SMA, PRICE_CLOSE, 0);

            //---- get Previous Moving Average
            double maHPrevious1 = iMA(symbol, 0, MovingPeriodHigh, 0, MODE_SMA, PRICE_CLOSE, 1);
            double maLPrevious1 = iMA(symbol, 0, MovingPeriodLow, 0, MODE_SMA, PRICE_CLOSE, 1);

            //---- compute the slope (coeff director)
            double slopeSmaH = maH - maHPrevious1;
            slopeSmaH *= 1000;
            double slopeSmaL = maL - maLPrevious1;
            slopeSmaL *= 1000;
          /*  if(slopeSmaL > smaValueCheck)
                //Console.WriteLine("AAAA " + TimeCurrent());
            if (slopeSmaL < -smaValueCheck)
               // Console.WriteLine("BBBB " + TimeCurrent());
            if (slopeSmaH > smaValueCheck)
               // Console.WriteLine("CCCC " + TimeCurrent());
            if (slopeSmaH < -smaValueCheck)
                //Console.WriteLine("DDDD " + TimeCurrent());
            */
            double minstoplevel = MarketInfo(Symbol(), MODE_STOPLEVEL);

            double slopMean =(slopeSmaH + slopeSmaL)/2;

            //Console.WriteLine("slopeSMAL " + slopeSmaL +" slopesmah " + slopeSmaH);



            double smaResult = (((smaValueCheck - slopMean) * 100) - 10) * -1;
       


            if(smaResult > 15)
                Console.WriteLine("BUY at Date : " + TimeCurrent() + " Result : " + smaResult);
            if (smaResult < -15)
                Console.WriteLine(" SELL at Date : " + TimeCurrent() + " Result : " + smaResult);

            //---- sell conditions
            if (maL < maH)
            {
               // Console.WriteLine("SELL");
               
                double lot = LotsOptimized();
                int ticket = OrderSend(Symbol(), OP_SELL, lot, Bid, 3, 0, 0, "", MAGICMA, DateTime.MinValue, Color.Blue);
                if(ticket == -1)
                {
                    //Console.WriteLine(GetLastError());
                }
                if (OrderSelect(ticket, SELECT_BY_TICKET))
                {
                   // Console.WriteLine("SELL " + Symbol() + " Lot : " + lot);
                }
               

            }
            //---- buy conditions
            if (maL > maH)
            {
               // Console.WriteLine("BUY");
                double lot = LotsOptimized();
                int ticket = OrderSend(Symbol(), OP_BUY, lot, Ask, 3, 0, 0, "", MAGICMA, DateTime.MinValue, Color.Blue);
                if (OrderSelect(ticket, SELECT_BY_TICKET))
                {
                   // Console.WriteLine("BUY " + Symbol() + " Lot : " + lot);
                }
            }
        }

        //+------------------------------------------------------------------+
        //| Check for close order conditions                                 |
        //+------------------------------------------------------------------+
          void CheckForClose(string symbol)
          {
             



                  //---- go trading only for first tiks of new bar
                  if (Volume[0] > 1) return;
                  //---- get Moving Average 
                  double maH = iMA(symbol, 0, MovingPeriodHigh, 0, MODE_SMA, PRICE_CLOSE, 0);
                  double maL = iMA(symbol, 0, MovingPeriodLow, 0, MODE_SMA, PRICE_CLOSE, 0);

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
            //if (CalculateCurrentOrders() <= 5)
            CheckForOpen(symbol);
            // else CheckForClose(symbol);

            return 0;
        }

    }
}
