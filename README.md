TimberWinR
==========
A Native Windows to Redis Logstash Agent which runs as a service.

## Why have TimberWinR?
TimberWinR is a native .NET implementation utilizing Microsoft's [LogParser](http://technet.microsoft.com/en-us/scriptcenter/dd919274.aspx).  This means
no JVM/JRuby is required, and LogParser does all the heavy lifting.    TimberWinR collects
the data from LogParser and ships it to Logstash via Redis.

## Configuration
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

## what is Markdown?
see [Wikipedia](http://en.wikipedia.org/wiki/Markdown)

> Markdown is a lightweight markup language, originally created by John Gruber and Aaron Swartz allowing people "to write using an easy-to-read, easy-to-write plain text format, then convert it to structurally valid XHTML (or HTML)".

----
## usage
1. Write markdown text in this textarea.
2. Click 'HTML Preview' button.

----
## markdown quick reference
# headers

*emphasis*

**strong**

* list

>block quote

    code (4 spaces indent)
[links](http://wikipedia.org)

----
## changelog
* 17-Feb-2013 re-design

----
## thanks
* [markdown-js](https://github.com/evilstreak/markdown-js)