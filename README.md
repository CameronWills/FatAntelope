FatAntelope
===========
A tool for generating an XDT transform (Micrsoft XML Document Transform) from two .config files.


How it works
============
FatAntelope parses the two config (xml) files into two trees and performs an unordered diff / comparison to identify nodes 
that have been updated, inserted or deleted. And then generates a XDT transform from the difference.

The XML diff algorithm is a C# port of the 'XDiff' algorithm described here: 
http://pages.cs.wisc.edu/~yuanwang/xdiff.html


Usage
=====

Following build, simply run the command-line tool passing in the parameters

  *FatAntelope source-file target-file output-file*

source-file: The original config file

target-file: The final config file

output-file: The generated XDT transform / diffgram

Known Issues
============
As it currently stands, the generated XDT transform may not have the most optimal values for the xdt:Locator and xdt:Transform attributes.
I recommend only using this output as a starting-point for your transforms.