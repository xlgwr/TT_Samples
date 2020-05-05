using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using tt_net_sdk;

namespace TTNETAPI_Sample_WPF_VolumeRatio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public tt_net_sdk.Dispatcher SDKDispatcher { get; set; }
    }

    public class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //var app_key = "53d632a1-fcb2-81e2-cbac-a715034472ff:4ac9c355-24a3-d3e9-fe54-df374657e2d9";
            //var env = ServiceEnvironment.ProdSim;
            //var mode = TTAPIOptions.SDKMode.Client;
            //var options = new TTAPIOptions(mode, env, app_key, 5000);
            using (var disp = Dispatcher.AttachUIDispatcher())
            {
                TTNETAPI_Sample_WPF_VolumeRatio.App app = new TTNETAPI_Sample_WPF_VolumeRatio.App();
                app.SDKDispatcher = disp;
                app.InitializeComponent();
                app.Run();
            } 
        }
    }
}


