using Xunit;
using TWF.Models;
using TWF.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TWF.Tests
{
    public class FileOperationsCollisionTests : IDisposable
    {
        private readonly string _testRoot;

        public FileOperationsCollisionTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "twf_collision_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }

        [Fact]
        public async Task CopyAsync_WithOverwriteAll_OverwritesAllSubsequentFiles()
        {
            // Arrange
            var sourceDir = Path.Combine(_testRoot, "source");
            var destDir = Path.Combine(_testRoot, "dest");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            var file1 = Path.Combine(sourceDir, "file1.txt");
            var file2 = Path.Combine(sourceDir, "file2.txt");
            File.WriteAllText(file1, "source1");
            File.WriteAllText(file2, "source2");

            File.WriteAllText(Path.Combine(destDir, "file1.txt"), "dest1");
            File.WriteAllText(Path.Combine(destDir, "file2.txt"), "dest2");

            var entries = new List<FileEntry>
            {
                new FileEntry { FullPath = file1, Name = "file1.txt", Size = 7, IsDirectory = false },
                new FileEntry { FullPath = file2, Name = "file2.txt", Size = 7, IsDirectory = false }
            };

            var fileOps = new FileOperations();
            int collisionCount = 0;

            // Handler returns OverwriteAll for the first collision
            Func<string, Task<FileCollisionResult>> collisionHandler = (path) =>
            {
                collisionCount++;
                return Task.FromResult(new FileCollisionResult { Action = FileCollisionAction.OverwriteAll });
            };

            // Act
            var result = await fileOps.CopyAsync(entries, destDir, CancellationToken.None, collisionHandler);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, collisionCount); // Should only be called once
            Assert.Equal("source1", File.ReadAllText(Path.Combine(destDir, "file1.txt")));
            Assert.Equal("source2", File.ReadAllText(Path.Combine(destDir, "file2.txt")));
        }

        [Fact]
        public async Task CopyAsync_WithSkipAll_SkipsAllSubsequentFiles()
        {
            // Arrange
            var sourceDir = Path.Combine(_testRoot, "source_skip");
            var destDir = Path.Combine(_testRoot, "dest_skip");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            var file1 = Path.Combine(sourceDir, "file1.txt");
            var file2 = Path.Combine(sourceDir, "file2.txt");
            File.WriteAllText(file1, "source1");
            File.WriteAllText(file2, "source2");

            File.WriteAllText(Path.Combine(destDir, "file1.txt"), "dest1");
            File.WriteAllText(Path.Combine(destDir, "file2.txt"), "dest2");

            var entries = new List<FileEntry>
            {
                new FileEntry { FullPath = file1, Name = "file1.txt", Size = 7, IsDirectory = false },
                new FileEntry { FullPath = file2, Name = "file2.txt", Size = 7, IsDirectory = false }
            };

            var fileOps = new FileOperations();
            int collisionCount = 0;

            // Handler returns SkipAll for the first collision
            Func<string, Task<FileCollisionResult>> collisionHandler = (path) =>
            {
                collisionCount++;
                return Task.FromResult(new FileCollisionResult { Action = FileCollisionAction.SkipAll });
            };

            // Act
            var result = await fileOps.CopyAsync(entries, destDir, CancellationToken.None, collisionHandler);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, collisionCount); // Should only be called once
            Assert.Equal("dest1", File.ReadAllText(Path.Combine(destDir, "file1.txt")));
            Assert.Equal("dest2", File.ReadAllText(Path.Combine(destDir, "file2.txt")));
        }
    }
}
