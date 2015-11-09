FatAntelope
===========
A tool for generating an XDT transform (Micrsoft XML Document Transform) from two .config files.
Useful for creating transforms for existing production web.config or app.config files.

How it works
============
FatAntelope parses the two config (xml) files into two trees and performs an unordered diff / comparison to identify nodes 
that have been updated, inserted or deleted. And then generates a XDT transform from the difference.

The XML diff / comparison algorithm is a C# port of the 'XDiff' algorithm described here: 
http://pages.cs.wisc.edu/~yuanwang/xdiff.html

Download
============
Download the command-line tool in [releases](https://github.com/CameronWills/FatAntelope/releases)

Usage
=====

Following build, you can use reference the FatAntelope library directly or otherwise run the command-line tool

Command Line
------------

**FatAntelope source-file target-file output-file [transformed-file]**

    source-file : (input) original config file path.  E.g. the development web.config

    target-file : (input) final config file path.  E.g. the production web.config

    output-file : (output) file path to save the generated patch .  E.g. web.release.config

    transformed-file : (output, optional) file path to save the result from applying the output-file to the source-file.

Known Issues
============
- The generated XDT transform may not have the most optimal values for the xdt:Locator and xdt:Transform attributes. I recommend only using this output as a starting-point for your transforms.

- The XML comparison algorithm used (XDiff) does an unordered comparison of XML nodes, so changes to child elements' position within the same parent will be ignored.