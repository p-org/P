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
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Pc\\Pcx64.csproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Pc\\Pc.csproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.vcxproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\PrtWinUser.vcxproj", PlatformX86),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\PrtDist.vcxproj", PlatformX64),
            new Tuple<bool, string, string>(true, "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\PrtDist.vcxproj", PlatformX86),
            
        };
        
        private static readonly Tuple<string, string>[] DebugMoveMap = new Tuple<string, string>[]
        {
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
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\PriorityDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\PriorityDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\PCTDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\PCTDelayingScheduler.dll"),

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
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\PriorityDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\PriorityDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\PCTDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\PCTDelayingScheduler.dll"),

            //Pc x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Debug\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Debug\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Debug\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Debug\\Pc.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\Pc.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Debug\\CParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\CParser.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Debug\\ZingParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Compiler\\ZingParser.pdb"),
            //Pc x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Debug\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Debug\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Debug\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\ZingParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Debug\\Pc.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\Pc.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Debug\\CParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\CParser.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Debug\\ZingParser.pdb", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Compiler\\ZingParser.pdb"),

            
            // x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\Prt.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtValues.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtValues_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Headers\\PrtDist.h"),
    
            // x64
             new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\Prt.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtValues.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtValues_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Headers\\PrtDist.h"),
                
            
            // win32 
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\Debug\\Win32\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtDist.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.exp", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.exp"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\Win32\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x86\\Runtime\\Lib\\PrtWinUser.pdb"),
            // x64 lib
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\Debug\\x64\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtDist.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.exp", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.exp"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Debug\\x64\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Debug_x64\\Runtime\\Lib\\PrtWinUser.pdb"),  
            
                
        };

        private static readonly Tuple<string, string>[] ReleaseMoveMap = new Tuple<string, string>[]
        {
            //formula  x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x86\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\libz3.dll"),
            //formula x64
             new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Formula\\x64\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\libz3.dll"),

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
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\PriorityDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\PriorityDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x86\\PCTDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\PCTDelayingScheduler.dll"),

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
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\PriorityDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\PriorityDelayingScheduler.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Ext\\Zing\\x64\\PCTDelayingScheduler.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\PCTDelayingScheduler.dll"),

            //Pc x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Release\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Release\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x86\\Release\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Compiler\\ZingParser.dll"),
            //Pc x64
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Release\\Pc.exe", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\Pc.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Release\\CParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\CParser.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Pc\\bin\\x64\\Release\\ZingParser.dll", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Compiler\\ZingParser.dll"),

           // x86
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\Prt.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtValues.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtValues_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Headers\\PrtDist.h"),
    
            // x64
             new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\Prt.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\Prt.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtBaseTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtBaseTypes_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtTypes_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtTypes_IDL.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\API\\PrtValues.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtValues.h"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Prt\\IDL\\PrtValues_IDL.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtValues_IDL.h"),
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
                "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\PrtDist.h", 
                "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Headers\\PrtDist.h"),

            // win32 
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\Release\\Win32\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtDist.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.exp", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.exp"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\Win32\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x86\\Runtime\\Lib\\PrtWinUser.pdb"),
            // x64 lib
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\PrtDist\\PrtDist\\PrtDist\\Release\\x64\\PrtDist.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtDist.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.lib", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.lib"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.exp", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.exp"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.dll", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.dll"),
            new Tuple<string, string>(
            "..\\..\\..\\..\\..\\Src\\Prt\\WinUser\\Release\\x64\\PrtWinUser.pdb", 
            "..\\..\\..\\..\\Drops\\Plang_Release_x64\\Runtime\\Lib\\PrtWinUser.pdb"), 
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
