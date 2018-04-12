using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockExchangeMarket
{
    public class Company
    {

        private String _symbol;
        private String _name;
        private Double _openPrice;
        private Double _closePrice;
        private Double _currentPrice;
        private RealTimedata market;
        public List<Order> BuyOrders = new List<Order>();
        public List<Order> SellOrders = new List<Order>();
        public List<Order> Transactions = new List<Order>();


        public Company(String symbol, String _name, double openPrice, RealTimedata handledBy)
        {
            this._symbol = symbol;
            this._openPrice = openPrice;
            this._closePrice = 0;
            this._currentPrice = 0;
            this.market = handledBy;
            this._name = _name;
        }

        // Gets or sets the price
        public double lastSale
        {
            get { return _currentPrice; }
            set
            {
                if (_currentPrice != value)
                {
                    _currentPrice = value;
                    market.Notify();
                }
            }
        }

        public double OpenPrice
        {
            get { return _openPrice; }
            set { _openPrice = value; }
        }

        public double ClosePrice
        {
            get { return _closePrice; }
            set { _closePrice = value; }
        }

        public String Symbol
        {
            get { return _symbol; }
            set { _symbol = value; }
        }

        public String Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public void addBuyOrder(Double buy_price, int buy_size)
        {
            market.buyOrder(Symbol, buy_size, buy_price);
        }

        public void addSellOrder(Double sale_price, int sale_size)
        {
            market.sellOrder(Symbol, sale_size, sale_price);
        }

        public List<Order> getBuyOrders()
        {
            return BuyOrders;
        }
        public List<Order> getSellOrders()
        {
            return SellOrders;
        }
        public int getVolume()
        {
            int shareVolume = 0;
            foreach (Order deal in Transactions)
            {
                shareVolume += deal.Size;
            }
            return shareVolume;
        }
    }
}
