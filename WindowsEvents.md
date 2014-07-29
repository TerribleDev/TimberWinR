
# Input: WindowsEvents

## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Legal Values                  |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *source*          | property:string |Windows event logs                                                       | Application,System,Security |  System   |
| *binaryFormat*    | property:string |Format of the "Data" binary field.                                       | ASC,HEX,PRINT               | **ASC** |
| *msgErrorMode*    | property:string |Behavior when event messages or event category names cannot be resolved. |NULL,ERROR,MSG               | **MSG** |
| *direction*       | property:string |Format of the "Data" binary field.                                       | FW,BW                        | **FW**  |
| *stringsSep*      | property:string |Separator between values of the "Strings" field.                         | any string                    | vertical bar |
| *fullEventCode*   | property:bool   |Return the full event ID code instead of the friendly code.              | true,false                   | **false** |
| *fullText*        | property:bool   |Retrieve the full text message                                           | true,false                   | **true** |
| *resolveSIDS*     | property:bool   |Resolve SID values into full account names                               | true,false                   | **true** |
| *formatMsg*       | property:bool   |Format the text message as a single line.                                | true,false                   | **true** |
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