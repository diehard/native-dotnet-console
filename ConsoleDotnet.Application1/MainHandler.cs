/**
@file
    MainHandler.cs
@brief
    Copyright 2011 Sandbox. All rights reserved.
@author
    William Chang
@version
    0.1
@date
    - Created: 2011-04-13
    - Modified: 2011-04-13
    .
@note
    References:
    - General:
        - Nothing.
        .
    .
*/

using System;
using System.Diagnostics;
using System.Text;

namespace ConsoleDotnet.Application1 {

public class MainHandler
{
    static void Main(String[] args)
    {
        // Init log.
        if(!EventLog.SourceExists(Service.Weather.logName)) {
            EventLog.CreateEventSource(Service.Weather.logName, Service.Weather.logType);
        }

        // Validate program arugments.
        Service.Weather.enumRun mode = Service.Weather.enumRun.Lean;
        if(args.Length > 0 && String.Equals(args[0], "-loaddata")) {
            mode = Service.Weather.enumRun.LoadData;
        }

        // Init service.
        EventLog.WriteEntry(Service.Weather.logName, "Service starting.", EventLogEntryType.Information);
        Service.Weather svc = new Service.Weather();
        svc.Run(mode);
        EventLog.WriteEntry(Service.Weather.logName, "Service ended.", EventLogEntryType.Information);
    }
}

} // END namespace ConsoleDotnet.Application1