namespace Microsoft.Diagnostics.Runtime;

public interface ISymbolNotification
{
	void FoundSymbolInCache(string localPath);

	void ProbeFailed(string url);

	void FoundSymbolOnPath(string url);

	void DownloadProgress(int bytesDownloaded);

	void DownloadComplete(string localPath, bool requiresDecompression);

	void DecompressionComplete(string localPath);
}
