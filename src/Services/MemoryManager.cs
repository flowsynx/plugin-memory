using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Memory;
using FlowSynx.Plugins.Memory.Extensions;
using FlowSynx.Plugins.Memory.Models;
using System.Data;
using System.Text;

namespace FlowSynx.Connectors.Storage.Memory.Services;

internal class MemoryManager: IMemoryManager
{
    private readonly IPluginLogger _logger;
    private readonly string _bucketName;
    private readonly Dictionary<string, Dictionary<string, PluginContext>> _entities;

    public MemoryManager(IPluginLogger logger, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(bucketName);
        _logger = logger;
        _bucketName = bucketName;
        _entities = new Dictionary<string, Dictionary<string, PluginContext>>();
    }

    public async Task Create(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var createParameters = parameters.ToObject<CreateParameters>();
        await CreateEntity(createParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Delete(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var deleteParameter = parameters.ToObject<DeleteParameters>();
        await DeleteEntity(deleteParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Exist(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var existParameters = parameters.ToObject<ExistParameters>();
        return await ExistEntity(existParameters, cancellationToken);
    }

    public async Task<IEnumerable<PluginContext>> List(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var listParameter = parameters.ToObject<ListParameters>();
        return await ListEntities(listParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task Purge(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var purgeParameters = parameters.ToObject<PurgeParameters>();
        await PurgeEntity(purgeParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var readParameters = parameters.ToObject<ReadParameters>();
        return await ReadEntity(readParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Write(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var writeParameters = parameters.ToObject<WriteParameters>();
        await WriteEntity(writeParameters, cancellationToken).ConfigureAwait(false);
    }

    #region internal methods
    private async Task CreateEntity(CreateParameters createParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(createParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        var isBucketExist = BucketExists(_bucketName);
        if (!isBucketExist)
        {
            _entities.Add(_bucketName, new Dictionary<string, PluginContext>());
            _logger.LogInfo($"Bucket '{_bucketName}' was created successfully.");
        }
    }

    private Task DeleteEntity(DeleteParameters deleteParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(deleteParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        var bucket = _entities[_bucketName];

        if (PathHelper.IsFile(path))
        {
            var isExist = ObjectExists(_bucketName, path);
            if (!isExist)
            {
                _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                return Task.CompletedTask;
            }

            bucket.Remove(path);
            _logger.LogInfo(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
            return Task.CompletedTask;
        }

        var folderPrefix = !path.EndsWith(PathHelper.PathSeparator) ? path + PathHelper.PathSeparator : path;
        var folderExist = bucket.Keys.Any(x => x.StartsWith(folderPrefix));

        if (!folderExist)
        {
            _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
            return Task.CompletedTask;
        }

        var itemToDelete = bucket.Keys.Where(x => x.StartsWith(folderPrefix)).Select(p => p);
        foreach (var item in itemToDelete)
        {
            bucket.Remove(item);
            _logger.LogInfo(string.Format(Resources.TheSpecifiedPathWasDeleted, item));
        }

        return Task.CompletedTask;
    }

    private Task<bool> ExistEntity(ExistParameters existParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(existParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (PathHelper.IsFile(path))
        {
            return Task.FromResult(ObjectExists(_bucketName, path));
        }

        var folderExist = FolderExist(_bucketName, path);
        return Task.FromResult(folderExist);
    }

    private async Task WriteEntity(WriteParameters writeParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(writeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        var dataValue = writeParameters.Data;
        var pluginContextes = new List<PluginContext>();

        if (dataValue is PluginContext pluginContext)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            pluginContextes.Add(pluginContext);
        }
        else if (dataValue is IEnumerable<PluginContext> pluginContextesList)
        {
            if (!PathHelper.IsDirectory(path))
                throw new Exception(Resources.ThePathIsNotDirectory);

            pluginContextes.AddRange(pluginContextesList);
        }
        else if (dataValue is string data)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            var context = CreateContextFromStringData(path, data);
            pluginContextes.Add(context);
        }
        else
        {
            throw new NotSupportedException("The entered data format is not supported!");
        }

        foreach (var context in pluginContextes)
        {
            await WriteEntityFromContext(path, context, writeParameters.Overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    private PluginContext CreateContextFromStringData(string path, string data)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var relativePath = Path.GetRelativePath(root, path);
        var dataBytesArray = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        return new PluginContext(relativePath, "File")
        {
            RawData = dataBytesArray,
        };
    }

    private async Task WriteEntityFromContext(string path, PluginContext context, bool overwrite,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] dataToWrite;

        if (context.RawData is not null)
            dataToWrite = context.RawData;
        else if (context.Content is not null)
            dataToWrite = Encoding.UTF8.GetBytes(context.Content);
        else
            throw new InvalidDataException($"The entered data is invalid for '{context.Id}'");

        var rootPath = Path.GetPathRoot(context.Id);
        string relativePath = context.Id;

        if (!string.IsNullOrEmpty(rootPath))
            relativePath = Path.GetRelativePath(rootPath, context.Id);

        var fullPath = PathHelper.IsDirectory(path) ? PathHelper.Combine(path, relativePath) : path;

        if (!PathHelper.IsFile(fullPath))
            throw new Exception(Resources.ThePathIsNotFile);

        var isBucketExist = BucketExists(_bucketName);
        if (!isBucketExist)
        {
            _entities.Add(_bucketName, new Dictionary<string, PluginContext>());
        }

        var isExist = await ExistEntity(new ExistParameters { Path = fullPath }, cancellationToken);
        if (isExist && overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, fullPath));

        var name = Path.GetFileName(fullPath);
        var bucket = _entities[_bucketName];
        bucket[path] = context;
    }

    private Task<PluginContext> ReadEntity(ReadParameters readParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        var isExist = ObjectExists(_bucketName, path);

        if (!isExist)
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var bucket = _entities[_bucketName];
        var memoryEntity = bucket[path];

        return Task.FromResult(memoryEntity);
    }

    private Task PurgeEntity(PurgeParameters purgeParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(purgeParameters.Path);
        if (!string.IsNullOrEmpty(path))
        {
            var bucket = _entities[_bucketName];
            var itemToDelete = bucket.Keys.Where(x => x.StartsWith(path)).Select(p => p);

            var toDelete = itemToDelete.ToList();
            if (toDelete.Any())
            {
                foreach (var item in toDelete)
                {
                    bucket.Remove(item);
                }
            }
        }
        else
        {
            if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
                _entities.Remove(_bucketName);
        }

        return Task.CompletedTask;
    }

    private async Task<IEnumerable<PluginContext>> ListEntities(ListParameters listParameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(listParameters.Path);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<PluginContext>();
        var buckets = new List<string>();

        if (_bucketName == "")
            throw new Exception(Resources.BucketNameIsRequired);
        
        buckets.Add(_bucketName);

        await Task.WhenAll(buckets.Select(b =>
            List(storageEntities, b, path, listParameters))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private bool FolderExist(string bucketName, string path)
    {
        if (!BucketExists(bucketName))
            return false;

        var bucket = _entities[bucketName];
        var folderPrefix = path + PathHelper.PathSeparator;
        return bucket.Keys.Any(x => x.StartsWith(folderPrefix));
    }

    private bool BucketExists(string bucketName)
    {
        try
        {
            return _entities.ContainsKey(bucketName);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ObjectExists(string bucketName, string path)
    {
        try
        {
            if (!BucketExists(bucketName))
                return false;

            var bucket = _entities[bucketName];
            return bucket.ContainsKey(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private Task List(ICollection<PluginContext> result, string bucketName, string path,
        ListParameters listParameters)
    {
        if (!_entities.ContainsKey(bucketName))
            throw new Exception(string.Format(Resources.BucketNotExist, bucketName));

        var bucket = _entities[bucketName];

        foreach (var (key, _) in bucket)
        {
            if (key.StartsWith(path))
            {
                var memPluginContext = bucket[key];
                result.Add(memPluginContext);
            }
        }

        return Task.CompletedTask;
    }
    #endregion
}