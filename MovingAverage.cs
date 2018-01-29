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
        //| Calculate TakeProfit                                             |
        //+------------------------------------------------------------------+
        Dictionary<String,Double> CalculateTakeProfit(String symbol)
        {
            //---- get ATR 
            double atr = iATR(symbol, 0, 20, 0);
            double takeProfit = 0;
            Dictionary<String, Double> result =  new Dictionary<string, double>();
            result.Add("valueATR", atr);
            if (atr >= 0.00032)
            {
                takeProfit = 2 * atr * 10;
                result.Add("valueTP", takeProfit);
                result.Add("level", 3);
            }  
            if (atr > 0.00022 && atr < 0.00032)
            {
                takeProfit = 1.5 * atr * 10;
                result.Add("valueTP", takeProfit);
                result.Add("level", 2);
            }
            if (atr <= 0.00022)
            {
                takeProfit = atr * 10;
                result.Add("valueTP", takeProfit);
                result.Add("level", 1);
            }

            return result;
        }




       


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
        //| Check for SMA conditions                                         |
        //+------------------------------------------------------------------+        
        int CheckSmaCondition(string symbol)
        {
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
          
            double minstoplevel = MarketInfo(Symbol(), MODE_STOPLEVEL);

            double slopMean = (slopeSmaH + slopeSmaL) / 2;
            double smaResult = (((smaValueCheck - slopMean) * 100) - 10) * -1;


            int decision = 0;
            if (smaResult > 5)
                decision = 1;
            if (smaResult < -5)
                decision = -1;

            


            return decision;
        }


        int ChechForMACD(string symbol)
        {
            int makeOrder = 0;
            //---- get MACD MAIN
            double main = iMACD(symbol, 0, 5,22, 10, PRICE_CLOSE, MODE_MAIN, 0);
            //double signal = iMACD(symbol, 0, 5,22, 10, PRICE_CLOSE, MODE_SIGNAL, 0);
            //---- get previous MACD
            double main1 = iMACD(symbol, 0, 5,22, 10, PRICE_CLOSE, MODE_MAIN, 1);
            double main2 = iMACD(symbol, 0, 5,22, 10, PRICE_CLOSE, MODE_MAIN, 2);
            double main3 = iMACD(symbol, 0, 5, 22, 10, PRICE_CLOSE, MODE_MAIN, 3);

            if (main> maxAbsoluteMACD )
            {
                if(main1< -maxAbsoluteMACD && main2< -maxAbsoluteMACD && main3<-maxAbsoluteMACD)
                {
                    makeOrder = 1;
                }
            }
            if (main < -maxAbsoluteMACD)
            {
                if (main1 > maxAbsoluteMACD && main2 > maxAbsoluteMACD && main3>maxAbsoluteMACD)
                {
                    makeOrder = -1;
                }
            }
            return makeOrder;
        }

        int CheckForSMA(string symbol)
        {
            int makeOrder = 0;
            //---- get SMA
            double smaLow = iMA(symbol, 0, 13, 0, MODE_SMMA, PRICE_CLOSE, 0);
            double smaHigh = iMA(symbol, 0, 22, 0, MODE_SMMA, PRICE_CLOSE, 0);

            if (smaHigh > smaLow)
            {
                makeOrder = -1;
            }
            if (smaHigh < smaLow)
            {
                makeOrder = 1;
            }
            return makeOrder;
        }

        void ChangeTrailingStop()
        {
            
            int trailingStop = 200;
            for (int i = 0; i < OrdersTotal(); i++)
            {
                if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) break;
                if (OrderSymbol() == Symbol() && OrderMagicNumber() == MAGICMA)
                {
                    if (OrderType() == OP_BUY)
                    {
                        
                        if(trailingStop>0)
                        {
                            if(Bid - OrderOpenPrice() > Point * trailingStop)
                            {
                                if(OrderStopLoss() < Bid-Point*trailingStop)
                                {
                                    double before = OrderStopLoss();
                                    bool result = OrderModify(OrderTicket(), OrderOpenPrice(),NormalizeDouble(Bid-Point*trailingStop,Digits),OrderTakeProfit(),OrderExpiration());
                                    double after = OrderStopLoss();
                                    if (!result)
                                        Console.WriteLine(GetLastError());
                                    else
                                        Console.WriteLine(" BUY trailing stop  before : " + before + " after : " + after);
                                }
                            }
                        }
                        

                    }
                    if (OrderType() == OP_SELL)
                    {
                        if (trailingStop > 0)
                        {
                            if (OrderOpenPrice() - Ask > Point * trailingStop)
                            {
                                if (OrderStopLoss() > Ask - Point * trailingStop )
                                {
                                    double before = OrderStopLoss();
                                    bool result = OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(Ask + Point * trailingStop, Digits), OrderTakeProfit(), OrderExpiration());
                                    double after = OrderStopLoss();
                                    if (!result)
                                        Console.WriteLine(GetLastError());
                                    else
                                        Console.WriteLine(" SELL trailing stop  before : " + before + " after : "+ after);
                                }
                            }
                        }
                    }
                }
            }
        }










        //+------------------------------------------------------------------+
        //| Check for open order conditions                                  |
        //+------------------------------------------------------------------+
        void CheckForOpen(string symbol)
        {
            ChangeTrailingStop();
            if (Volume[0] > 1) return;

            //---- sell conditions
            int macd = ChechForMACD(symbol);
            int sma = CheckForSMA(symbol);
            if (macd == -1 && sma == -1 )
            {
                double lot = LotsOptimized();
                Dictionary<String,double> tpResult = CalculateTakeProfit(symbol);
                double tp = tpResult["valueTP"];
                double sl2 = AccountBalance() * risk / (lot * 10) / 10000;
                OrderSend(Symbol(), OP_SELL, lot, Bid, 3, Ask + sl2, Ask - tp, "", MAGICMA, DateTime.MinValue, Color.Green);

            }
            //---- buy conditions
            if (macd == 1 && sma == 1)
            {
                double lot = LotsOptimized();
                //
                Dictionary<String, double> tpResult = CalculateTakeProfit(symbol);
                double tp = tpResult["valueTP"];
                double sl2 = AccountBalance() * risk / (lot * 10) / 10000;
                int ticket =  OrderSend(Symbol(), OP_BUY, lot, Ask, 3, Bid -  sl2, Bid + tp, "", MAGICMA, DateTime.MinValue, Color.Red);

                 if (OrderSelect(ticket, SELECT_BY_TICKET))
                 {
                     Console.WriteLine("BUY " + Symbol() + " Lot : " + lot + " Price : " +   OrderOpenPrice()  +" TP : " + tp) ;
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
           
                //Console.WriteLine("end  " + Point);
               // CheckForClose(symbol);



            return 0;
        }

    }
}
