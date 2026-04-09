using Godot;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace traiding.script{
    public partial class Client : Control
    {
        public int port = 4433;
        
        public PacketPeerUdp peerUdp = new PacketPeerUdp();
        public StreamPeerTcp peerTcp = new StreamPeerTcp();


        public Packet GetInformationUDP(){
            if (peerUdp.GetAvailablePacketCount() <= 0) return null;
            string packet = peerUdp.GetPacket().GetStringFromUtf8();
            
            return JsonSerializer.Deserialize<Packet>(packet);
        }

        public Packet GetInformationTCP(){
            if (peerTcp.GetStatus() != StreamPeerTcp.Status.Connected || peerTcp.GetAvailableBytes() <= 0) return null;
            return JsonSerializer.Deserialize<Packet>(peerTcp.GetUtf8String(peerTcp.GetAvailableBytes()));
        }

        public void TCPDataSend(object data){
            Error err = peerTcp.PutData(JsonSerializer.Serialize(data).ToUtf8Buffer());
            if (err != Error.Ok){
                GD.Print(" --- sending error : status ",err);
            }
        }

        public override void _Ready()
        {
            SetProcess(false);
        }

        public override void _Process(double delta)
        {
            peerTcp.Poll();
            if (peerTcp.GetStatus() == StreamPeerTcp.Status.None || peerTcp.GetStatus() == StreamPeerTcp.Status.Error){
                GetParent<Main>().SetProcess(false);
                SetProcess(false);
                Visible = true;

                peerTcp.DisconnectFromHost();
                _on_connect_button_down();
            }
        }






        // Fucked up Connection System taht just works tho
        public async void _on_connect_button_down()
        {
            if (peerTcp.GetStatus() != StreamPeerTcp.Status.None) return;

            string IP = GetNode<LineEdit>("SERVER IP").Text;

            if (peerTcp.ConnectToHost(IP != "" ? IP : "127.0.0.1", 55557) != Error.Ok)
            {
                GD.Print("TCP SERVER NOT EXISTANT");
                peerTcp.DisconnectFromHost();
                return;
            }

            if (!await awaitTCPConnection()) return;

            Packet packet = await awaitTCPAUTHResponse();
            if (packet == null) return;

            AUTHPacketHandler(packet);

            peerUdp.Bind(port);
        }

        private async Task<bool> awaitTCPConnection(){
            peerTcp.Poll();
            return await Task.Run(() => {
                    for ( int i = 0; i < 8; i++) {
                        GD.Print("Connecting ... ", peerTcp.GetStatus());
                        Thread.Sleep(500);
                        if (peerTcp.GetStatus() == StreamPeerTcp.Status.Connected) return true;
                        peerTcp.Poll();
                    }
                    GD.Print("Nie ma takiego numeru :<<<<<<<");
                    peerTcp.DisconnectFromHost();
                    return false;
            });
        }

        private async Task<Packet> awaitTCPAUTHResponse(){
            string name = GetNode<LineEdit>("Nazwa").Text;
            string pass = GetNode<LineEdit>("Haslo").Text;

            TCPDataSend( new Packet {
                    Option = PacketOptions.AUTH,
                    PlayerInformation = new PlayerInformation{
                        Name = name,
                        Password = pass,
                    }
                });

            return await Task.Run(() => {
                Packet packet = null;
                int count = 0;
                while (packet is null) {
                    packet = GetInformationTCP();
                    count++;
                    if (count >= 50) {
                        peerTcp.DisconnectFromHost();
                        return null;
                    }
                    Thread.Sleep(200);
                }
                GD.Print(" -- Packet Found In Ether -- ");
                return packet;
            });
        }

        private void AUTHPacketHandler(Packet packet){
            if (packet.Option == PacketOptions.AUTH_ASV_POSITIVE){
                GetParent<Main>().ID = packet.ID;
                GetParent<Main>().SetPlayerInformation(packet.PlayerInformation);
                GD.Print(peerUdp.GetAvailablePacketCount(), " -- packet count for client udp on start");
                GetParent<Main>().SetProcess(true);
                SetProcess(true);
                Hide();
                return;
            }

            if(packet.Option == PacketOptions.AUTH_ASV_NEGATIVE) {
                // Add something when negative - preferably with dragons :)
                GD.Print(" You might be a dragon ");
            }
        }
    }
}

