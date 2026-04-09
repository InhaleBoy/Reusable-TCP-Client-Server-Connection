using Godot;
using System.Text.Json;
using System.Threading.Tasks;


namespace traiding.script{
    public partial class Server {
        public async override void _Process(double delta){
            _peerUdp.PutPacket(JsonSerializer.Serialize(new Packet{
                Option = PacketOptions.UPDATE,
                StockInformation = StockInformation,
                TOP = _dbConnection.GetLadder()
            }).ToUtf8Buffer());

            StreamPeerTcp connection = _peerTcp.TakeConnection();
            if (connection is not null && !_connections.Contains(connection)){
                _connections.Add(connection);
                GD.Print(_connections.Count," --- connections count");
            }

            for (int i = 0; i < _connections.Count; i++) {
                StreamPeerTcp conn = _connections[i];
                conn.Poll();
                if (conn.GetStatus() != StreamPeerTcp.Status.Connected) {
                    GD.Print(" Host Disconnected ");
                    conn.DisconnectFromHost();
                    _connections.RemoveAt(i);
                }
            }

            foreach (StreamPeerTcp conn in _connections){
                int avBytes = conn.GetAvailableBytes();
                if (avBytes <= 0) continue;
                GD.Print("Packet aquired");
                await Task.Run(() => {
                    PacketHandler(JsonSerializer.Deserialize<Packet>(conn.GetUtf8String(avBytes)),conn);
                });
            }
        }

        public void PacketHandler(Packet packet, StreamPeerTcp conn){
            switch (packet.Option){
                case PacketOptions.AUTH:{
                    GD.Print(" == AUTH == ");

                    PlayerInformation playerInformation = packet.PlayerInformation;
                    if (!_dbConnection.IsConnected()) return;
                    if (!_dbConnection.AuthCheck(playerInformation.Name, playerInformation.Password)){
                        GD.Print("There might be dragons trying to connect");
                        conn.PutData(JsonSerializer.Serialize(new Packet{
                            Option = PacketOptions.AUTH_ASV_NEGATIVE,
                        }).ToUtf8Buffer());
                        return;
                    }

                    if (!_dbConnection.IsConnected()) return;
                    Packet record = _dbConnection.AuthResponsePacket(playerInformation.Name,playerInformation.Password);
                    conn.PutData(JsonSerializer.Serialize( record ).ToUtf8Buffer());
                    return;
                }

                case PacketOptions.BUY:{

                    if (packet.HowMuch <= 0) return;
                    PlayerInformation plrinfo = _dbConnection.GetPlayerInformation(packet.ID);
                    if (plrinfo.Money <= 0) return;


                    if ((packet.StockInformation.Money[^1] * packet.HowMuch) > plrinfo.Money){
                        packet.HowMuch = plrinfo.Money / packet.StockInformation.Money[^1];
                    }

                    _dbConnection.TransactionHandler(packet.ID,-(packet.StockInformation.Money[^1] * packet.HowMuch),packet.HowMuch);

                    GD.Print($" [BUY] Price per share : {packet.StockInformation.Money[^1]} | Shares Bought now : {packet.HowMuch} | Shares now : {plrinfo.OwnedShares} | Money now : {plrinfo.Money}");
                    conn.PutData(JsonSerializer.Serialize(new Packet{
                        Option = PacketOptions.BUY_ASV,
                        PlayerInformation = _dbConnection.GetPlayerInformation(packet.ID)
                    }).ToUtf8Buffer());
                    return;
                }

                case PacketOptions.SELL:{

                    if (packet.HowMuch <= 0) return;
                    PlayerInformation plrinfo = _dbConnection.GetPlayerInformation(packet.ID);
                    if (plrinfo.OwnedShares <= 0) return;

                    if (packet.HowMuch > plrinfo.OwnedShares){
                        packet.HowMuch = plrinfo.OwnedShares;
                    }

                    _dbConnection.TransactionHandler(packet.ID,packet.StockInformation.Money[^1] * packet.HowMuch,-packet.HowMuch);

                    GD.Print($" [SELL] Price per share : {packet.StockInformation.Money[^1]} | Shares Bought now : {packet.HowMuch} | Shares now : {plrinfo.OwnedShares} | Money now : {plrinfo.Money}");
                    conn.PutData(JsonSerializer.Serialize(new Packet{
                        Option = PacketOptions.SELL_ASV,
                        PlayerInformation = _dbConnection.GetPlayerInformation(packet.ID)
                    }).ToUtf8Buffer());
                    return;
                }
            }
        }
    }
}