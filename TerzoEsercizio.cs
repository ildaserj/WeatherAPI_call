using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using System.ServiceProcess;
using System.IO;
using System.Timers;
using System.Net.Http;
using System.Configuration;
using Newtonsoft.Json;


namespace TerzoEsercizio
{
    public partial class TerzoEsercizio : ServiceBase
    {
        Timer timer;

        int numberOfPlace = (Int32.Parse(ConfigurationManager.AppSettings["NumberOfPlace"]));

        List<Place> places = new List<Place>();
        List<string> appconfig =new List<string> { "FirstPlace", "SecondPlace", "ThirdPlace", "FourthPlace", "FifthPlace"};

        public TerzoEsercizio()
        {
            InitializeComponent();      
        }


        protected override void OnStart(string[] args)
        { 
            WriteLogfile("Service is started") ;

            timer = new Timer
            {
                Interval = (Int32.Parse(ConfigurationManager.AppSettings["TimeIntervall"]) * 60 * 1000)
            };
            for (int i = 0; i < numberOfPlace; i++)
            {
                AddPlace(i);
            }
            
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
  
            timer.Start();
            
        }
        public void AddPlace(int i)
        {
            places.Add(new Place(ConfigurationManager.AppSettings[appconfig[i].ToString()]));
        }
        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            try
            { 
                timer.Stop();

                for(int i = 0; i < numberOfPlace; i++)
                {
                    GenerateWeatherInfo(sender, e, places[i]);
                    GenerateWeatherInfoForecast(places[i]) ;
                } 
            }
            catch (Exception ex)
            {
                WriteLogfile(e.ToString() ) ;
            }
            finally
            {
                timer.Start();
            }
        }
        private void WriteLogfile(string message)
        {
            StreamWriter sw = null;
            sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\WindDataLogFile.txt", true); //AppDomain.CurrentDomain.BaseDirectory return [Application.exe location]
            sw.WriteLine($"{DateTime.Now.ToString()} : {message}");
            sw.Flush();
            sw.Close();
        }

        private void GenerateWeatherInfo(object sender, ElapsedEventArgs e, Place place_)
        {
            string APIKey = "1c94af854cd256475701cd4d0ed5f520";
            string url = string.Format("https://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}", place_.name, APIKey);
            try
            {
                HttpClient client = new HttpClient();
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = content.ReadAsStringAsync().Result;
                        WeatherAPI weatherInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherAPI>(json);
                        place_.currentWeather = weatherInfo;
                        WriteLogfile($"{place_.name} Current Wind Speed and Temperature: {place_.currentWeather.wind.speed}, {place_.currentWeather.main.temp}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogfile(ex.ToString());
            }
        }

        private void GenerateWeatherInfoForecast( Place place_)
        {
            Coord coord = new Coord();
            coord.lat = place_.currentWeather.coord.lat;
            coord.lon = place_.currentWeather.coord.lon;
            string APIKey = "0a0a67a57038fede56852d44452f4928";
            string url = string.Format("https://api.openweathermap.org/data/2.5/onecall?lat={0}&lon={1}&exclude=current,minutely,hourly,alerts&units=metric&appid={2}", coord.lat, coord.lon, APIKey);
            HttpClient client = new HttpClient();
            using (HttpResponseMessage response = client.GetAsync(url).Result)
            {
                using (HttpContent content = response.Content)
                {
                    var json = content.ReadAsStringAsync().Result;
                    WeatherForcastAPI weatherForcastAPI = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherForcastAPI>(json) ; //jsonSerialize
                    place_.forcastWeather = weatherForcastAPI;
                    printForecastWindTemp3NextDay(weatherForcastAPI);
                }
            }
      

            void printForecastWindTemp3NextDay(WeatherForcastAPI weatherForcastAPI)
            {
                WriteLogfile($"{place_.name} Forecast Wind Speed NEXT 3 DAYS: { weatherForcastAPI.daily[0].wind_speed } ; {weatherForcastAPI.daily[1].wind_speed}; {weatherForcastAPI.daily[2].wind_speed}");

                WriteLogfile($"{place_.name} Forecast Temperature NEXT 3 DAYS: {weatherForcastAPI.daily[0].temp.day}; {weatherForcastAPI.daily[1].temp.day}; {weatherForcastAPI.daily[2].temp.day}");
                    
            }
        }
        
        protected override void OnStop()
        {
            WriteLogfile("Service is stopped");  
        }
    }

    public class Place
    {
        public string name;
        public WeatherAPI currentWeather;
        public WeatherForcastAPI forcastWeather;

        public Place(string name)
        {
            this.name = name;
            currentWeather = new WeatherAPI();
            forcastWeather = new WeatherForcastAPI();
        }
    }
    public class WeatherForcastAPI
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public string timezone { get; set; }
        public int timezone_offset { get; set; }
        public List<Daily> daily { get; set; }

