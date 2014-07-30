# Input: Tcp

The Tcp input will open a port and listen for properly formatted JSON and will forward on the entire JSON.

## Parameters
The following parameters are allowed when configuring the Tcp input.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *port*        | integer  |Port number to open        | Must be an available port |     |

Example Input: Monitors all files (recursively) located at C:\Logs1\ matching *.log as a pattern.  I.e. C:\Logs1\foo.log, C:\Logs1\Subdir\Log2.log, etc.

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
