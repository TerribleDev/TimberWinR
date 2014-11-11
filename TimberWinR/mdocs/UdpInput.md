# Input: Udp

The Udp input will open a port and listen for properly formatted UDP datagrams to be broadcast.

## Parameters
The following parameters are allowed when configuring the Udp input.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *port*        | integer  |Port number to open        | Must be an available port |     |

Example Input: Listen on Port 5142

```json
{
    "TimberWinR": {
        "Inputs": {
            "Udp": [
                {
                    "port": 5142                
                }
            ]
		}
	}
}
```
## Fields
A field: "type": "Win32-Udp" is automatically appended, and the entire JSON is passed on vertabim.