        public WeatherForcastAPI()
        {
            lat = 0.0;
            lon = 0.0;
            timezone = null;
            timezone_offset = 0;
            daily = new List<Daily>();
        }
    }

    public class WeatherAPI
    {
        public Coord coord { get; set; }
        public List<Weather> weather { get; set; }
        public string @base { get; set; }
        public Main main { get; set; }
        public int visibility { get; set; }
        public Wind wind { get; set; }
        public Clouds clouds { get; set; }
        public long dt { get; set; }
        public Sys sys { get; set; }
        public int timezone { get; set; }
        public long id { get; set; }
        public string name { get; set; }
        public int cod { get; set; }

        public WeatherAPI()
        {
            coord = new Coord();
            weather = new List<Weather>();
            @base = null;
            main = new Main();
            visibility = 0;
            wind = new Wind();
            clouds = new Clouds();
            dt = 0;
            sys = new Sys();
            timezone = 0;
            id = 0;
            name = null;
            cod = 0;
        }
    }
    public class Coord
    {
        public double lon { get; set; }
        public double lat { get; set; }

        public Coord()
        {
            lon = 0.0;
            lat = 0.0;
        }
    }

    public class Weather
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }

        public Weather()

        {
            this.id = 0;
            this.main = null;
            this.description = null;
            this.icon = null;
        }
    }
    public class Main
    {
        public double temp { get; set; }
        public double feels_like { get; set; }
        public double temp_min { get; set; }
        public double temp_max { get; set; }
        public double pressure { get; set; }
        public int humidity { get; set; }
        public double sea_level { get; set; }
        public double grnd_level { get; set; }

        public Main()
        {
            this.temp = 0.0;
            this.feels_like = 0.0;
            this.temp_min = 0.0;
            this.temp_max = 0.0;
            this.pressure = 0.0;
            this.humidity = 0;
            this.sea_level = 0.0;
            this.grnd_level = 0.0;
        }
    }

    public class Wind
    {
        public double speed { get; set; }
        public double deg { get; set; }
        public double gust { get; set; }

        public Wind(double _speed, double _deg, double _gust)

        {
            this.speed = _speed;
            this.deg = _deg;    
            this.gust = _gust;
        }
        public Wind()
        {
            speed = 0.0;
            deg = 0.0;
            gust = 0.0;
        }
    }
    public class Clouds
    {
        public int all { get; set; }
        public Clouds()
        {
            all = 0;
        }
    }
    public class Rain
    {
        public double h1 { get; set; }

        public double h3 { get; set; }
        
        public Rain()
        {
         h1 = 0.0;
         h3 = 0.0;
        }
    }

    public class Snow
    {
        public double h1 { get; set; }
        public double h3 { get; set; }
        public Snow()
        {
            h1 = 0.0;
            h3= 0.0;
        }
    }

    public class Sys
    {
        public int type { get; set; }
        public long id { get; set; }
        public double message { get; set; }
        public string country { get; set; }
        public long sunrise { get; set; }
        public long sunset { get; set; }

        public Sys()
        {
            type = 0;
            id = 0;
            message = 0.0;
            country = null;
            sunrise = 0;
            sunset = 0;
        }
    }
    public class Daily
    {
        public long dt { get; set; }
        public long sunrise { set; get; }
        public long sunset { set; get; }
        public long moonrise { set; get; }
        public long moonset { set; get; }
        public float moon_phase { set; get; }
        public Temp temp { get; set; }
        public Feels_Like feels_like { get; set; }
        public int pressure { get; set; }
        public int humidity { get; set; }
        public float dew_point { get; set; }
        public float wind_speed { get; set; }
        public int wind_deg { get; set; }
        public List<Weather> weather { get; set; }
        public int clouds { get; set; }
        public float pop { get; set; }
        public float rain { get; set; }
        public float uvi { get; set; }

        public Daily()
        {
            dt = 0;
            sunrise = 0;
            sunset = 0;
            moonrise = 0;
            moonset = 0;
            moon_phase = 0;
            temp = new Temp();
            feels_like = new Feels_Like();
            pressure = 0;
            humidity = 0;
            dew_point = 0;
            wind_speed = 0;
            wind_deg = 0;
            weather = new List<Weather>();
            clouds = 0;
            pop = 0;
            rain = 0;
            uvi = 0;
        }
    }

    public class Temp
    {
        public float day { get; set; }
        public float min { get; set; }
        public float max { get; set; }
        public float night { get; set; }
        public float eve { get; set; }
        public float morn { get; set; }

        public Temp()
        {
            day = 0;
            min = 0;
            max = 0;
            night = 0;
            eve = 0;
            morn = 0;
        }
    }
    public class Feels_Like
    {
        public float day { get; set; }
        public float night { set; get; }
        public float eve { set; get; }
        public float morn { set; get; }

        public Feels_Like()
        {
            day = 0;
            night = 0;
            eve = 0;
            morn = 0;
        }
    }
}