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
        const double risk = 0.25 / 100;




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

        // 0: decision, 1: SMA10, 2: SMA20, 3: SMA65
        List<double> CheckForSMA(string symbol)
        {

            List<double> result = new List<double>();
            //---- get SMA
            double sma10 = iMA(symbol, 0, 10, 0, MODE_SMMA, PRICE_CLOSE, 0);
            double sma20 = iMA(symbol, 0, 20, 0, MODE_SMMA, PRICE_CLOSE, 0);
            double sma65 = iMA(symbol, 0, 65, 0, MODE_SMMA, PRICE_CLOSE, 0);

            //---- get Previous SMA
            double pSma10 = iMA(symbol, 0, 10, 0, MODE_SMMA, PRICE_CLOSE, 1);
            double pSma20 = iMA(symbol, 0, 20, 0, MODE_SMMA, PRICE_CLOSE, 1);
            double pSma65 = iMA(symbol, 0, 65, 0, MODE_SMMA, PRICE_CLOSE, 1);

            // 0 nothing;  1 buy sma10-20; 2 buy sma20-65
            // minus is sell

            if (pSma10 > pSma20 && sma10 < sma20) result.Add(-1);
            if (pSma10 < pSma20 && sma10 > sma20) result.Add(1);

            if (pSma20 > pSma65 && sma20 < sma65) result.Add(-2);
            if (pSma20 < pSma65 && sma20 > sma65) result.Add(2);
            result.Add(sma10);
            result.Add(sma20);
            result.Add(sma65);

            return result;
        }

        double f(int x)
        {
            return 1 / Math.Exp((x * x) / 0.33) + 0.1 / Math.Exp(x) + 0.2;
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

            List<double> data = new List<double>();
            data = CheckForSMA(symbol);
            double decision = data[0];
            

            // Sell 
            if (decision < 0)
            {
                double lot = LotsOptimized();
                Dictionary<String,double> tpResult = CalculateTakeProfit(symbol);
                double tp = tpResult["valueTP"];
                double sl2 = AccountBalance() * risk / (lot * 10) / 10000;
                OrderSend(Symbol(), OP_SELL, lot, Bid, 3, Ask + sl2, Ask - tp, "", MAGICMA, DateTime.MinValue, Color.Green);

            }
            //---- buy conditions
            if (decision > 0)
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
            //DataBase db = new DataBase();



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
