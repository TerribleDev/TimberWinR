# Input: WindowsEvents

The WindowsEvents input will collect events from the Windows Event Viewer.   The source parameter indicates which event 
logs to collect data from.  You can specify more than one log by using the comma, i.e.  "Application,System" will collect
logs from the Application and System event logs.  The default interval for scanning for new Events is 60 seconds.

## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Legal Values                  |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *source*          | string |Windows event logs                                                       | Application,System,Security |  System   |
| *binaryFormat*    | string |Format of the "Data" binary field.                                       | ASC,HEX,PRINT               | **ASC** |
| *msgErrorMode*    | string |Behavior when event messages or event category names cannot be resolved. |NULL,ERROR,MSG               | **MSG** |
| *direction*       | string |Direction to scan the event logs                                         | FW,BW                        | **FW**  |
| *stringsSep*      | string |Separator between values of the "Strings" field.                         | any string                   | vertical bar |
| *fullEventCode*   | bool   |Return the full event ID code instead of the friendly code.              | true,false                   | **false** |
| *fullText*        | bool   |Retrieve the full text message                                           | true,false                   | **true** |
| *resolveSIDS*     | bool   |Resolve SID values into full account names                               | true,false                   | **true** |
| *formatMsg*       | bool   |Format the text message as a single line.                                | true,false                   | **true** |
| *interval*        | integer  | Interval in seconds to sleep during checks                            | Interval                     | 60 |

### source format
The source indicates where to collect the event(s) from, it can be of these form(s): 
When specifying a windows path, make sure to escape the backslash(s).
```
"source": "System, Application, Security"
"source": "D:\\MyEVTLogs\\*.evt"
"source": "System, D:\\MyEVTLogs\\System.evt"
```
Example Input:
```json
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
		}
	}
}
```
## Fields
After a successful parse of an event, the following fields are added:

| Name | Type | Description |
| ---- |:-----| :-----------------------------------------------------------------------|
| EventLog | STRING |Name of the Event Log or Event Log backup file containing this event
| RecordNumber | INTEGER | Index of this event in the Event Log or Event Log backup file containing this event  |
| TimeGenerated | TIMESTAMP | The date and time at which the event was generated (local time)  |
| TimeWritten | TIMESTAMP | The date and time at which the event was logged (local time)  |
| EventID | INTEGER | The ID of the event  |
| EventType | INTEGER | The numeric type of the event  |
| EventTypeName | STRING | The descriptive type of the event  |
| EventCategory | INTEGER | The numeric category of the event  |
| EventCategoryName | STRING | The descriptive category of the event  |
| SourceName | STRING | The source that generated the event  |
| Strings | STRING | The textual data associated with the event 
| ComputerName | STRING | The name of the computer on which the event was generated  |
| SID | STRING | The Security Identifier associated with the event  |
| Message | STRING | The full event message  |
| Data | STRING | The binary data associated with the event  |



