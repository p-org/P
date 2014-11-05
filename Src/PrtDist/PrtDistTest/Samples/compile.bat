copy /Y C:\Users\qadeer\Work\codeplex\plang\Src\Prt\WinUser\Debug\Win32\PrtWinUser.pdb .
copy /Y C:\Users\qadeer\Work\codeplex\plang\Src\Prt\WinUser\Debug\Win32\PrtWinUser.dll .
cl main.c program.c /Zi /Fd /I. /IC:\Users\qadeer\Work\codeplex\plang\Src\Prt\WinUser /IC:\Users\qadeer\Work\codeplex\plang\Src\Prt\API /IC:\Users\qadeer\Work\codeplex\plang\Src\Prt\Core /DPRT_PLAT_WINUSER /DPRT_DEBUG /link C:\Users\qadeer\Work\codeplex\plang\Src\Prt\WinUser\Debug\Win32\PrtWinUser.lib
