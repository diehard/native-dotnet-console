/**
@file
    Weather.cs
@brief
    Copyright 2010 Sandbox. All rights reserved.
@author
    William Chang
@version
    0.1
@date
    - Created: 2010-04-12
    - Modified: 2010-06-03
    .
@note
    References:
    - General:
        - Nothing.
        .
    .
*/

using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Sandbox.Applications
{

public class Weather
{

#region Enumerations

    public enum enumRun {
        Lean = 1,
        LoadData = 2
    }

#endregion

#region Fields

    // Declaration for database.
    private DataContext _dbContext = null;
    private string _dbConnectionStringKey = "ConnectionString_Sandbox";
    private string _dbConnectionString = String.Empty;
    private string _paramConditionHistoryKey = "DataConditionsHistory";
    private int _paramConditionHistory = 1;
    // Declaration for logging.
    public static string logType = "Application";
    public static string logName = "Sandbox.Applications.Weather";

#endregion

    /// <summary>Default constructor.</summary>
    public Weather() {
        _dbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings[_dbConnectionStringKey].ConnectionString;
        _paramConditionHistory = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings[_paramConditionHistoryKey]);
    }

    /// <summary>Argument constructor.</summary>
    public Weather(string dbConnectionString, int paramDataHistory) : this()
    {
        _dbConnectionString = dbConnectionString;
        _paramConditionHistory = paramDataHistory;
    }

    /// <summary>Run operation.</summary>
    public void Run(enumRun mode)
    {
        // Init database.
        _dbContext = new BaseDataContext(_dbConnectionString);
        if(!_dbContext.DatabaseExists()) {
            _dbContext.CreateDatabase();
        }

        // Get (SELECT) locations from database.
        var locations = GetLocations();
        int locationsLength = locations.Count();

        // Create (INSERT) default data to database.
        if(mode == enumRun.LoadData && (locations == null || locationsLength <= 0)) {
            locations = CreateLocationsDefault();
            Console.WriteLine("\nLoaded data successfully.");
        } else if(mode == enumRun.LoadData) {
            Console.WriteLine("\nCannot load data because it exists. If you want to try again, then you must either drop the database or delete all the records.");
        }

        // Process condition from each location.
        for(int i = 0;i < locationsLength;i += 1) {
            // Create (INSERT) condition from XML.
            CreateConditions(locations[i]);
            // Remove (DELETE) extra conditions based on maximum length.
            RemoveConditions(locations[i], _paramConditionHistory);
        }
        // Remove (DELETE) extra conditions based on maximum length.
        RemoveConditions(_paramConditionHistory);
        // Submit data changes.
        _dbContext.SubmitChanges();
    }

    /// <summary>Create locations with default data.</summary>
    public IList<WeatherLocation> CreateLocationsDefault()
    {
        var objLocations = new WeatherLocation[] {
            new WeatherLocation() {
                locationName = "Orlando",
                locationUrl = "http://www.weather.gov/data/current_obs/KMCO.xml"
            },
            new WeatherLocation() {
                locationName = "San Diego",
                locationUrl = "http://www.weather.gov/data/current_obs/KSAN.xml"
            },
            new WeatherLocation() {
                locationName = "San Antonio",
                locationUrl = "http://www.weather.gov/data/current_obs/KSAT.xml"
            },
            new WeatherLocation() {
                locationName = "Tampa",
                locationUrl = "http://www.weather.gov/data/current_obs/KTPA.xml"
            },
            new WeatherLocation() {
                locationName = "Williamsburg",
                locationUrl = "http://www.weather.gov/data/current_obs/KJGG.xml"
            },
            new WeatherLocation() {
                locationName = "Trenton",
                locationUrl = "http://www.weather.gov/data/current_obs/KTTN.xml"
            }
        };
        _dbContext.GetTable<WeatherLocation>().InsertAllOnSubmit(objLocations);
        _dbContext.SubmitChanges();
        return objLocations;
    }

    /// <summary>Create weather conditions from location.</summary>
    protected void CreateConditions(WeatherLocation location)
    {
        // Validate HTTP implementation.
        try {
            // Create a HTTP request for XML document.
            HttpWebRequest webRequest = WebRequest.Create(location.locationUrl) as HttpWebRequest;
            // Get HTTP response.
            using(HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse) {
                // Validate HTTP response.
                if(!webResponse.ContentType.Contains("text/xml") || webResponse.ContentLength <= 0) {
                    EventLog.WriteEntry(logName, "HTTP Error: Malformed response. Goto source line: 142.", EventLogEntryType.Error);
                    return;
                }
                // Get stream from HTTP response.
                StreamReader streamResponse = new StreamReader(webResponse.GetResponseStream());
                // Retrieve XML document.
                XDocument xmlDocument = XDocument.Load(streamResponse);
                // Parse XML document.
                var objConditions = (
                    from x in xmlDocument.Descendants("current_observation")
                    select new WeatherCondition {
                        conditionLocation = location,
                        conditionDateUpdated = DateTime.Now,
                        conditionCredit = (string)x.Element("credit"),
                        conditionCreditUrl = (string)x.Element("credit_URL"),
                        conditionImageUrl = (string)x.Element("image").Element("url"),
                        conditionImageTitle = (string)x.Element("image").Element("title"),
                        conditionImageLink = (string)x.Element("image").Element("link"),
                        conditionSuggestedPickup = (string)x.Element("suggested_pickup"),
                        conditionSuggestedPickupPeriod = (float?)x.Element("suggested_pickup_period"),
                        conditionLocationString = (string)x.Element("location"),
                        conditionStationId = (string)x.Element("station_id"),
                        conditionLatitude = (float?)x.Element("latitude"),
                        conditionLongitude = (float?)x.Element("longitude"),
                        conditionObservationTime = (string)x.Element("observation_time"),
                        conditionObservationTimeRfc822 = (string)x.Element("observation_time_rfc822"),
                        conditionWeather = (string)x.Element("weather"),
                        conditionTemperatureString = (string)x.Element("temperature_string"),
                        conditionTempF = (float?)x.Element("temp_f"),
                        conditionTempC = (float?)x.Element("temp_c"),
                        conditionRelativeHumidity = (float?)x.Element("relative_humidity"),
                        conditionWindString = (string)x.Element("wind_string"),
                        conditionWindDir = (string)x.Element("wind_dir"),
                        conditionWindDegrees = (float?)x.Element("wind_degrees"),
                        conditionWindMph = (float?)x.Element("wind_mph"),
                        conditionWindGustMph = (float?)x.Element("wind_gust_mph"),
                        conditionWindKt = (float?)x.Element("wind_kt"),
                        conditionWindGustKt = (float?)x.Element("wind_gust_kt"),
                        conditionPressureString = (String)x.Element("pressure_string"),
                        conditionPressureMb = (float?)x.Element("pressure_mb"),
                        conditionPressureIn = (float?)x.Element("pressure_in"),
                        conditionDewpointString = (string)x.Element("dewpoint_string"),
                        conditionDewpointF = (float?)x.Element("dewpoint_f"),
                        conditionDewpointC = (float?)x.Element("dewpoint_c"),
                        conditionVisibilityMi = (float?)x.Element("visibility_mi"),
                        conditionIconUrlBase = (string)x.Element("icon_url_base"),
                        conditionTwoDayHistory = (string)x.Element("two_day_history_url"),
                        conditionIconUrlName = (string)x.Element("icon_url_name"),
                        conditionObUrl = (string)x.Element("ob_url"),
                        conditionDisclaimerUrl = (string)x.Element("disclaimer_url"),
                        conditionCopyrightUrl = (string)x.Element("copyright_url"),
                        conditionPrivacyPolicyUrl = (string)x.Element("privacy_policy_url")
                    }
                ).ToList();
                // Validate conditions.
                if(objConditions == null || objConditions.Count() != 1) {
                    EventLog.WriteEntry(logName, "HTTP Error: More than one parent element in XML. Goto source line: 150.", EventLogEntryType.Error);
                    return;
                }
                // Create (INSERT) conditions.
                _dbContext.GetTable<WeatherCondition>().InsertOnSubmit(objConditions[0]);
            }
        } catch(UriFormatException ex) {
            EventLog.WriteEntry(logName, "HTTP Error: Malformed URL from web request. Goto source line: 137. Details: " + ex, EventLogEntryType.Error);
            return;
        } catch(WebException ex) {
            EventLog.WriteEntry(logName, "HTTP Error: Malformed content from web response. Goto source line: 139. Details: " + ex, EventLogEntryType.Error);
            return;
        } catch(XmlException ex) {
            EventLog.WriteEntry(logName, "XML Error: " + ex, EventLogEntryType.Error);
            return;
        }
    }

    /// <summary>Get locations.</summary>
    protected IList<WeatherLocation> GetLocations()
    {
        //DataLoadOptions  dbLoadOptions = new DataLoadOptions();
        //dbLoadOptions.LoadWith<Location>(x => x.locationConditions);
        //dbLoadOptions.AssociateWith<Location>(x => x.locationConditions.Skip(_dbTableConditionHistory));
        //_dbContext.LoadOptions = dbLoadOptions;

        return (
            from x in _dbContext.GetTable<WeatherLocation>()
            select x
        ).ToList();
    }

    /// <summary>Remove (DELETE) conditions.</summary>
    protected void RemoveConditions(int maxLength)
    {
        string tableName = "WeatherConditions";
        string columnPrimaryKey = "conditionId";
        string columnGroupBy = "conditionLocationId";
        string columnOrderBy = "conditionDateUpdated DESC";
        string sbSql1 = String.Format("SELECT TOP {0} {1} FROM {2} AS t2 WHERE t2.{3} = t1.{3} ORDER BY {4}", maxLength, columnPrimaryKey, tableName, columnGroupBy, columnOrderBy);
        string sbSql2 = String.Format("DELETE t1 FROM {0} AS t1 WHERE {1} NOT IN ({2});", tableName, columnPrimaryKey, sbSql1.ToString());
        _dbContext.ExecuteCommand(sbSql2);
    }

    /// <summary>Remove (DELETE) conditions.</summary>
    protected void RemoveConditions(WeatherLocation location, int maxLength)
    {
        // Adjusted maximum length to account the new record.
        maxLength -= 1;
        if(location.locationConditions != null && location.locationConditions.Count() > maxLength) {
            var objConditions = location.locationConditions.OrderByDescending(condition => condition.conditionDateUpdated).Skip(maxLength);
            _dbContext.GetTable<WeatherCondition>().DeleteAllOnSubmit(objConditions);
        }
    }

#region Data Entities and Mappings

    public class BaseDataContext : DataContext
    {
        public Table<WeatherLocation> tableWeatherLocations = null;
        public Table<WeatherCondition> tableWeatherConditions = null;
        
        /// <summary>Argument constructor.</summary>
        public BaseDataContext(string dbConnectionString) : base(dbConnectionString) {}
    }

    [Table(Name = "WeatherLocations")]
    public class WeatherLocation
    {
        [Column(DbType = "uniqueidentifier DEFAULT NEWSEQUENTIALID() NOT NULL", IsPrimaryKey = true, IsDbGenerated = true)]
        public virtual Guid locationId {get;set;}

        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual string locationName {get;set;}

        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual string locationUrl {get;set;}

        private EntitySet<WeatherCondition> _locationConditions = new EntitySet<WeatherCondition>();
        [Association(Storage = "_locationConditions", ThisKey = "locationId", OtherKey = "conditionLocationId")]
        public virtual EntitySet<WeatherCondition> locationConditions
        {
            get {return _locationConditions;}
            set {_locationConditions.Assign(value);}
        }

        public WeatherLocation() {
            locationName = String.Empty;
            locationUrl = String.Empty;
        }
    }

    [Table(Name = "WeatherConditions")]
    public class WeatherCondition
    {
        [Column(DbType = "uniqueidentifier DEFAULT NEWSEQUENTIALID() NOT NULL",IsPrimaryKey = true, IsDbGenerated = true)]
        public virtual Guid conditionId {get;set;}

        [Column(DbType = "uniqueidentifier")]
        private Guid conditionLocationId {get;set;}
        private EntityRef<WeatherLocation> _conditionLocation;
        [Association(Storage = "_conditionLocation", IsForeignKey = true, ThisKey = "conditionLocationId")]
        public virtual WeatherLocation conditionLocation
        {
            get {return _conditionLocation.Entity;}
            set {_conditionLocation.Entity = value;}
        }

        [Column(DbType = "datetime NOT NULL")]
        public virtual DateTime conditionDateUpdated {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionCredit {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionCreditUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionImageUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionImageTitle {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionImageLink {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionSuggestedPickup {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionSuggestedPickupPeriod {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionLocationString {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionStationId {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionLatitude {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionLongitude {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionObservationTime {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionObservationTimeRfc822 {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionWeather {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionTemperatureString {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionTempF {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionTempC {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionRelativeHumidity {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionWindString {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionWindDir {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionWindDegrees {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionWindMph {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionWindGustMph {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionWindKt {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionWindGustKt {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionPressureString {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionPressureMb {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionPressureIn {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionDewpointString {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionDewpointF {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionDewpointC {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionVisibilityMi {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionIconUrlBase {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionTwoDayHistory {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionIconUrlName {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionObUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionDisclaimerUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionCopyrightUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string conditionPrivacyPolicyUrl {get;set;}

        public WeatherCondition() {}
    }

#endregion

}

} // END namespace Sandbox.Service