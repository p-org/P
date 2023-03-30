
The current project file interface is a simple mechanism to provide all the required inputs to the compiler in XML format.

The P project file below is taken from the [TwoPhaseCommit](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/TwoPhaseCommit.pproj) example in Tutorials.

``` xml
<!-- P project file for the Two Phase Commit example -->
<Project>
<ProjectName>TwoPhaseCommit</ProjectName>
<InputFiles>
	<PFile>./PSrc/</PFile>
	<PFile>./PSpec/</PFile>
	<PFile>./PTst/</PFile>
	<PFile>./PForeign/</PFile>
</InputFiles>
<OutputDir>./PGenerated/</OutputDir>
<!-- Add the dependencies for the Timer machine -->
<IncludeProject>../Common/Timer/Timer.pproj</IncludeProject>
<!-- Add the dependencies for the FailureInjector machine -->
<IncludeProject>../Common/FailureInjector/FailureInjector.pproj</IncludeProject>
</Project>
```

The `<InputFiles>` block provides all the P files that must be compiled together for this project.
In `<PFile>`, you can either specify the path to the P file or to a folder and the P compiler includes all the files in the folder during compilation.
The `<ProjectName>` block provides the name for the project which is used as the output file name.
The `<OutputDir>` block provides the output directory for the generated code.
Finally, `<IncludeProject>` block provides path to other P projects that must be included as dependencies during compilation.
The P compiler simply recursively copies all the P files in the dependency projects and compiles them together.
This feature provides a way to split the P models for a large system into subprojects that can share models.
