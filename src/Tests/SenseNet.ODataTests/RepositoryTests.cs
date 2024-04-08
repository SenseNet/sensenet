﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.i18n;
using SenseNet.ODataTests;
using SenseNet.Testing;
using System.Collections.Generic;
using System.Globalization;

namespace SenseNet.ContentRepository.Tests
{
	[TestClass]
	public class RepositoryToolsTest : ODataTestBase
	{
		[TestMethod]
		public void GetResourceClass_ShouldGiveBackJson()
		{
			Test(async () =>
			{
				var resManAcc = new ObjectAccessor(SenseNetResourceManager.Current);
				var hacked = GetTestResourceData();

				using (var x = new Swindler<Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>>>(hacked,
					() => (Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>>)resManAcc.GetField("_items"),
					backup => resManAcc.SetField("_items", backup)
				))
				{
					var response = await ODataGetAsync("/OData.svc/('Root')/GetResourceClass", "?className=Class1&langCode=en");
					var responseBody = Deserialize(response.Result);

					Assert.IsNotNull(responseBody);
					Assert.AreEqual("value2", responseBody["test2"]);
					Assert.AreEqual(200, response.StatusCode);
				}
			});
		}

		[TestMethod]
		public void GetResourceClass_ShouldGiveBackBadRequest()
		{
			Test(async () =>
			{
				var resManAcc = new ObjectAccessor(SenseNetResourceManager.Current);
				var hacked = GetTestResourceData();

				using (var x = new Swindler<Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>>>(hacked,
					() => (Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>>)resManAcc.GetField("_items"),
					backup => resManAcc.SetField("_items", backup)
				))
				{
					var response = await ODataGetAsync("/OData.svc/('Root')/GetResourceClass", "?className=Class1&langCode=ro");
					var error = GetError(response);

					Assert.AreEqual(400, response.StatusCode);
					Assert.AreEqual("ApplicationError", error.ExceptionType);
				}
			});
		}

		private Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>> GetTestResourceData()
		{
			var enDict = new Dictionary<string, Dictionary<string, object>>
				{
					{ "Class1", new Dictionary<string, object> {
						{ "test", "value1" },
						{ "test2", "value2" }
					} },
					{ "Class2", new Dictionary<string, object> {
						{ "test3", "value3" },
						{ "test4", "value4" }
					} }
				};

			var resourceData = new Dictionary<CultureInfo, Dictionary<string, Dictionary<string, object>>>
			{
				{ CultureInfo.GetCultureInfo("en"), enDict }
			};

			return resourceData;
		}
	}
}
