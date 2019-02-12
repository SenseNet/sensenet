## iTextSharp text extractor for sensenet
This is a legacy package containing a PDF text extractor for sensenet, built on the last free version of _iTextSharp_. The package is built on the .Net Framework so cannot be used in a .Net Core environment.

## Usage
Install the following NuGet package:

[![NuGet](https://img.shields.io/nuget/v/SenseNet.TextExtractors.Pdf.svg)](https://www.nuget.org/packages/SenseNet.TextExtractors.Pdf)

To configure the text extractor, please go to the `Indexing` settings in the Content Repository and set the following class for the `pdf` extension.

```json
{
  "TextExtractors": {
    "pdf": "SenseNet.TextExtractors.Pdf.iTextSharpPdfTextExtractor"
  }
}
```