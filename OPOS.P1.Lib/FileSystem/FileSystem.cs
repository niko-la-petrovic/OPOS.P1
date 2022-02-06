using DokanNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;

namespace OPOS.P1.Lib.FileSystem
{
    public abstract class IFsItem
    {
        // TODO adjust setters
        public string Name { get; set; }
        public Directory Parent { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime LastWriteTime { get; set; }

        public byte[] Data { get; set; } = new byte[0];
        public long Size => Data?.Length ?? 0;

        public FileAttributes FileAttributes { get; set; }

        public abstract FileInformation FileInformation { get; }

        public override string ToString()
        {
            return $"{Name}";
        }

    }

    public enum FileOperations
    {
        Write,
        Append,
        Copy,
        Move,
        Delete
    }

    public class FileOperationEvent
    {
        public FileOperations Operation { get; set; }
        public File File { get; set; }
    }

    public class File : IFsItem, IEquatable<File>
    {
        private static readonly SHA1 sha = SHA1.Create();

        private byte[] hash;
        public byte[] Hash
        {
            get
            {
                if (Data != null && hash == null)
                    hash = sha.ComputeHash(Data);

                return hash;
            }
        }

        public override FileInformation FileInformation =>
            new FileInformation
            {
                Attributes = FileAttributes.Normal,
                CreationTime = CreationTime,
                LastAccessTime = LastAccessTime,
                LastWriteTime = LastWriteTime,
                Length = Size,
                FileName = Name,
            };

        public override bool Equals(object obj)
        {
            if (obj is not File)
                return false;

            var o = obj as File;

            return Equals(Name, o.Name) && Equals(Hash, o.Hash)
                && Parent == o.Parent;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Hash, Parent);
        }

        bool IEquatable<File>.Equals(File other)
        {
            return Equals(other);
        }
    }

    public class Directory : IFsItem
    {
        public HashSet<Directory> Directories { get; set; } = new();
        public HashSet<File> Files { get; set; } = new();

        public IEnumerable<IFsItem> Children => (Files as IEnumerable<IFsItem>).Concat(Directories);

        public void Remove(IFsItem fsItem)
        {
            if (fsItem is null)
                return;

            if (fsItem is File file)
                Files.Remove(file);
            else if (fsItem is Directory dir)
                Directories.Remove(dir);
        }

        public override FileInformation FileInformation =>
            new FileInformation
            {
                Attributes = FileAttributes.Directory,
                CreationTime = CreationTime,
                LastAccessTime = LastAccessTime,
                LastWriteTime = LastWriteTime,
                Length = Data?.LongLength ?? 0,
                FileName = Name,
            };

        public override bool Equals(object obj)
        {
            if (obj is not Directory)
                return false;

            var o = obj as Directory;

            return Name == o.Name
                && Parent == o.Parent
                && (Directories?.SetEquals(o?.Directories) ?? false)
                && (Files?.SetEquals(o?.Files) ?? false);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Directories,
                Files,
                Parent,
                Name);
        }

        public static bool IsRoot(string filePath) => filePath == $"{Path.DirectorySeparatorChar}";

        public void Add(IFsItem item)
        {
            if (item is Directory dir)
                Directories.Add(dir);
            else if (item is File file)
                Files.Add(file);
        }

        public void RemoveByName(IFsItem item)
        {
            if (item is Directory dir)
                Directories.RemoveWhere(d => d.Name == dir.Name);
            if (item is File file)
                Files.RemoveWhere(f => f.Name == file.Name);
        }

