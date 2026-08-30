// The API client, query builder, routing and lookup logic now come from SamedisCare.Api, and
// the cross-cutting helpers from SamedisCare.Helper. Declared globally so the resource files
// read as they did before the migration.
global using SamedisCare.Api.Auth;
global using SamedisCare.Api.Common;
global using SamedisCare.Api.Http;
global using SamedisCare.Api.Lookup;
global using SamedisCare.Api.Query;
global using SamedisCare.Api.Routing;
global using SamedisCare.Api.V4.Common;
global using SamedisCare.Helper;
global using SamedisCare.Helper.Config;
global using SamedisCare.Helper.Data;
global using SamedisCare.Helper.IO;
global using SamedisCare.Helper.Logging;
global using SamedisCare.Helper.Text;
