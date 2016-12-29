# Snappy Profiler Viewer
<img src="https://dl.dropboxusercontent.com/u/900723/SnappyProfilerViewerScreenshot.png" width="879" height="292">

A simple profiler viewer for Unity that can display CPU frames. It handles with ease the kind of load that Unity's profiler struggles to display at more than 1fps.

This tool does not have any profiling ability of its own, instead its simply hooking into Unity's builtin profiler and displaying the data.

Scope
---
This is not intended to replace the Unity Profiler, since the amount of work that would go into essentially rewriting the profiler GUI isn't worth it. There is also the possibility that Unity Technologies fix the performance issues and this tool will no longer be necessary.

This tool was made simply so I can actually view a "large" frame without performance issues. Note that frame data is gathered and cached, and this inherently is slow. Unity's profiler is seemingly regathering the data every GUI frame. It would be nice to hide the hitch making a seperate thread display a progress bar or something, but as far as I know the GUI and the information gathering must run on the main thread.

Motivation
---

For multiple years I have needed to profile "large" frames of data. Unity's profiler can take upwards of 30 seconds to respond to input in these circumstances. Not only that, but it never gets any better, it seems that the Unity profiler continously retrieves the frame data from C++ to Mono, which can take a second or so depending on how much data there is. The Unity profiler seems to also be issuing commands to draw UI elements which aren't visible, which might account for the extra time it takes.

There is a decently sized forum post about it: ["Why is Unity's profiler so slow and unresponsive in a frame that has a lot of data?"](https://forum.unity3d.com/threads/why-is-unitys-profiler-so-slow-and-unresponsive-in-a-frame-that-has-a-lot-of-data.377358/)

Known Issues
---
- Interface is almost complete, but it a little ugly at the moment.
- I haven't had the time to tidy up all source code - there might be dead code and/or other not so good stuff.
- Resizing the window a lot seems to make Unity crash - an internal error related to disposing of the "ProfilerHierarchy".
- Profiling on external devices and such is not test (though it may work fine as it is)

License
---
[Mozilla Public License Version 2.0](https://www.mozilla.org/en-US/MPL/2.0/)
