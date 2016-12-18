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
- `PUT` is preferred over `POST` due to its idempotence. (e.g [OneDrive](https://dev.onedrive.com/items/upload_put.htm))
- `ContentType` is limited to `application/zip`. Can be easily changed/extended.
- Its anticipated that more writes will happen than reads, hence zip is directly being put in blob storage
- Each blob has associated metadata which is information about inner files (`name`, `length`, `lastModified`)
- Upload file size is handled on `IIS` level (`maxAllowedContentLength` is currently set to [60MB](https://docs.microsoft.com/en-us/azure/azure-subscription-service-limits#storage-limits) in `Web.config`)
- For downloading we use `ASP.NET Core`'s [Static Files Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files). This way we get some functionality for free.
- `Blob Storage` request options can be changed on the fly without having to reboot the server.
- Controllers are tested using `TestHost` so that routes and request headers can also be tested.
