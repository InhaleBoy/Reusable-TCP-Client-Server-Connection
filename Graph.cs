using Godot;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace traiding.script{
    public partial class Graph : Node2D{
        public Line2D graph;
        public Node2D helper_lines;

        // Those are grabbed from generator
        public float shares;
        public List<float> money = [10];
        public float highest_number;

        public float graph_top = 120;
        public float graph_bottom = 470;
        public float graph_left = 0;
        public float graph_right = 300;

        public PackedScene HelperLineScene = GD.Load("res://helper_line.tscn") as PackedScene;

        public override void _Ready(){
            graph = GetNode("Line2D") as Line2D;
            if (graph is null) {GD.Print("No graph");}
            helper_lines = (Node2D)GetNode("HelpLines");
            if (helper_lines is null) {GD.Print("No helper lines");}
        }

        public void Update(float Shares){
            shares = Shares;
            drawGraph();
            updateHelpLine(HelperLine.HelperLineOptions.update);
        }

        public void Update(List<float> Money, float HighestNumber){
            money = Money;
            highest_number = HighestNumber;
            drawGraph();
            updateHelpLine(HelperLine.HelperLineOptions.update);
        }

        public void drawGraph(){
            graph.ClearPoints();
            float size = graph_right / money.Count;

            for ( int i = 0; i < money.Count ; i++ ){
                var value = graph_bottom - (money[i] * 0.9 / highest_number * (graph_bottom-graph_top));
                graph.AddPoint(new Vector2(i*size,(float)value));
            }
        }

        public void updateHelpLine(HelperLine.HelperLineOptions action, [Optional] float price){

            switch (action)
            {
                case HelperLine.HelperLineOptions.add : 
                case HelperLine.HelperLineOptions.update : {

                    if (action == HelperLine.HelperLineOptions.add) { 
                        // cen i make it better ?
                        updateHelpLine(HelperLine.HelperLineOptions.delete);
                        
                        HelperLine helpline = (HelperLine)HelperLineScene.Instantiate();
                        helpline.price = money[^1];
                        helper_lines.AddChild(helpline);
                    }

                    foreach (HelperLine helpline in helper_lines.GetChildren()) {
                        helpline.Position = new Vector2(
                            graph_right - 120,
                            (float)(graph_bottom - (helpline.price * 0.9 / highest_number * (graph_bottom-graph_top)))
                        );
                    }
                    break;
                }
                case HelperLine.HelperLineOptions.delete : {
                    // This was not made for use with multiple instances
                    // if nedded -> rewrite

                    foreach (Node2D node in helper_lines.GetChildren()) {
                        node.QueueFree();
                    }

                    break;
                }
            }
        }
    }
}

