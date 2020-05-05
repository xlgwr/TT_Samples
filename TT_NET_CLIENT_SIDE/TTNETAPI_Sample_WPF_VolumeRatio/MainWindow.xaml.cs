using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using tt_net_sdk;

namespace TTNETAPI_Sample_WPF_VolumeRatio
{
    #region TreeRow Implementation
    public interface ITreeRow : INotifyPropertyChanged
    {
        string ProductName { get; }
        string ContractAlias { get; }
        decimal Volume { get; }
        int Score { get; }
        decimal Ratio { get; }
        bool IsExpanded { get; set; }
    }

    public class ProductTreeRow : ITreeRow
    {
        public ProductTreeRow(Instrument instr)
        {
            m_productKey = instr.Product.Key;
            CreateOrGetExistingChild(instr);
        }

        #region Tree Column Properties
        public string ProductName => m_productKey.Name;
        public string ContractAlias => string.Empty;
        public decimal Volume => this.Children.Sum(x => x.Volume);
        public int Score => this.Children.Sum(x => x.Score);
        public decimal Ratio => (this.Volume == 0) ? 0 : this.Score / this.Volume;
        #endregion

        #region TreeViewItem Methods/Properties
        public ulong ProductId => m_productKey.ProductId;
        public ObservableCollection<ITreeRow> Children { get; } = new ObservableCollection<ITreeRow>();

        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                m_isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Public Methods
        public ContractTreeRow CreateOrGetExistingChild(Instrument instr)
        {
            var child = this.Children.FirstOrDefault(x => (x as ContractTreeRow).ContractId == instr.InstrumentDetails.Id);
            if (child == null)
            {
                this.Children.Add(new ContractTreeRow(this, instr));
                child = this.Children.Last();
            }

            return child as ContractTreeRow;
        }
        #endregion

        #region Private Members
        private readonly ProductKey m_productKey = ProductKey.Empty;
        private bool m_isExpanded = false;
        #endregion
    }

    public class ContractTreeRow : ITreeRow
    {
        public ContractTreeRow(ProductTreeRow parent, Instrument instr)
        {
            m_parent = parent;
            m_instrKey = instr.Key;
            IsExpanded = false;
        }

        #region Tree Column Properties
        public string ProductName => string.Empty;
        public string ContractAlias => m_instrKey.Alias;
        public decimal Volume => m_volume;
        public int Score => (m_numNewOrders * NEW_ORDER_MULTIPLIER) + 
                            (m_numModifications * MODIFY_MULTIPLIER) + 
                            (m_numCancellations * CANCEL_MULTIPLIER);
        public decimal Ratio => (this.Volume == 0) ? 0 : this.Score / this.Volume;
        #endregion

        #region TreeViewItem Methods/Properties
        public ProductTreeRow Parent => m_parent;
        public ulong ContractId => m_instrKey.InstrumentId;

        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                m_isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Public Methods
        public void ProcessMessage(OrderAddedEventArgs e)
        {
            m_numNewOrders++;
        }

        public void ProcessMessage(OrderUpdatedEventArgs e)
        {
            m_numModifications++;
        }

        public void ProcessMessage(OrderDeletedEventArgs e)
        {
            m_numCancellations++;
        }

        public void ProcessMessage(OrderFilledEventArgs e)
        {
            m_volume += e.Fill.Quantity.Value;
        }
        #endregion

        #region Private Members
        private static int NEW_ORDER_MULTIPLIER = 0;
        private static int MODIFY_MULTIPLIER = 1;
        private static int CANCEL_MULTIPLIER = 3;
        private decimal m_volume = 0;
        private int m_numNewOrders = 0;
        private int m_numModifications = 0;
        private int m_numCancellations = 0;

        private readonly InstrumentKey m_instrKey = InstrumentKey.Empty;
        private readonly ProductTreeRow m_parent = null;
        private bool m_isExpanded = false;
        #endregion
    }

    public class ProductList : ObservableCollection<ProductTreeRow>
    {
        public ProductList() : base() { }
    }
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProductList ProductListData = null;
        private TTAPI m_api = null;
        private tt_net_sdk.Dispatcher m_dispatcher = null;
        private TradeSubscription m_ts = null;
        private bool m_shutdownComplete = false;

        public MainWindow()
        {
            InitializeComponent();

            ProductListData = new ProductList();
            treeviewList.ItemsSource = ProductListData;

            var app = App.Current as TTNETAPI_Sample_WPF_VolumeRatio.App;
            m_dispatcher = app.SDKDispatcher;

            var app_key = "09b8c74f-fc06-92fe-deeb-c1e27b1cb1b6:c7688113-bef0-7981-986a-5131316cedb9";
            var env = ServiceEnvironment.ProdSim;
            var mode = TTAPIOptions.SDKMode.Client;
            var options = new TTAPIOptions(mode, env, app_key, 5000);
            TTAPI.CreateTTAPI(m_dispatcher, options, new ApiInitializeHandler(OnSDKInitialized));

            lblStatus.Text = "Initializing...";
        }

