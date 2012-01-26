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
    - Modified: 2010-04-14
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
using System.Xml.Linq;

namespace ConsoleDotnet.Application1.Service {

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
    private BaseDataContext _dbContext = null;
    private String _dbConnectionStringKey = "ConnectionString_Sandbox";
    private String _dbConnectionString = String.Empty;
    private String _dbTableConditionHistoryKey = "TableConditionHistory";
    private int _dbTableConditionHistory = 1;
    // Declaration for logging.
    public static String logType = "Application";
    public static String logName = "Sandbox.Service.Weather";

#endregion

    /// <summary>Default constructor.</summary>
    public Weather() {
        _dbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings[_dbConnectionStringKey].ConnectionString;
        _dbTableConditionHistory = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings[_dbTableConditionHistoryKey]);
    }

    /// <summary>Argument constructor.</summary>
    public Weather(String dbConnectionString, int dbHistory) : this()
    {
        _dbConnectionString = dbConnectionString;
        _dbTableConditionHistory = dbHistory;
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
        for(int i = 0;i < locationsLength;i++) {
            // Create (INSERT) condition from XML.
            SetConditions(locations[i]);
            // Remove (DELETE) extra conditions based on maximum length.
            RemoveConditions(locations[i], _dbTableConditionHistory);
        }
        _dbContext.SubmitChanges();
    }

    /// <summary>Create locations with default data.</summary>
    public IList<Location> CreateLocationsDefault()
    {
        var objLocations = new Location[] {
            new Location() {
                locationName = "Orlando",
                locationUrl = "http://www.weather.gov/data/current_obs/KMCO.xml"
            },
            new Location() {
                locationName = "San Diego",
                locationUrl = "http://www.weather.gov/data/current_obs/KSAN.xml"
            },
            new Location() {
                locationName = "San Antonio",
                locationUrl = "http://www.weather.gov/data/current_obs/KSAT.xml"
            },
            new Location() {
                locationName = "Tampa",
                locationUrl = "http://www.weather.gov/data/current_obs/KTPA.xml"
            },
            new Location() {
                locationName = "Williamsburg",
                locationUrl = "http://www.weather.gov/data/current_obs/KJGG.xml"
            },
            new Location() {
                locationName = "Trenton",
                locationUrl = "http://www.weather.gov/data/current_obs/KTTN.xml"
            }
        };
        _dbContext.tableLocations.InsertAllOnSubmit(objLocations);
        _dbContext.SubmitChanges();
        return objLocations;
    }

    /// <summary>Get locations.</summary>
    protected IList<Location> GetLocations()
    {
        //DataLoadOptions  dbLoadOptions = new DataLoadOptions();
        //dbLoadOptions.LoadWith<Location>(x => x.locationConditions);
        //dbLoadOptions.AssociateWith<Location>(x => x.locationConditions.Skip(_dbTableConditionHistory));
        //_dbContext.LoadOptions = dbLoadOptions;

        return (
            from location in _dbContext.tableLocations
            select location
        ).ToList();
    }

    /// <summary>Set weather conditions from location.</summary>
    protected void SetConditions(Location location)
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
                    select new Condition {
                        conditionLocation = location,
                        conditionDateUpdated = DateTime.Now,
                        conditionCredit = (String)x.Element("credit"),
                        conditionCreditUrl = (String)x.Element("credit_URL"),
                        conditionImageUrl = (String)x.Element("image").Element("url"),
                        conditionImageTitle = (String)x.Element("image").Element("title"),
                        conditionImageLink = (String)x.Element("image").Element("link"),
                        conditionSuggestedPickup = (String)x.Element("suggested_pickup"),
                        conditionSuggestedPickupPeriod = (float?)x.Element("suggested_pickup_period"),
                        conditionLocationString = (String)x.Element("location"),
                        conditionStationId = (String)x.Element("station_id"),
                        conditionLatitude = (float?)x.Element("latitude"),
                        conditionLongitude = (float?)x.Element("longitude"),
                        conditionObservationTime = (String)x.Element("observation_time"),
                        conditionObservationTimeRfc822 = (String)x.Element("observation_time_rfc822"),
                        conditionWeather = (String)x.Element("weather"),
                        conditionTemperatureString = (String)x.Element("temperature_string"),
                        conditionTempF = (float?)x.Element("temp_f"),
                        conditionTempC = (float?)x.Element("temp_c"),
                        conditionRelativeHumidity = (float?)x.Element("relative_humidity"),
                        conditionWindString = (String)x.Element("wind_string"),
                        conditionWindDir = (String)x.Element("wind_dir"),
                        conditionWindDegrees = (float?)x.Element("wind_degrees"),
                        conditionWindMph = (float?)x.Element("wind_mph"),
                        conditionWindGustMph = (float?)x.Element("wind_gust_mph"),
                        conditionWindKt = (float?)x.Element("wind_kt"),
                        conditionWindGustKt = (float?)x.Element("wind_gust_kt"),
                        conditionPressureString = (String)x.Element("pressure_string"),
                        conditionPressureMb = (float?)x.Element("pressure_mb"),
                        conditionPressureIn = (float?)x.Element("pressure_in"),
                        conditionDewpointString = (String)x.Element("dewpoint_string"),
                        conditionDewpointF = (float?)x.Element("dewpoint_f"),
                        conditionDewpointC = (float?)x.Element("dewpoint_c"),
                        conditionVisibilityMi = (float?)x.Element("visibility_mi"),
                        conditionIconUrlBase = (String)x.Element("icon_url_base"),
                        conditionTwoDayHistory = (String)x.Element("two_day_history_url"),
                        conditionIconUrlName = (String)x.Element("icon_url_name"),
                        conditionObUrl = (String)x.Element("ob_url"),
                        conditionDisclaimerUrl = (String)x.Element("disclaimer_url"),
                        conditionCopyrightUrl = (String)x.Element("copyright_url"),
                        conditionPrivacyPolicyUrl = (String)x.Element("privacy_policy_url")
                    }
                ).ToList();
                // Validate conditions.
                if(objConditions != null && objConditions.Count() != 1) {
                    EventLog.WriteEntry(logName, "HTTP Error: More than one parent element in XML. Goto source line: 150.", EventLogEntryType.Error);
                    return;
                }
                // Create (INSERT) conditions.
                _dbContext.tableConditions.InsertOnSubmit(objConditions[0]);
            }
        } catch(UriFormatException ex) {
            EventLog.WriteEntry(logName, "HTTP Error: Malformed URL from web request. Goto source line: 137. Details: " + ex, EventLogEntryType.Error);
            return;
        } catch(WebException ex) {
            EventLog.WriteEntry(logName, "HTTP Error: Malformed content from web response. Goto source line: 139. Details: " + ex, EventLogEntryType.Error);
            return;
        }
    }

    /// <summary>Remove (DELETE) conditions.</summary>
    protected void RemoveConditions(Location location, int maxLength)
    {
        // Adjusted maximum length to account the new record.
        maxLength -= 1;
        if(location.locationConditions != null && location.locationConditions.Count() > maxLength) {
            var objConditions = location.locationConditions.OrderByDescending(condition => condition.conditionDateUpdated).Skip(maxLength);
            _dbContext.tableConditions.DeleteAllOnSubmit(objConditions);
        }
    }

