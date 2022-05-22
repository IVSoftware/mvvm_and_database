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
using System.Runtime.CompilerServices;

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
            EditCommand = new Command(OnEdit);
            AddCommand = new Command(OnAdd);
            DeleteSelectedCommand = new Command(OnDeleteSelected);

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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


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

        Record _SelectedItem = null;
        public Record SelectedItem
        {
            get => _SelectedItem;
            set
            {
                bool changed;
                if ((value == null) ^ (_SelectedItem == null))
                {
                    changed = true;
                }
                else if (value == null)
                {
                    changed = false;
                }
                else changed = !_SelectedItem.Equals(value);
                if (changed)
                {
                    _SelectedItem = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand EditCommand { get; private set; }
        private void OnEdit(object o)
        {
            if(SelectedItem != null)
            {
                // Some kind of UI interaction that changes the bound record
                SelectedItem.Description = $"{SelectedItem.Description} (Edited)";

                // You'll need to decide what kind of UI action merits a SQL
                // update, but when you're ready to do that here's the command:
                using (var cnx = new SQLiteConnection(mockConnectionString))
                {
                    cnx.Update(SelectedItem);
                }
            }
        }

        public ICommand AddCommand { get; private set; }
        private async void OnAdd(object o)
        {
            var result = await App.Current.MainPage.DisplayPromptAsync("New Item", "Describe the item");
            if(!string.IsNullOrWhiteSpace(result))
            {
                var newRecord = new Record { Description = result };
                Recordset.Add(newRecord);

                using (var cnx = new SQLiteConnection(mockConnectionString))
                {
                    cnx.Insert(newRecord);
                }
            }
        }
        public ICommand DeleteSelectedCommand { get; private set; }
        private void OnDeleteSelected(object o)
        {
            if (SelectedItem != null)
            {
                var removed = SelectedItem;
                Recordset.Remove(SelectedItem);
                // You'll need to decide what kind of UI action merits a SQL
                // update, but when you're ready to do that here's the command:
                using (var cnx = new SQLiteConnection(mockConnectionString))
                {
                   cnx.Delete(removed);
                }
            }
        }
    }

    [Table("items")]
    class Record : INotifyPropertyChanged
    {
        [PrimaryKey]
        public string guid { get; set; } = Guid.NewGuid().ToString().Trim().TrimStart('{').TrimEnd('}');

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        string _Description = string.Empty;
        public string Description
        {
            get => _Description;
            set
            {
                if(_Description != value)
                {
                    _Description = value;
                    OnPropertyChanged();
                }
            }
        }
        public override string ToString() => Description;
    }
}