        #region SDK Events
        private void OnSDKInitialized(TTAPI api, ApiCreationException ex)
        {
            if (ex == null)
            {
                lblStatus.Text = "Initialized.  Authenticating...";
                m_api = api;
                m_api.TTAPIStatusUpdate += OnSDKStatusUpdate;
                TTAPI.ShutdownCompleted += OnSDKShutdownComplete;
                m_api.Start();
            }
            else if (!ex.IsRecoverable)
            {
                MessageBox.Show($"API Initialization Failed: {ex.Message}");
            }
        }

        private void OnSDKStatusUpdate(object sender, TTAPIStatusUpdateEventArgs e)
        {
            if (e.IsReady && m_ts == null)
            {
                lblStatus.Text = "Authenticated.  Launching subscriptions...";
                m_ts = new TradeSubscription(m_dispatcher, false);
                m_ts.OrderBookDownload += OnOrderBookDownload;
                m_ts.OrderAdded += OnOrderAdded;
                m_ts.OrderDeleted += OnOrderDeleted;
                m_ts.OrderFilled += OnOrderFilled;
                m_ts.OrderUpdated += OnOrderUpdated;
                m_ts.OrderPendingAction += OnOrderPendingAction;
                m_ts.OrderRejected += OnOrderRejected;
                m_ts.Start();
            }
            else if (e.IsDown)
            {
                lblStatus.Text = $"SDK is down: {e.StatusMessage}";
            }
        }

        private void OnSDKShutdownComplete(object sender, EventArgs e)
        {
            m_shutdownComplete = true;
            this.Close();
        }
        #endregion

        #region TradeSubscription Events
        private void OnOrderBookDownload(object sender, OrderBookDownloadEventArgs e)
        {
            lblStatus.Text = "Running";
        }

        private void OnOrderAdded(object sender, OrderAddedEventArgs e)
        {
            var row = GetContractRow(e.Order.Instrument);
            if (row != null)
            {
                row.ProcessMessage(e);
                CollectionViewSource.GetDefaultView(this.ProductListData).Refresh();
            }
        }

        private void OnOrderUpdated(object sender, OrderUpdatedEventArgs e)
        {
            var row = GetContractRow(e.NewOrder.Instrument);
            if (row != null)
            {
                row.ProcessMessage(e);
                CollectionViewSource.GetDefaultView(this.ProductListData).Refresh();
            }
        }

        private void OnOrderDeleted(object sender, OrderDeletedEventArgs e)
        {
            var row = GetContractRow(e.DeletedUpdate.Instrument);
            if (row != null)
            {
                row.ProcessMessage(e);
                CollectionViewSource.GetDefaultView(this.ProductListData).Refresh();
            }
        }

        private void OnOrderFilled(object sender, OrderFilledEventArgs e)
        {
            var row = GetContractRow(e.NewOrder.Instrument);
            if (row != null)
            {
                row.ProcessMessage(e);
                CollectionViewSource.GetDefaultView(this.ProductListData).Refresh();
            }
        }

        private void OnOrderRejected(object sender, OrderRejectedEventArgs e)
        {
            // Not Implemented 
        }

        private void OnOrderPendingAction(object sender, OrderPendingActionEventArgs e)
        {
            // Not Implemented 
        }
        #endregion

        #region Form Events
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!m_shutdownComplete)
            {
                e.Cancel = true;
                Shutdown();
            }
        }
        #endregion

        #region Private Methods
        private ContractTreeRow GetContractRow(Instrument instr)
        {
            if (instr.Key.MarketId != MarketId.CME)
            {
                return null;
            }

            var parent = this.ProductListData.FirstOrDefault(x => x.ProductId == instr.Product.Key.ProductId);
            if (parent == null)
            {
                this.ProductListData.Add(new ProductTreeRow(instr));
                parent = this.ProductListData.Last();
            }

            return parent.CreateOrGetExistingChild(instr);
        }

        private void Shutdown()
        {
            if (m_ts != null)
            {
                m_ts.OrderBookDownload -= OnOrderBookDownload;
                m_ts.OrderAdded -= OnOrderAdded;
                m_ts.OrderDeleted -= OnOrderDeleted;
                m_ts.OrderFilled -= OnOrderFilled;
                m_ts.OrderUpdated -= OnOrderUpdated;
                m_ts.OrderPendingAction -= OnOrderPendingAction;
                m_ts.OrderRejected -= OnOrderRejected;
                m_ts.Dispose();
                m_ts = null;
            }

            TTAPI.Shutdown();
        }
        #endregion
    }
}
