/**
@file
    Twitter.cs
@brief
    Copyright 2010 Sandbox. All rights reserved.
@author
    William Chang
@version
    0.1
@date
    - Created: 2010-06-01
    - Modified: 2010-06-03
    .
@note
    References:
    - General:
        - http://dev.twitter.com/doc/get/statuses/user_timeline
        - http://blogs.msdn.com/b/bursteg/archive/2009/05/29/twitter-api-from-c-getting-a-user-s-time-line.aspx
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

public class Twitter
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
    private string _paramDataStatusesHistoryKey = "DataStatusesHistory";
    private int _paramDataStatusesHistory = 1;
    // Declaration for third party service.
    private string _urlGetStatusesKey = "GetStatusesUrl";
    private string _urlGetStatuses = String.Empty;
    private string _paramGetStatusesUserKey = "GetStatusesUserField";
    private string _paramGetStatusesUser = String.Empty;
    private string _paramGetStatusesUserValueKey = "GetStatusesUser";
    private string _paramGetStatusesUserValue = String.Empty;
    private string _paramGetStatusesSinceKey = "GetStatusesSinceField";
    private string _paramGetStatusesSince = String.Empty;
    private string _paramGetStatusesCountKey = "GetStatusesCountField";
    private string _paramGetStatusesCount = String.Empty;
    // Declaration for logging.
    public static string logType = "Application";
    public static string logName = "Sandbox.Applications.Twitter";

#endregion

    /// <summary>Default constructor.</summary>
    public Twitter() {
        _dbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings[_dbConnectionStringKey].ConnectionString;
        _urlGetStatuses = System.Configuration.ConfigurationManager.AppSettings[_urlGetStatusesKey];
        _paramDataStatusesHistory = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings[_paramDataStatusesHistoryKey]);
        _paramGetStatusesUser = System.Configuration.ConfigurationManager.AppSettings[_paramGetStatusesUserKey];
        _paramGetStatusesUserValue = System.Configuration.ConfigurationManager.AppSettings[_paramGetStatusesUserValueKey];
        _paramGetStatusesSince = System.Configuration.ConfigurationManager.AppSettings[_paramGetStatusesSinceKey];
        _paramGetStatusesCount = System.Configuration.ConfigurationManager.AppSettings[_paramGetStatusesCountKey];
    }

    /// <summary>Argument constructor.</summary>
    public Twitter(string dbConnectionString, string urlService, int paramDataHistory, string paramUserIds) : this()
    {
        _dbConnectionString = dbConnectionString;
        _urlGetStatuses = urlService;
        _paramDataStatusesHistory = paramDataHistory;
        _paramGetStatusesUserValue = paramUserIds;
    }

    /// <summary>Run operation.</summary>
    public void Run(enumRun mode)
    {
        // Validate application configuration file.
        if(String.IsNullOrEmpty(_paramGetStatusesSince) || String.IsNullOrEmpty(_paramGetStatusesCount)) {
            EventLog.WriteEntry(logName, "Application Error: Missing values from configuration file.", EventLogEntryType.Error);
            return;
        }

        // Init database.
        _dbContext = new BaseDataContext(_dbConnectionString);
        if(!_dbContext.DatabaseExists()) {
            _dbContext.CreateDatabase();
        }

        // Create (INSERT) default data to database.
        if(mode == enumRun.LoadData) {
            CreateStatusesDefault();
            Console.WriteLine("\nLoaded data successfully.");
        } else if(mode == enumRun.LoadData) {
            Console.WriteLine("\nCannot load data because it exists. If you want to try again, then you must either drop the database or delete all the records.");
        }

        // Iterate and process users.
        TwitterStatus userStatus = null;
        string userUrlGetStatuses = String.Empty;
        var userIds = _paramGetStatusesUserValue.RemoveWhitespaces().Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
        for(int i = 0;i < userIds.Length;i += 1) {
            userUrlGetStatuses = String.Empty;
            // Get (SELECT) status, most recent.
            userStatus = GetStatusLatest(userIds[i]);
            // Validate status.
            if(userStatus != null) {
                userUrlGetStatuses = _urlGetStatuses.AppendQueryString(String.Concat(_paramGetStatusesSince, "=", userStatus.Id));
            } else {
                userUrlGetStatuses = _urlGetStatuses.AppendQueryString(String.Concat(_paramGetStatusesCount, "=", _paramDataStatusesHistory));
            }
            // Create (INSERT) statue from XML.
            CreateStatus(userUrlGetStatuses.AppendQueryString(String.Concat(_paramGetStatusesUser, "=", userIds[i])));
        }
        // Remove (DELETE) extra statuses based on maximum length.
        RemoveStatuses(_paramDataStatusesHistory);
        // Submit data changes.
        _dbContext.SubmitChanges();
    }

    /// <summary>Create statuses with default data.</summary>
    public IList<TwitterStatus> CreateStatusesDefault()
    {
        return null;
    }

    /// <summary>Create status.</summary>
    protected void CreateStatus(string url)
    {
        // Validate HTTP implementation.
        try {
            // Create a HTTP request for XML document.
            var webRequest = WebRequest.Create(url) as HttpWebRequest;
            // Get HTTP response.
            using(var webResponse = webRequest.GetResponse() as HttpWebResponse) {
                // Validate HTTP response.
                if(!webResponse.ContentType.Contains("/xml") || webResponse.ContentLength <= 0) {
                    EventLog.WriteEntry(logName, "HTTP Error: Malformed response. Goto source line: 142.", EventLogEntryType.Error);
                    return;
                }
                // Get stream from HTTP response.
                var streamResponse = new StreamReader(webResponse.GetResponseStream());
                // Retrieve XML document.
                var xmlDocument = XDocument.Load(streamResponse);
                // Parse XML document.
                var objs1 = (
                    from x in xmlDocument.Descendants("status")
                    select new TwitterStatus {
                        Id = (long)x.Element("id"),
                        DateCreated = x.Element("created_at").Value.ToDateTime(),
                        Text = (string)x.Element("text"),
                        Source = (string)x.Element("source"),
                        IsTruncated = (bool)x.Element("truncated"),
                        UserId = (long)x.Element("user").Element("id"),
                        UserName = (string)x.Element("user").Element("name"),
                        UserAlias = (string)x.Element("user").Element("screen_name"),
                        UserLocation = (string)x.Element("user").Element("location"),
                        UserProfileImage = (string)x.Element("user").Element("profile_image_url"),
                        UserUrl = (string)x.Element("user").Element("url"),
                        UserDateCreated = x.Element("user").Element("created_at").Value.ToDateTime(),
                        UserUtcOffset = (string)x.Element("user").Element("utc_offset"),
                        UserTimeZone = (string)x.Element("user").Element("time_zone")
                    }
                ).ToList();
                // Validate conditions.
                if(objs1 == null) {
                    EventLog.WriteEntry(logName, "Application Error: Cannot parse XML. Goto source line: 151.", EventLogEntryType.Error);
                    return;
                }
                // Create (INSERT) conditions.
                _dbContext.GetTable<TwitterStatus>().InsertAllOnSubmit(objs1);
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

    /// <summary>Get latest status.</summary>
    protected TwitterStatus GetStatusLatest()
    {
        return (
            from x in _dbContext.GetTable<TwitterStatus>()
            orderby x.Id descending
            select x
        ).FirstOrDefault();
    }

    /// <summary>Get latest status.</summary>
    protected TwitterStatus GetStatusLatest(long userId)
    {
        return (
            from x in _dbContext.GetTable<TwitterStatus>()
            where x.UserId == userId
            orderby x.Id descending
            select x
        ).FirstOrDefault();
    }

    /// <summary>Get latest status.</summary>
    protected TwitterStatus GetStatusLatest(string userAlias)
    {
        return (
            from x in _dbContext.GetTable<TwitterStatus>()
            where x.UserAlias == userAlias
            orderby x.Id descending
            select x
        ).FirstOrDefault();
    }

    /// <summary>Remove (DELETE) statuses.</summary>
    protected void RemoveStatuses(int maxLength)
    {
        string tableName = "TwitterStatuses";
        string columnPrimaryKey = "Id";
        string columnGroupBy = "UserId";
        string columnOrderBy = "DateCreated DESC";
        string sbSql1 = String.Format("SELECT TOP {0} {1} FROM {2} AS t2 WHERE t2.{3} = t1.{3} ORDER BY {4}", maxLength, columnPrimaryKey, tableName, columnGroupBy, columnOrderBy);
        string sbSql2 = String.Format("DELETE t1 FROM {0} AS t1 WHERE {1} NOT IN ({2});", tableName, columnPrimaryKey, sbSql1.ToString());
        _dbContext.ExecuteCommand(sbSql2);
    }

#region Data Entities and Mappings

    public class BaseDataContext : DataContext
    {
        public Table<TwitterStatus> tableTwitterStatuses = null;
        
        /// <summary>Argument constructor.</summary>
        public BaseDataContext(string dbConnectionString) : base(dbConnectionString) {}
    }

    [Table(Name = "TwitterStatuses")]
    public class TwitterStatus
    {
        [Column(DbType = "bigint NOT NULL", IsPrimaryKey = true, IsDbGenerated = false)]
        public virtual long Id {get;set;}

        [Column(DbType = "datetime NOT NULL")]
        public virtual DateTime DateCreated {get;set;}

        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual string Text {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string Source {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual bool IsTruncated {get;set;}

        [Column(DbType = "bigint NOT NULL")]
        public virtual long UserId {get;set;}

        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual string UserName {get;set;}

        [Column(DbType = "nvarchar(255) NOT NULL")]
        public virtual string UserAlias {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string UserLocation {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string UserProfileImage {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string UserUrl {get;set;}

        [Column(DbType = "datetime")]
        public virtual DateTime UserDateCreated {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string UserUtcOffset {get;set;}

        [Column(DbType = "nvarchar(255)")]
        public virtual string UserTimeZone {get;set;}

        public TwitterStatus() {}
    }

#endregion

}

/// <summary>BaseUtility class.</summary>
public static class BaseUtility
{
    /// <summary>Append query string to url (question mark handler).</summary>
    /// <remarks>Extension method.</remarks>
    public static string AppendQueryString(this string url, string qs)
    {
        if(url.Contains('?')) {
            return String.Concat(url, '&', qs);
        } else {
            return String.Concat(url, '?', qs);
        }
    }

    /// <summary>Remove all whitespaces from string.</summary>
    /// <remarks>Extension method.</remarks>
    public static String RemoveWhitespaces(this String source) {
        return source.Trim().Replace(" ", String.Empty);
    }

    /// <summary>Parse string to DateTime.</summary>
    /// <remarks>Extension method.</remarks>
    public static DateTime ToDateTime(this string s)
    {
        string dayOfWeek = s.Substring(0, 3).Trim();
        string month = s.Substring(4, 3).Trim();
        string dayInMonth = s.Substring(8, 2).Trim();
        string time = s.Substring(11, 9).Trim();
        string offset = s.Substring(20, 5).Trim();
        string year = s.Substring(25, 5).Trim();
        string dateTime = String.Format("{0}-{1}-{2} {3}", dayInMonth, month, year, time);
        return DateTime.Parse(dateTime);
    }
}

} // END namespace Sandbox.Applications