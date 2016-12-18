# log-storage-service

## **Problem**

### Background: 
An automated test framework is used to run tens of thousands of tests per week, all of which generate various output files. Files are being zipped and should be uploaded to this service on Azure. 

### Requirements:
- An App Service with a Controller written in C#.  
- A URL where zipped files can be sent using RESTFul API, e.g. HTTP requests.  
- On success, API returns proper status code with a URL to the unzipped contents in the response body.  
- It supports load balancing (such as Azure Traffic Manager)
- Unit tests

## **Solution**

### Design choices:

- We chose `ASP.NET Core 1.1` since its latest, greatest and [fastest ASP.NET](https://blogs.msdn.microsoft.com/webdev/2016/11/16/announcing-asp-net-core-1-1/) yet! 
- HTTP `PUT` is preferred over `POST` due to its idempotence. (see [OneDrive API](https://dev.onedrive.com/items/upload_put.htm))
- `ContentType` is currently limited to `application/zip` but can easily be modified/extended.
- We've optimized for writes hence zip is directly being put in blob storage. Reads can be optimized by using ETag, response caching and CDN.
- Each blob has associated metadata which is information about inner files (`name`, `length`, `lastModified`)
- We chose `Block Blobs` since it allows us to upload multiple blocks in parallel to decrease upload time.
- Blob Storage request options (`SingleBlobUploadThresholdInBytes` and `ParallelOperationThreadCount`) can be configured in `appSettings` and can be changed on fly without having to reboot the server.
- Upload file size is handled on `IIS` level (`maxAllowedContentLength` is currently set to [60MB](https://docs.microsoft.com/en-us/azure/azure-subscription-service-limits#storage-limits) in `Web.config`)
- [Static Files Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files) is used for serving files inside zip archive. This way we get some functionality for free.
- Controllers are tested using `TestHost` so that routes and request headers can also be tested.
- Tests for `StorageService` require Azure Storage Emulator to be up and running.

### Assumptions:
- Re-uploading zip files with same name will override them.
- Zip archive has flat structure inside and no hierarchies.

### Future improvements:
- Multiple file uploads using `multipart/form-data`: This way Blob Storage can be utilized even more efficiently, however we must handle partial failures in that case.
- Distributed In-Memory Caching using `Redis`
- Protect API by Authorization (e.g Azure AD)
- Resumable upload if test zip outputs are expected to be large.
- `LZ4` for transferring zips: LZ4 is scalable with multi-cores CPU. [Benchmarks suggest](http://catchchallenger.first-world.info/wiki/Quick_Benchmark:_Gzip_vs_Bzip2_vs_LZMA_vs_XZ_vs_LZ4_vs_LZO) it offers fast compression/decompression at the expense of memory.
- Define fault-handling policies such as retry, wait or circuit-break using [Polly](https://github.com/App-vNext/Polly)
- Blob `MetaData` has a size limit of 8K including both name and value. Consider alternatives if several inner files are expected inside the archive.