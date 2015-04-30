# Input: Tcp

The Tcp input will open a port and listen for properly formatted JSON and will forward on the entire JSON.

## Parameters
The following parameters are allowed when configuring the Tcp input.

| Parameter         |     Type         |  Description                                                             | Details               |  Default |
| :---------------- |:-----------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *add_field*       | property:array   |Add field(s) to this event.  Field names can be dynamic and include parts of the event using the %{field} syntax.  This property must be specified in pairs.  |     |
| *port*            | integer          |Port number to open        | Must be an available port |     |
| *rename*          | property:array   |Rename one or more fields  |  |  |                             
| *type*            | string           |Typename for this Input    |  |  Win32-Tcp  |

Example Input: Listen on Port 5140

```json
{
    "TimberWinR": {
        "Inputs": {
            "Tcp": [
                {
                    "port": 5140                  
                }
            ]
		}
	}
}
```
## Fields
A field: "type": "Win32-Tcp" is automatically appended, and the entire JSON is passed on vertabim.
