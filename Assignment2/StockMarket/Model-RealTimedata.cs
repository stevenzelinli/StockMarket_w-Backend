using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Runtime.Serialization;

namespace StockExchangeMarket
{
    public class RealTimedata : StockMarket
    {
        private List<Company> StockCompanies = new List<Company>();
        private TcpClient connection;
        NetworkStream ioStream;
        private string clientID;
        private string session;
        //CONSTRUCTOR TO GET TCPCLIENT CONNECTION
        public RealTimedata(TcpClient connection, string clientID, string session)
        {
            //GET CONNECTION
            this.clientID = clientID;
            ioStream = connection.GetStream();
            this.session = session;
            this.connection = connection;
            //START WAITING FOR NOTIFY IN ANOTHER THREAD
            new Thread(new ThreadStart(waitForNotify)).Start();
        }
        //WAITING FOR NOTIFY REQUEST FROM SERVER IN WHICH CASE IT WILL REQUEST DATA AND UPDATE 'StockCompanies' 
        public void waitForNotify()
        {
            while (connection.Connected)
            {
                //GET RESPONSE AND UPDATE DATA IF NOTIFY
                var _data = new byte[256];
                StringBuilder response = new StringBuilder();
                do
                {
                    var numBytesRead = ioStream.Read(_data, 0, _data.Length);
                    response.AppendFormat("{0}", Encoding.ASCII.GetString(_data, 0, numBytesRead));
                } while (ioStream.DataAvailable);
                string fullMessage = response.ToString();
                //IN CASE OF DATA IS LONGER THAN DATA LENGTH
                // String to store the response ASCII representation.
                JObject json = JObject.Parse(fullMessage);
                JToken header = json["header"];
                string verb = (string)header["verb"];
                if(verb == "NOTIFY")
                {
                    //SEND DATA REQUEST
                    SMERequest smeRequest = new SMERequest("SME/TCP-1.0", "LIST COMPANIES", 700, clientID, session);
                    string request = JsonConvert.SerializeObject(smeRequest, Formatting.Indented);
                    Console.WriteLine(request);
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(request);
                    ioStream.Write(data, 0, data.Length);
                    ioStream.Flush();
                    //GET RESPONSE AND UPDATE DATA
                    _data = new byte[256];
                    response = new StringBuilder();
                    do
                    {
                        var numBytesRead = ioStream.Read(_data, 0, _data.Length);
                        response.AppendFormat("{0}", Encoding.ASCII.GetString(_data, 0, numBytesRead));
                    } while (ioStream.DataAvailable);
                    fullMessage = response.ToString();
                    //IN CASE OF DATA IS LONGER THAN DATA LENGTH
                    // String to store the response ASCII representation.
                    StockCompanies = new List<Company>();
                    json = JObject.Parse(fullMessage);
                    JObject Data = JObject.Parse((string)json["Data"]);
                    JArray companies = (JArray)Data["stockCompanies"];

                    foreach (JObject company in companies)
                    {
                        Company newCompany = addCompany((string)company["symbol"], (string)company["name"], (double)(company["openPrice"]));
                        newCompany.lastSale = (double) company["currentPrice"];
                        JArray buyOrders = (JArray)company["buyOrders"];
                        JArray sellOrders = (JArray)company["sellOrders"];
                        JArray transactions = (JArray)company["transactions"];
                        foreach (JToken order in buyOrders)
                        {
                            newCompany.BuyOrders.Add(new BuyOrder((double)order["price"], (int)order["size"]));
                        }
                        foreach (JToken order in sellOrders)
                        {
                            newCompany.SellOrders.Add(new SellOrder((double)order["price"], (int)order["size"]));
                        }
                        foreach (JToken order in transactions)
                        {
                            newCompany.Transactions.Add(new SellOrder((double)order["price"], (int)order["size"]));
                        }
                    }
                    
                    Notify();
                }  
            }
        }

        public Company addCompany(String symbol, String _name, double price)
        {
           Company _company = new Company(symbol, _name, price, this);
           StockCompanies.Add(_company);
            return _company;
        }

        public List<Company> getCompanies()
        {
            return StockCompanies;
        }
        //SENDS BUYORDER REQUEST
        public void buyOrder(string company, int size, double price)
        {
            SMERequest smeRequest = new SMERequest("SME/TCP-1.0", "BUYORDER", 700, clientID, session, company, size, price);
            string request = JsonConvert.SerializeObject(smeRequest, Formatting.Indented);
            Console.WriteLine(request);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(request);
            ioStream.Write(data, 0, data.Length);
        }
        //SENDS SELLORDER REQUEST
        public void sellOrder(string company, int size, double price)
        {
            SMERequest smeRequest = new SMERequest("SME/TCP-1.0", "SELLORDER", 700, clientID, session, company, size, price);
            string request = JsonConvert.SerializeObject(smeRequest, Formatting.Indented);
            Console.WriteLine(request);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(request);
            ioStream.Write(data, 0, data.Length);
        }
    }
}
