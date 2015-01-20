# Codec

## Parameters
The following parameters are allowed when configuring the Codec.

| Parameter         |     Type       |  Description                                                             | Details                       |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *type*            | enum           |Codec type 'multiline'                                                    | Must be 'multiline'           |     |
| *pattern*         | regex          |Regular expression to be matched                                          | Must be legal .NET Regex      |     |
| *what*            | enum           |Value can be previous or next                                             | If the pattern matched, does event belong to the next or previous event? |     |
| *negate*          | bool           |Inverts the pattern sense                                                 | If true, a message not matching the pattern will constitute a match of the multiline filter and the what will be applied. (vice-versa is also true) |  false   |

Example Input: Mutliline input log file

```json
{
    "TimberWinR": {
        "Inputs": {
            "Logs": [
            {
                "location": "C:\\Logs1\\multiline.log",
                "recurse": -1,
                "codec": {
                  "negate": false,
                  "type": "multiline",
                  "pattern": "(^.+Exception: .+)|(^\\s+at .+)|(^\\s+... \\d+ more)|(^\\s*Caused by:.+)",
                  "what": "previous"
                }
            }
		 }
	}
}
```
