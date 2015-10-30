using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace PlangBuild
{
    class PlangBuilder
    {
        private const string MsBuildCommand = "\"{0}\" /p:Configuration={1} /p:Platform={2}";
        private const string ConfigDebug = "Debug";
        private const string ConfigRelease = "Release";
        private const string PlatformX86 = "x86";
        private const string PlatformX64 = "x64";
        


        /// <summary>
        /// Project is described by:
        /// (1) true if can only be built with 32-bit version of MsBuild (e.g. VS extensions)
        /// (2) the relative location of the project file
        /// (3) the platform on which it should be built
        /// </summary>
        private static readonly Tuple<bool, string, string>[] Projects = new Tuple<bool, string, string>[]
        {
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PVisualizer\\PVisualizerx64.csproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PVisualizer\\PVisualizer.csproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Pc\\InteractiveCommandLine\\InteractiveCommandLinex64.csproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Pc\\InteractiveCommandLine\\InteractiveCommandLine.csproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\CommandLinex64.csproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\CommandLine.csproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.vcxproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.vcxproj", PlatformX86),
            
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDist.vcxproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\NodeManager\\NodeManager.vcxproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDist.vcxproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\NodeManager\\NodeManager.vcxproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\Deployer\\Deployer.csproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\Deployer\\Deployer.csproj", PlatformX86),
             
        };
        
        private static readonly Tuple<string, string>[] DebugMoveMap = new Tuple<string, string>[]
        {
            #region Formula
            //Formula x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\libz3.dll"),
            //Formula x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\libz3.dll"),
            #endregion

            #region AGL
            //AGL x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x86\\Microsoft.Msagl.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Msagl.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x86\\Microsoft.Msagl.Drawing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Msagl.Drawing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x86\\Microsoft.Msagl.GraphViewerGdi.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Msagl.GraphViewerGdi.dll"),
            //AGL x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x64\\Microsoft.Msagl.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Msagl.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x64\\Microsoft.Msagl.Drawing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Msagl.Drawing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x64\\Microsoft.Msagl.GraphViewerGdi.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Msagl.GraphViewerGdi.dll"),
            #endregion

            #region Zing
            //Zing x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\zc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\zc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Comega.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Comega.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Comega.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Zing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Zing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Zing.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\System.Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.Framework.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\System.Compiler.Framework.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\ZingExplorer.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingExplorer.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Zinger.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Zinger.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RandomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RoundRobinDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\RoundRobinDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RunToCompletionDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\RunToCompletionDelayingScheduler.dll"),
                new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\CustomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CustomDelayingScheduler.dll"),

            //Zing x64
           new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\zc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\zc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Comega.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Comega.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Comega.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Zing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Zing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Zing.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\System.Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.Framework.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\System.Compiler.Framework.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\ZingExplorer.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\ZingExplorer.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Zinger.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Zinger.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RandomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RoundRobinDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\RoundRobinDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RunToCompletionDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\RunToCompletionDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\CustomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\CustomDelayingScheduler.dll"),
#endregion

            #region Pc
            //Pc x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\InteractiveCommandLine\\bin\\x86\\Debug\\Pci.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Pci.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PVisualizer\\bin\\x86\\Debug\\PVisualizer.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\PVisualizer.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\Pc.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Pc.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PVisualizer\\bin\\x86\\Debug\\PVisualizer.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\PVisualizer.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\Compiler.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Compiler.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\CParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CParser.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Debug\\ZingParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingParser.pdb"),
                
            //Pc x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\InteractiveCommandLine\\bin\\x64\\Debug\\Pci.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Pci.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PVisualizer\\bin\\x64\\Debug\\PVisualizer.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\PVisualizer.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\ZingParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\Pc.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Pc.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PVisualizer\\bin\\x64\\Debug\\PVisualizer.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\PVisualizer.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\Compiler.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Compiler.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\CParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\CParser.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Debug\\ZingParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\ZingParser.pdb"),
            #endregion
            
            #region Headers
            // x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\Prt.h"),

             //all IDLs    
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtValues_IDL.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\NodeManager_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\NodeManager_h.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistIDL_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtDistIDL_h.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtConfig.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtProgram.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtProgram.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtTypes.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtTypes.h"),
  
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtValues.h"),
                
           
                
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\Core\\PrtExecution.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtExecution.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtWinUser.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUserConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtWinUserConfig.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtDist.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistInternals.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtDistInternals.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistConfigParser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtDistConfigParser.h"),
            
            // x64
             new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\Prt.h"),
            
            //all IDLs
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtValues_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\NodeManager_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\NodeManager_h.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistIDL_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtDistIDL_h.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtConfig.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtProgram.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtProgram.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtTypes.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtTypes.h"),

            
 
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtValues.h"),

            
 
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\Core\\PrtExecution.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtExecution.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtWinUser.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUserConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtWinUserConfig.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtDist.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistInternals.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtDistInternals.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistConfigParser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtDistConfigParser.h"),

            #endregion

            #region Libraries
            // win32 
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Lib\\Debug\\Win32\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtDist.lib"),
            
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.pdb"),
           
            // x64 lib
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Lib\\Debug\\x64\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtDist.lib"),
            
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.pdb"),  
            
            #endregion
            
            #region Binaries
            //x86
             new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Debug\\Win32\\NodeManager.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Binaries\\NodeManager.exe"),

            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Debug\\x86\\Deployer.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Binaries\\Deployer.exe"),

            //x64
             new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Debug\\x64\\NodeManager.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Binaries\\NodeManager.exe"),

            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Debug\\x64\\Deployer.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Binaries\\Deployer.exe"),

            //copy the dependencies
            //x86
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Binaries\\msvcp120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Binaries\\msvcp120d.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Binaries\\msvcr120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Binaries\\msvcr120d.dll"),

            //x64
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Binaries\\msvcp120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Binaries\\msvcp120d.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Binaries\\msvcr120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Binaries\\msvcr120d.dll"),
            #endregion

             #region Client files
           //x86
             
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfiguration.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\ClientFiles\\ClusterConfiguration.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfigurationDebug.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\ClientFiles\\ClusterConfigurationDebug.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\MainFunction.c", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\ClientFiles\\MainFunction.c"),
            
            //x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfiguration.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\ClientFiles\\ClusterConfiguration.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfigurationDebug.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\ClientFiles\\ClusterConfigurationDebug.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\MainFunction.c", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\ClientFiles\\MainFunction.c"),
            #endregion
        };

        private static readonly Tuple<string, string>[] ReleaseMoveMap = new Tuple<string, string>[]
        {
            #region Formula
            //Formula x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\libz3.dll"),
            //Formula x64
             new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\libz3.dll"),
            #endregion

            #region AGL
            //AGL x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x86\\Microsoft.Msagl.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Msagl.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x86\\Microsoft.Msagl.Drawing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Msagl.Drawing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x86\\Microsoft.Msagl.GraphViewerGdi.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Msagl.GraphViewerGdi.dll"),
            //AGL x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x64\\Microsoft.Msagl.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Msagl.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x64\\Microsoft.Msagl.Drawing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Msagl.Drawing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\AGL\\x64\\Microsoft.Msagl.GraphViewerGdi.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Msagl.GraphViewerGdi.dll"),
            #endregion

            #region Zing
            //Zing x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\zc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\zc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Comega.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Comega.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Comega.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Zing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Zing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Microsoft.Zing.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\System.Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.Framework.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\System.Compiler.Framework.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\System.Compiler.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\ZingExplorer.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingExplorer.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\Zinger.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Zinger.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RandomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RoundRobinDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\RoundRobinDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\RunToCompletionDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\RunToCompletionDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\CustomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\CustomDelayingScheduler.dll"),
            

            //Zing x64
           new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\zc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\zc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Comega.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Comega.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Comega.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Comega.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Zing.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Zing.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Microsoft.Zing.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Zing.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\System.Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.Framework.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\System.Compiler.Framework.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\System.Compiler.Runtime.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\System.Compiler.Runtime.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\ZingExplorer.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\ZingExplorer.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\Zinger.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Zinger.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RandomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\RandomDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RoundRobinDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\RoundRobinDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\RunToCompletionDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\RunToCompletionDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\CustomDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\CustomDelayingScheduler.dll"),
      
            #endregion 

            #region PC
            //Pc x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\InteractiveCommandLine\\bin\\x86\\Release\\Pci.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Pci.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Release\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PVisualizer\\bin\\x86\\Release\\PVisualizer.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\PVisualizer.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Release\\Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Release\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x86\\Release\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingParser.dll"),
            //Pc x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\InteractiveCommandLine\\bin\\x64\\Release\\Pci.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Pci.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Release\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PVisualizer\\bin\\x64\\Release\\PVisualizer.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\PVisualizer.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Release\\Compiler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Compiler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Release\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\CommandLine\\bin\\x64\\Release\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\ZingParser.dll"),

            #endregion

            #region Headers
            // x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\Prt.h"),

             //all IDLs    
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtValues_IDL.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\NodeManager_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\NodeManager_h.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistIDL_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtDistIDL_h.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtConfig.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtProgram.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtProgram.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtTypes.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtTypes.h"),
  
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtValues.h"),
                
           
                
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\Core\\PrtExecution.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtExecution.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtWinUser.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUserConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtWinUserConfig.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtDist.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistInternals.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtDistInternals.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistConfigParser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtDistConfigParser.h"),
            
            // x64
             new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\Prt.h"),
            
            //all IDLs
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtValues_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\NodeManager_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\NodeManager_h.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistIDL_h.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtDistIDL_h.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtConfig.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtProgram.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtProgram.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtTypes.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtTypes.h"),

            
 
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtValues.h"),

            
 
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\Core\\PrtExecution.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtExecution.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtWinUser.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUserConfig.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtWinUserConfig.h"),

            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtDist.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistInternals.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtDistInternals.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\Core\\PrtDistConfigParser.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtDistConfigParser.h"),

            #endregion

            #region Libraries
            // win32 
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Lib\\Release\\Win32\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtDist.lib"),
            
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.pdb"),
           
            // x64 lib
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Lib\\Release\\x64\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtDist.lib"),
            
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.pdb"),  
            
            #endregion
            
            #region Binaries
            //x86
             new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Release\\Win32\\NodeManager.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Binaries\\NodeManager.exe"),

            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Release\\x86\\Deployer.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Binaries\\Deployer.exe"),

            //x64
             new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Release\\x64\\NodeManager.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Binaries\\NodeManager.exe"),

            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\Binaries\\Release\\x64\\Deployer.exe", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Binaries\\Deployer.exe"),

            //copy the dependencies
            //x86
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Binaries\\msvcp120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Binaries\\msvcp120d.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Binaries\\msvcr120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Binaries\\msvcr120d.dll"),

            //x64
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Binaries\\msvcp120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcp120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Binaries\\msvcp120d.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Binaries\\msvcr120.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Ext\\VS2013\\msvcr120d.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Binaries\\msvcr120d.dll"),
            #endregion

            #region Client files
            //x86
             
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfiguration.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\ClientFiles\\ClusterConfiguration.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfigurationDebug.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\ClientFiles\\ClusterConfigurationDebug.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\MainFunction.c", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\ClientFiles\\MainFunction.c"),
            
            //x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfiguration.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\ClientFiles\\ClusterConfiguration.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\ClusterConfigurationDebug.xml", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\ClientFiles\\ClusterConfigurationDebug.xml"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\PrtDist\\ClientFiles\\MainFunction.c", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\ClientFiles\\MainFunction.c"),
            #endregion
            
        };

        public static bool Build(bool isBldDebug)
        {
            var result = true;
            FileInfo msbuild, msbuild32 = null;
            result = SourceDownloader.GetMsbuild(out msbuild) && 
                     SourceDownloader.GetMsbuild(out msbuild32, true) &&
                     result;
            if (!result)
            {
                Program.WriteError("Could not build Plang, unable to find msbuild");
                return false;
            }

            var config = isBldDebug ? ConfigDebug : ConfigRelease;
            foreach (var proj in Projects)
            {
                Program.WriteInfo("Building {0}: Config = {1}, Platform = {2}", proj.Item2, config, proj.Item3);
                result = BuildCSProj(proj.Item1 ? msbuild32 : msbuild, proj.Item2, config, proj.Item3) && result;
            }

            if (!result)
            {
                return false;
            }

            result = DoMove(isBldDebug ? DebugMoveMap : ReleaseMoveMap) && result;


            return result;
        }

        private static bool DoMove(Tuple<string, string>[] moveMap)
        {
            bool result = true;
            try
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                foreach (var t in moveMap)
                {
                    var inFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t.Item1));
                    if (!inFile.Exists)
                    {
                        result = false;
                        Program.WriteError("Could not find output file {0}", inFile.FullName);
                        continue;
                    }

                    var outFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t.Item2));
                    if (!outFile.Directory.Exists)
                    {
                        outFile.Directory.Create();
                    }

                    inFile.CopyTo(outFile.FullName, true);
                    Program.WriteInfo("Moved output {0} --> {1}", inFile.FullName, outFile.FullName);
                }

                return result;
            }
            catch (Exception e)
            {
                Program.WriteError("Unable to move output files - {0}", e.Message);
                return false;
            }
        }

        private static bool BuildCSProj(FileInfo msbuild, string projFileName, string config, string platform)
        {
            try
            {
                FileInfo projFile;
                if (!SourceDownloader.GetBuildRelFile(projFileName, out projFile) || !projFile.Exists)
                {
                    Program.WriteError("Could not find project file {0}", projFileName);
                }

                
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = projFile.Directory.FullName;
                psi.FileName = msbuild.FullName;
                psi.Arguments = string.Format(MsBuildCommand, projFile.Name, config, platform);
                psi.CreateNoWindow = true;
                psi.EnvironmentVariables.Add("MSBUILDTREATHIGHERTOOLSVERSIONASCURRENT", "TRUE");

                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += OutputReceived;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                Program.WriteInfo("EXIT: {0}", process.ExitCode);
                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Program.WriteError("Failed to build project {0} - {1}", projFileName, e.Message);
                return false;
            }
        }

        private static void OutputReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            Console.WriteLine("OUT: {0}", e.Data);
        }
    }
}
