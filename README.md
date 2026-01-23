# samedis-care-catalog-sync

.Net Core project to read excel file or query from any sql server source and insert or update staff records.
You can fork with project and modify it to your own needs.

## Setup

1. Copy and modify `config.yml.example` to `config.yml`
2. Adjust settings in `config.yml`
3. Compile the application to your target OS, modify `SamedisExternalSync.csproj` to your requirements
   - Follow https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli for more details on compile and deploy
4. Run the application manually or setup a `cron task` or `task` on windows systems.

## Proxy server support

To access Samedis.care you need internet access. If this requires a proxy server you can configure your settings in the `config.yml` section of `http`.

```
  proxy: ""
  proxy_username: ""
  proxy_password: ""
```

## WIP

> Documentation pending, when project first steps done.