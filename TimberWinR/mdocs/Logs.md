# Input: Logs

The Logs input will monitor a log (text) file similar to how a Linux "tail -f" command works. 

## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *location*        | string  |Location of file(s) to monitor                                           | Path to text file(s) including wildcards. |     |
| *recurse*         | integer |Max subdirectory recursion level.                                       | 0 disables subdirectory recursion; -1 enables unlimited recursion. | 0 |
| *splitLongLines*  | boolean |Behavior when event messages or event category names cannot be resolved. |When a text line is longer than 128K characters, the format truncates the line and either discards the remaining of the line (when this parameter is set to "false"), or processes the remainder of the line as a new line (when this parameter is set to "true").| false |
| *iCodepage*       | integer |Codepage of the text file.                                              | 0 is the system codepage, -1 is UNICODE.                         | 0  |

Example Input: Monitors all files (recursively) located at C:\Logs1\ matching *.log as a pattern.  I.e. C:\Logs1\foo.log, C:\Logs1\Subdir\Log2.log, etc.

```json
{
    "TimberWinR": {
        "Inputs": {
            "Logs": [
                {
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
