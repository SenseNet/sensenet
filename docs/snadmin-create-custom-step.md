# Create a custom SnAdmin step
**sensenet ECM** has a powerful [package installer infrastructure](https://github.com/SenseNet/sn-admin) that can be used by portal builders, developers and operators to patch and upgrade the core product or custom solution installations. In this article **developers** can learn how to create custom install steps for the packaging infrastructure, if none of the [built-in steps](snadmin-builtin-steps.md) are sufficient for a particular goal.

The packaging infrastructure in sensenet ECM allows developers to customize the package behavior by writing a **custom Step** class, built on the packaging API.

When you want to use a custom step class in a package, you need to provide the codebehind for the *SnAdmin* tool. You can do that by placing your custom library in one of the following places:
- the web\bin folder of the website (for custom steps that will be executed multiple times in subsequent packages)
- into the package itself: SnAdmin will look for custom steps in the following places inside the package zip:
   - the *PackageCustomization* folder
   - any folder defined as a custom binary holder in the manifest file

# Custom step
We will create a custom step to execute a content query for discovering the count of a content set and terminate package execution or write to the log if it is out of a specified range.

## Create a step class
- Create a new .Net project and add a new class that inherits the *SenseNet.Packaging.Steps.Step* base class.
- Implements the abstract *Execute* method.
``` csharp
public class ContentQueryPrerequisit : Step
{
    public override void Execute(ExecutionContext context)
    {
        ...
    }
}
```
>**Note:** before you choose your base class, please take a look at the [built-in steps](snadmin-builtin-steps.md), there are many useful basic steps there that you can **inherit** from (e.g. text editing or xml-related stuff, loops and conditional statements).

**Optional**: override the *ElementName* property *if the class name is not appropriate*. You will need to provide this name in the package manifest XML when using your custom step.
``` csharp
public override string ElementName { get { return "ContentCountCondition"; } }
```
Implement the properties that you want to expose to the package builder.
``` csharp
public string Query { get; set; }
public int CountMin { get; set; }
public int CountMax { get; set; }
public bool Terminate { get; set; }
```
We would like to provide the content query as an XML element text in the manifest so mark the *Query* property with the *DefaultPropertyattribute*:
``` csharp
[DefaultProperty]
public string Query { get; set; }
```

## Implement the operation
``` csharp
public override void Execute(ExecutionContext context)
{
    // Execute the provided query and get the count.
    var count = ContentQuery.Query(this.Query).Count;
    // Echo the query.
    Logger.LogMessage("Query: " + Query);
    // Terminate if the step specifies and the count is out of the expected range
    if (Terminate && (count < CountMin || count > CountMax))
        throw new PackagePreconditionException(String.Format("Count is {0} but the expected range: {1} - {2}", count, CountMin, CountMax));
    // Log the count and expectations.
    Logger.LogMessage(String.Format("Count is {0}, expected range: {1} - {2}.", count, CountMin, CountMax));
}
```

## Terminate package execution
Take a look at the package termination: if you throw a common exception in a custom step, the package will be terminated and the full exception (inner exceptions and stack trace) will be written to the console and to the log. But if the exception is *PackagePreconditionException*, only the message will be printed. For example:
``` text
================================================== #2/2 ContentCountCondition
Query: TypeIs:ContentType
PRECONDITION FAILED:
Count is 0 but the expected range: 100 - 200
===============================================================================
SnAdmin stopped with error.
Ok
```

## Start the Content Repository
Executing a content query needs a running repository, so you need to make sure that you place a **StartRepository** step before your custom step into the manifest.

It is possible to check if the repository is running: the *AssertRepositoryStarted* method of the *execution context* (see below) checks if the StartRepository step has already been executed. The current execution context is passed to every step so insert this at the start of the Execute method:
``` csharp
// Check the running state of the repository.
context.AssertRepositoryStarted();
```

## Check sensenet ECM version
It is possible to check for the current version of any of the installed components (including the core Services layer, which is a component itself). Here is an example for that:
``` csharp
var version = RepositoryVersionInfo.Instance.Version;
```
You can use the values above (or a few other helper properties on the objects above) to determine if your custom step is able to execute in a certain environment.

## Create and execute the package
Create a *TestPackage1* directory under your *web\Admin* directory. Create the *PackageCustomization* directory under TestPackage1 and copy your assembly there. Create a text file under TestPackage1 with the name *manifest.xml* and the following content:
``` xml
<?xml version="1.0" encoding="utf-8"?>
<Package type="Tool">
  <Id>MyCompany.TestTool</Id>
  <ReleaseDate>2014-04-01</ReleaseDate>
  <Steps>
    <StartRepository />  
    <ContentCountCondition CountMin="100" CountMax="200" terminate="true">Type:ContentType .AUTOFILTERS:OFF .COUNTONLY</ContentCountCondition>
  </Steps>
</Package>
```
Start SnAdmin with this command from the *web\Admin\bin* folder:
``` xml
SnAdmin TestPackage1
```
After a few seconds you will see the results:
``` text
===============================================================================
                              SnAdmin v1.0
===============================================================================
Start at 2014-04-29 08:21:58
Target:  C:\Dev10\SenseNet\Development\Budapest\Source\SenseNet\WebSite
Package: C:\Dev10\SenseNet\Development\Budapest\Source\SenseNet\WebSite\Admin\TestPackage1
Loading package customizations:
  DemoInstallSteps.dll
Name:    TestTool
Type:    Tool
Current version: 6.3.1
===============================================================================
                              Executing phase 1/1
===============================================================================
Executing steps
================================================== #1/2 StartRepository
Starting ... Ok.
Assemblies:
  References: 29.
  Loaded before start: 54.
  Plugins: 0.
Index: C:\Dev10\SenseNet\Development\Budapest\Source\SenseNet\WebSite\App_Data\LuceneIndex\20140428102242.
Index was read only: false
-------------------------------------------------------------
Time: 00:00:05.3780843
================================================== #2/2 ContentCountCondition
Query: Type:ContentType .AUTOFILTERS:OFF .COUNTONLY
Count is 157, expected range: 100 - 200.
-------------------------------------------------------------
Time: 00:00:00.0532635
=============================================================
All steps are executed.
Aggregated time: 00:00:05.4361648
Errors: 0
-------------------------------------------------------------
Stopping repository ... Ok.
===============================================================================
SnAdmin has been successfully finished.
Ok
See log file: C:\Dev10\SenseNet\Development\Budapest\Source\SenseNet\WebSite\Admin\log\TestPackage1_20140429-082158.log
```

# Packaging API
There are two elements to aid you when writing custom steps:
- Protected helper methods of the abstract Step class.
- **ExecutionContext** object passed to step execution.

## Step base
``` csharp
/// <summary>Represents one activity in the package execution sequence</summary>
public abstract class Step
{
    /*=========================================================== Instance part ===========================================================*/
    /// <summary>Returns the XML name of the step element in the manifest. Default: simple or fully qualified name of the class.</summary>
    public virtual string ElementName { get { return this.GetType().Name; } }
    /// <summary>Order number in the phase.</summary>
    public int StepId { get; private set; }
    /// <summary>The method that executes the activity. Called by packaging framework.</summary>
    public abstract void Execute(ExecutionContext context);
    
    /*=========================================================== Common tools ===========================================================*/
    /// <summary>Returns with a full path under the package if the path is relative.</summary>
    protected static string ResolvePackagePath(string path, ExecutionContext context);
    /// <summary>Returns with a full path under the target directory on the local server if the path is relative.</summary>
    protected static string ResolveTargetPath(string path, ExecutionContext context);
    /// <summary>Returns with a full paths under the target directories on the network servers if the path is relative.</summary>
    protected static string[] ResolveNetworkTargets(string path, ExecutionContext context);
    /// <summary>Returns with a full paths under the target directories on all servers if the path is relative.</summary>
    protected static string[] ResolveAllTargets(string path, ExecutionContext context);
}
```

## Execution context
Every step runs in an execution context. It is an object that is passed to every step's *Execute* method. This object contains all information of the manifest and the current executing phase.
``` csharp
/// <summary>Contains package information for executing a step.</summary>
public class ExecutionContext
{
    /// <summary>Returns a named value that was memorized in the current phase.</summary>
    public object GetVariable(string name);
    /// <summary>Memorize a named value at the end of the current phase.</summary>
    public void SetVariable(string name, object value);
    /// <summary>Resolves a variable name (e.g. @path) to its actual value stored in the context.</summary>
    public object ResolveVariable(string text);
    /// <summary>Fully qualified path of the executing extracted package.</summary>
    public string PackagePath { get; }
    /// <summary>Fully qualified path of the executing extracted package.</summary>
    public string TargetPath { get; }
    /// <summary>UNC paths of the related network server web directories.</summary>
    public string[] NetworkTargets { get;}
    /// <summary>Parsed manifest.</summary>
    public Manifest Manifest { get; 
    /// <summary>Zero based index of the executing phase.</summary>
    public int CurrentPhase { get; }
    /// <summary>Phase count of the currently executed package.</summary>
    public int CountOfPhases { get; }
    /// <summary>Console out of the executing SnAdmin. Write here any information that you do not want to log.</summary>
    public TextWriter Console { get; }
    /// <summary>True if the StartRepository step has already executed.</summary>
    public bool RepositoryStarted { get; }
}
```

## Variable support
It is possible to use variables to pass information from one step to another. In the *Execute* method you can store and recall any object associated with a name:
``` csharp
// setting
context.SetVariable("MeaningOfLife", 42);
// getting (in another step)
var ml = (int)context.GetVariable("MeaningOfLife");
```
The execution context stores these variables for the **lifetime of the whole phase** so these are available in all steps after the one you set them in. Starting a new phase resets variables.
