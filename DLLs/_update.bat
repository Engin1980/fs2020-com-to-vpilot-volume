cd ..\..\ESystem.NET
call .\updateRelease.bat

cd C:\Users\Vajgl\source\repos\fs2020-com-to-vpilot-volume\DLLs
copy ..\..\ESystem.NET\_Release\* .\

del updateRelease.bat