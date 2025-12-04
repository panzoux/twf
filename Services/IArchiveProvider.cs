namespace TWF.Services
{
    using TWF.Models;

    /// <summary>
    /// Interface for archive format providers
    /// </summary>
    public interface IArchiveProvider
    {
        /// <summary>
        /// Gets the file extensions supported by this provider (e.g., ".zip", ".tar")
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// Lists the contents of an archive file
        /// </summary>
        /// <param name="archivePath">Path to the archive file</param>
        /// <returns>List of file entries within the archive</returns>
        List<FileEntry> List(string archivePath);

        /// <summary>
        /// Extracts an archive to a destination directory
        /// </summary>
        /// <param name="archivePath">Path to the archive file</param>
        /// <param name="destination">Destination directory path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> Extract(string archivePath, string destination, CancellationToken cancellationToken);

        /// <summary>
        /// Compresses files into an archive
        /// </summary>
        /// <param name="sources">List of source file paths</param>
        /// <param name="archivePath">Path for the output archive file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> Compress(List<string> sources, string archivePath, CancellationToken cancellationToken);
    }
}
