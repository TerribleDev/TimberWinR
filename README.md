TimberWinR
==========
A Native Windows to Redis Logstash Agent which runs as a service.

## Why have TimberWinR?
TimberWinR is a native .NET implementation utilizing Microsoft's [LogParser](http://technet.microsoft.com/en-us/scriptcenter/dd919274.aspx).  This means
no JVM/JRuby is required, and LogParser does all the heavy lifting.    TimberWinR collects
the data from LogParser and ships it to Logstash via Redis.
## Supported Input Formats

 - [WindowsEvents](https://github.com/efontana/TimberWinR/WindowsEvents.md)
 - IIS Logs (W3C)
 - LogFiles (Tailing of files)
 - TCP Port
## Supported Output Formats
 - Redis

## Sample Configuration
TimberWinR reads a JSON configuration file, an example file is shown here:

    {
    "TimberWinR": {
        "Inputs": {
            "WindowsEvents": [
                {
                    "source": "System,Application",
                    "binaryFormat": "PRINT",
                    "resolveSIDS": true
                }
            ]
        },
        "Outputs": {
            "Redis": [
                { 
                    "host": [
                        "server1.host.com"
                    ]
                }
            ]
        }
    }
This configuration collects Events from the Windows Event Logs (System, Application) and forwards them
to Redis.

