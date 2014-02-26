#! /usr/bin/python
import sys;
import re;
from argparse import *;

def error(msg):
    print(msg);
    sys.exit(-1);

def write(text, path):
    f = open(path, "w");
    f.write(text);
    f.close();

def generateVSProject(tdir, name, sm_headers, sm_libs, entryM, generateStubs, Timeout=10):
    machines = [];
    machRe = re.compile("MachineType_([a-zA-Z0-9_]*)");

    for l in open(tdir + "/PublicEnumTypes.h"):
        m = machRe.search(l);
        if (m):
            machines.append(m.groups()[0]);
    if (entryM not in machines):
        error("Can't find entry machine " + entryM + \
            ", and model doesnt have exactly 1 machine");

    filtersTmpl = """<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <Filter Include="Source Files">
      <UniqueIdentifier>{{4FC737F1-C7A5-4376-A066-2A32D752A2FF}}</UniqueIdentifier>
      <Extensions>cpp;c;cc;cxx;def;odl;idl;hpj;bat;asm;asmx</Extensions>
    </Filter>
    <Filter Include="Header Files">
      <UniqueIdentifier>{{93995380-89BD-4b04-88EB-625FBE52EBFB}}</UniqueIdentifier>
      <Extensions>h;hpp;hxx;hm;inl;inc;xsd</Extensions>
    </Filter>
    <Filter Include="Resource Files">
      <UniqueIdentifier>{{67DA6AB6-F800-4c08-8B7A-83BB121AAD01}}</UniqueIdentifier>
      <Extensions>rc;ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe;resx;tiff;tif;png;wav;mfcribbon-ms</Extensions>
    </Filter>
  </ItemGroup>
  <ItemGroup>
    <ClInclude Include="FunctionPrototypes.h">
      <Filter>Header Files</Filter>
    </ClInclude>
    <ClInclude Include="ProtectedDriverDecl.h">
      <Filter>Header Files</Filter>
    </ClInclude>
    <ClInclude Include="ProtectedEnumTypes.h">
      <Filter>Header Files</Filter>
    </ClInclude>
    <ClInclude Include="ProtectedMachineDecls.h">
      <Filter>Header Files</Filter>
    </ClInclude>
    <ClInclude Include="PublicComplexTypes.h">
      <Filter>Header Files</Filter>
    </ClInclude>
    <ClInclude Include="PublicEnumTypes.h">
      <Filter>Header Files</Filter>
    </ClInclude>
    <ClInclude Include="{name}.h">
      <Filter>Header Files</Filter>
    </ClInclude>
  </ItemGroup>
  <ItemGroup>
    <ClCompile Include="EntryFunctions.c">
      <Filter>Source Files</Filter>
    </ClCompile>
    <ClCompile Include="{name}.c">
      <Filter>Source Files</Filter>
    </ClCompile>
    <ClCompile Include="ComplexTypesMethods.c">
      <Filter>Source Files</Filter>
    </ClCompile>
  </ItemGroup>
</Project>"""

    write(filtersTmpl.format(name=name), tdir + "/" + name + ".vcxproj.filters")

    vcxprojTmpl = """<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|Win32">
      <Configuration>Debug</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|Win32">
      <Configuration>Release</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label="Globals">
    <ProjectGuid>{{585A2D9E-11AD-414D-AE8D-AC7B0E9D6D13}}</ProjectGuid>
    <SccProjectName>SAK</SccProjectName>
    <SccAuxPath>SAK</SccAuxPath>
    <SccLocalPath>SAK</SccLocalPath>
    <SccProvider>SAK</SccProvider>
    <Keyword>Win32Proj</Keyword>
    <RootNamespace>ConsoleApplication2</RootNamespace>
    <ProjectName>{name}</ProjectName>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <UseDebugLibraries>true</UseDebugLibraries>
    <PlatformToolset>v110</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'" Label="Configuration">
    <ConfigurationType>Application</ConfigurationType>
    <UseDebugLibraries>false</UseDebugLibraries>
    <PlatformToolset>v110</PlatformToolset>
    <WholeProgramOptimization>true</WholeProgramOptimization>
    <CharacterSet>Unicode</CharacterSet>
  </PropertyGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <ImportGroup Label="ExtensionSettings">
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <ImportGroup Label="PropertySheets" Condition="'$(Configuration)|$(Platform)'=='Release|Win32'">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
  </ImportGroup>
  <PropertyGroup Label="UserMacros" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">
    <LinkIncremental>true</LinkIncremental>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'">
    <LinkIncremental>false</LinkIncremental>
  </PropertyGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">
    <ClCompile>
      <PrecompiledHeader>
      </PrecompiledHeader>
      <WarningLevel>Level3</WarningLevel>
      <Optimization>Disabled</Optimization>
      <PreprocessorDefinitions>WIN32;_DEBUG;_CONSOLE;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>{headers};C:\Program Files %28x86%29\Windows Kits\8.0\Include\km;C:\Program Files %28x86%29\Windows Kits\8.0\Include\wdf\kmdf\\1.11;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
    </ClCompile>
    <Link>
      <SubSystem>Console</SubSystem>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <AdditionalDependencies>SMRuntime_User.lib;%(AdditionalDependencies)</AdditionalDependencies>
      <AdditionalLibraryDirectories>{libs};%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'">
    <ClCompile>
      <WarningLevel>Level3</WarningLevel>
      <PrecompiledHeader>
      </PrecompiledHeader>
      <Optimization>MaxSpeed</Optimization>
      <FunctionLevelLinking>true</FunctionLevelLinking>
      <IntrinsicFunctions>true</IntrinsicFunctions>
      <PreprocessorDefinitions>WIN32;NDEBUG;_CONSOLE;%(PreprocessorDefinitions)</PreprocessorDefinitions>
    </ClCompile>
    <Link>
      <SubSystem>Console</SubSystem>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <EnableCOMDATFolding>true</EnableCOMDATFolding>
      <OptimizeReferences>true</OptimizeReferences>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <ClInclude Include="FunctionPrototypes.h" />
    <ClInclude Include="ProtectedDriverDecl.h" />
    <ClInclude Include="ProtectedEnumTypes.h" />
    <ClInclude Include="ProtectedMachineDecls.h" />
    <ClInclude Include="PublicComplexTypes.h" />
    <ClInclude Include="PublicEnumTypes.h" />
    <ClInclude Include="{name}.h" />
  </ItemGroup>
  <ItemGroup>
    <ClCompile Include="EntryFunctions.c" />
    <ClCompile Include="ComplexTypesMethods.c" />
    <ClCompile Include="{name}.c" />
  </ItemGroup>
  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets">
  </ImportGroup>
</Project>
"""

    write(vcxprojTmpl.format(name=name, libs=sm_libs, headers=sm_headers),\
        tdir + "/" + name + ".vcxproj");

    cnstrTmp = """VOID Constructor_{machine}(PVOID constrParam, PSMF_EXCONTEXT exContext)
{{
    exContext->FreeThis = FALSE;
    exContext->PExMem = NULL;
}}
"""
    constructors = "\n".join(map(lambda m:   cnstrTmp.format(machine=m), machines))

    funProtos = filter(lambda s:    s!= "", map(lambda s:   s.strip()[:-1],\
        open(tdir + "/FunctionPrototypes.h").readlines()[3:]));

    foreignFuns = ""

    if (generateStubs):
        for proto in funProtos:
            retT = proto.split(" ")[0];
            if (retT == "void" or retT == "VOID"):
                body = "return;";
            else:
                body = "return (" + retT + ") 0;";
        
            foreignFuns += proto + "{ " + body + " }\n"

    mainTmpl = """#include "SmfPublicTypes.h"
#include "SmfPublic.h"
#include "PublicEnumTypes.h"
#include "PublicComplexTypes.h"
#include <stdlib.h>
#include <signal.h>
#include "{name}.h"
#include <Windows.h>

DWORD WINAPI WatchDog(__in LPVOID ignored) {{
    LONG timeout = {Timeout} * 1000;
    Sleep(timeout);
    printf("I got bored and killed your program after %f seconds.", ((float)timeout)/1000.0);
    exit(5);
}}

void SegfaultWatcher(int sig) {{
    exit(139);
}}

int main(int argc, char ** argv)
{{
    DWORD wdId;
    //create state-machine
    PSMF_MACHINE_ATTRIBUTES mAttributes;
    SMF_MACHINE_HANDLE smHandle;
    SMF_PACKED_VALUE val;
    val.Type = SmfNullType;
    val.Value = SmfNull;

    signal(SIGSEGV, SegfaultWatcher);

    // Always set same seed, so failures are reproducible
    srand(0);

    // Create a timer for one minute in case this computation is infinite
    CreateThread(NULL, 0, WatchDog, 0, 0, &wdId); 

    //CreateEmployee machine
    mAttributes = (PSMF_MACHINE_ATTRIBUTES)malloc(sizeof(SMF_MACHINE_ATTRIBUTES));
    SmfInitAttributes(mAttributes, &DriverDecl_{name}, MachineType_{MainMachine}, &val, NULL);
    SmfCreate(mAttributes, &smHandle);

    return 0;
}}


{Constructors}

{ForeignFuns}

BOOLEAN NONDET()
{{
    if (rand() > (RAND_MAX/2))
        return TRUE;
    else
        return FALSE;
}}
"""

    write(mainTmpl.format(name=name, Constructors=constructors, \
        ForeignFuns=foreignFuns, MainMachine=entryM, Timeout=Timeout),\
        tdir + "/" + name + ".c");

if __name__ == "__main__":
    p = ArgumentParser("Generate a VS Project for generated C Machine");
    p.add_argument("dir", type=str, nargs=1);
    p.add_argument("name", type=str, nargs=1, help="name of test");
    p.add_argument("runtime_headers", type=str, nargs=1, \
        help="path to runtime headrs");
    p.add_argument("runtime_libs", type=str, nargs=1, \
        help="path to runtime libraries");
    p.add_argument("--entryMachine", type=str, \
        help="path to runtime libraries");
    p.add_argument("--noStubs", action='store_const', dest='generateStubs', \
        const=False, default=True, \
        help="don't generate foreign function and NONDET stubs.", );

    args = p.parse_args();

    tdir = args.dir[0]
    name = args.name[0]
    sm_headers = args.runtime_headers[0]
    sm_libs = args.runtime_libs[0] 
    entryM=args.entryMachine
    generateVsProject(tdir, sm_headers, sm_libs, entryM, args.generateStubs);
