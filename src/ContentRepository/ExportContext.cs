using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using SenseNet.ContentRepository.Storage;
using System.IO;

namespace SenseNet.ContentRepository
{
	public class ExportContext
	{
		private List<string> _outerReferences;
		public string SourceFsPath { get; private set; }
		public string CurrentDirectory { get; set; }
		public string ContentTypeDirectory { get; private set; }

		private string _sourceRoot;

		public ExportContext(string sourceRepositoryPath, string targetFsPath)
		{
			if (RepositoryPath.IsValidPath(sourceRepositoryPath) != RepositoryPath.PathResult.Correct)
				throw new InvalidPathException("Source path is invalid: " + sourceRepositoryPath);
			SourceFsPath = sourceRepositoryPath;
			CurrentDirectory = targetFsPath;
			_sourceRoot = String.Concat(sourceRepositoryPath, sourceRepositoryPath.EndsWith("/") ? "" : "/");
			_outerReferences = new List<string>();

			if (sourceRepositoryPath == Repository.RootPath)
			{
				ContentTypeDirectory = Path.Combine(targetFsPath, Repository.RootName);
				ContentTypeDirectory = Path.Combine(ContentTypeDirectory, Repository.SystemFolderName);
				ContentTypeDirectory = Path.Combine(ContentTypeDirectory, Repository.SchemaFolderName);
				ContentTypeDirectory = Path.Combine(ContentTypeDirectory, Repository.ContentTypesFolderName);
			}
			else if (sourceRepositoryPath == Repository.SystemFolderPath)
			{
				ContentTypeDirectory = Path.Combine(targetFsPath, Repository.SystemFolderName);
				ContentTypeDirectory = Path.Combine(ContentTypeDirectory, Repository.SchemaFolderName);
				ContentTypeDirectory = Path.Combine(ContentTypeDirectory, Repository.ContentTypesFolderName);
			}
			else if (sourceRepositoryPath == Repository.SchemaFolderPath)
			{
				ContentTypeDirectory = Path.Combine(targetFsPath, Repository.SchemaFolderName);
				ContentTypeDirectory = Path.Combine(ContentTypeDirectory, Repository.ContentTypesFolderName);
			}
			else if (sourceRepositoryPath == Repository.ContentTypesFolderPath)
			{
				ContentTypeDirectory = Path.Combine(targetFsPath, Repository.ContentTypesFolderName);
			}
		}
		public void AddReference(string path)
		{
			if (path == SourceFsPath)
				return;
			if (path.StartsWith(_sourceRoot))
				return;
			if(_outerReferences.Contains(path))
				return;
			_outerReferences.Add(path);
		}

		public ReadOnlyCollection<string> GetOuterReferences()
		{
			return _outerReferences.AsReadOnly();
		}
	}
}
