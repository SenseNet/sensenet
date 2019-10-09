# How to contribute
Thank you for checking out our project! :star2: :sunny: :deciduous_tree: :earth_americas:

All kinds of contributions are welcome, including ideas, requests for improvement (be it code or [documentation](http://wiki.sensenet.com)), bugfixes or features. We are happy if you simply use these projects, but it is marvelous :sparkling_heart: if you decide to make your changes public for the benefit of others.

Please start by **creating an issue** (if you do not find an existing one) either in the [main repository](https://github.com/SenseNet/sensenet) or in one of the [smaller ones](https://github.com/SenseNet). Issues may vary from a small bug to a discussion about a large architectural change, feel free to create them! 

## Enterprise customers
sensenet ECM has an *Enterprise Edition* containing all the modules in the *Community Edition*. The source code is identical in case of these editions - and it is published here on GitHub.

> The Enterprise Edition may contain some additional modules that are not published here. If you have an issue with one of those, please contact us to sort it out. 

If you work with the Enterprise Edition and want to report an urgent issue, you can file tickets in our [support system](http://support.sensenet.com), or you can create an issue here on GitHub, if you are OK with discussing it in public.

If you already have an exact source code change in your mind, it is easier (and more agile) if you make that change here the same way as others - it will flow through the pipes to the same place and will be included in the next patch.

## sensenet and its components
This is a huge project that consists of multiple repositories:
- main [sensenet github repository](https://github.com/SenseNet/sensenet) for the core product
- many [smaller repositories](https://github.com/SenseNet) for optional components that can be installed on top of the main prodoct
- an [awesome list](https://github.com/SenseNet/awesome-sensenet) of sensenet-related tools and plugins

These components have their own version number and they are developed and released independently of the main product. Some of them (e.g. [Tools](https://github.com/SenseNet/sn-tools) or [Task Management](https://github.com/SenseNet/sn-taskmanagement)) can be used in any project, others (e.g. [sensenet Client for .Net](https://github.com/SenseNet/sn-client-dotnet)) are more closely tied to the core product.

## Reporting a bug
When creating a bug report, please:

- Provide a short, clear **title** and **description**.
- State the exact **version number** of the project you are using (look for it in dll properties or *AssemblyInfo.cs* files if you have the source code).
- Provide some details on the environment (browser type in case of client-side issues, dev machine or server, stuff like that).
- List the steps you took (where did you click? what input did you provide? which method did you call?).
- Code samples, screenshots, **log entries** (Event log, [SnTrace](https://github.com/SenseNet/sn-tools/tree/master/src/SenseNet.Tools/Diagnostics), UI error messages in text format) are welcome!

## Participate in the discussion
It also helps if you share your experience, thoughts or opinion on existing issues, filed by others (and commenting on others' pull requests). You may also consider helping out others by checking out sensenet-related posts in [Stack Overflow](http://stackoverflow.com/questions/tagged/sensenet).

## Making a change

1. Github has a cool [overview](https://guides.github.com) of the workflows and basic git stuff, please check it out if you are not familiar with how things work here :ok_hand:.
2. If possible, avoid making broad changes (e.g. a huge refactor) before talking to us; the more files you change, the harder it is to review and merge the commit.
3. Start work by [forking](https://help.github.com/articles/working-with-forks) the repository you want to improve, then make a *branch* for the fix/feature.
4. The dev environment is usually not complicated in case of smaller components (for example [sensenet Tools](https://github.com/SenseNet/sn-tools) or [Client for .Net](https://github.com/SenseNet/sn-client-dotnet)) - just build it, and you're good to go. In case of bigger components you may install them in a web application, build a custom library and try it out in your environment.
5. We have a list of [Coding Conventions](http://wiki.sensenet.com/Coding_Conventions) for sensenet projects. Please try to follow that guide when you write code (it contains the usual stuff: code formatting, best practices and common mistakes).
6. Unit tests are nice, please execute existing tests and add new ones if possible.
7. You may also use our [benchmark tool](https://github.com/SenseNet/sn-benchmark) to measure the performance of the product before and after the change, if necessary.
7. When you are confident with your fix/feature, create a [pull request](https://help.github.com/articles/creating-a-pull-request-from-a-fork). We will get notified right away :smiley:.

Please be patient if we do not accept the pull request immediately or ask for changes. We'll try to justify our change requests so that you know our intentions. It may speed up the process, if you [allow us to modify your branch](https://help.github.com/articles/allowing-changes-to-a-pull-request-branch-created-from-a-fork) when you create the pull request.

## How can I be awesome?
Do you have a sensenet-related tool, plugin or sample library? We would be happy to include it in our awesome [awesome list](https://github.com/SenseNet/awesome-sensenet)! We collect all sensenet components and cool 3rd party repos there that make our community strong. Just drop us a message and let us share your magic with others!

Please follow these guidelines in your repo to make your stuff more accessible (a good example for an informative readme is the [Client JS repo](https://github.com/SenseNet/sn-client-js)):
- When creating the repo, please choose a license that fits your plans. We use **GPL v2**, but you can choose any of the usual permissive ones.
- Please provide a short description for your project on the top so that others can see at a glance what your tool does
- Write a detailed README file that contains essential stuff for the community:
  - the problem you solved
  - the technology you used (is it a Javascript plugin, a server component or a command line tool?)
  - an install guide: is it just a NuGet or npm package, or do we have to compile the source or execute additional steps - e.g. install a sensenet SnAdmin package - before using it?
  - a few source code examples (if there is coding involved) so that other developers have something to start with

Thanks!

*snteam*
