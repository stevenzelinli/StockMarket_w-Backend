using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Runtime.Serialization;

namespace StockExchangeMarket
{

    public partial class StockSecuritiesExchange : Form
    {
        
        //LOCAL DATA
        RealTimedata Subject;
        //NETWORK TOOLS
        TcpClient tcpClient;
        NetworkStream ioStream;
        string session, name;
        public StockSecuritiesExchange()
        {
            InitializeComponent();
            //CONNECT TO SERVER
            //tcpClient = new TcpClient("localhost",8000);
            //ioStream = tcpClient.GetStream();
        }
        /**'
         * Will be run in a separate thread and will handle reading and writing to the server. Clicking Join will start this thread.
         */
        private void StartNetworkManager()
        {
            //CONNECT TO SERVER
            tcpClient = new TcpClient(serverIP.Text, Int32.Parse(serverPort.Text));
            //GET IOSTREAM
            ioStream = tcpClient.GetStream();

            //SEND REGISTER REQUEST
            SMERequest smeRequest = new SMERequest("SME/TCP-1.0", "REGISTER", 700, clientID.Text);
            string request = JsonConvert.SerializeObject(smeRequest, Formatting.Indented);
            Console.WriteLine(request);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(request); 
            ioStream.Write(data, 0, data.Length);
            ioStream.Flush();
            //GET RESPONSE AND SESSION ID
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
            session = (string) json["session"];
            name = clientID.Text;
        }

        private void beginTradingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartNetworkManager();
            //new Thread(new ThreadStart(StartNetworkManager)).Start();
            // Create three stocks and add them to the market
            Subject = new RealTimedata(tcpClient, clientID.Text, session);

            // In this lab assignment we will add three companies only using the following format:
            // Company symbol , Company name , Open price

            //SEND DATA REQUEST
            SMERequest smeRequest = new SMERequest("SME/TCP-1.0", "LIST COMPANIES", 700, name, session);
            string request = JsonConvert.SerializeObject(smeRequest, Formatting.Indented);
            Console.WriteLine(request);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(request);
            ioStream.Write(data, 0, data.Length);
            ioStream.Flush();
            //GET RESPONSE AND UPDATE DATA
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
            JObject Data = JObject.Parse((string)json["Data"]);
            JArray companies = (JArray) Data["stockCompanies"];

            foreach(JToken company in companies)
            {
                Company newCompany = Subject.addCompany((string)company["symbol"], (string)company["name"], (double)(company["openPrice"]));
                newCompany.lastSale = (double)company["currentPrice"];
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

            this.watchToolStripMenuItem.Visible = true;
            this.ordersToolStripMenuItem.Visible = true;
            this.beginTradingToolStripMenuItem.Enabled = false;
            this.marketToolStripMenuItem.Text = "&Market <<Open>>";

            MarketDepthSubMenu(this.marketByOrderToolStripMenuItem1);
            MarketDepthSubMenu(this.marketByPriceToolStripMenuItem1);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcpClient.Close();
            ioStream.Close();
            this.Close();
        }

        private void StockStateSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StockStateSummary summaryObserver = new StockStateSummary(Subject);
            summaryObserver.MdiParent = this;

            // Display the new form.
            summaryObserver.Show();
        }
        private void cascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Cascade all MDI child windows.
            this.LayoutMdi(MdiLayout.Cascade);
        }

        private void arrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tile all child forms vertically.
            this.LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void horizontalTileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tile all child forms horizontally.
            this.LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void verticalTileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tile all child forms vertically.
            this.LayoutMdi(MdiLayout.TileVertical);

        }

        private void stopTradingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SEND DATA REQUEST
            SMERequest smeRequest = new SMERequest("SME/TCP-1.0", "UNREGISTER", 700, name, session);
            string request = JsonConvert.SerializeObject(smeRequest, Formatting.Indented);
            Console.WriteLine(request);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(request);
            ioStream.Write(data, 0, data.Length);
            ioStream.Flush();
            this.Close();
        }

        public void MarketDepthSubMenu(ToolStripMenuItem MnuItems)
        {
            ToolStripMenuItem SSSMenu;
            List<Company> StockCompanies = Subject.getCompanies();
            foreach (Company company in StockCompanies)
            {
                if (MnuItems.Name == "marketByPriceToolStripMenuItem1")
                    SSSMenu = new ToolStripMenuItem(company.Name, null, marketByPriceToolStripMenuItem_Click);
                else
                    SSSMenu = new ToolStripMenuItem(company.Name, null, marketByOrderToolStripMenuItem_Click);
                MnuItems.DropDownItems.Add(SSSMenu);
            }
        }

        public void marketByOrderToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
           
            MarketByOrder newMDIChild = new MarketByOrder(Subject, sender.ToString());
            // Set the parent form of the child window.
            newMDIChild.Text = "Market Depth By Order (" + sender.ToString() + ")";
            newMDIChild.MdiParent = this;
            // Display the new form.
            newMDIChild.Show();
        }
        private void marketByPriceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MarketByPrice newMDIChild = new MarketByPrice(Subject, sender.ToString());
            // Set the parent form of the child window.

            newMDIChild.Text = "Market Depth By Price (" + sender.ToString() + ")";

            newMDIChild.MdiParent = this;
            // Display the new form.
            newMDIChild.Show();
        }

        private void bidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlaceBidOrder newMDIChild = new PlaceBidOrder(Subject);
            // Set the parent form of the child window.
            newMDIChild.MdiParent = this;
            // Display the new form.
            newMDIChild.Show();
        }

        private void askToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlaceSellOrder newMDIChild = new PlaceSellOrder(Subject);
            // Set the parent form of the child window.
            newMDIChild.MdiParent = this;
            // Display the new form.
            newMDIChild.Show();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