#region Data Entities and Mappings

    public class BaseDataContext : DataContext
    {
        public Table<Location> tableLocations = null;
        public Table<Condition> tableConditions = null;
        
        /// <summary>Argument constructor.</summary>
        public BaseDataContext(String dbConnectionString) : base(dbConnectionString) {}
    }

    [Table(Name = "Locations")]
    public class Location
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public virtual int locationId {get;set;}
        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual String locationName {get;set;}
        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual String locationUrl {get;set;}

        private EntitySet<Condition> _locationConditions = new EntitySet<Condition>();
        [Association(Storage = "_locationConditions", ThisKey = "locationId", OtherKey = "conditionLocationId")]
        public virtual EntitySet<Condition> locationConditions
        {
            get {return _locationConditions;}
            set {_locationConditions.Assign(value);}
        }

        public Location() {
            locationName = String.Empty;
            locationUrl = String.Empty;
        }
    }

    [Table(Name = "Conditions")]
    public class Condition
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public virtual int conditionId {get;set;}

        [Column(DbType = "int")]
        private int conditionLocationId {get;set;}
        private EntityRef<Location> _conditionLocation;
        [Association(Storage = "_conditionLocation", IsForeignKey = true, ThisKey = "conditionLocationId")]
        public virtual Location conditionLocation
        {
            get {return _conditionLocation.Entity;}
            set {_conditionLocation.Entity = value;}
        }

        [Column(DbType = "datetime NOT NULL")]
        public virtual DateTime conditionDateUpdated {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionCredit {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionCreditUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionImageUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionImageTitle {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionImageLink {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionSuggestedPickup {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionSuggestedPickupPeriod {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionLocationString {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionStationId {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionLatitude {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionLongitude {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionObservationTime {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionObservationTimeRfc822 {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionWeather {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionTemperatureString {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionTempF {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionTempC {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionRelativeHumidity {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionWindString {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionWindDir {get;set;}

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
        public virtual String conditionPressureString {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionPressureMb {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionPressureIn {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionDewpointString {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionDewpointF {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionDewpointC {get;set;}

        [Column(DbType = "float")]
        public virtual float? conditionVisibilityMi {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionIconUrlBase {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionTwoDayHistory {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionIconUrlName {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionObUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionDisclaimerUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionCopyrightUrl {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual String conditionPrivacyPolicyUrl {get;set;}

        public Condition() {}
    }

#endregion

}

public static class DataHelper {
    /// <summary>Convert string to nullable type.</summary>
    /// <remarks>Extension method.</remarks>
    public static T? ToNullable<T>(this String value) where T : struct
    {
        if(!String.IsNullOrEmpty(value.Trim())) {
            return (T)Convert.ChangeType(value, typeof(T));
        } else {
            return null;
        }
    }
}

} // END namespace ConsoleDotnet.Application1.Service