## P Project File

The P project file (`.pproj`) is a simple XML mechanism to provide all the required inputs to the compiler.

---

### Example

The project file below is from the [TwoPhaseCommit](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/TwoPhaseCommit.pproj) example:

```xml
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

---

### Project File Reference

| Element | Description |
|---------|-------------|
| **`<ProjectName>`** | Name for the project, used as the output file name |
| **`<InputFiles>` / `<PFile>`** | P files or folders to compile. When a folder is specified, all `*.p` files in it are included |
| **`<OutputDir>`** | Output directory for the generated code |
| **`<IncludeProject>`** | Path to other `.pproj` files to include as dependencies. The compiler recursively copies all P files from dependency projects and compiles them together |

!!! tip ""
    The `<IncludeProject>` feature provides a way to split P models for a large system into subprojects that can share models.
