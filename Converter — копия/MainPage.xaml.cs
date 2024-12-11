
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Flurl.Http;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;

namespace Converter
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private ValuteItem _selectedMainValute;
        private ValuteItem _selectedSecondaryValute;
        private DateTime _selectedDate = DateTime.Now;
        private string _courseOnDay;
        private double _amountMainValute;
        private string _convValute;
        private ObservableCollection<ValuteItem> _valuteItems; 
        private Dictionary<DateTime, ObservableCollection<ValuteItem>> _savedList;

        public string apiUrl = "https://www.cbr-xml-daily.ru";
        public MainViewModel()
        {
            _valuteItems = new ObservableCollection<ValuteItem>();
            _savedList = new Dictionary<DateTime, ObservableCollection<ValuteItem>>();
            FillListValutes();
        }

        public ValuteItem SelectedSecondaryValute
        {
            get => _selectedSecondaryValute;
            set
            {
                if (_selectedSecondaryValute == value) return;
                _selectedSecondaryValute = value;
                OnPropertyChanged();
                ConvertValute();
            }
        }
        public ValuteItem SelectedMainValute
        {
            get => _selectedMainValute;
            set
            {
                if (_selectedMainValute == value) return;
                _selectedMainValute = value;
                OnPropertyChanged();
                ConvertValute();
            }
        }
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate == value) return;
                _selectedDate = value;
                OnPropertyChanged();
                FillListValutes();
            }
        }
        public string CourseOnDay
        {
            get => _courseOnDay;
            private set
            {
                if (_courseOnDay == value) return;
                _courseOnDay = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ValuteItem> Valutes 
        {
            get => _valuteItems;
            set
            {
                if (_valuteItems != value)
                {
                    _valuteItems = value;
                    OnPropertyChanged();
                }
            }
        }
        public double AmountMainValute
        {
            get => _amountMainValute;
            set
            {
                if (_amountMainValute == value) return;
                _amountMainValute = value;
                OnPropertyChanged();
                ConvertValute();
            }
        }

        public string ConvValute
        {
            get => _convValute;
            private set
            {
                if (_convValute == value) return;
                _convValute = value;
                OnPropertyChanged();
            }
        }
      
        public async Task<ValuteData> GetValuteOnDate(DateTime nowDate)
        {
            ValuteData valList = null;
            
            for (DateTime data = nowDate; data <= DateTime.Now && valList == null; data = data.AddDays(-1))
            {
                string Url = $"{apiUrl}/archive/{$"{data:yyyy/MM/dd}".Replace(".", "//")}/daily_json.js";
                try
                {
                    valList = await Url.GetJsonAsync<ValuteData>();
                  
                }
                catch (FlurlHttpException exception)
                {
                    if (exception.StatusCode != 404)
                    {
                        throw new Exception($"Error code: {exception.StatusCode}");
                    }
                }
                CourseOnDay = $"Курс на дату: {data:dd.MM.yyyy}";
            }
            if (valList != null)
                valList.Valute.Add("RUB", new ValuteItem
                {
                    CharCode = "RUB",
                    ID = "0",
                    Name = "Российский рубль",
                    Nominal = 1,
                    Value = 1.0
                });
            return valList;
        }

        public async void FillListValutes()
        {
            if (_savedList.ContainsKey(SelectedDate))
            {
                _valuteItems = _savedList[SelectedDate];
                
            }
            else {
                ValuteItem savedSecondary = SelectedSecondaryValute;
                ValuteItem savedMain = SelectedMainValute;
                ValuteData data = await GetValuteOnDate(SelectedDate);
                if (data == null) return;

                _valuteItems.Clear(); 

                foreach (var valuteItem in data.Valute.Values)
                {
                    if (valuteItem != null)
                    {
                        _valuteItems.Add(valuteItem);
                    
                    }
                }
                if (savedMain != null && savedSecondary!=null) {
                    SelectedMainValute = _valuteItems.FirstOrDefault<ValuteItem>(x => x.CharCode == savedMain.CharCode);
                    SelectedSecondaryValute = _valuteItems.FirstOrDefault<ValuteItem>(x => x.CharCode == savedSecondary.CharCode);
                }
                
                _savedList[SelectedDate] = new ObservableCollection<ValuteItem>(_valuteItems);
            }
            OnPropertyChanged(nameof(Valutes));
            
        }

        private void ConvertValute()
        {
            if (SelectedMainValute == null || SelectedSecondaryValute == null || AmountMainValute == 0)
            {
                ConvValute = "0";
                return;
            }

            double mainValuteRate = SelectedMainValute.Value / SelectedMainValute.Nominal;
            double secondaryValuteRate = SelectedSecondaryValute.Value / SelectedSecondaryValute.Nominal;

            ConvValute = ((AmountMainValute * mainValuteRate) / secondaryValuteRate).ToString("F4");
        }
       

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }


}
