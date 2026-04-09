using System.Globalization;
using Godot;

namespace traiding{
    public partial class HelperLine : Line2D
    {
        [Export] public float price;

        public enum HelperLineOptions
        {
            add,
            delete,
            update
        }

        public override void _Ready(){
            (GetNode("Label") as Label).Text = price.ToString(CultureInfo.CurrentCulture);
        }
    }
}

