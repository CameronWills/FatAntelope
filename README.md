# FatAntelope

A tool for comparing two .config files and generating an XDT transform (Microsoft XML Document Transform).
Useful for creating config transforms for existing production web.config or app.config files.

[![Build status](https://ci.appveyor.com/api/projects/status/iii1m7n3cdq3v5xm?svg=true)](https://ci.appveyor.com/project/CameronWills/fatantelope)
[![Release](https://img.shields.io/github/release/CameronWills/fatantelope.svg)](https://github.com/CameronWills/FatAntelope/releases/latest)
[![NuGet](https://buildstats.info/nuget/fatantelope)](https://www.nuget.org/packages/FatAntelope/)
[![Stars](https://img.shields.io/github/stars/CameronWills/fatantelope.svg)](https://github.com/CameronWills/FatAntelope/stargazers)

## How it works

FatAntelope parses the two config (xml) files into two trees and performs an unordered diff / comparison to identify nodes 
that have been updated, inserted or deleted, and then generates a XDT transform from these differences.

The XML diff / comparison algorithm is a C# port of the 'XDiff' algorithm described here: 
http://pages.cs.wisc.edu/~yuanwang/xdiff.html

## Try It
A demonstration is setup here: [fatantelopetester-app.azurewebsites.net](https://fatantelopetester-app.azurewebsites.net/)

## Download

Download the command-line tool in [releases](https://github.com/CameronWills/FatAntelope/releases)

## Usage

Following build, you can use reference the FatAntelope.Core library directly or otherwise run the command-line tool

### Command Line Syntax

```
FatAntelope source-file target-file output-file [transformed-file]

   source-file : (input) original config file path.  E.g. the development web.config

   target-file : (input) final config file path.  E.g. the production web.config

   output-file : (output) file path to save the generated patch.  E.g. web.release.config

   transformed-file : (output, optional) file path to save the result from applying the output-file to the source-file.
```

## Example

`FatAntelope app.config prod_app.config app.release.config`

Source File (app.config):

```xml
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
```

Target File (prod_app.config) :

```xml
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
```

Patch File (app.release.config) :

```xml
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
```

## Caveats

- The generated XDT transform may not have the most optimal values for the xdt:Locator and xdt:Transform attributes. I recommend only using this output as a starting-point for your transforms.

- The XML comparison algorithm used (XDiff) does an unordered comparison of XML nodes, so changes to child elements' position within the same parent will be ignored.

## Why 'FatAntelope'

A fat antelope for a [slow cheetah](https://github.com/microsoft/slow-cheetah)

![FatAntelope](banner.jpg)
