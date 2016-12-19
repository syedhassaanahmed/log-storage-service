# log-storage-service

## **Problem**

### Background: 
An automated test framework is used to run tens of thousands of tests per week, all of which generate various output files. Files are being zipped and should be uploaded to this service on Azure. 

### Requirements:
- An `App Service` with a `Controller` written in `C#`.  
- A URL where zipped files can be sent using RESTFul API, e.g. HTTP requests.  
- On success, API returns proper status code with a URL to the unzipped contents in the response body.  
- It supports load balancing (such as `Azure Traffic Manager`)
- Unit tests

## **Solution**
[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

### Setup Requirements:
- Visual Studio 2015
- [.NET Core 1.1 SDK](https://www.microsoft.com/net/download/core#/current)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/storage-use-emulator) for running `StorageServiceTests`

### Design choices:
- We chose `ASP.NET Core 1.1` since its latest, greatest and [fastest ASP.NET](https://blogs.msdn.microsoft.com/webdev/2016/11/16/announcing-asp-net-core-1-1/) yet! 
- HTTP `PUT` is preferred over `POST` due to its idempotence. (see [OneDrive API](https://dev.onedrive.com/items/upload_put.htm))
- `ContentType` is currently limited to `application/zip` but can easily be modified/extended.
- We've optimized for writes hence zip is directly being put in blob storage. Reads can be optimized by using `ETag`, response caching, `CDN` or even `Redis` Cache.
- Each blob has associated metadata which is information about inner files (`name`, `length`, `lastModified`)
- We chose `Block Blobs` since it allows us to upload multiple blocks in parallel to decrease upload time.
- Blob Storage request options (`SingleBlobUploadThresholdInBytes` and `ParallelOperationThreadCount`) can be configured in `appSettings.json` or Azure portal and do not require app restart in order to be changed.
- Upload file size is handled on `IIS` level (`maxAllowedContentLength` is currently set to [60MB](https://docs.microsoft.com/en-us/azure/azure-subscription-service-limits#storage-limits) in `Web.config`)
- [Static Files Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files) is used for serving files inside zip archive. This way we get some functionality for free.
- For each upload `MD5` hash is computed and stored inside Blob `Properties`. It gets verified for each download. On a **dual-core i7** CPU it takes **~150ms** to calculate hash on a **60MB** file. If raw performance is needed, this can be turned off.
- Controllers are tested using `TestHost` so that routes and request headers can also be asserted.

### Assumptions:
- Re-uploading zip files with same name will override them.
- Zip archive has flat structure inside and no hierarchies.

### Future improvements:
- Instead of directly putting in Blob storage, store archive on local disk first and let a `WebJob` upload them to Blob storage. Download Requests in the meantime can be served from disk.
- Multiple file uploads using `multipart/form-data` adds some flexibility for the API consumer however we must handle partial failures in that case.
- Protect API by adding Authorization Middleware.
- Resumable upload if test zip outputs are expected to be large.
- `LZ4` for transferring zips: `LZ4` is scalable with multi-cores CPU. [Benchmarks suggest](http://catchchallenger.first-world.info/wiki/Quick_Benchmark:_Gzip_vs_Bzip2_vs_LZMA_vs_XZ_vs_LZ4_vs_LZO) it offers fast compression/decompression at the expense of memory.
- Blob `MetaData` has a size limit of **8K** including both name and value. Consider alternatives if several inner files are expected inside the archive.
- Consider keeping fresh archives in `hot tier` while moving older ones to `cool tier`.
- Monitoring and diagnostics using `ApplicationInsights`