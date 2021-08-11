The P compiler does not support fancy project management features like separate compilation and dependency analysis (coming soon).
The current project file interface is a simple mechanism to provide all the required inputs to the compiler in XML format.
The P project file below is taken from the [ClientServer](../tutorial/clientserver.md) from Tutorials.

``` xml
<Project>
<IncludeProject>../CommonUtils/Common.pproj</IncludeProject>
<InputFiles>
    <PFile>./PSrc/</PFile>
    <PFile>./PSpec/</PFile>
    <PFile>./PTst/</PFile>
</InputFiles>
<Target>CSharp</Target>
<ProjectName>ClientServer</ProjectName>
<OutputDir>./PGenerated/</OutputDir>
</Project>
```
The `<InputFiles>` block provides all the P files that must be compiled together for this project.
In `<PFile>` can either specify the path to the P file or to a folder and the P compiler includes all the files in the folder during compilation.
The `<Target>` block specifies the target language for code generation (options are: CSharp, C, RVM and we are adding support for Java).
The `<ProjectName>` block provides the name for the project which is used as the output file name.
The `<OutputDir>` block provides the output directory for the generated code.
Finally, `<IncludeProject>` block provides path to other P projects that must be included as dependencies during compilation.
The P compiler simply recursively copies all the P files in the dependency projects and compiles them together.
This feature provides a way to split the P models for a large system into sub projects that can share models.

!!! Note "Coming Soon"
    Example about how to use `<IncludeProject>` feature will be added soon.
