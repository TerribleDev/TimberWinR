# Output: File

The File output passes on data into a text file.

## Parameters
The following parameters are allowed when configuring the File output.

| Parameter     |   Type   |  Description                                                | Details               |  Default |
| :-------------|:---------|:------------------------------------------------------------| :---------------------------  | :-- |
| *interval*    | integer  | Interval in milliseconds to sleep before appending data       | Interval      | 1000 |
| *file_name*   | string   | Name of the file to be created                              |               | timberwinr.out |

Example Input:
```json
{
    "TimberWinR": {
        "Outputs": {
            "File": [
               { 
                    "file_name":  "foo.out",   
                    "interval": 1000                   
                }
            ]
		}
	}
}
```