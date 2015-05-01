Building PLANG
==========================================================
In order to build PLANG you must acquire and build dependencies.
These dependencies are not part of the PLANG project, though
the PLANG build scripts offer assistance in downloading and compiling
other Codeplex dependencies. 

Non-Codeplex Dependencies
----------------------------------------------------------
You must acquire and install these yourself.

1. Microsoft .NET 4.5 (http://www.microsoft.com/en-us/download/details.aspx?id=30653)
2. Microsoft .NET 3.0 Service Pack 1 (http://www.microsoft.com/en-us/download/details.aspx?id=3005)
3. Visual Studio 2013
4a. Python 2.7.5 - to build dependencies (http://www.python.org/download/releases/2.7.5/)
4b. "python" must be in PATH

Codeplex Dependencies
----------------------------------------------------------
The PLANG build scripts will try to automatically download these dependencies from Codeplex.
Or you may compile the dependencies manually and place them in the required locations. 

1. Gardens Point Scanner Generator (http://gplex.codeplex.com/)
2. Gardens Point Parser Generator (http://gppg.codeplex.com/)
3. Z3 SMT Solver (http://z3.codeplex.com/)
4. Formula (http://formula.codeplex.com/)
5. Zing (http://zing.codeplex.com/)

To *automatically* download and compile any missing Codeplex dependencies open a command
prompt (cmd.exe):

cd Somewhere\Plang\Bld
build.bat

As long as dependency artifacts are detected, then build.bat will not try to rebuild them.
If you delete external dependencies from Somewhere\Plang\Ext, then this will be detected
and rebuild will occur. You can also force all dependencies to be re-acquired and rebuilt by:

cd Somewhere\Plang\Bld
build.bat -e

To *manually* download and compile dependencies open a command prompt:

cd Somewhere\Plang\Bld
build.bat -l

The "-l" option lists the URLs to download the required sources and lists where the compiled artifacts
must be placed in order for PLANG to find them.  

Compiling PLANG
----------------------------------------------------------
Release version - open a command prompt:

cd Somewhere\Plang\Bld
build.bat

Debug version - open a command prompt:

cd Somewhere\Plang\Bld
build.bat -d

Outputs of the build are placed in Somewhere\Plang\Bld\Drops

Running regression tests
----------------------------------------------------------
cd Somewhere\Plang\Tst
testP.bat RegressionTests.txt

As a side effect, PLANG will be rebuilt with build.bat -d and
regressions will be run against the x86 debug version placed in the Bld\Drops folder.
