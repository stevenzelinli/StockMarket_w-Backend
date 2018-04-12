using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Threading;
using System.Runtime.Serialization;

namespace StockExchangeMarket
{
    [DataContract]
    class SMERequest
    {
        //HEADER OBJECT
        [DataContract]
        class Header
        {
            [DataMember]
            string protocol;
            [DataMember]
            string verb;
            public Header(string protocol, string verb)
            {
                var nam = protocol;
                this.protocol = protocol;
                this.verb = verb;
            }
        }
        //ORDER (BUY & SELL)
        [DataContract]
        class OrderRequest
        {
            [DataContract]
            class OrderObj
            {
                [DataMember]
                int size;
                [DataMember]
                double price;
                public OrderObj(int size, double price)
                {
                    this.size = size;
                    this.price = price;
                }
            }
            [DataMember]
            string company;
            [DataMember]
            OrderObj order;
            public OrderRequest(string company, int size, double price)
            {
                this.company = company;
                this.order = new OrderObj(size, price);
            }
        }
        //OBJECT VARS
        [DataMember]
        Header header;
        [DataMember]
        int CSeq;
        [DataMember]
        string ID;
        [DataMember]
        string session;
        [DataMember]
        OrderRequest Data;
        //NO DATA OR SESSION
        public SMERequest(string protocol, string verb, int CSeq, string ID)
        {
            this.header = new Header(protocol, verb);
            this.CSeq = CSeq;
            this.ID = ID;
        }
        //NO DATA
        public SMERequest(string protocol, string verb, int CSeq, string ID, string session)
        {
            this.header = new Header(protocol, verb);
            this.CSeq = CSeq;
            this.session = session;
            this.ID = ID;
        }
        //WITH DATA
        public SMERequest(string protocol, string verb, int CSeq, string ID, string session, string company, int size, double price)
        {
            this.header = new Header(protocol, verb);
            this.CSeq = CSeq;
            this.session = session;
            this.ID = ID;
            this.Data = new OrderRequest(company, size, price);
        }
    }
}
