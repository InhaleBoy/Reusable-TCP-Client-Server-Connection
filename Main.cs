using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace traiding.script{
    public partial class Main : Node2D{
        public LineEdit your_money;
        public LineEdit much;
        public LineEdit current_price;
        public LineEdit shares;

        public Client Client;
        public Graph Graph;
        public Control Ladder;

        public bool connection_established = false;
        public long ID;
        public StockInformation information;
        

        public override void _Ready(){
            Ladder = GetNode<Control>("Ladder");
            if (Ladder is null) GD.Print("Graph not found");
            Graph = GetNode<Graph>("Graph");
            if (Graph is null) GD.Print("Graph not found");
            Client = GetNode<Client>("Client");
            if (Client is null) GD.Print("Client not found");

            your_money = (LineEdit)GetNode("Main Butons/Money");
            if (your_money is null) {GD.Print("No place your money");}
            much = (LineEdit)GetNode("Main Butons/Much");
            if (much is null) {GD.Print("No place much");}
            current_price = (LineEdit)GetNode("Main Butons/CurrentPrice");
            if (current_price is null) {GD.Print("No place current price");}
            shares = (LineEdit)GetNode("Main Butons/Shares");
            if (shares is null) {GD.Print("No place shares");}

            SetProcess(false);
        }


        public override void _Process(double delta){
            InformationProcessorUDP(Client.GetInformationUDP());
            InformationProcessorTCP(Client.GetInformationTCP());
        }

        public void InformationProcessorTCP(Packet msg){
            if ( msg is null ) return;
            switch(msg.Option){
                case PacketOptions.BUY_ASV : 
                case PacketOptions.SELL_ASV : {
                    BuySellResponseHandler(msg);
                    return;
                }
                default : {
                    GD.Print(" UDP packet was wrong !!! ");
                    return;
                }
            }
        }

        public void InformationProcessorUDP(Packet msg){
            if ( msg is null ) return;
            if (msg.Option == PacketOptions.UPDATE) {
                information = msg.StockInformation;

                current_price.Text = Graph.money[^1].ToString(CultureInfo.CurrentCulture);
                shares.Text = Graph.shares.ToString(CultureInfo.CurrentCulture);
                
                Graph.Update(information.Money,information.HighestNumber);
                LadderUpdate(msg.TOP);
            }
        }

        public void BuySellResponseHandler(Packet msg){
            GD.Print("Transaction Established");
            SetPlayerInformation(msg.PlayerInformation);
            Graph.updateHelpLine(
                msg.Option == PacketOptions.BUY_ASV ? HelperLine.HelperLineOptions.add : HelperLine.HelperLineOptions.delete
            );
        }

        public void SetPlayerInformation(PlayerInformation info){
            much.Text = "0";
            your_money.Text = info.Money.ToString(CultureInfo.CurrentCulture);
            shares.Text = info.OwnedShares.ToString(CultureInfo.CurrentCulture);
            Graph.Update(info.OwnedShares);
        }

        public void TransactionHandler(PacketOptions option){
            if (!(option == PacketOptions.BUY || option == PacketOptions.SELL)){
                GD.Print("The dumbasses want to kill SB");
                return;
            }

            float howmuch;

            try{
                howmuch = float.Parse(much.Text);
            }
            catch (Exception){
                GD.Print("We cant do it :<<<");
                return;
            }

            Client.TCPDataSend(new Packet{
                Option = option,
                HowMuch = howmuch,
                ID = ID,
                StockInformation = new StockInformation{
                    Money = Graph.money,
                    HighestNumber = Graph.highest_number
                },  
            });
        }

        public void LadderUpdate(Dictionary<string,float> ladder){
            TextEdit LadderText = Ladder.GetNode<TextEdit>("LadderText");
            LadderText.Text = "";

            foreach(var rec in ladder){
                LadderText.Text += $"{rec.Key} : {rec.Value} \n";
            }
        }

        public void _on_ladder_but_button_down(){
            TextEdit LText = Ladder.GetNode<TextEdit>("LadderText");
            LText.Visible = !LText.Visible;
        }

        public void _on_buy_pressed(){
            GD.Print("Buy");
            TransactionHandler(PacketOptions.BUY);
        }

        public void _on_sell_pressed(){
            GD.Print("Sell");
            TransactionHandler(PacketOptions.SELL);
        }
    }
}

