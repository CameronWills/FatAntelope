FatAntelope README file
==============================
https://github.com/CameronWills/FatAntelope

1. What is FatAntelope
2. Syntax
3. Example

1. What is FatAntelope
----------------------
A command line utility that diffs / compares two web.config or app.config files and generates 
an XDT file (Micrsoft XML Document Transform).

Useful for creating transforms for existing production web.config or app.config files.

2. Syntax
---------
usage: FatAntelope source-file target-file output-file [transformed-file]

	source-file			: (input) original config file name.

	target-file			: (input) final config file name.

	output-file			: (output) generated patch file name.

	transformed-file	: (output, optional) config file name resulting from applying the output-file to the source-file just for checking. 
							This file should be semantically equal to the target-file.

3. Example
----------
The example snippets below show the input source file, target file and then the generated patch file.

Source File :

<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
  </startup>
  <connectionStrings>
    <add name="Users" connectionString="Data Source=127.0.0.1;initial catalog=UserDB;user id=myUser;password=myPassword" providerName="System.Data.EntityClient" />
    <add name="Posts" connectionString="Data Source=127.0.0.1;initial catalog=PostDB;user id=myUser;password=myPassword" providerName="System.Data.EntityClient" />
  </connectionStrings>
  <appSettings>
    <add key="MyUrl" value="http://localhost:50634" />
    <add key="MyService.Api" value="http://localhost:10424" />
  </appSettings>
</configuration>


Target File :

<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
  </startup>
  <connectionStrings>
    <add name="Users" connectionString="Data Source=203.1.1.1;initial catalog=UserDB;user id=myUser;password=myPassword" providerName="System.Data.EntityClient" />
    <add name="Posts" connectionString="Data Source=203.1.1.1;initial catalog=PostDB;user id=myUser;password=myPassword" providerName="System.Data.EntityClient" />
  </connectionStrings>
  <appSettings>
    <add key="MyUrl" value="http://MyAmazingWebsite.com" />
    <add key="MyService.Api" value="http://MyAmazingWebsite.com/Api" />
  </appSettings>
</configuration>


Patch File :

<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
  <connectionStrings>
    <add name="Users" xdt:Locator="Match(name)" connectionString="Data Source=203.1.1.1;initial catalog=UserDB;user id=myUser;password=myPassword" xdt:Transform="SetAttributes(connectionString)" />
    <add name="Posts" xdt:Locator="Match(name)" connectionString="Data Source=203.1.1.1;initial catalog=PostDB;user id=myUser;password=myPassword" xdt:Transform="SetAttributes(connectionString)" />
  </connectionStrings>
  <appSettings>
    <add key="MyUrl" xdt:Locator="Match(key)" value="http://MyAmazingWebsite.com" xdt:Transform="SetAttributes(value)" />
    <add key="MyService.Api" xdt:Locator="Match(key)" value="http://MyAmazingWebsite.com/Api" xdt:Transform="SetAttributes(value)" />
  </appSettings>
</configuration>