        public static IEnumerable<string> GetPathItems(string filePath)
        {
            return filePath.Split($"{Path.DirectorySeparatorChar}")
                .Where(pi => !string.IsNullOrWhiteSpace(pi));
        }
    }

    public class FileSystem : IDokanOperations
    {
        private readonly Directory rootDirectory = new()
        {
            Name = "root",
            LastAccessTime = DateTime.Now,
            CreationTime = DateTime.Now,
            LastWriteTime = DateTime.Now
        };

        // TODO handle case where file is deleted
        private readonly ConcurrentDictionary<IFsItem, System.Timers.Timer> fileWriteTimers = new();

        //private ConsoleLogger logger = new("[IMFS] ");

        private Func<long> totalMemory = () => 0;
        private Func<long> freeMemory = () => 0;

        public event EventHandler<FileOperationEvent> OnFileWrite;

        protected virtual void OnWriteOccurred(FileOperationEvent e)
        {
            OnFileWrite?.Invoke(this, e);
        }

        public FileSystem(Func<long> getTotalMemory, Func<long> getFreeMemory)
        {
            totalMemory = getTotalMemory;
            freeMemory = getFreeMemory;
        }

        public IFsItem GetFsItem(string filePath)
        {
            FindItemParent(filePath, out var parent, out var itemName);
            if (IsRootDir(parent, itemName))
                return rootDirectory;

            return parent?.Children?.FirstOrDefault(c => c.Name == itemName);
        }

        public IEnumerable<IFsItem> GetFsItems(string filePath, string pattern)
        {
            FindItemParent(filePath, out var parent, out var itemName);

            IEnumerable<IFsItem> children;
            if (IsRootDir(parent, itemName))
                children = rootDirectory.Children;
            else
            {
                if (parent is null || string.IsNullOrWhiteSpace(itemName))
                    return null;

                var dirToList = parent.Children.First(c => c.Name == itemName) as Directory;
                children = dirToList.Children;
            }

            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
                return children;

            var filteredChildren = children
                .Where(c => c.Name.StartsWith(pattern));

            return filteredChildren;
        }

        public void Cleanup(string filePath, IDokanFileInfo info)
        {
            ClearContext(filePath, info);

            if (info.DeleteOnClose)
            {
                var item = GetFsItem(filePath);
                item?.Parent?.Remove(item);
            }
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            ClearContext(fileName, info);
        }

        private void ClearContext(string fileName, IDokanFileInfo info)
        {
            if (info.Context is FileOperationEvent fileOperationEvent)
            {
                OnFileWrite?.Invoke(this, fileOperationEvent);
            }

            (info as IDisposable)?.Dispose();
            info.Context = null;
        }

        public NtStatus CreateFile(
            string filePath,
            DokanNet.FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            FileAttributes attributes,
            IDokanFileInfo info)
        {
            Directory parent;
            IFsItem newChild;
            string itemName;
            FindItemParent(filePath, out parent, out itemName);

            if (IsRootDir(parent, itemName))
            {
                info.IsDirectory = true;
                if (mode is FileMode.Open or FileMode.OpenOrCreate or FileMode.CreateNew)
                    return NtStatus.Success;
            }

            var existingChild = parent?.Children?.FirstOrDefault(c => c.Name == itemName);
            if (existingChild is not null)
            {
                var isDirectory = existingChild is Directory;
                info.IsDirectory = isDirectory;

                if (isDirectory && mode is FileMode.OpenOrCreate or FileMode.Create or FileMode.Open)
                    return NtStatus.Success;
                else if (isDirectory && mode is FileMode.CreateNew)
                    return DokanResult.AlreadyExists;

                if (!isDirectory)
                {
                    if (mode is FileMode.OpenOrCreate or FileMode.Create)
                        return DokanResult.AlreadyExists;
                    else if (mode is FileMode.Open)
                        return NtStatus.Success;
                    else if (mode is FileMode.CreateNew)
                        return DokanResult.AlreadyExists;
                }
            }
            else if (mode is FileMode.Open or FileMode.Append or FileMode.Truncate)
                return DokanResult.FileNotFound;

            if (mode is FileMode.Create or FileMode.CreateNew)
            {
                if (info.IsDirectory)
                {
                    newChild = new Directory
                    {
                        Parent = parent,
                        Name = itemName,
                        CreationTime = DateTime.Now,
                        LastAccessTime = DateTime.Now,
                        LastWriteTime = DateTime.Now
                    };
                }
                else
                {
                    newChild = new File
                    {
                        Parent = parent,
                        Name = itemName,
                        CreationTime = DateTime.Now,
                        LastAccessTime = DateTime.Now,
                        LastWriteTime = DateTime.Now
                    };
                }

                if (mode is FileMode.Create)
                    parent?.RemoveByName(newChild);
                parent.Add(newChild);
            }

            return NtStatus.Success;
        }

        public bool IsRootDir(Directory parent, string itemName)
        {
            return parent is null && (itemName is null || itemName == Path.DirectorySeparatorChar.ToString() || itemName == rootDirectory.Name);
        }

        public void FindItemParent(string filePath, out Directory parent, out string itemName)
        {
            var pathItems = Directory.GetPathItems(filePath);
            itemName = pathItems.LastOrDefault();
            if (!pathItems.Any() || (pathItems.Count() == 1 && pathItems.Last() == rootDirectory.Name))
            {
                parent = null;
                return;
            }
            if (pathItems.Count() < 2)
                parent = rootDirectory;
            else
            {
                var currentParent = rootDirectory;
                Queue<string> pathItemQ = new(pathItems);
                while (pathItemQ.Any() && currentParent is not null)
                {
                    string currentItem = pathItemQ.Dequeue();
                    if (currentItem == itemName)
                        break;
                    currentParent = currentParent.Directories.FirstOrDefault(dir => dir.Name == currentItem);
                }
                parent = currentParent;
            }
        }

        public NtStatus DeleteDirectory(string filePath, IDokanFileInfo info)
        {
            var directory = GetFsItem(filePath);
            if (directory is Directory dir)
            {
                info.DeleteOnClose = true;
                return NtStatus.Success;
            }

            return DokanResult.FileNotFound;
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            if (info.IsDirectory)
                return NtStatus.Error;

            info.DeleteOnClose = true;
            return NtStatus.Success;
        }

        public NtStatus FindFiles(
            string fileName,
            out IList<FileInformation> files,
            IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            var matchedFiles = GetFsItems(fileName, searchPattern);

            files = matchedFiles
                .Select(file =>
                {
                    return file.FileInformation;
                })
                .ToList();

            return NtStatus.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus GetDiskFreeSpace(
            out long freeBytesAvailable,
            out long totalNumberOfBytes,
            out long totalNumberOfFreeBytes,
            IDokanFileInfo info)
        {
            freeBytesAvailable = freeMemory();
            totalNumberOfFreeBytes = freeMemory();

            totalNumberOfBytes = totalMemory();
            return NtStatus.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            var item = GetFsItem(fileName);
            if (item is null)
            {
                fileInfo = default;
                return NtStatus.Error;
            }

            fileInfo = item.FileInformation;

            return NtStatus.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
            // TODO implement
        }

        public NtStatus GetVolumeInformation(
            out string volumeLabel,
            out FileSystemFeatures features,
            out string fileSystemName,
            out uint maximumComponentLength,
            IDokanFileInfo info)
        {
            volumeLabel = "IMFS";
            features = FileSystemFeatures.None;
            fileSystemName = "IMFS";
            maximumComponentLength = 255;

            return NtStatus.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            // TODO check
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldFilePath, string newFilePath, bool replace, IDokanFileInfo info)
        {
            FindItemParent(oldFilePath, out var oldParent, out var itemName);
            var oldItem = GetFsItem(oldFilePath);

            var existingNewItem = GetFsItem(newFilePath);
            if (existingNewItem is IFsItem && replace)
                existingNewItem?.Parent?.Remove(existingNewItem);

            var pathItems = Directory.GetPathItems(newFilePath);
            var newName = pathItems.Last();
            var parentPathItems = pathItems.SkipLast(1).ToList();
            if (!parentPathItems.Any())
                parentPathItems.Add(Path.DirectorySeparatorChar.ToString());

            var parentDirPath = string.Join(Path.DirectorySeparatorChar, parentPathItems);

            var parent = GetFsItem(parentDirPath) as Directory;
            if (parent is null)
                return NtStatus.Error;

            oldParent.RemoveByName(oldItem);
            oldItem.Parent = parent;
            oldItem.Name = newName;
            parent.Add(oldItem);

            return NtStatus.Success;
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            var item = GetFsItem(fileName);
            if (item is null)
            {
                bytesRead = 0;
                return NtStatus.Error;
            }

            lock (item.Data)
            {
                int toRead = (int)Math.Min(item.Size - offset, buffer.Length);
                item.Data.AsSpan().Slice((int)offset, toRead).CopyTo(buffer.AsSpan());
                bytesRead = toRead;
            }

            return NtStatus.Success;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            return NtStatus.Error;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            var item = GetFsItem(fileName);
            if (item is null)
                return NtStatus.Error;

            item.FileAttributes = attributes;
            return NtStatus.Success;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            var item = GetFsItem(fileName);
            if (item is null)
                return NtStatus.Error;

            item.CreationTime = creationTime ?? DateTime.Now;
            item.LastAccessTime = lastAccessTime ?? DateTime.Now;
            item.LastWriteTime = lastWriteTime ?? DateTime.Now;

            return NtStatus.Success;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            var append = offset == -1;

            var file = GetFsItem(fileName) as File;
            if (file is null)
            {
                bytesWritten = 0;
                return NtStatus.Error;
            }

            var fileOpEvent = new FileOperationEvent
            {
                File = file,
            };
            info.Context = fileOpEvent;

            if (fileWriteTimers.TryGetValue(file, out var timer)){
                timer.Stop();
                timer.Dispose();
                fileWriteTimers.TryRemove(file, out _);
            }
            var newTimer = new System.Timers.Timer();
            newTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            newTimer.Elapsed += (s, e) => OnWriteOccurred(fileOpEvent);
            newTimer.AutoReset = false;
            fileWriteTimers.TryAdd(file, newTimer);
            newTimer.Start();

            lock (file.Data)
            {
                byte[] newData;
                if (append)
                {
                    newData = file.Data.Concat(buffer).ToArray();
                    fileOpEvent.Operation = FileOperations.Append;
                }
                else
                {
                    newData = file.Data.Take((int)offset).Concat(buffer).ToArray();
                    fileOpEvent.Operation = FileOperations.Write;
                }

                file.Data = newData;
                bytesWritten = buffer.Length;
            }
            file.LastAccessTime = DateTime.Now;
            file.LastWriteTime = DateTime.Now;

            return NtStatus.Success;
        }
    }
}
