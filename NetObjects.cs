using System.Collections.Generic;

namespace traiding.script{

    public class Packet{
        public required PacketOptions Option { get; set; }
        public long ID { get; set; }
        public PlayerInformation PlayerInformation  { get; set; }
        public StockInformation StockInformation { get; set; }
        public Dictionary<string,float> TOP { get; set; }
        public float HowMuch { get; set; }
    }

    public enum PacketOptions{
        AUTH,
        AUTH_ASV_POSITIVE,
        AUTH_ASV_NEGATIVE,
        UPDATE,
        BUY,
        BUY_ASV,
        SELL,
        SELL_ASV
    }

    public class PlayerInformation{
        public string Name { get; set; }
        public string Password  { get; set; }
        public float Money  { get; set; }
        public float OwnedShares { get; set; }
    }

    /// <summary>
    /// UDP Packet sent from Serever to Everybody to get information about current stock prices
    /// </summary>
    public class StockInformation{
        public List<float> Money { get; set; }
        public float HighestNumber { get; set; }
    }
}

