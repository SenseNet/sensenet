# How to contribute
Thank you for checking out our project! :star2: :sunny: :deciduous_tree: :earth_americas:

All kinds of contributions are welcome, including ideas, requests for improvement (be it code or [documentation](http://wiki.sensenet.com)), bugfixes or features. We are happy if you simply use these projects, but it is marvelous :sparkling_heart: if you decide to make your changes public for the benefit of others.

Please start by **creating an issue** (if you do not find an existing one) either in the [main repository](https://github.com/SenseNet/sensenet) or in one of the [smaller ones](https://github.com/SenseNet). Issues may vary from a small bug to a discussion about a large architectural change, feel free to create them! 

## Enterprise customers
SenseNet ECM has an *Enterprise Edition* that is not published here on Gihub. However the source code is **identical to the Community Edition** published here - except for some modules that are not available in the *Community Edition* (for example Active Directory synchronization). For differences please visit [this article](http://wiki.sensenet.com/Differences_between_Community_and_Enterprise_editions) on our wiki.

If you work with the Enterprise Edition, you can file tickets in our [JIRA](http://support.sensenet.com), but if you already have an exact source code change in your mind, it is easier (and more agile) if you make that change here the same way as others - it will flow through the pipes to the same place in the Enterprise Edition and will be included in the next patch.

## SenseNet and its components
This is a huge project that consists of multiple repositories:
- main [SenseNet github repository](https://github.com/SenseNet/sensenet) for the core product
- many [smaller repositories](https://github.com/SenseNet) for the components used either by the main prodoct or anybody in the ecosystem.

These components have their own version number and they are developed and released independently from the main product. Some of them (e.g. [Tools](https://github.com/SenseNet/sn-tools) or [Task Management](https://github.com/SenseNet/sn-taskmanagement)) can be used in any project, others (e.g. [SenseNet Client for .Net](https://github.com/SenseNet/sn-client-dotnet)) are more closely tied to the core product.

## Reporting a bug
When creating a bug report, please:

- Provide a short, clear **title** and **description**.
- State the exact **version number** of the project you are using (look for it in dll properties or *AssemblyInfo.cs* files if you have the source code).
- Provide some details on the environment (browser type in case of client-side issues, dev machine or server, stuff like that).
- List the steps you took (where did you click? what input did you provide? which method did you call?).
- Code samples, screenshots, **log entries** (Event log, [SnTrace](https://github.com/SenseNet/sn-tools/tree/master/src/SenseNet.Tools/Diagnostics), UI error messages in text format) are welcome!

## Participate in the discussion
It also helps if you share your experience, thoughts or opinion on existing issues, filed by others (and commenting on others' pull requests). You may also consider helping out others by checking out SenseNet-related posts in [Stack Overflow](http://stackoverflow.com/questions/tagged/sensenet).

## Making a change

1. Github has a cool [overview](https://guides.github.com) of the workflows and basic git stuff, please check it out if you are not familar with how things work here :ok_hand:.
2. If possible, avoid making broad changes (e.g. a huge refactor) before talking to us; the more files you change, the harder it is to review and merge the commit.
3. Start working by [forking](https://help.github.com/articles/working-with-forks) the repository you want to improve, than making a *branch* for the fix/feature.
4. The dev environment is usually not complicated in case of smaller components (for example [SenseNet Tools](https://github.com/SenseNet/sn-tools) or [Client for .Net](https://github.com/SenseNet/sn-client-dotnet)) - just build it, and you're good to go. In case of the main product we have a [list of steps](http://wiki.sensenet.com/How_to_install_Sense/Net_from_source_package_(IIS_7.5_and_IIS_7.0)) you should take to assemble the environment.
5. We have a list of [Coding Conventions](http://wiki.sensenet.com/Coding_Conventions) for SenseNet projects. Please try to follow that guide when you write code (it contains the usual stuff: code formatting, best practices and common mistakes).
6. Unit tests are nice, please add some, if possible.
7. You may also use our [benchmark tool](https://github.com/SenseNet/sn-benchmark) to measure the performance of the product before and after the change, if necessary.
7. When you are confident with your fix/feature, create a [pull request](https://help.github.com/articles/creating-a-pull-request-from-a-fork). We will get notified right away :smiley:.

Please be patient if we do not accept the pull request immediately or ask for changes. We'll try to justify our change requests so that you know our intentions. It may speed up the process, if you [allow us to modify your branch](https://help.github.com/articles/allowing-changes-to-a-pull-request-branch-created-from-a-fork) when you create the pull request.

Thanks!

*snteam*