using System.Xml.Linq;

if (args.Length < 3)
{
	Console.WriteLine("Usage: Nuspec update <csproj-path> <nuspec-path>");
	return;
}

string type = args[0];
string csprojPath = args[1];
string nuspecPath = args[2];

if (type != "update")
{
	Console.WriteLine("Invalid command. Only 'update' is supported.");
	return;
}

if (!File.Exists(csprojPath) || !File.Exists(nuspecPath))
{
	Console.WriteLine("Both .csproj and .nuspec files must exist.");
	return;
}

try
{
	// Load the .csproj file
	var namespacePrefix = "{http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd}";
	var csprojXml = XDocument.Load(csprojPath);
	var packageReferences = csprojXml.Descendants("PackageReference")
		.Select(pr => new
		{
			Id = pr.Attribute("Include")?.Value,
			Version = pr.Attribute("Version")?.Value,
			PrivateAssets = pr.Attribute("privateAssets")?.Value.Equals("true", StringComparison.OrdinalIgnoreCase)
			                ?? pr.Elements().Any(e => e.Name.LocalName.Equals("PrivateAssets", StringComparison.OrdinalIgnoreCase))
		})
		.Where(pr => pr.Id != null && pr.Version != null && pr.PrivateAssets == false)

		.ToList();

	// Load the .nuspec file
	var nuspecXml = XDocument.Load(nuspecPath);


	XElement? metadataElement = nuspecXml.Root?.Element(namespacePrefix + "metadata");
	if (metadataElement == null || metadataElement.Name.LocalName != "metadata")
	{
		Console.WriteLine("Invalid .nuspec file. Missing <metadata> as first element.");
		return;
	}

	XElement? dependenciesElement = metadataElement.Element(namespacePrefix + "dependencies");
	if (dependenciesElement == null)
	{
		dependenciesElement = new XElement("dependencies");
		metadataElement.Add(dependenciesElement);
	}
	else
	{
		dependenciesElement.RemoveAll();
	}

	// Add each package reference to the .nuspec file
	foreach (var package in packageReferences)
	{
		if (package.Id == null || package.Version == null)
		{
			Console.WriteLine("Invalid package reference. Skipping.");
			continue;
		}

		var dependency = new XElement("dependency",
			new XAttribute("id", package.Id),
			new XAttribute("version", package.Version));

		dependenciesElement.Add(dependency);
	}

	// Save the updated .nuspec file
	nuspecXml.Save(nuspecPath);

	// remove xmlns=""
	File.WriteAllText(nuspecPath, File.ReadAllText(nuspecPath).Replace(" xmlns=\"\"", ""));

	Console.WriteLine("Successfully updated the .nuspec file with current NuGet package references.");
}
catch (Exception ex)
{
	Console.WriteLine($"An error occurred: {ex.Message}");
}
