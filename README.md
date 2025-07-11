## FlowSynx In-Memory Storage Plugin

The In-Memory Storage Plugin is a lightweight, plug-and-play integration component for the FlowSynx engine. It enables working with in-memory storage as a temporary bucket for managing data during workflow execution. This is particularly useful for caching, testing, or ephemeral storage needs within FlowSynx workflows.

This plugin is automatically installed by the FlowSynx engine when selected within the platform. It is not intended for manual installation or standalone developer use outside the FlowSynx environment.

---

## Purpose

The In-Memory Storage Plugin allows FlowSynx users to:

- Store and retrieve data within an in-memory bucket during workflow execution.
- Manage objects in memory with create, delete, and purge operations.
- List contents of the in-memory bucket with filtering and metadata support.
- Perform existence checks for objects without writing code.

---

## Supported Operations

- **create**: Creates a new object in the specified bucket and path.
- **delete**: Deletes an object at the specified path in the bucket.
- **exist**: Checks if an object exists at the specified path.
- **list**: Lists objects under a specified path (prefix), with filtering and optional metadata.
- **purge**: Deletes all objects under the specified path, optionally forcing deletion.
- **read**: Reads and returns the contents of an object at the specified path.
- **write**: Writes data to a specified path in the bucket, with support for overwrite.

---

## Plugin Specifications

The plugin requires the following configuration:

- `Bucket` (string): **Required.** The name of the in-memory bucket to use.

### Example Configuration

```json
{
  "Bucket": "tempbucket"
}
```

---

## Input Parameters

Each operation accepts specific parameters:

### Create
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path where the new object is created.|

### Delete
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path of the object to delete.        |

### Exist
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path of the object to check.         |

### List
| Parameter         | Type    | Required | Description                                         |
|--------------------|---------|----------|-----------------------------------------------------|
| `Path`             | string  | Yes      | The prefix path to list objects from.              |
| `Filter`           | string  | No       | A filter pattern for object names.                 |
| `Recurse`          | bool    | No       | Whether to list recursively. Default: `false`.     |
| `CaseSensitive`    | bool    | No       | Whether the filter is case-sensitive. Default: `false`. |
| `IncludeMetadata`  | bool    | No       | Whether to include object metadata. Default: `false`. |
| `MaxResults`       | int     | No       | Maximum number of objects to list. Default: `2147483647`. |

### Purge
| Parameter     | Type    | Required | Description                                    |
|---------------|---------|----------|------------------------------------------------|
| `Path`        | string  | Yes      | The path prefix to purge.                     |
| `Force`       | bool    | No       | Whether to force deletion without confirmation.|

### Read
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path of the object to read.          |

### Write
| Parameter     | Type    | Required | Description                                                  |
|---------------|---------|----------|--------------------------------------------------------------|
| `Path`        | string  | Yes      | The path where data should be written.                      |
| `Data`        | object  | Yes      | The data to write to the in-memory object.                   |
| `Overwrite`   | bool    | No       | Whether to overwrite if the object already exists. Default: `false`. |

### Example input (Write)

```json
{
  "Operation": "write",
  "Path": "documents/report.json",
  "Data": {
    "title": "Monthly Report",
    "content": "This is the report content."
  },
  "Overwrite": true
}
```

---

## Debugging Tips

- Verify the `Bucket` value is correct and unique per workflow if needed.
- Ensure the `Path` is valid and does not conflict with existing objects (especially for create/write).
- For write operations, confirm that `Data` is properly encoded or formatted for storage.
- When using list, adjust filters carefully to match object names (wildcards like `*.txt` are supported).
- Purge will clear all data under the specified path; use with caution.

---

## In-Memory Storage Considerations

- Ephemeral Storage: Data stored in memory is lost when the workflow ends or the engine restarts.
- Case Sensitivity: Paths are case-sensitive if `CaseSensitive` is set to `true` in list operations.
- Hierarchy Simulation: Paths simulate folder hierarchies (e.g., `folder1/file.txt`).

---

## Security Notes

- The plugin stores data only in memory during execution and does not persist it beyond the workflow lifecycle.
- Only authorized FlowSynx platform users can view or modify plugin configurations.

---

## License

© FlowSynx. All rights reserved.