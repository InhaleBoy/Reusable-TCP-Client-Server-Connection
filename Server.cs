using Godot;
using System.Collections.Generic;


namespace traiding.script{

    public partial class Server : Control{
        private PacketPeerUdp _peerUdp = new PacketPeerUdp();
        private TcpServer _peerTcp = new TcpServer();
        private List<StreamPeerTcp> _connections = new();
        private DBConnection _dbConnection = DBConnection.Instance();

        public Timer ChangeTimer;
        public RandomNumberGenerator Rng = new Godot.RandomNumberGenerator();

        public StockInformation StockInformation = new StockInformation{
            Money = [10],
            HighestNumber = 10
        };
        
        public int Port = 55556;
        public bool StartWorking;

        public override void _Ready(){
            SetProcess(false);
            SetPhysicsProcess(false);
        }

        public void _on_start_server_button_down(){
            Rng.Randomize();

            Error err = _peerTcp.Listen(55557);
            GD.Print(err," --- server conn status error");

            _peerUdp.SetBroadcastEnabled(true);
            _peerUdp.SetDestAddress("255.255.255.255", 4433);
            _peerUdp.Bind(Port);

            StartWorking = true;
            ChangeTimer = GetNode<Timer>("ChangeTimer");

            SetPhysicsProcess(true);
            SetProcess(true);   
        }

        public int TimeCount;
        public override void _PhysicsProcess(double delta){
            
            if (!StartWorking) return;
            TimeCount++;
            if (TimeCount != 60) return;
            TimeCount = 0;

            float change = Rng.RandfRange(-10,10);
            float price = StockInformation.Money[^1]+change;

            StockInformation.Money.Add(price <= 0.1?(float)0.1:price);

            if (StockInformation.Money.Count > 50) StockInformation.Money.RemoveAt(0);

            if (price > StockInformation.HighestNumber) {
                StockInformation.HighestNumber = price;
            }

            GD.Print(change," --- ",StockInformation.Money[^1]," --- ");
        }
    }

}