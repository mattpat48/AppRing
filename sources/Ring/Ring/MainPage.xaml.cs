using MQTTnet;
using MQTTnet.Server;

namespace Ring
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnMessageClicked(object sender, EventArgs e)
        {
            msgButton.Text = "Opening";
            msgButton.IsEnabled = false;
        }
    }

}