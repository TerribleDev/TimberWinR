# Input: TailFiles

The TailFiles input will monitor a log (text) file similar to how a Linux "tail -f" command works.  This uses
a native implementation rather than uses LogParser

## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *type*            | string  |Typename for this Input                                         |  |  Win32-TailLog  |
| *location*        | string  |Location of file(s) to monitor                                           | Path to text file(s) including wildcards. |     |
| *logSource*       | string  |Source name                                  | Used for conditions |     |
| *recurse*         | integer |Max subdirectory recursion level.                                       | 0 disables subdirectory recursion; -1 enables unlimited recursion. | 0 |
| *interval*        | integer |Polling interval in seconds                                     | Defaults every 60 seconds | 60 |
| [codec](https://github.com/Cimpress-MCP/TimberWinR/blob/master/TimberWinR/mdocs/Codec.md)  | object | Codec to use  |

Example Input: Monitors all files (recursively) located at C:\Logs1\ matching *.log as a pattern.  I.e. C:\Logs1\foo.log, C:\Logs1\Subdir\Log2.log, etc.

```json
{
    "TimberWinR": {
        "Inputs": {
            "TailFiles": [
                {
                    "logSource": "log files",
                    "location": "C:\\Logs1\\*.log",
                    "recurse": -1
                }
            ]
		}
	}
}
```
## Fields
After a successful parse of an event, the following fields are added:

| Name | Type | Description |
| ---- |:-----| :-----------|
| LogFilename | STRING |Full path of the file containing this line | 
| Index | INTEGER | Line number |
| Text | STRING | Text line content  |
| type | STRING | Win32-TailLog |
