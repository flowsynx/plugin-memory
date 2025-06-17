## FlowSynx Memoey Plugin � FlowSynx Platform Integration

The **Memoey Plugin** is a pre-packaged, plug-and-play integration component for the FlowSynx engine. It enables secure and configurable access to Memoey as part of FlowSynx�s no-code/low-code automation workflows.

This plugin can be installed automatically by the FlowSynx engine when selected within the platform. It is not intended for manual installation or developer use outside the FlowSynx environment.

---

## Purpose

This plugin allows FlowSynx users to interact with Memoey for a variety of storage-related operations�without writing code. Once installed, the plugin becomes available as a storage connector within the platform's workflow builder.

---

## Supported Operations

The plugin supports the following operations, which can be selected and configured within the FlowSynx UI:

| Operation | Description                          |
|----------|--------------------------------------|
| `create` | Upload a new object to the S3 bucket. |
| `delete` | Remove an object from the bucket.     |
| `exist`  | Check if an object exists.            |
| `list`   | List all objects in the bucket.       |
| `purge`  | Remove all contents in the bucket.    |
| `read`   | Read and return the contents of an object. |
| `write`  | Overwrite or write new data to an object. |

---

## Notes

- This plugin is only supported on the FlowSynx platform.
- It is installed automatically by the FlowSynx engine.
- All operational logic is encapsulated and executed under FlowSynx control.
- Credentials are configured through platform-managed settings.

---

## License

� FlowSynx. All rights reserved.