**With a Little Help From ...**

![logo1](https://github.com/fire-eggs/yagp/blob/master/Files/jetbrains_sm.png) - [JetBrains](https://www.jetbrains.com/) : the Acme of .NET tool suites!

![logo2](https://github.com/fire-eggs/yagp/blob/master/Files/deleaker_logo.png) - [Deleaker](https://www.deleaker.com) : the _best_ tool for finding memory, GDI and other leaks!

# CivOne

## What is it?
A re-creation of Sid Meier's Civilization 1 using C# and .NET.

## What is in this repository?
Bugfixes and enhancements to [SWY's original](https://github.com/SWY1985/CivOne). The primary goal is to emulate CIV Dos as closely as convenient, 
barring those areas where no one knows exactly how CIV Dos works. Some cheats and features will be provided
as 'patches', accessible via the setup menu.

Incremental releases have been made - see the [Releases](https://github.com/fire-eggs/CivOne/releases) page for details.

**NOTE:**
This program is not complete in itself. You need to have a copy of the files, especially the graphic assets, from CIV Dos! In addition, in order
to use sound support, you need the sound files from CIV Win.

## Contributing

I am not the original author. My focus has been to fix bugs and add features, and I'm still learning my way 
around large parts of the code and architecture. That said, I've started writing up some notes on parts of the 
code, and placed them in the [Wiki](https://github.com/fire-eggs/CivOne/wiki).

So far I'm adhering to SWY's original intent of not making any radical changes to the behavior, attempting to
maintain close parity to the "Microprose original". New features are welcome, if they can be implemented via
a setting (e.g. see Enock Nitti's A* movement).

SWY intended to provde alternative graphics (instead of requiring the original CIV Dos data files); someone
could survey the state of that and provide better, copyright-free versions.

In the Issues you can find a number of bugs that can be worked on. In a few cases I've tried to narrow down
the likely location for the fix. In SWY's repo, there may be some useful Issues as well. I'm also trying to
enhance the Unit Testing support. And there is a high-level list of stuff to work on in the [Wiki](https://github.com/fire-eggs/CivOne/wiki).

For Pull Requests, I would appreciate it if you could stick to the existing coding style.

## More information
- AI API developer onboarding: [AI/README.md](AI/README.md)
- Discussion thread: https://forums.civfanatics.com/threads/civone-an-open-source-remake-of-civilization-1.535036/
