using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using SQLite;
using System.Windows.Input;
using System.IO;

namespace mvvm_and_database
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            BindingContext = new MainPageBinding();
            InitializeComponent();
        }
    }
    class MainPageBinding : INotifyPropertyChanged
    {
        string mockConnectionString;
        public MainPageBinding()
        {
            ClearCommand = new Command(OnClear);
            QueryCommand = new Command(OnQuery);

            makeFreshDatabaseForDemo();
        }

        private void makeFreshDatabaseForDemo()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mvvm_and_database");
            Directory.CreateDirectory(appData);
            mockConnectionString = Path.Combine(appData, "MyDatabase.db");

            if (File.Exists(mockConnectionString)) File.Delete(mockConnectionString);

            using (var cnx = new SQLiteConnection(mockConnectionString))
            {
                cnx.CreateTable<Record>();
                for (int i = 0; i < 5; i++)
                {
                    cnx.Insert(new Record { Description = $"Item {i}" });
                }
            }
        }

        public ObservableCollection<Record> Recordset { get; } = new ObservableCollection<Record>();
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand ClearCommand { get; private set; }
        private void OnClear(object o)
        {
            Recordset.Clear();
        }
        public ICommand QueryCommand { get; private set; }
        private void OnQuery(object o)
        {
            Recordset.Clear();
            List<Record> queryResult;
            using (var cnx = new SQLiteConnection(mockConnectionString))
            {
                queryResult = cnx.Query<Record>("SELECT * FROM items");
                foreach (var record in queryResult)
                {
                    Recordset.Add(record);
                }
            }
        }
    }

    [Table("items")]
    class Record
    {
        [PrimaryKey]
        public string guid { get; set; } = Guid.NewGuid().ToString().Trim().TrimStart('{').TrimEnd('}');
        public string Description { get; set; } = string.Empty;
        public override string ToString() => Description;
    }
}
